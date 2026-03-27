using System.ServiceModel.Syndication;
using System.Xml;
using FeedRSS.Data;
using FeedRSS.Models;
using FeedRSS.ViewModels;

namespace FeedRSS.Services;

public class RssService : IRssService
{
    private readonly MvcFeedContext _context;
    private readonly IFeedService _feedService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RssService> _logger;

    public RssService(
        MvcFeedContext context,
        IFeedService feedService,
        IHttpClientFactory httpClientFactory,
        ILogger<RssService> logger)
    {
        _context = context;
        _feedService = feedService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FeedReloadResult> ReloadFeedAsync(int feedId, CancellationToken cancellationToken = default)
    {
        var details = await GetFeedDetailsOrThrowAsync(feedId, cancellationToken);
        var feed = details.Feed;
        using var response = await DownloadResponseOrThrowAsync(feedId, feed.Url, cancellationToken);
        var syndicationFeed = await ParseFeedOrThrowAsync(feedId, feed.Url, response, cancellationToken);
        var result = UpsertItems(feedId, details, syndicationFeed);

        feed.LastReloadedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<FeedDetailsViewModel> GetFeedDetailsOrThrowAsync(int feedId, CancellationToken cancellationToken)
    {
        var details = await _feedService.GetDetailsAsync(feedId, track: true, cancellationToken: cancellationToken);
        if (details is null)
        {
            throw new FeedReloadException(
                FeedReloadFailureReason.FeedNotFound,
                $"Feed with id '{feedId}' was not found.");
        }

        return details;
    }

    private async Task<HttpResponseMessage> DownloadResponseOrThrowAsync(int feedId, string feedUrl, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout while loading feed {FeedId} from {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.Timeout, "Reload timed out while downloading the feed.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid URL for feed {FeedId}: {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.InvalidFeedUrl, "Feed URL is invalid.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while loading feed {FeedId} from {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.RequestFailed, "Feed request failed.", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Feed endpoint returned 404 for feed {FeedId}: {FeedUrl}.", feedId, feedUrl);
            response.Dispose();
            throw new FeedReloadException(FeedReloadFailureReason.FeedUnavailable, "Feed endpoint was not found (404).");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Feed endpoint returned status {StatusCode} for feed {FeedId}: {FeedUrl}.",
                (int)response.StatusCode,
                feedId,
                feedUrl);
            response.Dispose();
            throw new FeedReloadException(
                FeedReloadFailureReason.RequestFailed,
                $"Feed endpoint returned status {(int)response.StatusCode}.");
        }

        return response;
    }

    private async Task<SyndicationFeed> ParseFeedOrThrowAsync(
        int feedId,
        string feedUrl,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        SyndicationFeed? syndicationFeed;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            syndicationFeed = SyndicationFeed.Load(reader);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Invalid XML while parsing feed {FeedId} from {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.InvalidRss, "Feed content is not a valid XML/RSS document.", ex);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid RSS format while parsing feed {FeedId} from {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.InvalidRss, "Feed content has an invalid RSS/Atom format.", ex);
        }

        if (syndicationFeed is null)
        {
            _logger.LogWarning("Syndication feed parser returned null for feed {FeedId} from {FeedUrl}.", feedId, feedUrl);
            throw new FeedReloadException(FeedReloadFailureReason.InvalidRss, "Feed content could not be parsed.");
        }

        return syndicationFeed;
    }

    private FeedReloadResult UpsertItems(
        int feedId,
        FeedDetailsViewModel details,
        SyndicationFeed syndicationFeed)
    {
        var feed = details.Feed;
        var knownLinks = details.Articles
            .Where(a => !string.IsNullOrWhiteSpace(a.Link))
            .Select(a => new { Link = NormalizeLink(a.Link), Article = a })
            .Where(x => !string.IsNullOrWhiteSpace(x.Link))
            .ToDictionary(
                x => x.Link!,
                x => x.Article,
                StringComparer.OrdinalIgnoreCase);

        var addedCount = 0;
        var updatedCount = 0;

        foreach (var item in syndicationFeed.Items)
        {
            var articleLink = NormalizeLink(ResolveItemLink(item));
            if (string.IsNullOrWhiteSpace(articleLink))
            {
                _logger.LogDebug("Skipping RSS item without URL in feed {FeedId} from {FeedUrl}.", feedId, feed.Url);
                continue;
            }

            if (knownLinks.TryGetValue(articleLink, out var knownArticle))
            {
                if (!ShouldUpdateArticle(knownArticle, item))
                {
                    continue;
                }

                ApplyItemToArticle(knownArticle, item);
                updatedCount++;
                continue;
            }

            var article = new Article();
            ApplyItemToArticle(article, item);
            article.FeedId = feed.Id;
            article.Link = articleLink;

            _context.Article.Add(article);
            addedCount++;

            knownLinks[articleLink] = article;
        }

        return new FeedReloadResult(addedCount, updatedCount);
    }

    private static string? ResolveItemLink(SyndicationItem item)
    {
        var preferredLink = item.Links.FirstOrDefault(l => string.Equals(l.RelationshipType, "alternate", StringComparison.OrdinalIgnoreCase));
        return (preferredLink ?? item.Links.FirstOrDefault())?.Uri?.ToString();
    }

    private static string? ResolveExternalId(SyndicationItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Id))
        {
            return item.Id.Trim();
        }

        return null;
    }

    private static string? NormalizeLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        var normalized = link.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string ResolveTitle(SyndicationItem item)
    {
        return item.Title?.Text?.Trim() ?? "(Untitled)";
    }

    private static string? ResolveAuthor(SyndicationItem item)
    {
        return item.Authors.FirstOrDefault()?.Name;
    }

    private static DateTime? ResolvePublishedAt(SyndicationItem item)
    {
        if (item.PublishDate != DateTimeOffset.MinValue)
        {
            return item.PublishDate.UtcDateTime;
        }

        if (item.LastUpdatedTime != DateTimeOffset.MinValue)
        {
            return item.LastUpdatedTime.UtcDateTime;
        }

        return null;
    }

    private static DateTime? ResolveLastUpdatedAt(SyndicationItem item)
    {
        if (item.LastUpdatedTime != DateTimeOffset.MinValue)
        {
            return item.LastUpdatedTime.UtcDateTime;
        }

        return null;
    }

    private static bool ShouldUpdateArticle(Article article, SyndicationItem item)
    {
        var incomingPublishedAt = ResolvePublishedAt(item);
        var incomingLastUpdatedAt = ResolveLastUpdatedAt(item);
        return IsNewer(incomingPublishedAt, article.PublishedAt) || IsNewer(incomingLastUpdatedAt, article.LastUpdatedAt);
    }

    private static bool IsNewer(DateTime? incoming, DateTime? existing)
    {
        return incoming.HasValue && (!existing.HasValue || incoming.Value > existing.Value);
    }

    private static void ApplyItemToArticle(Article article, SyndicationItem item)
    {
        article.Title = ResolveTitle(item);
        article.Summary = item.Summary?.Text;
        article.Author = ResolveAuthor(item);
        article.PublishedAt = ResolvePublishedAt(item);
        article.LastUpdatedAt = ResolveLastUpdatedAt(item);
        article.ExternalId = ResolveExternalId(item);
    }
}

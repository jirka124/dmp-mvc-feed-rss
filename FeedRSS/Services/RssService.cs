using System.ServiceModel.Syndication;
using System.Xml;
using FeedRSS.Data;
using FeedRSS.Models;

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
        var details = await _feedService.GetDetailsAsync(feedId, cancellationToken: cancellationToken);
        if (details is null)
        {
            throw new InvalidOperationException($"Feed with id '{feedId}' was not found.");
        }
        var feed = details.Feed;

        var client = _httpClientFactory.CreateClient();
        using var stream = await client.GetStreamAsync(feed.Url, cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

        var syndicationFeed = SyndicationFeed.Load(reader);
        if (syndicationFeed is null)
        {
            _logger.LogWarning("Unable to parse RSS feed from URL {FeedUrl}.", feed.Url);
            return new FeedReloadResult(0, 0);
        }

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
                _logger.LogDebug("Skipping RSS item without URL in feed {FeedUrl}.", feed.Url);
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

        feed.LastReloadedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

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

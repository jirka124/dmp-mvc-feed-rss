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

    public async Task<int> ReloadFeedAsync(int feedId, CancellationToken cancellationToken = default)
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
            return 0;
        }

        var knownLinks = details.Articles
            .Where(a => !string.IsNullOrWhiteSpace(a.Link))
            .Select(a => NormalizeLink(a.Link)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedCount = 0;

        foreach (var item in syndicationFeed.Items)
        {
            var articleLink = NormalizeLink(ResolveItemLink(item));
            if (string.IsNullOrWhiteSpace(articleLink))
            {
                _logger.LogDebug("Skipping RSS item without URL in feed {FeedUrl}.", feed.Url);
                continue;
            }

            if (knownLinks.Contains(articleLink))
            {
                continue;
            }

            var publishedAt = item.PublishDate != DateTimeOffset.MinValue
                ? item.PublishDate.UtcDateTime
                : item.LastUpdatedTime != DateTimeOffset.MinValue
                    ? item.LastUpdatedTime.UtcDateTime
                    : (DateTime?)null;

            var article = new Article
            {
                FeedId = feed.Id,
                Title = item.Title?.Text?.Trim() ?? "(Untitled)",
                Link = articleLink,
                Summary = item.Summary?.Text,
                Author = item.Authors.FirstOrDefault()?.Name,
                PublishedAt = publishedAt,
                ExternalId = ResolveExternalId(item)
            };

            _context.Article.Add(article);
            addedCount++;

            knownLinks.Add(articleLink);
        }

        feed.LastReloadedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return addedCount;
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
}

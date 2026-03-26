using FeedRSS.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedRSS.Data;

public static class DbInitializer
{
    public static void Migrate(MvcFeedContext context)
    {
        context.Database.Migrate();
    }

    public static void Seed(MvcFeedContext context)
    {
        if (context.Feed.Any())
        {
            return;
        }

        var testFeed = new Feed
        {
            Name = "Local Test Feed",
            Url = "https://example.local/rss.xml",
            LastReloadedAt = DateTime.UtcNow
        };

        testFeed.Articles.Add(new Article
        {
            Title = "Test Article 1",
            Link = "https://example.local/articles/1",
            Summary = "Seeded test article for quick UI verification.",
            Author = "Seeder",
            PublishedAt = DateTime.UtcNow.AddHours(-6),
            ExternalId = "local-test-001"
        });

        testFeed.Articles.Add(new Article
        {
            Title = "Test Article 2",
            Link = "https://example.local/articles/2",
            Summary = "Second seeded test article.",
            Author = "Seeder",
            PublishedAt = DateTime.UtcNow.AddHours(-4),
            ExternalId = "local-test-002"
        });

        testFeed.Articles.Add(new Article
        {
            Title = "Test Article 3",
            Link = "https://example.local/articles/3",
            Summary = "Third seeded test article.",
            Author = "Seeder",
            PublishedAt = DateTime.UtcNow.AddHours(-2),
            ExternalId = "local-test-003"
        });

        context.Feed.AddRange(
            testFeed,
            new Feed
            {
                Name = "BBC News",
                Url = "https://feeds.bbci.co.uk/news/rss.xml"
            },
            new Feed
            {
                Name = "The Verge",
                Url = "https://www.theverge.com/rss/index.xml"
            },
            new Feed
            {
                Name = "Hacker News Frontpage",
                Url = "https://hnrss.org/frontpage"
            },
            new Feed
            {
                Name = "Stack Overflow Blog",
                Url = "https://stackoverflow.blog/feed/"
            },
            new Feed
            {
                Name = "iDNES Zpravodaj",
                Url = "https://servis.idnes.cz/rss.aspx?c=zpravodaj"
            });

        context.SaveChanges();
    }
}

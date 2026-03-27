using FeedRSS.Data;
using FeedRSS.Models;
using FeedRSS.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FeedRSS.Tests;

public class FeedServiceTests
{
    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingFeeds()
    {
        var (db, connection) = await CreateDbContextAsync();
        await using var _ = db;
        await using var __ = connection;
        db.Feed.AddRange(
            new Feed { Name = "Dotnet Blog", Url = "https://example.com/dotnet.xml" },
            new Feed { Name = "Tech News", Url = "https://example.com/tech.xml" });
        await db.SaveChangesAsync();

        var service = new FeedService(db);

        var result = await service.GetAllAsync(" dot ");

        Assert.Single(result);
        Assert.Equal("Dotnet Blog", result[0].Name);
    }

    [Fact]
    public async Task DeleteBulkAsync_RemovesOnlyDistinctExistingFeeds()
    {
        var (db, connection) = await CreateDbContextAsync();
        await using var _ = db;
        await using var __ = connection;
        var feedA = new Feed { Name = "A", Url = "https://example.com/a.xml" };
        var feedB = new Feed { Name = "B", Url = "https://example.com/b.xml" };
        db.Feed.AddRange(feedA, feedB);
        await db.SaveChangesAsync();

        var service = new FeedService(db);

        var deleted = await service.DeleteBulkAsync([feedA.Id, feedA.Id, 999_999]);

        Assert.Equal(1, deleted);
        Assert.False(await db.Feed.AnyAsync(f => f.Id == feedA.Id));
        Assert.True(await db.Feed.AnyAsync(f => f.Id == feedB.Id));
    }

    [Fact]
    public async Task GetDetailsAsync_AppliesDateRangeAndTitleFilter()
    {
        var (db, connection) = await CreateDbContextAsync();
        await using var _ = db;
        await using var __ = connection;
        var feed = new Feed { Name = "Filter Feed", Url = "https://example.com/filter.xml" };
        db.Feed.Add(feed);
        await db.SaveChangesAsync();

        var from = new DateOnly(2026, 3, 10);
        var to = new DateOnly(2026, 3, 11);
        var inRangeStart = ToUtcStartOfLocalDay(from).AddHours(8);
        var inRangeEnd = ToUtcStartOfLocalDay(to).AddHours(8);
        var outOfRange = ToUtcStartOfLocalDay(to.AddDays(1)).AddHours(8);

        db.Article.AddRange(
            new Article { FeedId = feed.Id, Title = "C# Weekly", Link = "https://example.com/a1", PublishedAt = inRangeStart },
            new Article { FeedId = feed.Id, Title = "C# Update", Link = "https://example.com/a2", PublishedAt = inRangeEnd },
            new Article { FeedId = feed.Id, Title = "Python Digest", Link = "https://example.com/a3", PublishedAt = inRangeStart },
            new Article { FeedId = feed.Id, Title = "C# Future", Link = "https://example.com/a4", PublishedAt = outOfRange });
        await db.SaveChangesAsync();

        var service = new FeedService(db);

        var details = await service.GetDetailsAsync(feed.Id, from, to, "C#");

        Assert.NotNull(details);
        Assert.Equal(2, details.Articles.Count);
        Assert.All(details.Articles, a => Assert.Contains("C#", a.Title, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(MvcFeedContext Context, SqliteConnection Connection)> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MvcFeedContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MvcFeedContext(options);
        await context.Database.EnsureCreatedAsync();
        return (context, connection);
    }

    private static DateTime ToUtcStartOfLocalDay(DateOnly date)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        return localStart.ToUniversalTime();
    }
}

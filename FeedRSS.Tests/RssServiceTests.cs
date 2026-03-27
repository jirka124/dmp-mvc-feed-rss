using System.Net;
using System.Text;
using FeedRSS.Data;
using FeedRSS.Models;
using FeedRSS.Services;
using FeedRSS.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeedRSS.Tests;

public class RssServiceTests
{
    [Fact]
    public async Task ReloadFeedAsync_AddsNewAndUpdatesExistingArticles()
    {
        var (db, connection) = await CreateDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var feed = new Feed { Name = "Demo", Url = "https://example.com/feed.xml" };
        db.Feed.Add(feed);
        await db.SaveChangesAsync();

        db.Article.Add(new Article
        {
            FeedId = feed.Id,
            Title = "Old title",
            Link = "https://example.com/existing",
            PublishedAt = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var rss = """
                  <?xml version="1.0" encoding="UTF-8"?>
                  <rss version="2.0">
                    <channel>
                      <title>Demo feed</title>
                      <item>
                        <title>Updated title</title>
                        <link>https://example.com/existing</link>
                        <pubDate>Wed, 11 Mar 2026 10:00:00 GMT</pubDate>
                      </item>
                      <item>
                        <title>Brand new</title>
                        <link>https://example.com/new</link>
                        <pubDate>Thu, 12 Mar 2026 10:00:00 GMT</pubDate>
                      </item>
                    </channel>
                  </rss>
                  """;

        var httpClientFactory = new FakeHttpClientFactory(HttpResponseMessage(HttpStatusCode.OK, rss, "application/rss+xml"));
        var feedService = new FeedService(db);
        var service = new RssService(db, feedService, httpClientFactory, NullLogger<RssService>.Instance);

        var result = await service.ReloadFeedAsync(feed.Id);

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.UpdatedCount);

        var updated = await db.Article.SingleAsync(a => a.Link == "https://example.com/existing");
        var added = await db.Article.SingleAsync(a => a.Link == "https://example.com/new");

        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("Brand new", added.Title);
    }

    [Fact]
    public async Task ReloadFeedAsync_InvalidRss_ThrowsFeedReloadException()
    {
        var (db, connection) = await CreateDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var feed = new Feed { Name = "Broken", Url = "https://example.com/bad.xml" };
        db.Feed.Add(feed);
        await db.SaveChangesAsync();

        var httpClientFactory = new FakeHttpClientFactory(HttpResponseMessage(HttpStatusCode.OK, "not-xml-content", "text/plain"));
        var feedService = new FeedService(db);
        var service = new RssService(db, feedService, httpClientFactory, NullLogger<RssService>.Instance);

        var ex = await Assert.ThrowsAsync<FeedReloadException>(() => service.ReloadFeedAsync(feed.Id));

        Assert.Equal(FeedReloadFailureReason.InvalidRss, ex.Reason);
    }

    private static HttpResponseMessage HttpResponseMessage(HttpStatusCode statusCode, string content, string mediaType)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        };
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

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public FakeHttpClientFactory(HttpResponseMessage responseMessage)
        {
            _httpClient = new HttpClient(new StaticHttpMessageHandler(responseMessage), disposeHandler: true);
        }

        public HttpClient CreateClient(string name) => _httpClient;
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _responseMessage;

        public StaticHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseMessage);
        }
    }
}

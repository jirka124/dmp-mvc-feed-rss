using FeedRSS.Controllers;
using FeedRSS.Models;
using FeedRSS.Services;
using FeedRSS.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FeedRSS.Tests;

public class FeedControllerTests
{
    [Fact]
    public async Task Reload_WhenInvalidRss_SetsDangerStatusAndRedirectsToDetails()
    {
        var controller = new FeedController(
            new DummyFeedService(),
            new ThrowingRssService(new FeedReloadException(FeedReloadFailureReason.InvalidRss, "bad rss")))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new DictionaryTempDataProvider())
        };

        var result = await controller.Reload(42, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(42, redirect.RouteValues?["id"]);

        Assert.Equal("danger", controller.TempData["StatusType"]);
        Assert.Equal("Feed content is invalid or not a supported RSS/Atom format.", controller.TempData["StatusMessage"]);
    }

    private sealed class ThrowingRssService : IRssService
    {
        private readonly Exception _exceptionToThrow;

        public ThrowingRssService(Exception exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<FeedReloadResult> ReloadFeedAsync(int feedId, CancellationToken cancellationToken = default)
            => Task.FromException<FeedReloadResult>(_exceptionToThrow);
    }

    private sealed class DummyFeedService : IFeedService
    {
        public Task<List<Feed>> GetAllAsync(string? searchTerm = null, bool track = false, CancellationToken cancellationToken = default) => Task.FromResult(new List<Feed>());
        public Task<Feed?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default) => Task.FromResult<Feed?>(null);
        public Task CreateAsync(Feed feed, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Feed feed, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> DeleteBulkAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<FeedDetailsViewModel?> GetDetailsAsync(int id, DateOnly? from = null, DateOnly? to = null, string? titleSearch = null, bool track = false, CancellationToken cancellationToken = default)
            => Task.FromResult<FeedDetailsViewModel?>(null);
    }

    private sealed class DictionaryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _values = new();

        public IDictionary<string, object?> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
            _values = new Dictionary<string, object?>(values);
        }
    }
}

using FeedRSS.Models;
using FeedRSS.ViewModels;

namespace FeedRSS.Services;

public interface IFeedService
{
    Task<List<Feed>> GetAllAsync(string? searchTerm = null, bool track = false, CancellationToken cancellationToken = default);
    Task<Feed?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default);
    Task CreateAsync(Feed feed, CancellationToken cancellationToken = default);
    Task UpdateAsync(Feed feed, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> DeleteBulkAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<FeedDetailsViewModel?> GetDetailsAsync(
        int id,
        DateOnly? from = null,
        DateOnly? to = null,
        string? titleSearch = null,
        bool track = false,
        CancellationToken cancellationToken = default);
}

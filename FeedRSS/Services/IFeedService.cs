using FeedRSS.Models;

namespace FeedRSS.Services;

public interface IFeedService
{
    Task<List<Feed>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Feed?> GetByIdAsync(int id, bool includeArticles = false, CancellationToken cancellationToken = default);
    Task CreateAsync(Feed feed, CancellationToken cancellationToken = default);
    Task UpdateAsync(Feed feed, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}

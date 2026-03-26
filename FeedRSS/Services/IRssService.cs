namespace FeedRSS.Services;

public interface IRssService
{
    Task<int> ReloadFeedAsync(int feedId, CancellationToken cancellationToken = default);
}

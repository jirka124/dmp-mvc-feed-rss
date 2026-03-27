namespace FeedRSS.Services;

public interface IRssService
{
    Task<FeedReloadResult> ReloadFeedAsync(int feedId, CancellationToken cancellationToken = default);
}

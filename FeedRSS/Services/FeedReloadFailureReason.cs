namespace FeedRSS.Services;

public enum FeedReloadFailureReason
{
    FeedNotFound,
    InvalidFeedUrl,
    FeedUnavailable,
    RequestFailed,
    Timeout,
    InvalidRss
}

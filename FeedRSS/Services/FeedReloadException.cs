namespace FeedRSS.Services;

public sealed class FeedReloadException : Exception
{
    public FeedReloadException(FeedReloadFailureReason reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public FeedReloadFailureReason Reason { get; }
}

using FeedRSS.Models;

namespace FeedRSS.ViewModels;

public class FeedDetailsViewModel
{
    public required Feed Feed { get; init; }
    public required IReadOnlyList<Article> Articles { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? TitleSearch { get; init; }
}

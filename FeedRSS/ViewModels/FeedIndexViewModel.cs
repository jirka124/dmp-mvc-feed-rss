using FeedRSS.Models;

namespace FeedRSS.ViewModels;

public class FeedIndexViewModel
{
    public required IReadOnlyList<Feed> Feeds { get; init; }
    public string? SearchTerm { get; init; }
}

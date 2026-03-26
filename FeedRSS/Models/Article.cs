using System.ComponentModel.DataAnnotations;

namespace FeedRSS.Models;

public class Article
{
    public int Id { get; set; }

    [Required]
    public int FeedId { get; set; }

    public Feed? Feed { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2048)]
    [Url]
    public string Link { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Summary { get; set; }

    [StringLength(250)]
    public string? Author { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    [StringLength(500)]
    public string? ExternalId { get; set; }
}

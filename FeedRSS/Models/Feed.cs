using System.ComponentModel.DataAnnotations;

namespace FeedRSS.Models;

public class Feed
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    public DateTimeOffset? LastReloadedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}

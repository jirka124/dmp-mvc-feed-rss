using System.ComponentModel.DataAnnotations;

namespace FeedRSS.Models;

public class Article
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Feed")]
    public int FeedId { get; set; }

    public Feed? Feed { get; set; }

    [Required]
    [StringLength(300)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2048)]
    [Url]
    [Display(Name = "Article URL")]
    public string Link { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Summary")]
    public string? Summary { get; set; }

    [StringLength(250)]
    [Display(Name = "Author")]
    public string? Author { get; set; }

    [Display(Name = "Published")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTimeOffset? PublishedAt { get; set; }

    [StringLength(500)]
    [Display(Name = "External ID")]
    public string? ExternalId { get; set; }
}

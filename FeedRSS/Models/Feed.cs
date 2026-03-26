using System.ComponentModel.DataAnnotations;

namespace FeedRSS.Models;

public class Feed
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Feed Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(2048)]
    [Display(Name = "RSS URL")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Last Reloaded")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime? LastReloadedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}

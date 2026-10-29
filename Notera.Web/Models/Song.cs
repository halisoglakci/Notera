namespace Notera.Web.Models;

public class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Album { get; set; }
    public string? YoutubeUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // İlişki: Bir şarkının birden fazla notası/enstrüman dizilimi olabilir
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
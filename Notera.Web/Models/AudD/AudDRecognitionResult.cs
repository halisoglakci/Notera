namespace Notera.Web.Models.AudD;

public class AudDRecognitionResult
{
    public bool Success { get; set; }
    public bool IsRecognized { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? SongLink { get; set; }
    public int? MatchedSongId { get; set; }
    public bool HasDatabaseMatch => MatchedSongId.HasValue;
    public string Message { get; set; } = string.Empty;
}
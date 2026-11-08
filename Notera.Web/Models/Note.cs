namespace Notera.Web.Models;

public class Note
{
    public int Id { get; set; }
    
    public int SongId { get; set; }
    public Song? Song { get; set; }

    public int InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public string Content { get; set; } = string.Empty; // Nota içeriği (Tab, Do-Re-Mi veya harf notasyonu)
    public string Key { get; set; } = string.Empty; // Karar sesi / Akor (Örn: Am, Do)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
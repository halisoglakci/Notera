namespace Notera.Web.Models;

public class Instrument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Örn: Bağlama, Gitar, Piyano
    public string? Description { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
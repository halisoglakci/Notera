
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notera.Web.Data;
using Notera.Web.Models;

namespace Notera.Web.Controllers;

[ApiController]
[Route("api/songs")]
public class SongsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SongsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/songs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Song>>> GetSongs()
    {
        return await _context.Songs.AsNoTracking().ToListAsync();
    }

    // GET: api/songs/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Song>> GetSong(int id)
    {
        var song = await _context.Songs.FindAsync(id);

        if (song == null)
            return NotFound();

        return song;
    }

    // POST: api/songs
    [HttpPost]
    public async Task<ActionResult<Song>> CreateSong(Song song)
    {
        song.Id = 0;
        song.CreatedAt = DateTime.UtcNow;
        song.Notes = new List<Note>();

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSong), new { id = song.Id }, song);
    }

    // PUT: api/songs/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSong(int id, Song updatedSong)
    {
        var song = await _context.Songs.FindAsync(id);

        if (song == null)
            return NotFound();

        song.Title = updatedSong.Title;
        song.Artist = updatedSong.Artist;
        song.Album = updatedSong.Album;
        song.YoutubeUrl = updatedSong.YoutubeUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/songs/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSong(int id)
    {
        var song = await _context.Songs.FindAsync(id);

        if (song == null)
            return NotFound();

        _context.Songs.Remove(song);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notera.Web.Data;

namespace Notera.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("Songs")]
public class SongMvcController : Controller
{
    private readonly AppDbContext _context;

    public SongMvcController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Songs
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Songs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(term) ||
                s.Artist.ToLower().Contains(term) ||
                (s.Album != null && s.Album.ToLower().Contains(term)));
        }

        var songs = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        return View("~/Views/Songs/Index.cshtml", songs);
    }

    // GET: /Songs/Details/1
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var song = await _context.Songs
            .AsNoTracking()
            .Include(s => s.Notes)
                .ThenInclude(n => n.Instrument)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (song == null)
        {
            return NotFound();
        }

        return View("~/Views/Songs/Details.cshtml", song);
    }
}
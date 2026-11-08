using Microsoft.EntityFrameworkCore;
using Notera.Web.Models;

namespace Notera.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Song> Songs { get; set; }
    public DbSet<Instrument> Instruments { get; set; }
    public DbSet<Note> Notes { get; set; }
}
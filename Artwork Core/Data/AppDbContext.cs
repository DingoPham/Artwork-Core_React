using Artwork_Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Artwork_Core.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Illustration> Illustrations { get; set; }
    }
}

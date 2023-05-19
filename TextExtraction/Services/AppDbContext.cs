using Microsoft.EntityFrameworkCore;
using TextExtraction.Model;

namespace TextExtraction.Services
{
    internal class AppDbContext : DbContext
    {
        public DbSet<ImageOcr> ImageOcr { get; set; }
       // public DbSet<ConfigurationSettings> Template { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base (options)
        {

        }
    }
}

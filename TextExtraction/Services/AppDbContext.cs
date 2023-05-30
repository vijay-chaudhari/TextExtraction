using Microsoft.EntityFrameworkCore;
using TextExtraction.Model;

namespace TextExtraction.Services
{
    internal class AppDbContext : DbContext
    {
        public DbSet<ImageOcr> ImageOcr { get; set; }
        public DbSet<ConfigurationSettings> AttributeConfig { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base (options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<FieldConfig>(b =>
            {
                b.ToTable("AppFieldConfig");
            });

            modelBuilder.Entity<ConfigurationSettings>(b =>
            {
                b.ToTable("AppConfigurationSettings").HasKey(x => x.Id);
                b.HasMany(x => x.Fields).WithOne(x => x.ConfigurationSetting).HasForeignKey(x => x.TemplateId).IsRequired(false);
            });
        }
    }
}

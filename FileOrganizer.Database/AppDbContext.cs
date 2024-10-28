using FileOrganizer.Shared;
using Microsoft.EntityFrameworkCore;

namespace FileOrganizer.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Analysis> Analyses { get; set; }
        public DbSet<FileItem> FileItems { get; set; }
        public DbSet<Configuration> Configurations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Analysis>()
                .HasMany(a => a.Files)
                .WithOne(f => f.Analysis)
                .HasForeignKey(f => f.AnalysisId);

            modelBuilder.Entity<Analysis>()
                .HasMany(a => a.Vectors)
                .WithOne(f => f.Analysis)
                .HasForeignKey(f => f.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileItem>();

            modelBuilder.Entity<Configuration>();

        }
    }
}

using aibabag.Models;
using Microsoft.EntityFrameworkCore;

namespace aibabag.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AstrologyInsight> AstrologyInsights => Set<AstrologyInsight>();
    public DbSet<CompatibilityMatch> CompatibilityMatches => Set<CompatibilityMatch>();
    public DbSet<DetailedAstrologyInsight> DetailedAstrologyInsights => Set<DetailedAstrologyInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.GoogleId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(x => x.Insights)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.CompatibilityMatches)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.DetailedInsights)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

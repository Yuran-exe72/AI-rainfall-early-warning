using AI_rainfall_early_warning.Domain.Entities;
using AI_rainfall_early_warning.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<ClimateObservation> ClimateObservations => Set<ClimateObservation>();
    public DbSet<DataImport> DataImports => Set<DataImport>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<ModelVersion> ModelVersions => Set<ModelVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClimateObservation>()
            .HasIndex(o => new { o.RegionId, o.Date, o.Source })
            .IsUnique();

        modelBuilder.Entity<Region>()
            .HasIndex(r => r.Name)
            .IsUnique();
    }
}
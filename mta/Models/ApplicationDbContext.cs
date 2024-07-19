using Microsoft.EntityFrameworkCore;
using mta.Models;
using mta.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantService _currentTenantService;
    public string CurrentTenantId { get; set; }
    private string _currentSchema;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantService currentTenantService) : base(options)
    {
        _currentTenantService = currentTenantService;
        CurrentTenantId = _currentTenantService.TenantId;
        _currentSchema = $"mta_{_currentTenantService.TenantId}";
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(_currentSchema);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(options =>
        {
            options.MigrationsHistoryTable("__EFMigrationsHistory", _currentSchema);
        });
    }

    public override int SaveChanges()
    {
        SetSchema();
        ValidateTenantId();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetSchema();
        ValidateTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantId()
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            throw new InvalidOperationException("Current tenant ID is not set.");
        }
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.TenantId = CurrentTenantId;
                    break;
            }
        }
    }

    public void SetTenantSchema(string tenantKey)
    {
        _currentSchema = $"mta_{tenantKey}";
        SetSchema();
    }

    private void SetSchema()
    {
        Database.ExecuteSqlRaw($"SET search_path TO \"{_currentSchema}\";");
    }
}

using Microsoft.EntityFrameworkCore;
using mta.Services;

namespace mta.Models
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentTenantService _currentTenantService;
        public string CurrentTenantId { get; set; }

        // Constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantService currentTenantService) : base(options)
        { 
            _currentTenantService = currentTenantService;
            CurrentTenantId = _currentTenantService.TenantId;
        }

        // DbSet
        public DbSet<Product> Products { get; set; }

        // On App Startup
        protected override void OnModelCreating(ModelBuilder builder)
        {
            if (!string.IsNullOrEmpty(CurrentTenantId))
            {
                // Configure schema name with TenantId
                builder.HasDefaultSchema(CurrentTenantId);
            }

            builder.Entity<Product>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        }

        // On Configuring
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=mtaDb;Username=mnduc9802;Password=123456");
        }

        // Save Changes
        public override int SaveChanges()
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

            var result = base.SaveChanges();
            return result;
        }
    }
}

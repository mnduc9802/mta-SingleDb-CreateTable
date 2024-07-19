using Microsoft.EntityFrameworkCore;

namespace mta.Services
{
    public class MigrationService
    {
        private readonly IServiceProvider _serviceProvider;

        public MigrationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void MigrateSchema(string schemaName)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Tạo schema nếu chưa tồn tại
            dbContext.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";");

            // Áp dụng migrations cho schema mới
            ApplyMigrations(dbContext, schemaName);
        }

        private void ApplyMigrations(ApplicationDbContext dbContext, string schemaName)
        {
            // Thay đổi schema hiện tại
            dbContext.Database.ExecuteSqlRaw($"SET search_path TO \"{schemaName}\";");

            try
            {
                // Áp dụng migrations
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
            }
        }
    }
}

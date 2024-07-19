using Microsoft.EntityFrameworkCore;
using mta.Models;
using mta.Services;
using Npgsql;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly TenantDbContext _context;

    public CurrentTenantService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SetTenant(string tenantName, string key)
    {
        var tenantInfo = await _context.Tenants.FirstOrDefaultAsync(x => x.Name == tenantName && x.Key == key);
        if (tenantInfo != null)
        {
            TenantId = tenantInfo.Id;
            return true;
        }
        else
        {
            TenantId = Guid.NewGuid().ToString();
            string schemaName = $"mta_{key}";
            CreateSchema(schemaName);

            var newTenant = new Tenant
            {
                Id = TenantId,
                Name = tenantName,
                Key = key,
            };
            _context.Tenants.Add(newTenant);
            await _context.SaveChangesAsync();

            // Create tables in the new schema
            CreateTables(schemaName);

            return true;
        }
    }

    private void CreateTables(string schemaName)
    {
        using var connection = new NpgsqlConnection(_context.Database.GetConnectionString());
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""Products"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" TEXT,
                ""Description"" TEXT,
                ""TenantId"" TEXT
            );";
            command.ExecuteNonQuery();
        }
    }

    private void CreateSchema(string schemaName)
    {
        using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";";
                command.ExecuteNonQuery();
            }
        }
    }

    public string? TenantId { get; set; }
}

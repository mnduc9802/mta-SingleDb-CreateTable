using Microsoft.EntityFrameworkCore;
using mta.Models;
using mta.Services;
using Npgsql;

namespace mta.Services
{
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

            // Get all tables in the default schema
            var tableNames = GetTableNames(connection);

            // Create tables in the new schema
            foreach (var tableName in tableNames)
            {
                if (tableName != "Tenants") // Exclude 'Tenants' table
                {
                    CreateTableInSchema(connection, tableName, schemaName);
                }
            }
        }

        private IEnumerable<string> GetTableNames(NpgsqlConnection connection)
        {
            var tableNames = new List<string>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public' -- Replace 'public' with your default schema if different
          AND table_type = 'BASE TABLE';";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
            }

            return tableNames;
        }

        private void CreateTableInSchema(NpgsqlConnection connection, string tableName, string schemaName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
        CREATE TABLE IF NOT EXISTS ""{schemaName}"".""{tableName}"" (
            LIKE public.""{tableName}"" INCLUDING ALL
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
}

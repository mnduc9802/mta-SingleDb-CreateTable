﻿using Microsoft.EntityFrameworkCore;
using mta.Models;
using mta.Services;
using System;
using System.Threading.Tasks;

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
                // Tạo mới tenant và cơ sở dữ liệu
                TenantId = Guid.NewGuid().ToString(); // Tạo GUID mới
                ConnectionString = $"Host=localhost;Database=mtaDb-mtDb-{key};Username=mnduc9802;Password=123456";
                await CreateDatabaseIfNotExists(ConnectionString);

                // Thêm tenant vào cơ sở dữ liệu chung
                var newTenant = new Tenant
                {
                    Id = TenantId,
                    Name = tenantName,
                    Key = key, // Set Key
                };
                _context.Tenants.Add(newTenant);
                await _context.SaveChangesAsync();

                return true;
            }
        }

        private async Task CreateDatabaseIfNotExists(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new ApplicationDbContext(optionsBuilder.Options, this);
            await context.Database.MigrateAsync();
        }

        public string? TenantId { get; set; }
    }
}

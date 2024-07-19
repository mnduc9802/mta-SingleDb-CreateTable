using Microsoft.EntityFrameworkCore;
using mta.Models;
using mta.Services;
using mta.Services.TenantService;
using mta.Services.TenantService.DTOs;
using Npgsql;
using System;
using System.Linq;

public class TenantService : ITenantService
{
    private readonly TenantDbContext _context;
    private readonly MigrationService _migrationService;
    private readonly ApplicationDbContext _applicationDbContext;

    public TenantService(TenantDbContext context, MigrationService migrationService, ApplicationDbContext applicationDbContext)
    {
        _context = context;
        _migrationService = migrationService;
        _applicationDbContext = applicationDbContext;
    }

    public Tenant CreateTenant(CreateTenantRequest request)
    {
        // Check if the Key already exists
        var existingTenant = _context.Tenants.FirstOrDefault(t => t.Key == request.Key);
        if (existingTenant != null)
        {
            throw new DuplicateKeyException($"Key '{request.Key}' already exists. Please choose a different key.");
        }

        string schemaName = $"mta_{request.Key}";
        CreateSchema(schemaName);

        Tenant tenant = new Tenant
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Key = request.Key,
        };

        _context.Add(tenant);
        _context.SaveChanges();

        // Migrate schema
        _migrationService.MigrateSchema(schemaName);

        return tenant;
    }

    private void CreateSchema(string schemaName)
    {
        _applicationDbContext.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";");
    }

    public class DuplicateKeyException : Exception
    {
        public DuplicateKeyException(string message) : base(message)
        {
        }
    }
}

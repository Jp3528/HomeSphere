using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        HomeSphereDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        // 1. Seed Roles
        var roleNames = new[]
        {
            (Role.Customer, "Registered customer with shopping permissions"),
            (Role.Admin, "System administrator with full access"),
            (Role.Warehouse, "Warehouse staff managing inventory and fulfillment"),
            (Role.Manager, "Store manager supervising orders, promotions and metrics")
        };

        foreach (var (name, description) in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == name))
            {
                context.Roles.Add(new Role
                {
                    Name = name,
                    Description = description
                });
                logger.LogInformation("Seeded role {RoleName}", name);
            }
        }

        await context.SaveChangesAsync();

        // 2. Seed Development Admin User
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@homesphere.dev";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

        var existingAdmin = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == adminEmail.ToLowerInvariant());

        if (existingAdmin == null)
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == Role.Admin);
            var managerRole = await context.Roles.FirstAsync(r => r.Name == Role.Manager);

            var adminUser = new User
            {
                Email = adminEmail.ToLowerInvariant(),
                PasswordHash = passwordHasher.HashPassword(adminPassword),
                FirstName = "Admin",
                LastName = "HomeSphere",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            adminUser.AddRole(adminRole);
            adminUser.AddRole(managerRole);

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded development admin user: {AdminEmail}", adminEmail);
        }
    }
}

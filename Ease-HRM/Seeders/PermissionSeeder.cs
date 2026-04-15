using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Api.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var permissions = new[]
        {
            "userrole.view",
            "rolepermission.view"
        };

        var now = DateTime.UtcNow;

        foreach (var name in permissions)
        {
            if (!await context.Permissions.AnyAsync(x => x.Name == name))
            {
                context.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
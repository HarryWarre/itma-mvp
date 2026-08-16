using Microsoft.EntityFrameworkCore;

namespace Itam.Web.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

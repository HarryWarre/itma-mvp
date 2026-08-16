using Itam.Web.Application.Abstractions;

namespace Itam.Web.Infrastructure.Persistence;

public sealed class EfCoreApplicationPersistence(ApplicationDbContext dbContext)
    : IApplicationPersistence
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

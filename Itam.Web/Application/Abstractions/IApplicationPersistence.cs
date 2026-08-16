namespace Itam.Web.Application.Abstractions;

public interface IApplicationPersistence
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

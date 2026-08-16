namespace Itam.Web.Application.Abstractions;

public interface IApplicationEmailSender
{
    Task SendAsync(ApplicationEmail email, CancellationToken cancellationToken = default);
}

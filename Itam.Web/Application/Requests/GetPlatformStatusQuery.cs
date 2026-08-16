using MediatR;

namespace Itam.Web.Application.Requests;

public sealed record GetPlatformStatusQuery : IRequest<PlatformStatus>;

public sealed record PlatformStatus(string ApplicationName, string FoundationStatus);

public sealed class GetPlatformStatusQueryHandler
    : IRequestHandler<GetPlatformStatusQuery, PlatformStatus>
{
    public Task<PlatformStatus> Handle(
        GetPlatformStatusQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PlatformStatus("ITMA", "Application foundation ready"));
}

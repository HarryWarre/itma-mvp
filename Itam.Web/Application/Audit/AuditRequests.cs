using Itam.Web.Application.Authorization;
using Itam.Web.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Itam.Web.Application.Audit;

public sealed record AuditEntryView(
    DateTimeOffset TimestampUtc,
    string Action,
    string Target,
    string Outcome,
    string? ActorUserId);

public sealed record GetAuditLogQuery(string UserId, int Page, int PageSize)
    : IRequest<PagedAuditLog>;

public sealed record PagedAuditLog(
    IReadOnlyList<AuditEntryView> Entries,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class GetAuditLogQueryHandler(ApplicationDbContext dbContext)
    : IRequestHandler<GetAuditLogQuery, PagedAuditLog>
{
    public async Task<PagedAuditLog> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == request.UserId && item.IsActive, cancellationToken);
        if (membership is null || !await dbContext.RolePermissions.AnyAsync(
                item => item.RoleName == membership.RoleName && item.PermissionName == PermissionCatalog.AuditLogsView,
                cancellationToken))
        {
            return new([], page, pageSize, 0);
        }

        var query = dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.TenantId == membership.TenantId)
            .OrderByDescending(entry => entry.TimestampUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditEntryView(
                entry.TimestampUtc,
                entry.Action,
                entry.Target,
                entry.Outcome,
                entry.ActorUserId))
            .ToListAsync(cancellationToken);

        return new(entries, page, pageSize, totalCount);
    }
}

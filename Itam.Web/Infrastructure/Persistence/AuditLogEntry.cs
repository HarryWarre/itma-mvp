namespace Itam.Web.Infrastructure.Persistence;

public sealed class AuditLogEntry
{
    public long Id { get; set; }

    public Guid TenantId { get; set; }

    public string? ActorUserId { get; set; }

    public required string Action { get; set; }

    public required string Target { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public required string Outcome { get; set; }

    public string? MetadataJson { get; set; }
}

namespace Itam.Web.Infrastructure.Persistence;

public sealed class SettingEntry
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }

    public required string Scope { get; set; }

    public Guid? TenantId { get; set; }

    public string? UserId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

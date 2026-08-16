namespace Itam.Web.Infrastructure.Persistence;

public sealed class Tenant
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<TenantMembership> Memberships { get; } = [];
}

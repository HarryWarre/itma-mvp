namespace Itam.Web.Infrastructure.Persistence;

public sealed class TenantMembership
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string UserId { get; set; }

    public required string RoleName { get; set; }

    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}

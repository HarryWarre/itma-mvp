namespace Itam.Web.Infrastructure.Persistence;

public sealed class RolePermission
{
    public required string RoleName { get; set; }

    public required string PermissionName { get; set; }

    public PermissionDefinition Permission { get; set; } = null!;
}

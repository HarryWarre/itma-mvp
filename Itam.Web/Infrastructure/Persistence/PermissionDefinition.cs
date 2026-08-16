namespace Itam.Web.Infrastructure.Persistence;

public sealed class PermissionDefinition
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public ICollection<RolePermission> RolePermissions { get; } = [];
}

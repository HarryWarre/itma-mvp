using Microsoft.AspNetCore.Identity;

namespace Itam.Web.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;

    public ICollection<TenantMembership> TenantMemberships { get; } = [];
}

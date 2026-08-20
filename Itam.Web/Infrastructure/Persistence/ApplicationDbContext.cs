using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Itam.Web.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<SettingEntry> Settings => Set<SettingEntry>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL")
            .HasDatabaseName("EmailIndex");

        builder.Entity<Tenant>(entity =>
        {
            entity.Property(tenant => tenant.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(tenant => tenant.Name);
        });

        builder.Entity<TenantMembership>(entity =>
        {
            entity.Property(membership => membership.UserId).HasMaxLength(450).IsRequired();
            entity.Property(membership => membership.RoleName).HasMaxLength(100).IsRequired();
            entity.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
            entity.HasOne(membership => membership.Tenant)
                .WithMany(tenant => tenant.Memberships)
                .HasForeignKey(membership => membership.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.User)
                .WithMany(user => user.TenantMemberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PermissionDefinition>(entity =>
        {
            entity.Property(permission => permission.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(permission => permission.Name).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(permission => new { permission.RoleName, permission.PermissionName });
            entity.Property(permission => permission.RoleName).HasMaxLength(100);
            entity.Property(permission => permission.PermissionName).HasMaxLength(150);
            entity.HasOne(permission => permission.Permission)
                .WithMany(definition => definition.RolePermissions)
                .HasForeignKey(permission => permission.PermissionName)
                .HasPrincipalKey(definition => definition.Name)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SettingEntry>(entity =>
        {
            entity.Property(setting => setting.Key).HasMaxLength(150).IsRequired();
            entity.Property(setting => setting.Value).HasMaxLength(4000).IsRequired();
            entity.Property(setting => setting.Scope).HasMaxLength(20).IsRequired();
            entity.Property(setting => setting.UserId).HasMaxLength(450);
            entity.HasIndex(setting => new { setting.Key, setting.Scope, setting.TenantId, setting.UserId }).IsUnique();
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.Property(entry => entry.Action).HasMaxLength(150).IsRequired();
            entity.Property(entry => entry.Target).HasMaxLength(300).IsRequired();
            entity.Property(entry => entry.Outcome).HasMaxLength(50).IsRequired();
            entity.HasIndex(entry => new { entry.TenantId, entry.TimestampUtc });
        });
    }
}

using Itam.Web.Application.Authorization;
using Itam.Web.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Itam.Web.Application.Settings;

public sealed record SettingValue
{
    public required string Key { get; init; }

    public string Value { get; set; } = string.Empty;

    public required string Scope { get; init; }
}

public static class ApplicationSettingDefaults
{
    public static IReadOnlyDictionary<string, string> Values { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["language"] = "vi",
            ["compact-navigation"] = "false"
        };

    public static bool IsSupported(string key) => Values.ContainsKey(key);

    public static bool IsValid(string key, string value) => key switch
    {
        "language" => value is "vi" or "en",
        "compact-navigation" => bool.TryParse(value, out _),
        _ => false
    };
}

public sealed record GetSettingsQuery(string UserId) : IRequest<IReadOnlyList<SettingValue>>;

public sealed class GetSettingsQueryHandler(ApplicationDbContext dbContext)
    : IRequestHandler<GetSettingsQuery, IReadOnlyList<SettingValue>>
{
    public async Task<IReadOnlyList<SettingValue>> Handle(
        GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == request.UserId && item.IsActive, cancellationToken);
        if (membership is null || !await HasPermission(membership.RoleName, PermissionCatalog.SettingsView, cancellationToken))
        {
            return [];
        }

        var settings = await dbContext.Settings
            .AsNoTracking()
            .Where(item =>
                item.Key != null &&
                (item.TenantId == membership.TenantId || item.UserId == request.UserId))
            .ToListAsync(cancellationToken);

        return ApplicationSettingDefaults.Values
            .Select(defaultValue =>
            {
                var userSetting = settings.SingleOrDefault(item =>
                    item.Key == defaultValue.Key && item.Scope == "user" && item.UserId == request.UserId);
                var tenantSetting = settings.SingleOrDefault(item =>
                    item.Key == defaultValue.Key && item.Scope == "tenant" && item.TenantId == membership.TenantId);
                return userSetting is not null
                    ? new SettingValue { Key = defaultValue.Key, Value = userSetting.Value, Scope = "user" }
                    : tenantSetting is not null
                        ? new SettingValue { Key = defaultValue.Key, Value = tenantSetting.Value, Scope = "tenant" }
                        : new SettingValue { Key = defaultValue.Key, Value = defaultValue.Value, Scope = "system" };
            })
            .ToArray();
    }

    private Task<bool> HasPermission(string roleName, string permissionName, CancellationToken cancellationToken) =>
        dbContext.RolePermissions.AnyAsync(
            item => item.RoleName == roleName && item.PermissionName == permissionName,
            cancellationToken);
}

public sealed record SaveSettingCommand(
    string UserId,
    string Key,
    string Value,
    string Scope) : IRequest<SaveSettingResult>;

public sealed record SaveSettingResult(bool Succeeded, string? Error = null);

public sealed class SaveSettingCommandHandler(ApplicationDbContext dbContext)
    : IRequestHandler<SaveSettingCommand, SaveSettingResult>
{
    public async Task<SaveSettingResult> Handle(
        SaveSettingCommand request,
        CancellationToken cancellationToken)
    {
        var key = request.Key.Trim().ToLowerInvariant();
        var value = request.Value.Trim();
        var scope = request.Scope.Trim().ToLowerInvariant();
        if (!ApplicationSettingDefaults.IsSupported(key) ||
            !ApplicationSettingDefaults.IsValid(key, value) ||
            scope is not ("tenant" or "user"))
        {
            return new(false, "The setting value is invalid.");
        }

        var membership = await dbContext.TenantMemberships
            .SingleOrDefaultAsync(item => item.UserId == request.UserId && item.IsActive, cancellationToken);
        if (membership is null ||
            !await dbContext.RolePermissions.AnyAsync(
                item => item.RoleName == membership.RoleName && item.PermissionName == PermissionCatalog.SettingsManage,
                cancellationToken))
        {
            return new(false, "You are not allowed to change this setting.");
        }

        var setting = await dbContext.Settings.SingleOrDefaultAsync(item =>
            item.Key == key &&
            item.Scope == scope &&
            item.TenantId == (scope == "tenant" ? membership.TenantId : null) &&
            item.UserId == (scope == "user" ? request.UserId : null), cancellationToken);
        if (setting is null)
        {
            setting = new SettingEntry
            {
                Id = Guid.NewGuid(),
                Key = key,
                Scope = scope,
                TenantId = scope == "tenant" ? membership.TenantId : null,
                UserId = scope == "user" ? request.UserId : null,
                Value = value,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            TenantId = membership.TenantId,
            ActorUserId = request.UserId,
            Action = "settings.updated",
            Target = key,
            TimestampUtc = DateTimeOffset.UtcNow,
            Outcome = "succeeded"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true);
    }
}

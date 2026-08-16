namespace Itam.Web.Application.Authorization;

public static class PermissionCatalog
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";
    public const string AuditLogsView = "audit_logs.view";

    public static IReadOnlyList<string> All { get; } =
    [
        UsersView,
        UsersManage,
        RolesManage,
        SettingsView,
        SettingsManage,
        AuditLogsView
    ];
}

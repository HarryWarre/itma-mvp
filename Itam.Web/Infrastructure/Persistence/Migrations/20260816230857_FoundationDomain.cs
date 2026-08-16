using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Itam.Web.Infrastructure.Persistence;

#nullable disable

namespace Itam.Web.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260816230857_FoundationDomain")]
public partial class FoundationDomain : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "AspNetUsers");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail",
            unique: true,
            filter: "\"NormalizedEmail\" IS NOT NULL");

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "AuditLogEntries",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<string>(type: "text", nullable: true),
                Action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Target = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                MetadataJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogEntries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PermissionDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PermissionDefinitions", x => x.Id);
                table.UniqueConstraint("AK_PermissionDefinitions_Name", x => x.Name);
            });

        migrationBuilder.CreateTable(
            name: "Settings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Settings", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Tenants", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PermissionName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleName, x.PermissionName });
                table.ForeignKey(
                    name: "FK_RolePermissions_PermissionDefinitions_PermissionName",
                    column: x => x.PermissionName,
                    principalTable: "PermissionDefinitions",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TenantMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_TenantMemberships_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TenantMemberships_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_TenantId_TimestampUtc",
            table: "AuditLogEntries",
            columns: new[] { "TenantId", "TimestampUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_PermissionDefinitions_Name",
            table: "PermissionDefinitions",
            column: "Name",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionName",
            table: "RolePermissions",
            column: "PermissionName");
        migrationBuilder.CreateIndex(
            name: "IX_Settings_Key_Scope_TenantId_UserId",
            table: "Settings",
            columns: new[] { "Key", "Scope", "TenantId", "UserId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships",
            columns: new[] { "TenantId", "UserId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_UserId",
            table: "TenantMemberships",
            column: "UserId");
        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Name",
            table: "Tenants",
            column: "Name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "AspNetUsers");
        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.DropTable(name: "AuditLogEntries");
        migrationBuilder.DropTable(name: "RolePermissions");
        migrationBuilder.DropTable(name: "Settings");
        migrationBuilder.DropTable(name: "TenantMemberships");
        migrationBuilder.DropTable(name: "PermissionDefinitions");
        migrationBuilder.DropTable(name: "Tenants");
        migrationBuilder.DropColumn(name: "IsActive", table: "AspNetUsers");
    }
}

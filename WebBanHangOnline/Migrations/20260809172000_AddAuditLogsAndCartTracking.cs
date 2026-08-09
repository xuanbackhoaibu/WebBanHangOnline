using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260809172000_AddAuditLogsAndCartTracking")]
    public partial class AddAuditLogsAndCartTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CartItems");
        }
    }
}

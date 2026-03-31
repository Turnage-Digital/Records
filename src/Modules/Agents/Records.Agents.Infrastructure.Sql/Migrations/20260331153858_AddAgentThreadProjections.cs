using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Records.Agents.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentThreadProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentThreadProjections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentThreadProjections", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                INSERT INTO `AgentThreadProjections` (`Id`, `UserId`, `TenantId`, `Title`, `UpdatedAt`)
                SELECT
                    `Id`,
                    `UserId`,
                    NULLIF(TRIM(`TenantId`), ''),
                    `Title`,
                    `UpdatedAt`
                FROM `AgentThreads`;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AgentThreadProjections_UserId_TenantId_UpdatedAt",
                table: "AgentThreadProjections",
                columns: new[] { "UserId", "TenantId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentThreadProjections");
        }
    }
}

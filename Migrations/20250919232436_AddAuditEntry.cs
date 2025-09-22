using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassItOnAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    DataJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_PerformedByUserId",
                table: "AuditEntries",
                column: "PerformedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}

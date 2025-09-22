using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassItOnAcademy.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPaymentreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "ProviderReference",
                table: "Payments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Payments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedUtc",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderReference",
                table: "Payments",
                column: "ProviderReference");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Reference",
                table: "Payments",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Reference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedUtc",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Payments",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayPaymentId",
                table: "Payments",
                column: "GatewayPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantReference",
                table: "Payments",
                column: "MerchantReference");
        }
    }
}

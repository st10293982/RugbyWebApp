using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassItOnAcademy.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "Payments",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(24)",
                oldMaxLength: 24);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayPaymentId",
                table: "Payments",
                column: "GatewayPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantReference",
                table: "Payments",
                column: "MerchantReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_BookingId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "Payments",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(24)",
                oldMaxLength: 24,
                oldNullable: true);
        }
    }
}

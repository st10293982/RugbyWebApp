using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassItOnAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademySetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CancelCutoffHours = table.Column<int>(type: "int", nullable: false),
                    SeatHoldMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultCapacity = table.Column<int>(type: "int", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CoachName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CoachBio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    Instagram = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Facebook = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LinkedIn = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademySettings");
        }
    }
}

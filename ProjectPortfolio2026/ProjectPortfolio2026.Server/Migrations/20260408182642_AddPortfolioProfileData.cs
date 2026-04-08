using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPortfolio2026.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioProfileData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactHeadline = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ContactIntro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvailabilityHeadline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AvailabilitySummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioContactMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioProfileId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Href = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioContactMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioContactMethods_PortfolioProfiles_PortfolioProfileId",
                        column: x => x.PortfolioProfileId,
                        principalTable: "PortfolioProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioSocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioProfileId = table.Column<int>(type: "int", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSocialLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioSocialLinks_PortfolioProfiles_PortfolioProfileId",
                        column: x => x.PortfolioProfileId,
                        principalTable: "PortfolioProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioContactMethods_PortfolioProfileId",
                table: "PortfolioContactMethods",
                column: "PortfolioProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSocialLinks_PortfolioProfileId",
                table: "PortfolioSocialLinks",
                column: "PortfolioProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioContactMethods");

            migrationBuilder.DropTable(
                name: "PortfolioSocialLinks");

            migrationBuilder.DropTable(
                name: "PortfolioProfiles");
        }
    }
}

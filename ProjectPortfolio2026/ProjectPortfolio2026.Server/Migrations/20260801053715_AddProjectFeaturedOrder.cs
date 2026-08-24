using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPortfolio2026.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectFeaturedOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeaturedOrder",
                table: "Projects",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturedOrder",
                table: "Projects");
        }
    }
}

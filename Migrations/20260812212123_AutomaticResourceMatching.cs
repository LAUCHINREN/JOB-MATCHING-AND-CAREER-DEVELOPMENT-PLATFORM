using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AutomaticResourceMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceRecommendations");

            migrationBuilder.AddColumn<string>(
                name: "RelatedCategory",
                table: "CareerResources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedSkill",
                table: "CareerResources",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedCategory",
                table: "CareerResources");

            migrationBuilder.DropColumn(
                name: "RelatedSkill",
                table: "CareerResources");

            migrationBuilder.CreateTable(
                name: "ResourceRecommendations",
                columns: table => new
                {
                    ResourceRecommendationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerResourceId = table.Column<int>(type: "int", nullable: false),
                    CareerAdvisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobSeekerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecommendedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRecommendations", x => x.ResourceRecommendationId);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_CareerResources_CareerResourceId",
                        column: x => x.CareerResourceId,
                        principalTable: "CareerResources",
                        principalColumn: "CareerResourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_CareerResourceId",
                table: "ResourceRecommendations",
                column: "CareerResourceId");
        }
    }
}

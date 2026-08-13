using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerAdvisorFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerResources",
                columns: table => new
                {
                    CareerResourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerResources", x => x.CareerResourceId);
                    table.ForeignKey(
                        name: "FK_CareerResources_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CounsellingSessions",
                columns: table => new
                {
                    CounsellingSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobSeekerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CareerAdvisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounsellingSessions", x => x.CounsellingSessionId);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRecommendations",
                columns: table => new
                {
                    ResourceRecommendationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerResourceId = table.Column<int>(type: "int", nullable: false),
                    JobSeekerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CareerAdvisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "IX_CareerResources_CreatedByUserId",
                table: "CareerResources",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_CareerResourceId",
                table: "ResourceRecommendations",
                column: "CareerResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CounsellingSessions");

            migrationBuilder.DropTable(
                name: "ResourceRecommendations");

            migrationBuilder.DropTable(
                name: "CareerResources");
        }
    }
}

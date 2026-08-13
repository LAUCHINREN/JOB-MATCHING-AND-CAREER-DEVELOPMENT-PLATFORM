using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeS3Key",
                table: "JobSeekerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "JobPostings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "JobPostings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "JobPostings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacancyStatus",
                table: "JobPostings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "JobApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyProfileTable",
                columns: table => new
                {
                    CompanyProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanySize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoS3Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfileTable", x => x.CompanyProfileId);
                });

            migrationBuilder.CreateTable(
                name: "FitScoreSettings",
                columns: table => new
                {
                    FitScoreSettingsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyProfileId = table.Column<int>(type: "int", nullable: false),
                    SalaryWeight = table.Column<int>(type: "int", nullable: false),
                    CategoryWeight = table.Column<int>(type: "int", nullable: false),
                    LocationWeight = table.Column<int>(type: "int", nullable: false),
                    EducationWeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitScoreSettings", x => x.FitScoreSettingsId);
                    table.ForeignKey(
                        name: "FK_FitScoreSettings_CompanyProfileTable_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfileTable",
                        principalColumn: "CompanyProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId",
                table: "JobApplications",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_FitScoreSettings_CompanyProfileId",
                table: "FitScoreSettings",
                column: "CompanyProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_JobPostings_JobId",
                table: "JobApplications",
                column: "JobId",
                principalTable: "JobPostings",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_JobPostings_JobId",
                table: "JobApplications");

            migrationBuilder.DropTable(
                name: "FitScoreSettings");

            migrationBuilder.DropTable(
                name: "CompanyProfileTable");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobId",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeS3Key",
                table: "JobSeekerProfiles");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "VacancyStatus",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "JobApplications");
        }
    }
}

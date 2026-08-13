using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillAssessmentAuthorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SkillAssessments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "SkillAssessments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessments_CreatedByUserId",
                table: "SkillAssessments",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SkillAssessments_AspNetUsers_CreatedByUserId",
                table: "SkillAssessments",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillAssessments_AspNetUsers_CreatedByUserId",
                table: "SkillAssessments");

            migrationBuilder.DropIndex(
                name: "IX_SkillAssessments_CreatedByUserId",
                table: "SkillAssessments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SkillAssessments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SkillAssessments");
        }
    }
}

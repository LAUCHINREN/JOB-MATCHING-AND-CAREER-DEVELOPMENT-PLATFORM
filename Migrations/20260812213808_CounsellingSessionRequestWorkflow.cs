using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class CounsellingSessionRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CareerAdvisorUserId",
                table: "CounsellingSessions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "CounsellingSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Pre-existing sessions used "Scheduled" for an advisor-confirmed session;
            // the new status vocabulary calls that state "Approved".
            migrationBuilder.Sql("UPDATE [CounsellingSessions] SET [Status] = 'Approved' WHERE [Status] = 'Scheduled';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "CounsellingSessions");

            migrationBuilder.AlterColumn<string>(
                name: "CareerAdvisorUserId",
                table: "CounsellingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

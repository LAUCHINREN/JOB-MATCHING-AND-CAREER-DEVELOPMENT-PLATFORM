using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCareerPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddCounsellingSessionMeetingLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "CounsellingSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "CounsellingSessions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAiGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiGrade",
                table: "FeedbackDrafts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiGrade",
                table: "FeedbackDrafts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

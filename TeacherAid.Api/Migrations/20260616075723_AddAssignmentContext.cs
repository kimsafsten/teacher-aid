using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentId",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentId",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentId",
                table: "CourseDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "CourseDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "CourseDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "CourseDocuments");
        }
    }
}

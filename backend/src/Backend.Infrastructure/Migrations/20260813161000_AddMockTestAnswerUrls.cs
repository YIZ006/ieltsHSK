using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMockTestAnswerUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ListeningAnswerUrl",
                table: "MockTests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadingAnswerUrl",
                table: "MockTests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpeakingAnswerUrl",
                table: "MockTests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WritingAnswerUrl",
                table: "MockTests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListeningAnswerUrl",
                table: "MockTests");

            migrationBuilder.DropColumn(
                name: "ReadingAnswerUrl",
                table: "MockTests");

            migrationBuilder.DropColumn(
                name: "SpeakingAnswerUrl",
                table: "MockTests");

            migrationBuilder.DropColumn(
                name: "WritingAnswerUrl",
                table: "MockTests");
        }
    }
}

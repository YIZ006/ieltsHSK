using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIeltsVocabularyImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "audio_key",
                table: "test_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "graded_at",
                table: "test_submissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "test_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "teacher_feedback",
                table: "test_submissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_key",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "graded_at",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "teacher_feedback",
                table: "test_submissions");
        }
    }
}

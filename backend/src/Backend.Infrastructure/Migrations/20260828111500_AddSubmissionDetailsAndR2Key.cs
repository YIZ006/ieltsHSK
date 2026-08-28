using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionDetailsAndR2Key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "student_name",
                table: "test_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "test_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exam_title",
                table: "test_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_number",
                table: "test_submissions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "r2_storage_key",
                table: "test_submissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "student_name",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "exam_title",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "attempt_number",
                table: "test_submissions");

            migrationBuilder.DropColumn(
                name: "r2_storage_key",
                table: "test_submissions");
        }
    }
}

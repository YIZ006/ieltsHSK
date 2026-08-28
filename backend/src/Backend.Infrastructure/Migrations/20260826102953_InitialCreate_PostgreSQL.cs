using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đã xóa nội dung để tránh lỗi "relation already exists" do merge nhánh gây ra hai file InitialCreate.
            // Bảng đã được tạo bởi file 20260826062942_InitialPostgreSQL.cs
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "hsk_vocabularies");

            migrationBuilder.DropTable(
                name: "hsk_vocabulary_imports");

            migrationBuilder.DropTable(
                name: "hsk_vocabulary_progresses");

            migrationBuilder.DropTable(
                name: "learning_resources");

            migrationBuilder.DropTable(
                name: "learning_sections");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "listen_videos");

            migrationBuilder.DropTable(
                name: "mock_tests");

            migrationBuilder.DropTable(
                name: "stories");

            migrationBuilder.DropTable(
                name: "test_submissions");

            migrationBuilder.DropTable(
                name: "user_activity_logs");

            migrationBuilder.DropTable(
                name: "websites");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHskMockTestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hsk_mock_tests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_name = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    listening_url = table.Column<string>(type: "text", nullable: true),
                    reading_url = table.Column<string>(type: "text", nullable: true),
                    writing_url = table.Column<string>(type: "text", nullable: true),
                    speaking_url = table.Column<string>(type: "text", nullable: true),
                    listening_answer_url = table.Column<string>(type: "text", nullable: true),
                    reading_answer_url = table.Column<string>(type: "text", nullable: true),
                    writing_answer_url = table.Column<string>(type: "text", nullable: true),
                    speaking_answer_url = table.Column<string>(type: "text", nullable: true),
                    hsk_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hsk_mock_tests", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hsk_mock_tests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHskSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HskUrl",
                table: "MockTests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HskVocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HskLevel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Hanzi = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pinyin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Meaning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExampleSentence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamplePinyin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExampleMeaning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HskVocabularies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HskVocabularies_HskLevel",
                table: "HskVocabularies",
                column: "HskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_HskVocabularies_HskLevel_Hanzi",
                table: "HskVocabularies",
                columns: new[] { "HskLevel", "Hanzi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HskVocabularies");

            migrationBuilder.DropColumn(
                name: "HskUrl",
                table: "MockTests");
        }
    }
}

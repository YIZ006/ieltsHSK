using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dọn cột user_id1 còn sót từ drift snapshot cũ — có điều kiện để chạy an toàn
            // trên mọi môi trường (một số DB không hề có cột này)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'hsk_vocabulary_progresses'
                          AND column_name = 'user_id1'
                    ) THEN
                        ALTER TABLE hsk_vocabulary_progresses DROP CONSTRAINT IF EXISTS fk_hsk_vocabulary_progresses_users_user_id1;
                        DROP INDEX IF EXISTS ix_hsk_vocabulary_progresses_user_id1;
                        ALTER TABLE hsk_vocabulary_progresses DROP COLUMN user_id1;
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_expires_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            // Khôi phục cột user_id1 chỉ khi môi trường từng có cột này (drift cũ)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'hsk_vocabulary_progresses'
                          AND column_name = 'user_id1'
                    ) THEN
                        ALTER TABLE hsk_vocabulary_progresses ADD COLUMN user_id1 integer;
                        CREATE INDEX ix_hsk_vocabulary_progresses_user_id1 ON hsk_vocabulary_progresses (user_id1);
                    END IF;
                END $$;
            ");
        }
    }
}

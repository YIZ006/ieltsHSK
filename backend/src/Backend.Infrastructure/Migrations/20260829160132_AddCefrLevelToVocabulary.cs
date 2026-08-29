using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCefrLevelToVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm user_id vào listen_videos (nếu chưa tồn tại)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                                   WHERE table_name='listen_videos' AND column_name='user_id') THEN
                        ALTER TABLE listen_videos ADD COLUMN user_id integer NULL;
                    END IF;
                END $$;
            ");

            // Thêm cefr_level vào ielts_vocabularies
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                                   WHERE table_name='ielts_vocabularies' AND column_name='cefr_level') THEN
                        ALTER TABLE ielts_vocabularies ADD COLUMN cefr_level text NULL;
                    END IF;
                END $$;
            ");

            // Thêm user_id1 vào hsk_vocabulary_progresses (nếu chưa tồn tại)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                                   WHERE table_name='hsk_vocabulary_progresses' AND column_name='user_id1') THEN
                        ALTER TABLE hsk_vocabulary_progresses ADD COLUMN user_id1 integer NULL;
                    END IF;
                END $$;
            ");

            // Tạo bảng ielts_vocabulary_progresses (nếu chưa tồn tại)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ielts_vocabulary_progresses (
                    id serial PRIMARY KEY,
                    user_id integer NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    vocabulary_id integer NOT NULL REFERENCES ielts_vocabularies(id) ON DELETE CASCADE,
                    status text NOT NULL,
                    learned_at timestamptz NOT NULL
                );
            ");

            // Indexes (IF NOT EXISTS)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_listen_videos_user_id ON listen_videos(user_id);
                CREATE INDEX IF NOT EXISTS ix_hsk_vocabulary_progresses_user_id1 ON hsk_vocabulary_progresses(user_id1);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_ielts_vocabulary_progresses_user_id_vocabulary_id ON ielts_vocabulary_progresses(user_id, vocabulary_id);
                CREATE INDEX IF NOT EXISTS ix_ielts_vocabulary_progresses_vocabulary_id ON ielts_vocabulary_progresses(vocabulary_id);
            ");

            // Foreign keys (IF NOT EXISTS via DO block)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_hsk_vocabulary_progresses_users_user_id1') THEN
                        ALTER TABLE hsk_vocabulary_progresses
                            ADD CONSTRAINT fk_hsk_vocabulary_progresses_users_user_id1
                            FOREIGN KEY (user_id1) REFERENCES users(id);
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_listen_videos_users_user_id') THEN
                        ALTER TABLE listen_videos
                            ADD CONSTRAINT fk_listen_videos_users_user_id
                            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_test_submissions_users_user_id') THEN
                        ALTER TABLE test_submissions
                            ADD CONSTRAINT fk_test_submissions_users_user_id
                            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hsk_vocabulary_progresses_users_user_id1",
                table: "hsk_vocabulary_progresses");

            migrationBuilder.DropForeignKey(
                name: "fk_listen_videos_users_user_id",
                table: "listen_videos");

            migrationBuilder.DropForeignKey(
                name: "fk_test_submissions_users_user_id",
                table: "test_submissions");

            migrationBuilder.DropTable(
                name: "ielts_vocabulary_progresses");

            migrationBuilder.DropIndex(
                name: "ix_listen_videos_user_id",
                table: "listen_videos");

            migrationBuilder.DropIndex(
                name: "ix_hsk_vocabulary_progresses_user_id1",
                table: "hsk_vocabulary_progresses");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "listen_videos");

            migrationBuilder.DropColumn(
                name: "cefr_level",
                table: "ielts_vocabularies");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "hsk_vocabulary_progresses");
        }
    }
}

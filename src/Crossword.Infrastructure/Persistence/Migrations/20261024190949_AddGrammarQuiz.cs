using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Crossword.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrammarQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quiz_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    topic = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    options = table.Column<string[]>(type: "text[]", nullable: false),
                    correct_index = table.Column<int>(type: "integer", nullable: false),
                    explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    daily_date = table.Column<DateOnly>(type: "date", nullable: true),
                    topic = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    question_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    choices = table.Column<int[]>(type: "integer[]", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_progress",
                columns: table => new
                {
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    seen_count = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    last_correct = table.Column<bool>(type: "boolean", nullable: false),
                    last_answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_answered_on = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_progress", x => x.question_id);
                    table.ForeignKey(
                        name: "fk_question_progress_quiz_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "quiz_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_key",
                table: "quiz_questions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_topic",
                table: "quiz_questions",
                column: "topic");

            migrationBuilder.CreateIndex(
                name: "ix_quiz_sessions_daily_date",
                table: "quiz_sessions",
                column: "daily_date",
                unique: true,
                filter: "daily_date IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "question_progress");

            migrationBuilder.DropTable(
                name: "quiz_sessions");

            migrationBuilder.DropTable(
                name: "quiz_questions");
        }
    }
}

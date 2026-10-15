using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Crossword.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    total_xp = table.Column<int>(type: "integer", nullable: false),
                    puzzles_completed = table.Column<int>(type: "integer", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    last_played_on = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "puzzles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    seed = table.Column<int>(type: "integer", nullable: false),
                    daily_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puzzles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unlocked_achievements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unlocked_achievements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "words",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    english = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    turkish = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    part_of_speech = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    level = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    frequency_rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_words", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    puzzle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    letters = table.Column<string>(type: "text", nullable: false),
                    revealed_mask = table.Column<string>(type: "text", nullable: false),
                    mistake_mask = table.Column<string>(type: "text", nullable: false),
                    elapsed_seconds = table.Column<int>(type: "integer", nullable: false),
                    checks_used = table.Column<int>(type: "integer", nullable: false),
                    reveals_used = table.Column<int>(type: "integer", nullable: false),
                    failed_submits = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    xp_earned = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_game_sessions_puzzles_puzzle_id",
                        column: x => x.puzzle_id,
                        principalTable: "puzzles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "puzzle_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    puzzle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    orientation = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    column_index = table.Column<int>(type: "integer", nullable: false),
                    answer = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    clue = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puzzle_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_puzzle_entries_puzzles_puzzle_id",
                        column: x => x.puzzle_id,
                        principalTable: "puzzles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_puzzle_entries_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "word_masteries",
                columns: table => new
                {
                    word_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_masteries", x => new { x.word_id, x.mode });
                    table.ForeignKey(
                        name: "fk_word_masteries_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_puzzle_id_status",
                table: "game_sessions",
                columns: new[] { "puzzle_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_entries_puzzle_id",
                table: "puzzle_entries",
                column: "puzzle_id");

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_entries_word_id",
                table: "puzzle_entries",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_puzzles_mode_daily_date",
                table: "puzzles",
                columns: new[] { "mode", "daily_date" },
                unique: true,
                filter: "daily_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_puzzles_mode_difficulty",
                table: "puzzles",
                columns: new[] { "mode", "difficulty" });

            migrationBuilder.CreateIndex(
                name: "ix_unlocked_achievements_code",
                table: "unlocked_achievements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_words_english",
                table: "words",
                column: "english",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_sessions");

            migrationBuilder.DropTable(
                name: "player_profiles");

            migrationBuilder.DropTable(
                name: "puzzle_entries");

            migrationBuilder.DropTable(
                name: "unlocked_achievements");

            migrationBuilder.DropTable(
                name: "word_masteries");

            migrationBuilder.DropTable(
                name: "puzzles");

            migrationBuilder.DropTable(
                name: "words");
        }
    }
}

using Crossword.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crossword.Infrastructure.Persistence;

// Enums are stored as short strings so the tables stay readable in psql.

internal sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> b)
    {
        b.ToTable("words");
        b.Property(w => w.English).HasMaxLength(40);
        b.Property(w => w.Turkish).HasMaxLength(40);
        b.Property(w => w.PartOfSpeech).HasConversion<string>().HasMaxLength(16);
        b.Property(w => w.Level).HasConversion<string>().HasMaxLength(4);
        b.HasIndex(w => w.English).IsUnique();
    }
}

internal sealed class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> b)
    {
        b.ToTable("puzzles");
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Mode).HasConversion<string>().HasMaxLength(16);
        b.Property(p => p.Difficulty).HasConversion<string>().HasMaxLength(16);
        b.HasMany(p => p.Entries).WithOne(e => e.Puzzle).HasForeignKey(e => e.PuzzleId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(p => p.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(p => new { p.Mode, p.Difficulty });
        b.HasIndex(p => new { p.Mode, p.DailyDate }).IsUnique().HasFilter("daily_date IS NOT NULL");
    }
}

internal sealed class PuzzleEntryConfiguration : IEntityTypeConfiguration<PuzzleEntry>
{
    public void Configure(EntityTypeBuilder<PuzzleEntry> b)
    {
        b.ToTable("puzzle_entries");
        b.Property(e => e.Orientation).HasConversion<string>().HasMaxLength(8);
        b.Property(e => e.Row).HasColumnName("row_index");
        b.Property(e => e.Column).HasColumnName("column_index");
        b.Property(e => e.Answer).HasMaxLength(40);
        b.Property(e => e.Clue).HasMaxLength(40);
        b.HasOne(e => e.Word).WithMany().HasForeignKey(e => e.WordId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.WordId);
    }
}

internal sealed class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> b)
    {
        b.ToTable("game_sessions");
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        b.HasOne(s => s.Puzzle).WithMany().HasForeignKey(s => s.PuzzleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(s => new { s.PuzzleId, s.Status });
    }
}

internal sealed class WordMasteryConfiguration : IEntityTypeConfiguration<WordMastery>
{
    public void Configure(EntityTypeBuilder<WordMastery> b)
    {
        b.ToTable("word_masteries");
        b.HasKey(m => new { m.WordId, m.Mode });
        b.Property(m => m.Mode).HasConversion<string>().HasMaxLength(16);
        b.HasOne(m => m.Word).WithMany().HasForeignKey(m => m.WordId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlayerProfileConfiguration : IEntityTypeConfiguration<PlayerProfile>
{
    public void Configure(EntityTypeBuilder<PlayerProfile> b)
    {
        b.ToTable("player_profiles");
        b.Property(p => p.Id).ValueGeneratedNever();
    }
}

internal sealed class UnlockedAchievementConfiguration : IEntityTypeConfiguration<UnlockedAchievement>
{
    public void Configure(EntityTypeBuilder<UnlockedAchievement> b)
    {
        b.ToTable("unlocked_achievements");
        b.Property(a => a.Code).HasMaxLength(40);
        b.HasIndex(a => a.Code).IsUnique();
    }
}

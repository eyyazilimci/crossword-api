using Crossword.Application.Abstractions;
using Crossword.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Puzzle> Puzzles => Set<Puzzle>();
    public DbSet<PuzzleEntry> PuzzleEntries => Set<PuzzleEntry>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<WordMastery> WordMasteries => Set<WordMastery>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<UnlockedAchievement> UnlockedAchievements => Set<UnlockedAchievement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

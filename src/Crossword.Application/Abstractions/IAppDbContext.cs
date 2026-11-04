using Crossword.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Word> Words { get; }
    DbSet<Puzzle> Puzzles { get; }
    DbSet<PuzzleEntry> PuzzleEntries { get; }
    DbSet<GameSession> GameSessions { get; }
    DbSet<WordMastery> WordMasteries { get; }
    DbSet<PlayerProfile> PlayerProfiles { get; }
    DbSet<UnlockedAchievement> UnlockedAchievements { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizSession> QuizSessions { get; }
    DbSet<QuestionProgress> QuestionProgress { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

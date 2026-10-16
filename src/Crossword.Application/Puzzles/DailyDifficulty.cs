using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Gamification;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

/// <param name="Difficulty">Difficulty chosen for today.</param>
/// <param name="PreviousDifficulty">Difficulty of the last daily puzzle, null on the first day.</param>
/// <param name="PreviousAccuracy">Word accuracy (0–1) on that puzzle, null if it was not finished.</param>
public sealed record DailyDifficultyDecision(Difficulty Difficulty, Difficulty? PreviousDifficulty, double? PreviousAccuracy);

internal static class DailyDifficulty
{
    /// <summary>Applies <see cref="DailyDifficultyPolicy"/> to the last daily puzzle before <paramref name="today"/>.</summary>
    public static async Task<DailyDifficultyDecision> DecideAsync(
        this IAppDbContext db, GameMode mode, DateOnly today, CancellationToken ct)
    {
        var last = await db.Puzzles.AsNoTracking()
            .Where(p => p.Mode == mode && p.DailyDate != null && p.DailyDate < today)
            .OrderByDescending(p => p.DailyDate)
            .Select(p => new { p.Id, p.Difficulty })
            .FirstOrDefaultAsync(ct);

        if (last is null)
            return new DailyDifficultyDecision(DailyDifficultyPolicy.Next(null, null), null, null);

        // The first finished attempt counts; replays of an already-known grid would inflate the score.
        var session = await db.GameSessions.AsNoTracking()
            .Include(s => s.Puzzle).ThenInclude(p => p.Entries)
            .Where(s => s.PuzzleId == last.Id && s.Status == SessionStatus.Completed)
            .OrderBy(s => s.CompletedAt)
            .FirstOrDefaultAsync(ct);

        var accuracy = session?.WordAccuracy();
        return new DailyDifficultyDecision(DailyDifficultyPolicy.Next(last.Difficulty, accuracy), last.Difficulty, accuracy);
    }
}

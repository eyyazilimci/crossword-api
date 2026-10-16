using Crossword.Application.Abstractions;
using Crossword.Application.Gamification;
using Crossword.Application.Puzzles;
using Crossword.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Stats;

public sealed record InProgressGameDto(
    Guid SessionId,
    Guid PuzzleId,
    GameMode Mode,
    Difficulty Difficulty,
    bool IsDaily,
    int FilledPercent,
    int ElapsedSeconds,
    DateTime UpdatedAt);

/// <param name="Difficulty">Today's difficulty: the existing puzzle's, or the one that will be generated.</param>
/// <param name="Decision">Why the adaptive rule picks this level (based on the previous daily puzzle).</param>
public sealed record DailyStatusDto(GameMode Mode, Guid? PuzzleId, bool Completed, Difficulty Difficulty, DailyDifficultyDecision Decision);

public sealed record DashboardDto(
    ProfileDto Profile,
    IReadOnlyList<InProgressGameDto> InProgress,
    IReadOnlyList<DailyStatusDto> Daily,
    int LibraryPuzzlesLeft);

public sealed record GetDashboardQuery : IQuery<DashboardDto>;

public sealed class GetDashboardHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetDashboardQuery, DashboardDto>
{
    public async ValueTask<DashboardDto> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var profile = await db.GetProfileAsync(ct);
        var today = clock.Today;

        var sessions = await db.GameSessions.AsNoTracking()
            .Where(s => s.Status == SessionStatus.InProgress)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(6)
            .Select(s => new
            {
                s.Id,
                s.PuzzleId,
                s.Puzzle.Mode,
                s.Puzzle.Difficulty,
                IsDaily = s.Puzzle.DailyDate != null,
                s.Letters,
                s.ElapsedSeconds,
                s.UpdatedAt,
            })
            .ToListAsync(ct);

        var puzzleIds = sessions.Select(s => s.PuzzleId).ToList();
        var openCells = (await db.Puzzles.AsNoTracking()
                .Where(p => puzzleIds.Contains(p.Id))
                .Include(p => p.Entries)
                .ToListAsync(ct))
            .ToDictionary(p => p.Id, p => p.BuildLayout().Count(c => c != Domain.Entities.Puzzle.BlockedCell));

        var inProgress = sessions
            .Select(s =>
            {
                var filled = s.Letters.Count(char.IsLetter);
                var total = Math.Max(1, openCells.GetValueOrDefault(s.PuzzleId, 1));
                return new InProgressGameDto(s.Id, s.PuzzleId, s.Mode, s.Difficulty, s.IsDaily,
                    Math.Min(100, filled * 100 / total), s.ElapsedSeconds, s.UpdatedAt);
            })
            .ToList();

        var daily = new List<DailyStatusDto>();
        foreach (var mode in Enum.GetValues<GameMode>())
        {
            var existing = await db.Puzzles
                .Where(p => p.Mode == mode && p.DailyDate == today)
                .Select(p => new { p.Id, p.Difficulty })
                .FirstOrDefaultAsync(ct);
            var completed = existing is not null && await db.GameSessions
                .AnyAsync(s => s.PuzzleId == existing.Id && s.Status == SessionStatus.Completed, ct);
            var decision = await db.DecideAsync(mode, today, ct);
            daily.Add(new DailyStatusDto(mode, existing?.Id, completed, existing?.Difficulty ?? decision.Difficulty, decision));
        }

        var left = await db.Puzzles.CountAsync(p => p.DailyDate == null && !db.GameSessions.Any(s => s.PuzzleId == p.Id), ct);

        return new DashboardDto(
            new ProfileDto(
                LevelDto.FromXp(profile.TotalXp),
                profile.PuzzlesCompleted,
                profile.ActiveStreak(today),
                profile.LongestStreak,
                profile.LastPlayedOn),
            inProgress,
            daily,
            left);
    }
}

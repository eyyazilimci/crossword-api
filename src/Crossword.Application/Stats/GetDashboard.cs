using Crossword.Application.Abstractions;
using Crossword.Application.Gamification;
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

public sealed record DailyStatusDto(GameMode Mode, Guid? PuzzleId, bool Completed);

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
            var puzzleId = await db.Puzzles
                .Where(p => p.Mode == mode && p.DailyDate == today)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(ct);
            var completed = puzzleId is not null && await db.GameSessions
                .AnyAsync(s => s.PuzzleId == puzzleId && s.Status == SessionStatus.Completed, ct);
            daily.Add(new DailyStatusDto(mode, puzzleId, completed));
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

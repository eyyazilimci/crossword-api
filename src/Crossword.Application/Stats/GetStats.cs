using Crossword.Application.Abstractions;
using Crossword.Application.Gamification;
using Crossword.Domain;
using Crossword.Domain.Gamification;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Stats;

public sealed record ProfileDto(LevelDto Level, int PuzzlesCompleted, int CurrentStreak, int LongestStreak, DateOnly? LastPlayedOn);

public sealed record ModeStatsDto(GameMode Mode, int PuzzlesCompleted, int WordsSeen, int WordsLearned, int TotalWords);

public sealed record RecentGameDto(
    Guid SessionId,
    Guid PuzzleId,
    GameMode Mode,
    Difficulty Difficulty,
    int Score,
    int ElapsedSeconds,
    DateTime CompletedAt);

public sealed record WeakWordDto(string English, string Turkish, GameMode Mode, int CorrectCount, int WrongCount);

public sealed record StatsDto(
    ProfileDto Profile,
    IReadOnlyList<ModeStatsDto> Modes,
    IReadOnlyList<RecentGameDto> RecentGames,
    IReadOnlyList<WeakWordDto> WeakWords,
    IReadOnlyList<AchievementDto> Achievements);

public sealed record GetStatsQuery : IQuery<StatsDto>;

public sealed class GetStatsHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetStatsQuery, StatsDto>
{
    public async ValueTask<StatsDto> Handle(GetStatsQuery query, CancellationToken ct)
    {
        var profile = await db.GetProfileAsync(ct);
        var totalWords = await db.Words.CountAsync(ct);

        var modes = new List<ModeStatsDto>();
        foreach (var mode in Enum.GetValues<GameMode>())
        {
            modes.Add(new ModeStatsDto(
                mode,
                await db.GameSessions.CountAsync(s => s.Status == SessionStatus.Completed && s.Puzzle.Mode == mode, ct),
                await db.WordMasteries.CountAsync(m => m.Mode == mode, ct),
                await db.WordMasteries.CountAsync(m => m.Mode == mode && m.CorrectCount > 0 && m.CorrectCount > m.WrongCount, ct),
                totalWords));
        }

        var recent = await db.GameSessions.AsNoTracking()
            .Where(s => s.Status == SessionStatus.Completed)
            .OrderByDescending(s => s.CompletedAt)
            .Take(10)
            .Select(s => new RecentGameDto(s.Id, s.PuzzleId, s.Puzzle.Mode, s.Puzzle.Difficulty, s.Score, s.ElapsedSeconds, s.CompletedAt!.Value))
            .ToListAsync(ct);

        var weak = await db.WordMasteries.AsNoTracking()
            .Where(m => m.WrongCount > 0)
            .OrderByDescending(m => m.WrongCount - m.CorrectCount)
            .ThenByDescending(m => m.LastSeenAt)
            .Take(15)
            .Select(m => new WeakWordDto(m.Word.English, m.Word.Turkish, m.Mode, m.CorrectCount, m.WrongCount))
            .ToListAsync(ct);

        var achievements = await AchievementList.LoadAsync(db, ct);

        return new StatsDto(
            new ProfileDto(
                LevelDto.FromXp(profile.TotalXp),
                profile.PuzzlesCompleted,
                profile.ActiveStreak(clock.Today),
                profile.LongestStreak,
                profile.LastPlayedOn),
            modes,
            recent,
            weak,
            achievements);
    }
}

public sealed record GetAchievementsQuery : IQuery<IReadOnlyList<AchievementDto>>;

public sealed class GetAchievementsHandler(IAppDbContext db) : IQueryHandler<GetAchievementsQuery, IReadOnlyList<AchievementDto>>
{
    public async ValueTask<IReadOnlyList<AchievementDto>> Handle(GetAchievementsQuery query, CancellationToken ct) =>
        await AchievementList.LoadAsync(db, ct);
}

internal static class AchievementList
{
    public static async Task<IReadOnlyList<AchievementDto>> LoadAsync(IAppDbContext db, CancellationToken ct)
    {
        var unlocked = await db.UnlockedAchievements.AsNoTracking().ToDictionaryAsync(a => a.Code, a => a.UnlockedAt, ct);
        return Achievements.All
            .Select(a => AchievementDto.From(a, unlocked.TryGetValue(a.Code, out var at) ? at : null))
            .ToList();
    }
}

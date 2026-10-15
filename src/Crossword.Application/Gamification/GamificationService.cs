using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Gamification;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Gamification;

public sealed record AchievementDto(string Code, string Title, string Description, string Icon, DateTime? UnlockedAt)
{
    public static AchievementDto From(AchievementDefinition d, DateTime? unlockedAt) =>
        new(d.Code, d.Title, d.Description, d.Icon, unlockedAt);
}

public sealed record LevelDto(int Level, string Title, int TotalXp, int LevelStartXp, int NextLevelXp)
{
    public static LevelDto FromXp(int xp)
    {
        var level = LevelProgression.LevelForXp(xp);
        return new LevelDto(
            level,
            LevelProgression.TitleFor(level),
            xp,
            LevelProgression.TotalXpForLevel(level),
            LevelProgression.TotalXpForLevel(level + 1));
    }
}

/// <summary>Shared XP / streak / achievement bookkeeping.</summary>
internal static class GamificationService
{
    public static async Task<PlayerProfile> GetProfileAsync(this IAppDbContext db, CancellationToken ct)
    {
        var profile = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.Id == PlayerProfile.SingletonId, ct);
        if (profile is not null)
            return profile;

        profile = PlayerProfile.CreateDefault();
        db.PlayerProfiles.Add(profile);
        return profile;
    }

    public static Task<int> CountLearnedWordsAsync(this IAppDbContext db, CancellationToken ct) =>
        db.WordMasteries.CountAsync(m => m.CorrectCount > 0 && m.CorrectCount > m.WrongCount, ct);

    /// <summary>Unlocks every achievement whose rule is now met and returns the new ones.</summary>
    public static async Task<List<AchievementDto>> UnlockAchievementsAsync(
        this IAppDbContext db,
        PlayerProfile profile,
        GameSession session,
        DateOnly today,
        DateTime now,
        CancellationToken ct)
    {
        var completedModes = await db.GameSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .Select(s => s.Puzzle.Mode)
            .Distinct()
            .ToListAsync(ct);

        var context = new AchievementContext(
            profile.PuzzlesCompleted,
            profile.ActiveStreak(today),
            profile.Level,
            await db.CountLearnedWordsAsync(ct),
            completedModes.Count >= 2,
            session.IsFlawless,
            session.ElapsedSeconds,
            session.Puzzle.Difficulty,
            session.Puzzle.DailyDate is not null);

        var unlocked = await db.UnlockedAchievements.Select(a => a.Code).ToListAsync(ct);
        var fresh = Achievements.Earned(context).Where(a => !unlocked.Contains(a.Code)).ToList();

        foreach (var achievement in fresh)
            db.UnlockedAchievements.Add(UnlockedAchievement.Create(achievement.Code, now));

        return fresh.Select(a => AchievementDto.From(a, now)).ToList();
    }
}

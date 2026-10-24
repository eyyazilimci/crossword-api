namespace Crossword.Domain.Quiz;

/// <summary>Answers given at one CEFR level.</summary>
public sealed record LevelResult(CefrLevel Level, int Seen, int Correct);

/// <summary>
/// The level new questions aim at: one above the highest level the player passes
/// (at least <see cref="MinAnswers"/> answers, <see cref="PassAt"/> correct), never below the starting level.
/// </summary>
public static class QuizLevelPolicy
{
    public const CefrLevel Starting = CefrLevel.B1;
    public const int MinAnswers = 8;
    public const double PassAt = 0.70;

    public static CefrLevel Target(IEnumerable<LevelResult> results, CefrLevel starting = Starting)
    {
        var passed = results
            .Where(r => r.Seen >= MinAnswers && (double)r.Correct / r.Seen >= PassAt)
            .Select(r => (int)r.Level)
            .DefaultIfEmpty(0)
            .Max();

        return (CefrLevel)Math.Clamp(passed + 1, (int)starting, (int)CefrLevel.C2);
    }
}

/// <summary>Daily quiz streak: consecutive days with a finished daily quiz.</summary>
public static class QuizStreak
{
    /// <summary>Counts back from today, or from yesterday while today's quiz is still open.</summary>
    public static int Current(IEnumerable<DateOnly> completedDays, DateOnly today)
    {
        var days = completedDays.ToHashSet();
        var day = days.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    public static int Longest(IEnumerable<DateOnly> completedDays)
    {
        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var day in completedDays.Distinct().Order())
        {
            run = previous == day.AddDays(-1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = day;
        }

        return longest;
    }
}

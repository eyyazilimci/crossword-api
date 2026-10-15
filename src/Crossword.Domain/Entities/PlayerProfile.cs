using Crossword.Domain.Gamification;

namespace Crossword.Domain.Entities;

/// <summary>The single player's progress. There is exactly one row with <see cref="SingletonId"/>.</summary>
public sealed class PlayerProfile
{
    public const int SingletonId = 1;

    private PlayerProfile()
    {
    }

    public int Id { get; private set; }
    public int TotalXp { get; private set; }
    public int PuzzlesCompleted { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateOnly? LastPlayedOn { get; private set; }

    public int Level => LevelProgression.LevelForXp(TotalXp);

    public static PlayerProfile CreateDefault() => new() { Id = SingletonId };

    public void RegisterCompletion(int xp, DateOnly today)
    {
        TotalXp += xp;
        PuzzlesCompleted++;

        if (LastPlayedOn == today)
        {
            // Streak already counted today.
        }
        else if (LastPlayedOn == today.AddDays(-1))
        {
            CurrentStreak++;
        }
        else
        {
            CurrentStreak = 1;
        }

        LastPlayedOn = today;
        LongestStreak = Math.Max(LongestStreak, CurrentStreak);
    }

    /// <summary>Streak shown to the player: drops to zero once a whole day was skipped.</summary>
    public int ActiveStreak(DateOnly today) =>
        LastPlayedOn is { } last && last >= today.AddDays(-1) ? CurrentStreak : 0;
}

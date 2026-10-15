namespace Crossword.Domain.Gamification;

/// <summary>Each level needs 50 XP more than the previous one: 100, 150, 200, ...</summary>
public static class LevelProgression
{
    private const int BaseXp = 100;
    private const int StepXp = 50;

    /// <summary>Total XP required to reach <paramref name="level"/> (level 1 = 0 XP).</summary>
    public static int TotalXpForLevel(int level)
    {
        var n = level - 1;
        return n * BaseXp + StepXp * n * (n - 1) / 2;
    }

    public static int LevelForXp(int xp)
    {
        var level = 1;
        while (TotalXpForLevel(level + 1) <= xp)
            level++;
        return level;
    }

    public static string TitleFor(int level) => level switch
    {
        < 3 => "Çaylak",
        < 6 => "Kelime Avcısı",
        < 10 => "Bulmaca Kurdu",
        < 15 => "Sözlük Ustası",
        < 25 => "İki Dilli Bilge",
        _ => "Efsane",
    };
}

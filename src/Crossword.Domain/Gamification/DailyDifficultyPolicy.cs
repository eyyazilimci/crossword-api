namespace Crossword.Domain.Gamification;

/// <summary>
/// Picks the daily puzzle's difficulty from how well the previous daily puzzle went:
/// a (nearly) clean solve moves one level up, a poor one moves one level down.
/// </summary>
public static class DailyDifficultyPolicy
{
    /// <summary>Share of words solved without a reveal or a wrong check needed to move up.</summary>
    public const double PromoteAt = 0.90;

    /// <summary>Below this share the next daily puzzle gets easier.</summary>
    public const double DemoteBelow = 0.70;

    public const Difficulty Starting = Difficulty.Easy;

    /// <param name="previous">Difficulty of the last daily puzzle, or null when there was none.</param>
    /// <param name="accuracy">Word accuracy of that puzzle (0–1), or null when it was not finished.</param>
    public static Difficulty Next(Difficulty? previous, double? accuracy)
    {
        if (previous is not { } current)
            return Starting;

        return accuracy switch
        {
            >= PromoteAt when current < Difficulty.Expert => current + 1,
            < DemoteBelow when current > Difficulty.Easy => current - 1,
            _ => current,
        };
    }
}

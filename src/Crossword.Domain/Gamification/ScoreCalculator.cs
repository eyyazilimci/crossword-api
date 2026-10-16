using Crossword.Domain.Entities;

namespace Crossword.Domain.Gamification;

public sealed record ScoreBreakdown(
    int WordPoints,
    int TimeBonus,
    int FlawlessBonus,
    double DifficultyMultiplier,
    int Total);

public static class ScoreCalculator
{
    /// <summary>Seconds per word that still earns a time bonus.</summary>
    private const int TargetSecondsPerWord = 40;

    /// <summary>XP is a tenth of the score, so a typical puzzle is worth about one level early on.</summary>
    public static int XpFor(int score) => Math.Max(10, score / 10);

    public static ScoreBreakdown Calculate(Puzzle puzzle, GameSession session)
    {
        // Words with a revealed letter earn nothing; everything else earns by length.
        var wordPoints = puzzle.Entries
            .Where(e => !session.HadRevealIn(e, puzzle.Width))
            .Sum(e => 10 + 2 * e.Answer.Length);

        var targetSeconds = puzzle.Entries.Count * TargetSecondsPerWord;
        var timeBonus = Math.Min(wordPoints / 2, Math.Max(0, targetSeconds - session.ElapsedSeconds) / 5);
        var flawlessBonus = session.IsFlawless ? wordPoints / 4 : 0;

        var multiplier = puzzle.Difficulty switch
        {
            Difficulty.Easy => 1.0,
            Difficulty.Medium => 1.25,
            Difficulty.Hard => 1.5,
            _ => 1.75,
        };

        var total = (int)Math.Round((wordPoints + timeBonus + flawlessBonus) * multiplier);
        return new ScoreBreakdown(wordPoints, timeBonus, flawlessBonus, multiplier, total);
    }
}

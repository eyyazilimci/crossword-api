namespace Crossword.Domain.Crosswords;

/// <summary>Which word levels make up each difficulty. The lower bands overlap so puzzles don't feel abruptly different.</summary>
public static class DifficultyBands
{
    /// <summary>Words per level when levels are derived from frequency rank.</summary>
    public const int WordsPerLevel = 350;

    public static IReadOnlyList<CefrLevel> LevelsFor(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => [CefrLevel.A1, CefrLevel.A2],
        Difficulty.Medium => [CefrLevel.A2, CefrLevel.B1],
        Difficulty.Hard => [CefrLevel.B1, CefrLevel.B2],
        Difficulty.Expert => [CefrLevel.C1, CefrLevel.C2],
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public static bool Contains(Difficulty difficulty, CefrLevel level) => LevelsFor(difficulty).Contains(level);

    /// <summary>
    /// Level from frequency rank: the most frequent words are the easiest. Every word is still a common
    /// everyday word; C1/C2 here means "the least frequent part of our list", not an official CEFR rating.
    /// </summary>
    public static CefrLevel LevelForRank(int rank) => ((rank - 1) / WordsPerLevel) switch
    {
        0 => CefrLevel.A1,
        1 => CefrLevel.A2,
        2 => CefrLevel.B1,
        3 => CefrLevel.B2,
        4 => CefrLevel.C1,
        _ => CefrLevel.C2,
    };
}

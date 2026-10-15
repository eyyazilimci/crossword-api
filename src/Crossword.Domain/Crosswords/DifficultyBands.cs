namespace Crossword.Domain.Crosswords;

/// <summary>Which word levels make up each difficulty. Bands overlap so puzzles don't feel abruptly different.</summary>
public static class DifficultyBands
{
    public static IReadOnlyList<CefrLevel> LevelsFor(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => [CefrLevel.A1, CefrLevel.A2],
        Difficulty.Medium => [CefrLevel.A2, CefrLevel.B1],
        Difficulty.Hard => [CefrLevel.B1, CefrLevel.B2],
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public static bool Contains(Difficulty difficulty, CefrLevel level) => LevelsFor(difficulty).Contains(level);

    /// <summary>Level from frequency rank: the most frequent words are the easiest.</summary>
    public static CefrLevel LevelForRank(int rank) => rank switch
    {
        <= 500 => CefrLevel.A1,
        <= 1000 => CefrLevel.A2,
        <= 1500 => CefrLevel.B1,
        _ => CefrLevel.B2,
    };
}

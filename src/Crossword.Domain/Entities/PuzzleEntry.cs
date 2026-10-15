namespace Crossword.Domain.Entities;

/// <summary>One word placed in a puzzle, with the clue and answer as they were when the puzzle was built.</summary>
public sealed class PuzzleEntry
{
    private PuzzleEntry()
    {
    }

    internal PuzzleEntry(int wordId, int number, Orientation orientation, int row, int column, string answer, string clue)
    {
        WordId = wordId;
        Number = number;
        Orientation = orientation;
        Row = row;
        Column = column;
        Answer = answer;
        Clue = clue;
    }

    public int Id { get; private set; }
    public Guid PuzzleId { get; private set; }
    public Puzzle Puzzle { get; private set; } = null!;
    public int WordId { get; private set; }
    public Word Word { get; private set; } = null!;
    public int Number { get; private set; }
    public Orientation Orientation { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public string Answer { get; private set; } = null!;
    public string Clue { get; private set; } = null!;

    public IEnumerable<int> CellIndexes(int width)
    {
        for (var i = 0; i < Answer.Length; i++)
        {
            yield return Orientation == Orientation.Across
                ? Row * width + Column + i
                : (Row + i) * width + Column;
        }
    }
}

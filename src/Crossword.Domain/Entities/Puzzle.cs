using Crossword.Domain.Crosswords;

namespace Crossword.Domain.Entities;

public sealed class Puzzle
{
    public const char EmptyCell = '.';
    public const char BlockedCell = '#';

    private readonly List<PuzzleEntry> _entries = new();

    private Puzzle()
    {
    }

    public Guid Id { get; private set; }
    public GameMode Mode { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Seed { get; private set; }

    /// <summary>Set for the "puzzle of the day"; null for library puzzles.</summary>
    public DateOnly? DailyDate { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<PuzzleEntry> Entries => _entries;

    public int CellCount => Width * Height;

    public static Puzzle Create(
        GeneratedPuzzle generated,
        GameMode mode,
        Difficulty difficulty,
        int seed,
        DateTime now,
        DateOnly? dailyDate = null)
    {
        var puzzle = new Puzzle
        {
            Id = Guid.CreateVersion7(),
            Mode = mode,
            Difficulty = difficulty,
            Width = generated.Width,
            Height = generated.Height,
            Seed = seed,
            DailyDate = dailyDate,
            CreatedAt = now,
        };

        foreach (var e in generated.Entries)
        {
            puzzle._entries.Add(new PuzzleEntry(
                e.Word.WordId, e.Number, e.Orientation, e.Row, e.Column, e.Word.Answer, e.Word.Clue));
        }

        return puzzle;
    }

    /// <summary>Solution as a row-major string; blocked cells are <see cref="BlockedCell"/>.</summary>
    public string BuildSolution()
    {
        var cells = Enumerable.Repeat(BlockedCell, CellCount).ToArray();
        foreach (var entry in _entries)
        {
            var i = 0;
            foreach (var index in entry.CellIndexes(Width))
                cells[index] = entry.Answer[i++];
        }

        return new string(cells);
    }

    /// <summary>Grid shape without letters: <see cref="BlockedCell"/> or <see cref="EmptyCell"/>.</summary>
    public string BuildLayout() =>
        new(BuildSolution().Select(c => c == BlockedCell ? BlockedCell : EmptyCell).ToArray());
}

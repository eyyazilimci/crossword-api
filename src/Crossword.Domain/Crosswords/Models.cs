namespace Crossword.Domain.Crosswords;

/// <summary>A word that may be placed into a grid.</summary>
/// <param name="WordId">Id of the source <see cref="Entities.Word"/>.</param>
/// <param name="Answer">Upper-case text written into the grid.</param>
/// <param name="Clue">The word in the other language, shown as the clue.</param>
/// <param name="Priority">Higher priority words are tried first and make a grid score better.</param>
public sealed record WordCandidate(
    int WordId,
    string Answer,
    string Clue,
    PartOfSpeech PartOfSpeech,
    CefrLevel Level,
    double Priority = 0);

/// <summary>A word placed in a finished grid, with its clue number.</summary>
public sealed record GridEntry(int Number, WordCandidate Word, int Row, int Column, Orientation Orientation)
{
    public int Length => Word.Answer.Length;

    public IEnumerable<(int Row, int Column)> Cells()
    {
        for (var i = 0; i < Length; i++)
            yield return Orientation == Orientation.Across ? (Row, Column + i) : (Row + i, Column);
    }
}

public sealed record GeneratedPuzzle(int Width, int Height, IReadOnlyList<GridEntry> Entries)
{
    /// <summary>Solution letters in row-major order; '\0' marks a blocked cell.</summary>
    public char[] BuildSolution()
    {
        var cells = new char[Width * Height];
        foreach (var entry in Entries)
        {
            var i = 0;
            foreach (var (r, c) in entry.Cells())
                cells[r * Width + c] = entry.Word.Answer[i++];
        }

        return cells;
    }
}

public sealed class GeneratorOptions
{
    /// <summary>Working area width. The finished grid is cropped to the used area, so it can come out smaller.</summary>
    public int Width { get; init; } = 21;

    public int Height { get; init; } = 21;

    /// <summary>Independent layouts to try; the best scoring one wins.</summary>
    public int Attempts { get; init; } = 10;

    /// <summary>Stop adding words once this many are placed. Defaults to roughly one word per seven cells.</summary>
    public int? MaxWords { get; init; }

    /// <summary>When set, this word is placed first so it is guaranteed to appear.</summary>
    public WordCandidate? AnchorWord { get; init; }

    public int EffectiveMaxWords => MaxWords ?? Width * Height / 7;
}

namespace Crossword.Domain.Crosswords;

/// <summary>
/// Mutable grid used while building a crossword. Placement rules guarantee that every
/// horizontal or vertical run of two or more letters is exactly one placed word.
/// </summary>
internal sealed class WorkingGrid
{
    private const byte AcrossFlag = 1;
    private const byte DownFlag = 2;

    private readonly char[] _letters;
    private readonly byte[] _directions;
    private readonly Dictionary<char, List<int>> _letterPositions = new();
    private readonly List<(WordCandidate Word, int Row, int Column, Orientation Orientation)> _placements = new();

    public WorkingGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _letters = new char[width * height];
        _directions = new byte[width * height];
    }

    public int Width { get; }
    public int Height { get; }
    public int IntersectionCount { get; private set; }
    public IReadOnlyList<(WordCandidate Word, int Row, int Column, Orientation Orientation)> Placements => _placements;

    public readonly record struct Spot(int Row, int Column, Orientation Orientation, int Intersections);

    /// <summary>Finds every legal spot for <paramref name="answer"/> that crosses an existing word.</summary>
    public IEnumerable<Spot> FindCrossingSpots(string answer)
    {
        for (var i = 0; i < answer.Length; i++)
        {
            if (!_letterPositions.TryGetValue(answer[i], out var positions))
                continue;

            foreach (var pos in positions)
            {
                var flags = _directions[pos];
                Orientation orientation;
                if (flags == AcrossFlag) orientation = Orientation.Down;
                else if (flags == DownFlag) orientation = Orientation.Across;
                else continue; // already crossed both ways

                var row = pos / Width;
                var col = pos % Width;
                var startRow = orientation == Orientation.Down ? row - i : row;
                var startCol = orientation == Orientation.Across ? col - i : col;

                if (TryCheck(answer, startRow, startCol, orientation, out var crossings) && crossings > 0)
                    yield return new Spot(startRow, startCol, orientation, crossings);
            }
        }
    }

    public bool TryCheck(string answer, int row, int col, Orientation orientation, out int intersections)
    {
        intersections = 0;
        var (dr, dc) = orientation == Orientation.Across ? (0, 1) : (1, 0);
        var flag = orientation == Orientation.Across ? AcrossFlag : DownFlag;
        var endRow = row + dr * (answer.Length - 1);
        var endCol = col + dc * (answer.Length - 1);

        if (row < 0 || col < 0 || endRow >= Height || endCol >= Width)
            return false;

        // The cells just before and after the word must stay empty so it doesn't merge with another word.
        if (IsLetter(row - dr, col - dc) || IsLetter(endRow + dr, endCol + dc))
            return false;

        for (var i = 0; i < answer.Length; i++)
        {
            var r = row + dr * i;
            var c = col + dc * i;
            var idx = r * Width + c;
            var existing = _letters[idx];

            if (existing != '\0')
            {
                if (existing != answer[i] || (_directions[idx] & flag) != 0)
                    return false;
                intersections++;
            }
            else if (IsLetter(r + dc, c + dr) || IsLetter(r - dc, c - dr))
            {
                // A fresh letter must not touch a parallel neighbour, otherwise it would form an unintended word.
                return false;
            }
        }

        return intersections < answer.Length;
    }

    public void Place(WordCandidate word, int row, int col, Orientation orientation)
    {
        var (dr, dc) = orientation == Orientation.Across ? (0, 1) : (1, 0);
        var flag = orientation == Orientation.Across ? AcrossFlag : DownFlag;

        for (var i = 0; i < word.Answer.Length; i++)
        {
            var idx = (row + dr * i) * Width + col + dc * i;
            if (_letters[idx] == '\0')
            {
                _letters[idx] = word.Answer[i];
                if (!_letterPositions.TryGetValue(word.Answer[i], out var list))
                    _letterPositions[word.Answer[i]] = list = new List<int>();
                list.Add(idx);
            }
            else
            {
                IntersectionCount++;
            }

            _directions[idx] |= flag;
        }

        _placements.Add((word, row, col, orientation));
    }

    public int FilledCellCount => _letters.Count(ch => ch != '\0');

    private bool IsLetter(int row, int col) =>
        row >= 0 && col >= 0 && row < Height && col < Width && _letters[row * Width + col] != '\0';

    /// <summary>Crops to the used area and assigns clue numbers in reading order.</summary>
    public GeneratedPuzzle ToPuzzle()
    {
        var minRow = _placements.Min(p => p.Row);
        var minCol = _placements.Min(p => p.Column);
        var maxRow = _placements.Max(p => p.Orientation == Orientation.Down ? p.Row + p.Word.Answer.Length - 1 : p.Row);
        var maxCol = _placements.Max(p => p.Orientation == Orientation.Across ? p.Column + p.Word.Answer.Length - 1 : p.Column);

        var numbers = new Dictionary<(int, int), int>();
        var next = 1;
        foreach (var start in _placements
                     .Select(p => (Row: p.Row - minRow, Col: p.Column - minCol))
                     .Distinct()
                     .OrderBy(s => s.Row)
                     .ThenBy(s => s.Col))
        {
            numbers[start] = next++;
        }

        var entries = _placements
            .Select(p =>
            {
                var r = p.Row - minRow;
                var c = p.Column - minCol;
                return new GridEntry(numbers[(r, c)], p.Word, r, c, p.Orientation);
            })
            .OrderBy(e => e.Orientation)
            .ThenBy(e => e.Number)
            .ToList();

        return new GeneratedPuzzle(maxCol - minCol + 1, maxRow - minRow + 1, entries);
    }
}

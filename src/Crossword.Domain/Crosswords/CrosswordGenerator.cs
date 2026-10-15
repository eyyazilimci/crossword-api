namespace Crossword.Domain.Crosswords;

/// <summary>
/// Builds free-form ("çengel") crosswords: words cross each other on shared letters and
/// unused cells stay blocked. Several randomised layouts are tried and the best one is kept.
/// </summary>
public static class CrosswordGenerator
{
    private const int MinWords = 4;

    public static GeneratedPuzzle Generate(IReadOnlyList<WordCandidate> candidates, GeneratorOptions options, Random random)
    {
        var fitting = candidates
            .Where(c => c.Answer.Length >= 2 && c.Answer.Length <= Math.Max(options.Width, options.Height))
            .ToList();

        if (fitting.Count < MinWords)
            throw new DomainException("Bulmaca üretmek için yeterli kelime yok.");

        WorkingGrid? best = null;
        var bestScore = double.MinValue;

        for (var attempt = 0; attempt < options.Attempts; attempt++)
        {
            var grid = BuildOnce(fitting, options, random);
            if (grid.Placements.Count < MinWords)
                continue;

            var score = Score(grid);
            if (score > bestScore)
            {
                bestScore = score;
                best = grid;
            }
        }

        return best?.ToPuzzle() ?? throw new DomainException("Bulmaca üretilemedi, lütfen tekrar deneyin.");
    }

    private static double Score(WorkingGrid grid)
    {
        var priority = grid.Placements.Sum(p => p.Word.Priority);
        var density = (double)grid.FilledCellCount / (grid.Width * grid.Height);
        return priority + grid.Placements.Count + grid.IntersectionCount * 0.5 + density * 20;
    }

    private static WorkingGrid BuildOnce(List<WordCandidate> candidates, GeneratorOptions options, Random random)
    {
        var grid = new WorkingGrid(options.Width, options.Height);
        var maxWords = options.EffectiveMaxWords;

        // Highest priority first; jitter keeps every attempt different.
        var ordered = candidates
            .Select(c => (Word: c, Key: c.Priority + random.NextDouble() * 2))
            .OrderByDescending(x => x.Key)
            .Select(x => x.Word)
            .ToList();

        var usedAnswers = new HashSet<string>();
        var usedClues = new HashSet<string>();
        var usedIds = new HashSet<int>();

        bool CanUse(WordCandidate w) =>
            !usedIds.Contains(w.WordId) && !usedAnswers.Contains(w.Answer) && !usedClues.Contains(w.Clue);

        void MarkUsed(WordCandidate w)
        {
            usedIds.Add(w.WordId);
            usedAnswers.Add(w.Answer);
            usedClues.Add(w.Clue);
        }

        var first = options.AnchorWord ?? PickFirstWord(ordered, options, random);
        PlaceFirst(grid, first, random);
        MarkUsed(first);

        var centerRow = options.Height / 2.0;
        var centerCol = options.Width / 2.0;

        for (var pass = 0; pass < 6 && grid.Placements.Count < maxWords; pass++)
        {
            var placedThisPass = 0;
            foreach (var word in ordered)
            {
                if (grid.Placements.Count >= maxWords)
                    break;
                if (!CanUse(word))
                    continue;

                WorkingGrid.Spot? bestSpot = null;
                var bestSpotScore = double.MinValue;
                foreach (var spot in grid.FindCrossingSpots(word.Answer))
                {
                    // Prefer many crossings and staying near the middle so the grid grows evenly.
                    var midRow = spot.Orientation == Orientation.Down ? spot.Row + word.Answer.Length / 2.0 : spot.Row;
                    var midCol = spot.Orientation == Orientation.Across ? spot.Column + word.Answer.Length / 2.0 : spot.Column;
                    var distance = Math.Abs(midRow - centerRow) / options.Height + Math.Abs(midCol - centerCol) / options.Width;
                    var spotScore = spot.Intersections * 3 - distance * 2 + random.NextDouble() * 0.5;
                    if (spotScore > bestSpotScore)
                    {
                        bestSpotScore = spotScore;
                        bestSpot = spot;
                    }
                }

                if (bestSpot is { } s)
                {
                    grid.Place(word, s.Row, s.Column, s.Orientation);
                    MarkUsed(word);
                    placedThisPass++;
                }
            }

            if (placedThisPass == 0)
                break;
        }

        return grid;
    }

    private static WordCandidate PickFirstWord(List<WordCandidate> ordered, GeneratorOptions options, Random random)
    {
        var limit = Math.Min(options.Width, options.Height) - 2;
        var pool = ordered.Take(40).Where(w => w.Answer.Length >= 6 && w.Answer.Length <= limit).ToList();
        if (pool.Count == 0)
            pool = ordered.Where(w => w.Answer.Length <= limit).Take(40).ToList();
        if (pool.Count == 0)
            pool = ordered.Take(1).ToList();
        return pool[random.Next(pool.Count)];
    }

    private static void PlaceFirst(WorkingGrid grid, WordCandidate word, Random random)
    {
        var length = word.Answer.Length;
        var across = length <= grid.Width && (length > grid.Height || random.Next(2) == 0);

        if (across)
        {
            var row = grid.Height / 2;
            var col = Math.Max(0, (grid.Width - length) / 2 + random.Next(-1, 2));
            col = Math.Min(col, grid.Width - length);
            grid.Place(word, row, col, Orientation.Across);
        }
        else
        {
            var col = grid.Width / 2;
            var row = Math.Max(0, (grid.Height - length) / 2 + random.Next(-1, 2));
            row = Math.Min(row, grid.Height - length);
            grid.Place(word, row, col, Orientation.Down);
        }
    }
}

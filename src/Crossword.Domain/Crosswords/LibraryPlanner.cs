namespace Crossword.Domain.Crosswords;

public sealed record PlannedPuzzle(GeneratedPuzzle Puzzle, Difficulty Difficulty, int Seed);

public sealed class LibraryOptions
{
    public int MinSize { get; init; } = 19;
    public int MaxSize { get; init; } = 23;
    public int Attempts { get; init; } = 8;

    /// <summary>Keep generating until every difficulty has at least this many puzzles.</summary>
    public int MinPerDifficulty { get; init; }

    /// <summary>Puzzles to add on top of coverage and the per-difficulty minimum.</summary>
    public int ExtraPuzzles { get; init; }

    /// <summary>Safety limit so a broken word list can never loop forever.</summary>
    public int MaxPuzzles { get; init; } = 600;
}

/// <summary>
/// Produces a series of puzzles that together use every word at least once.
/// Each puzzle is anchored on a not-yet-used word and fills up with as many other unused words as fit.
/// Once everything is covered it tops up the difficulties that have the fewest puzzles.
/// </summary>
public static class LibraryPlanner
{
    private const double UncoveredPriority = 10;

    public static IEnumerable<PlannedPuzzle> Plan(
        IReadOnlyList<WordCandidate> words,
        IReadOnlySet<int> alreadyCovered,
        IReadOnlyDictionary<Difficulty, int> existingPerDifficulty,
        LibraryOptions options,
        Random random)
    {
        var uncovered = words
            .Where(w => !alreadyCovered.Contains(w.WordId) && w.Answer.Length <= options.MinSize)
            .Select(w => w.WordId)
            .ToHashSet();

        var difficulties = Enum.GetValues<Difficulty>();
        var counts = difficulties.ToDictionary(d => d, d => existingPerDifficulty.GetValueOrDefault(d));
        var extraLeft = options.ExtraPuzzles;

        for (var produced = 0; produced < options.MaxPuzzles; produced++)
        {
            Difficulty difficulty;
            if (uncovered.Count > 0)
            {
                // Work on the band that still has the most unused words.
                difficulty = difficulties
                    .OrderByDescending(d => words.Count(w => uncovered.Contains(w.WordId) && DifficultyBands.Contains(d, w.Level)))
                    .First();
            }
            else
            {
                difficulty = difficulties.OrderBy(d => counts[d]).First();
                if (counts[difficulty] >= options.MinPerDifficulty)
                {
                    if (extraLeft == 0)
                        yield break;
                    extraLeft--;
                }
            }

            var pool = words
                .Where(w => DifficultyBands.Contains(difficulty, w.Level))
                .Select(w => w with { Priority = uncovered.Contains(w.WordId) ? UncoveredPriority : random.NextDouble() })
                .ToList();

            var anchorPool = pool.Where(w => uncovered.Contains(w.WordId)).ToList();
            var anchor = anchorPool.Count > 0
                ? anchorPool.OrderByDescending(w => w.Answer.Length).ThenBy(_ => random.Next()).First()
                : null;

            var seed = random.Next();
            var size = random.Next(options.MinSize, options.MaxSize + 1);
            var generatorOptions = new GeneratorOptions
            {
                Width = size,
                Height = Math.Clamp(size + random.Next(-1, 2), options.MinSize, options.MaxSize),
                Attempts = options.Attempts,
                AnchorWord = anchor,
            };

            var puzzle = CrosswordGenerator.Generate(pool, generatorOptions, new Random(seed));
            foreach (var entry in puzzle.Entries)
                uncovered.Remove(entry.Word.WordId);

            counts[difficulty]++;
            yield return new PlannedPuzzle(puzzle, difficulty, seed);
        }
    }
}

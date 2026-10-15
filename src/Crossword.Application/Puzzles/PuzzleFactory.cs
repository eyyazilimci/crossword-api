using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Crosswords;
using Crossword.Domain.Entities;
using Crossword.Domain.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crossword.Application.Puzzles;

/// <summary>Turns stored words into puzzles: single on-demand puzzles and whole coverage libraries.</summary>
public sealed class PuzzleFactory(IAppDbContext db, IClock clock, ILogger<PuzzleFactory> logger)
{
    private const int LibrarySaveBatch = 10;

    public async Task<List<WordCandidate>> LoadCandidatesAsync(GameMode mode, CancellationToken ct)
    {
        var words = await db.Words.AsNoTracking().ToListAsync(ct);
        return words
            .Select(w => new WordCandidate(
                w.Id,
                AnswerNormalizer.ToAnswer(w.AnswerSourceFor(mode), mode),
                w.ClueFor(mode),
                w.PartOfSpeech,
                w.Level))
            .Where(c => AnswerNormalizer.IsValidAnswer(c.Answer, mode))
            .ToList();
    }

    /// <summary>
    /// Builds and stores a fresh puzzle. Words the player keeps missing get a higher priority so they come back.
    /// </summary>
    public async Task<Puzzle> CreateAsync(
        GameMode mode,
        Difficulty difficulty,
        int seed,
        DateOnly? dailyDate,
        CancellationToken ct)
    {
        var random = new Random(seed);
        var mastery = await db.WordMasteries.AsNoTracking()
            .Where(m => m.Mode == mode)
            .ToDictionaryAsync(m => m.WordId, ct);

        var pool = (await LoadCandidatesAsync(mode, ct))
            .Where(c => DifficultyBands.Contains(difficulty, c.Level))
            .Select(c =>
            {
                var priority = mastery.TryGetValue(c.WordId, out var m) ? m.ReviewWeight * 2 : 1;
                return c with { Priority = priority + random.NextDouble() };
            })
            .ToList();

        var size = random.Next(19, 24);
        var options = new GeneratorOptions { Width = size, Height = Math.Clamp(size + random.Next(-1, 2), 19, 23) };
        var generated = CrosswordGenerator.Generate(pool, options, random);

        var puzzle = Puzzle.Create(generated, mode, difficulty, seed, clock.UtcNow, dailyDate);
        db.Puzzles.Add(puzzle);
        await db.SaveChangesAsync(ct);
        return puzzle;
    }

    /// <summary>
    /// Generates puzzles until every word of the mode has appeared at least once and each difficulty
    /// has <paramref name="minPerDifficulty"/> library puzzles, then adds <paramref name="extraPuzzles"/> more.
    /// </summary>
    public async Task<int> GenerateLibraryAsync(GameMode mode, int minPerDifficulty, int extraPuzzles, CancellationToken ct)
    {
        var candidates = await LoadCandidatesAsync(mode, ct);
        var covered = (await db.PuzzleEntries
                .Where(e => e.Puzzle.Mode == mode)
                .Select(e => e.WordId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var existing = await db.Puzzles
            .Where(p => p.Mode == mode && p.DailyDate == null)
            .GroupBy(p => p.Difficulty)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var options = new LibraryOptions { MinPerDifficulty = minPerDifficulty, ExtraPuzzles = extraPuzzles };
        var random = new Random();
        var created = 0;

        foreach (var planned in LibraryPlanner.Plan(candidates, covered, existing, options, random))
        {
            ct.ThrowIfCancellationRequested();
            db.Puzzles.Add(Puzzle.Create(planned.Puzzle, mode, planned.Difficulty, planned.Seed, clock.UtcNow));
            created++;

            if (created % LibrarySaveBatch == 0)
            {
                await db.SaveChangesAsync(ct);
                logger.LogInformation("{Mode} library: {Count} puzzles generated so far", mode, created);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("{Mode} library: generated {Count} puzzles", mode, created);
        return created;
    }

    public static int DailySeed(DateOnly date, GameMode mode) => date.DayNumber * 10 + (int)mode;
}

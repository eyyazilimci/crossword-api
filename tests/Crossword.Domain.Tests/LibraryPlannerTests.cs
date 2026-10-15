using System.Diagnostics;
using Crossword.Domain.Crosswords;
using Crossword.Domain.Text;
using Xunit.Abstractions;

namespace Crossword.Domain.Tests;

public class LibraryPlannerTests(ITestOutputHelper output)
{
    /// <summary>Reads the real seed files (linked into the test output) the same way the seeder does.</summary>
    private static List<WordCandidate> RealWords(GameMode mode)
    {
        var seen = new HashSet<string>();
        var result = new List<WordCandidate>();
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Seed"), "words-*.txt").Order())
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var p = line.Split(';');
                if (!seen.Add(p[0])) continue;

                var rank = seen.Count;
                var answer = AnswerNormalizer.ToAnswer(mode == GameMode.TrToEn ? p[0] : p[1], mode);
                var clue = mode == GameMode.TrToEn ? p[1] : p[0];
                if (AnswerNormalizer.IsValidAnswer(answer, mode))
                    result.Add(new WordCandidate(rank, answer, clue, PartOfSpeech.Noun, DifficultyBands.LevelForRank(rank)));
            }
        }

        return result;
    }

    [Theory]
    [InlineData(GameMode.TrToEn)]
    [InlineData(GameMode.EnToTr)]
    public void Library_uses_every_word_at_least_once(GameMode mode)
    {
        var words = RealWords(mode);
        Assert.True(words.Count > 1900, $"Only {words.Count} words loaded.");

        var stopwatch = Stopwatch.StartNew();
        var options = new LibraryOptions { MinPerDifficulty = 30 };
        var puzzles = LibraryPlanner.Plan(words, new HashSet<int>(), new Dictionary<Difficulty, int>(), options, new Random(2026)).ToList();

        Assert.All(Enum.GetValues<Difficulty>(), d => Assert.True(puzzles.Count(p => p.Difficulty == d) >= 30));

        var used = puzzles.SelectMany(p => p.Puzzle.Entries).Select(e => e.Word.WordId).ToHashSet();
        var missing = words.Where(w => !used.Contains(w.WordId)).Select(w => w.Answer).ToList();
        Assert.Empty(missing);

        foreach (var p in puzzles)
            GridAssertions.AssertValid(p.Puzzle);

        output.WriteLine($"{mode}: {words.Count} words -> {puzzles.Count} puzzles in {stopwatch.ElapsedMilliseconds} ms");
        output.WriteLine($"avg words/puzzle {puzzles.Average(p => p.Puzzle.Entries.Count):F1}, " +
                         $"avg size {puzzles.Average(p => p.Puzzle.Width):F1}x{puzzles.Average(p => p.Puzzle.Height):F1}, " +
                         $"min words {puzzles.Min(p => p.Puzzle.Entries.Count)}");
        foreach (var g in puzzles.GroupBy(p => p.Difficulty))
            output.WriteLine($"  {g.Key}: {g.Count()} puzzles");
    }
}

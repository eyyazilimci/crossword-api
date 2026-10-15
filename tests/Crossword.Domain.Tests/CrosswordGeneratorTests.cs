using Crossword.Domain.Crosswords;

namespace Crossword.Domain.Tests;

public class CrosswordGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(42)]
    [InlineData(1234)]
    public void Generated_grid_is_a_valid_crossword(int seed)
    {
        var puzzle = CrosswordGenerator.Generate(TestWords.Candidates(), new GeneratorOptions(), new Random(seed));

        GridAssertions.AssertValid(puzzle);
        Assert.True(puzzle.Entries.Count >= 20, $"Only {puzzle.Entries.Count} words placed.");
        Assert.InRange(puzzle.Width, 5, 21);
        Assert.InRange(puzzle.Height, 5, 21);
    }

    [Fact]
    public void Same_seed_produces_same_puzzle()
    {
        var a = CrosswordGenerator.Generate(TestWords.Candidates(), new GeneratorOptions(), new Random(7));
        var b = CrosswordGenerator.Generate(TestWords.Candidates(), new GeneratorOptions(), new Random(7));

        Assert.Equal(a.Entries.Select(e => (e.Word.WordId, e.Row, e.Column)), b.Entries.Select(e => (e.Word.WordId, e.Row, e.Column)));
    }

    [Fact]
    public void Anchor_word_is_always_used()
    {
        var words = TestWords.Candidates();
        var anchor = words.Single(w => w.Answer == "BREAKFAST");

        var puzzle = CrosswordGenerator.Generate(words, new GeneratorOptions { AnchorWord = anchor }, new Random(5));

        Assert.Contains(puzzle.Entries, e => e.Word.WordId == anchor.WordId);
    }

    [Fact]
    public void Clue_numbers_follow_reading_order()
    {
        var puzzle = CrosswordGenerator.Generate(TestWords.Candidates(), new GeneratorOptions(), new Random(11));

        var starts = puzzle.Entries
            .GroupBy(e => e.Number)
            .Select(g => (Number: g.Key, g.First().Row, g.First().Column))
            .OrderBy(s => s.Number)
            .ToList();

        Assert.Equal(Enumerable.Range(1, starts.Count), starts.Select(s => s.Number));
        Assert.Equal(starts.OrderBy(s => s.Row).ThenBy(s => s.Column).Select(s => s.Number), starts.Select(s => s.Number));
        Assert.All(puzzle.Entries.GroupBy(e => e.Number), g => Assert.Single(g.Select(e => (e.Row, e.Column)).Distinct()));
    }

    [Fact]
    public void A_word_and_a_duplicate_answer_are_never_used_twice()
    {
        var words = TestWords.Candidates();
        words.Add(words[0] with { WordId = 999, Clue = "other clue" });

        var puzzle = CrosswordGenerator.Generate(words, new GeneratorOptions(), new Random(3));

        Assert.Equal(puzzle.Entries.Count, puzzle.Entries.Select(e => e.Word.Answer).Distinct().Count());
    }
}

using Crossword.Domain.Quiz;
using Xunit.Abstractions;

namespace Crossword.Domain.Tests;

/// <summary>Checks the real question bank files (linked into the test output) the way the seeder reads them.</summary>
public class QuestionBankTests(ITestOutputHelper output)
{
    private const int MinQuestionsPerTopic = 15;

    /// <summary>No answer letter may hold more than this share of a topic's questions.</summary>
    private const double MaxLetterShare = 0.40;

    private static readonly Lazy<QuestionBankParseResult> Bank = new(() =>
    {
        var questions = new List<SeedQuestion>();
        var errors = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Seed"), "quiz-*.txt").Order())
        {
            var result = QuestionBankParser.Parse(File.ReadLines(file), Path.GetFileName(file));
            questions.AddRange(result.Questions);
            errors.AddRange(result.Errors);
        }

        return new QuestionBankParseResult(questions, errors);
    });

    [Fact]
    public void Every_line_parses() => Assert.Empty(Bank.Value.Errors);

    [Fact]
    public void Keys_are_unique()
    {
        var duplicates = Bank.Value.Questions.GroupBy(q => q.Key).Where(g => g.Count() > 1).Select(g => g.Key);
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Prompts_are_unique()
    {
        var duplicates = Bank.Value.Questions.GroupBy(q => q.Prompt).Where(g => g.Count() > 1).Select(g => g.Key);
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Every_topic_has_enough_questions()
    {
        var counts = Bank.Value.Questions.GroupBy(q => q.Topic).ToDictionary(g => g.Key, g => g.Count());
        foreach (var topic in GrammarTopics.All)
            output.WriteLine($"{topic.Code}: {counts.GetValueOrDefault(topic.Code)}");

        var thin = GrammarTopics.All.Where(t => counts.GetValueOrDefault(t.Code) < MinQuestionsPerTopic).Select(t => t.Code);
        Assert.Empty(thin);
    }

    [Fact]
    public void Correct_answers_are_spread_over_the_letters()
    {
        var skewed = Bank.Value.Questions
            .GroupBy(q => q.Topic)
            .Where(g => g.GroupBy(q => q.CorrectIndex).Max(l => l.Count()) > g.Count() * MaxLetterShare)
            .Select(g => g.Key);
        Assert.Empty(skewed);
    }

    [Fact]
    public void Every_option_fills_every_gap()
    {
        // "drinks / is drinking" fills two gaps; each option must have one part per ___ in the prompt.
        var mismatched = Bank.Value.Questions
            .Where(q =>
            {
                var gaps = q.Prompt.Split(QuestionBankParser.Blank).Length - 1;
                return q.Options.Any(o => o.Split(" / ").Length != gaps);
            })
            .Select(q => q.Key);
        Assert.Empty(mismatched);
    }
}

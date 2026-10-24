namespace Crossword.Domain.Quiz;

/// <summary>One multiple-choice question as written in the question bank files.</summary>
public sealed record SeedQuestion(
    string Key,
    string Topic,
    CefrLevel Level,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Explanation);

public sealed record QuestionBankParseResult(IReadOnlyList<SeedQuestion> Questions, IReadOnlyList<string> Errors);

/// <summary>
/// Reads question bank lines: <c>key | topic | level | prompt | A | B | C | D | answer | explanation</c>.
/// The prompt marks the gap with <see cref="Blank"/>, the answer is a letter A–D. Empty lines and lines
/// starting with <c>#</c> are skipped. Invalid lines are reported in <see cref="QuestionBankParseResult.Errors"/>.
/// </summary>
public static class QuestionBankParser
{
    public const string Blank = "___";
    public const int OptionCount = 4;

    private const int FieldCount = 10;
    private static readonly string[] Letters = ["A", "B", "C", "D"];

    public static QuestionBankParseResult Parse(IEnumerable<string> lines, string source)
    {
        var questions = new List<SeedQuestion>();
        var errors = new List<string>();
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var error = TryParseLine(line, out var question);
            if (error is null)
                questions.Add(question!);
            else
                errors.Add($"{source}:{lineNumber}: {error}");
        }

        return new QuestionBankParseResult(questions, errors);
    }

    private static string? TryParseLine(string line, out SeedQuestion? question)
    {
        question = null;
        var parts = line.Split('|').Select(p => p.Trim()).ToArray();
        if (parts.Length != FieldCount)
            return $"{FieldCount} alan bekleniyordu, {parts.Length} bulundu";

        var (key, topic, level, prompt) = (parts[0], parts[1], parts[2], parts[3]);
        var options = parts[4..8];
        var (answer, explanation) = (parts[8], parts[9]);

        if (key.Length == 0)
            return "anahtar boş";
        if (!GrammarTopics.Exists(topic))
            return $"bilinmeyen konu '{topic}'";
        if (!Enum.TryParse<CefrLevel>(level, ignoreCase: true, out var cefr) || !Enum.IsDefined(cefr))
            return $"geçersiz seviye '{level}'";
        if (!prompt.Contains(Blank, StringComparison.Ordinal))
            return $"soru kökünde boşluk ({Blank}) yok";
        if (options.Any(o => o.Length == 0))
            return "boş şık var";
        if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != OptionCount)
            return "şıklar birbirinden farklı değil";

        var correct = Array.IndexOf(Letters, answer.ToUpperInvariant());
        if (correct < 0)
            return $"cevap A–D olmalı, '{answer}' bulundu";
        if (explanation.Length == 0)
            return "açıklama boş";

        question = new SeedQuestion(key, topic, cefr, prompt, options, correct, explanation);
        return null;
    }
}

using Crossword.Application.Abstractions;
using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crossword.Infrastructure.Persistence;

/// <summary>
/// Loads the embedded grammar question bank (Seed/quiz-*.txt) into the database, matched by question key.
/// Existing questions are updated, so fixing a question in the file and restarting is enough.
/// </summary>
public sealed class QuestionSeeder(AppDbContext db, IClock clock, ILogger<QuestionSeeder> logger)
{
    public static QuestionBankParseResult ReadEmbeddedBank()
    {
        var assembly = typeof(QuestionSeeder).Assembly;
        var questions = new List<SeedQuestion>();
        var errors = new List<string>();

        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.Contains(".Seed.quiz-", StringComparison.Ordinal))
                     .Order(StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);

            var result = QuestionBankParser.Parse(lines, name);
            questions.AddRange(result.Questions);
            errors.AddRange(result.Errors);
        }

        return new QuestionBankParseResult(questions, errors);
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        var bank = ReadEmbeddedBank();
        foreach (var error in bank.Errors)
            logger.LogWarning("Question bank line skipped: {Error}", error);

        var existing = await db.QuizQuestions.ToDictionaryAsync(q => q.Key, ct);
        var added = 0;
        var seen = new HashSet<string>();

        foreach (var q in bank.Questions)
        {
            if (!seen.Add(q.Key))
            {
                logger.LogWarning("Duplicate question key skipped: {Key}", q.Key);
                continue;
            }

            if (existing.TryGetValue(q.Key, out var question))
            {
                question.UpdateFrom(q);
            }
            else
            {
                db.QuizQuestions.Add(QuizQuestion.Create(q, clock.UtcNow));
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Question bank ready: {Total} questions ({Added} new)", seen.Count, added);
    }
}

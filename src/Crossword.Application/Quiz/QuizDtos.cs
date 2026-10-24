using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Quiz;

public sealed class QuizOptions
{
    public const string Section = "Quiz";

    public int DailySize { get; init; } = 10;

    public int PracticeSize { get; init; } = 10;

    /// <summary>Level questions aim at before the player has passed any level (see <see cref="QuizLevelPolicy"/>).</summary>
    public CefrLevel StartingLevel { get; init; } = QuizLevelPolicy.Starting;

    /// <summary>Starting profile: topics treated as weak until they have been played (see <see cref="QuizPlanOptions.FocusTopics"/>).</summary>
    public string[] FocusTopics { get; init; } = [];
}

/// <param name="ChosenIndex">The player's answer; null until answered.</param>
/// <param name="CorrectIndex">Shown only once the question is answered.</param>
/// <param name="Explanation">Shown only once the question is answered.</param>
public sealed record QuizQuestionDto(
    int Position,
    int QuestionId,
    string Topic,
    string TopicTitle,
    GrammarCategory Category,
    CefrLevel Level,
    string Prompt,
    IReadOnlyList<string> Options,
    int? ChosenIndex,
    int? CorrectIndex,
    string? Explanation);

public sealed record QuizSessionDto(
    Guid Id,
    QuizKind Kind,
    DateOnly? DailyDate,
    string? Topic,
    string? TopicTitle,
    SessionStatus Status,
    int CorrectCount,
    int AnsweredCount,
    IReadOnlyList<QuizQuestionDto> Questions,
    DateTime StartedAt,
    DateTime? CompletedAt)
{
    public static QuizSessionDto From(QuizSession session, IReadOnlyDictionary<int, QuizQuestion> questions) => new(
        session.Id,
        session.Kind,
        session.DailyDate,
        session.Topic,
        session.Topic is { } code ? GrammarTopics.Find(code)?.Title : null,
        session.Status,
        session.CorrectCount,
        session.AnsweredCount,
        session.QuestionIds.Select((id, i) => QuestionDto(i, questions[id], session.Choices[i])).ToList(),
        session.StartedAt,
        session.CompletedAt);

    private static QuizQuestionDto QuestionDto(int position, QuizQuestion q, int choice)
    {
        var topic = GrammarTopics.Find(q.Topic);
        var answered = choice != QuizSession.Unanswered;
        return new QuizQuestionDto(
            position,
            q.Id,
            q.Topic,
            topic?.Title ?? q.Topic,
            topic?.Category ?? GrammarCategory.Tenses,
            q.Level,
            q.Prompt,
            q.Options,
            answered ? choice : null,
            answered ? q.CorrectIndex : null,
            answered ? q.Explanation : null);
    }
}

internal static class QuizLoading
{
    public static async Task<QuizSessionDto> ToDtoAsync(this IAppDbContext db, QuizSession session, CancellationToken ct)
    {
        var questions = await db.QuizQuestions.AsNoTracking()
            .Where(q => session.QuestionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);
        return QuizSessionDto.From(session, questions);
    }

    public static async Task<QuizSession> LoadQuizSessionAsync(this IAppDbContext db, Guid id, CancellationToken ct) =>
        await db.QuizSessions.FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException("Test", id);

    /// <summary>Active questions with the player's history, optionally limited to one topic.</summary>
    public static async Task<List<QuizCandidate>> LoadQuizCandidatesAsync(this IAppDbContext db, string? topic, CancellationToken ct)
    {
        var query = db.QuizQuestions.AsNoTracking().Where(q => q.IsActive);
        if (topic is not null)
            query = query.Where(q => q.Topic == topic);

        var rows = await query
            .GroupJoin(db.QuestionProgress.AsNoTracking(), q => q.Id, p => p.QuestionId, (q, p) => new { q, p })
            .SelectMany(x => x.p.DefaultIfEmpty(), (x, p) => new
            {
                x.q.Id,
                x.q.Topic,
                x.q.Level,
                Seen = p == null ? 0 : p.SeenCount,
                Correct = p == null ? 0 : p.CorrectCount,
                LastCorrect = p != null && p.LastCorrect,
                LastOn = p == null ? (DateOnly?)null : p.LastAnsweredOn,
            })
            .ToListAsync(ct);

        return rows
            .OrderBy(r => r.Id)
            .Select(r => new QuizCandidate(r.Id, r.Topic, r.Level, r.Seen, r.Correct, r.LastCorrect, r.LastOn))
            .ToList();
    }

    public static async Task<CefrLevel> QuizTargetLevelAsync(this IAppDbContext db, CefrLevel startingLevel, CancellationToken ct)
    {
        var results = await db.QuestionProgress.AsNoTracking()
            .GroupBy(p => p.Question.Level)
            .Select(g => new { Level = g.Key, Seen = g.Sum(p => p.SeenCount), Correct = g.Sum(p => p.CorrectCount) })
            .ToListAsync(ct);
        return QuizLevelPolicy.Target(results.Select(r => new LevelResult(r.Level, r.Seen, r.Correct)), startingLevel);
    }
}

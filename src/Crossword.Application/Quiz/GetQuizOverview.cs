using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crossword.Application.Quiz;

/// <param name="SessionId">Null until today's quiz is opened.</param>
public sealed record DailyQuizStatusDto(Guid? SessionId, bool Completed, int AnsweredCount, int CorrectCount, int QuestionCount);

/// <param name="Strength">Smoothed accuracy 0–1 (see <see cref="DailyQuizPlanner.TopicStrength"/>); 0.5 when unplayed.</param>
public sealed record TopicStatsDto(
    string Code,
    string Title,
    GrammarCategory Category,
    string CategoryTitle,
    int QuestionCount,
    int SeenCount,
    int CorrectCount,
    double Strength);

public sealed record RecentQuizDto(
    Guid SessionId,
    QuizKind Kind,
    DateOnly? DailyDate,
    string? TopicTitle,
    int CorrectCount,
    int QuestionCount,
    DateTime CompletedAt);

public sealed record QuizOverviewDto(
    DailyQuizStatusDto Today,
    int CurrentStreak,
    int LongestStreak,
    int AnsweredCount,
    int CorrectCount,
    CefrLevel TargetLevel,
    IReadOnlyList<TopicStatsDto> Topics,
    IReadOnlyList<RecentQuizDto> Recent);

public sealed record GetQuizOverviewQuery : IQuery<QuizOverviewDto>;

public sealed class GetQuizOverviewHandler(IAppDbContext db, IClock clock, IOptions<QuizOptions> options)
    : IQueryHandler<GetQuizOverviewQuery, QuizOverviewDto>
{
    private const int RecentCount = 10;

    public async ValueTask<QuizOverviewDto> Handle(GetQuizOverviewQuery query, CancellationToken ct)
    {
        var today = clock.Today;

        var todays = await db.QuizSessions.AsNoTracking().FirstOrDefaultAsync(s => s.DailyDate == today, ct);
        var todayDto = todays is null
            ? new DailyQuizStatusDto(null, false, 0, 0, 0)
            : new DailyQuizStatusDto(todays.Id, todays.IsCompleted, todays.AnsweredCount, todays.CorrectCount, todays.QuestionIds.Length);

        var completedDays = await db.QuizSessions.AsNoTracking()
            .Where(s => s.DailyDate != null && s.Status == SessionStatus.Completed)
            .Select(s => s.DailyDate!.Value)
            .ToListAsync(ct);

        var questionCounts = await db.QuizQuestions.AsNoTracking()
            .Where(q => q.IsActive)
            .GroupBy(q => q.Topic)
            .Select(g => new { Topic = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Topic, x => x.Count, ct);

        var progress = await db.QuestionProgress.AsNoTracking()
            .GroupBy(p => p.Question.Topic)
            .Select(g => new { Topic = g.Key, Seen = g.Sum(p => p.SeenCount), Correct = g.Sum(p => p.CorrectCount) })
            .ToDictionaryAsync(x => x.Topic, ct);

        var topics = GrammarTopics.All
            .Select(t =>
            {
                var p = progress.GetValueOrDefault(t.Code);
                var seen = p?.Seen ?? 0;
                var correct = p?.Correct ?? 0;
                return new TopicStatsDto(
                    t.Code,
                    t.Title,
                    t.Category,
                    GrammarTopics.CategoryTitles[t.Category],
                    questionCounts.GetValueOrDefault(t.Code),
                    seen,
                    correct,
                    DailyQuizPlanner.TopicStrength(seen, correct));
            })
            .ToList();

        var recent = (await db.QuizSessions.AsNoTracking()
                .Where(s => s.Status == SessionStatus.Completed)
                .OrderByDescending(s => s.CompletedAt)
                .Take(RecentCount)
                .ToListAsync(ct))
            .Select(s => new RecentQuizDto(
                s.Id,
                s.Kind,
                s.DailyDate,
                s.Topic is { } code ? GrammarTopics.Find(code)?.Title : null,
                s.CorrectCount,
                s.QuestionIds.Length,
                s.CompletedAt!.Value))
            .ToList();

        return new QuizOverviewDto(
            todayDto,
            QuizStreak.Current(completedDays, today),
            QuizStreak.Longest(completedDays),
            topics.Sum(t => t.SeenCount),
            topics.Sum(t => t.CorrectCount),
            await db.QuizTargetLevelAsync(options.Value.StartingLevel, ct),
            topics,
            recent);
    }
}

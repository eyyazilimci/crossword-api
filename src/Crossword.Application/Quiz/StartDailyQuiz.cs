using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crossword.Application.Quiz;

/// <summary>Returns today's quiz, creating it on first request from weak topics and missed questions.</summary>
public sealed record StartDailyQuizCommand : ICommand<QuizSessionDto>;

public sealed class StartDailyQuizHandler(IAppDbContext db, IClock clock, IOptions<QuizOptions> options)
    : ICommandHandler<StartDailyQuizCommand, QuizSessionDto>
{
    public async ValueTask<QuizSessionDto> Handle(StartDailyQuizCommand command, CancellationToken ct)
    {
        var today = clock.Today;
        var existing = await db.QuizSessions.FirstOrDefaultAsync(s => s.DailyDate == today, ct);
        if (existing is not null)
            return await db.ToDtoAsync(existing, ct);

        var candidates = await db.LoadQuizCandidatesAsync(null, ct);
        if (candidates.Count == 0)
            throw new DomainException("Soru bankası boş. Sunucu açılışta soruları yüklüyor olabilir, birazdan tekrar dene.");

        var plan = DailyQuizPlanner.PlanDaily(
            candidates,
            await db.QuizTargetLevelAsync(options.Value.StartingLevel, ct),
            today,
            new Random(today.DayNumber),
            new QuizPlanOptions { Size = options.Value.DailySize, FocusTopics = options.Value.FocusTopics });

        var session = QuizSession.StartDaily(plan, today, clock.UtcNow);
        db.QuizSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return await db.ToDtoAsync(session, ct);
    }
}

using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;

namespace Crossword.Application.Quiz;

/// <summary>Starts a new practice quiz on one topic; questions missed before come first.</summary>
public sealed record StartPracticeQuizCommand(string Topic) : ICommand<QuizSessionDto>;

public sealed class StartPracticeQuizValidator : AbstractValidator<StartPracticeQuizCommand>
{
    public StartPracticeQuizValidator() =>
        RuleFor(x => x.Topic).NotEmpty().Must(GrammarTopics.Exists).WithMessage("Bilinmeyen konu.");
}

public sealed class StartPracticeQuizHandler(IAppDbContext db, IClock clock, IOptions<QuizOptions> options)
    : ICommandHandler<StartPracticeQuizCommand, QuizSessionDto>
{
    public async ValueTask<QuizSessionDto> Handle(StartPracticeQuizCommand command, CancellationToken ct)
    {
        var candidates = await db.LoadQuizCandidatesAsync(command.Topic, ct);
        if (candidates.Count == 0)
            throw new DomainException("Bu konuda henüz soru yok.");

        var plan = DailyQuizPlanner.PlanPractice(
            candidates,
            await db.QuizTargetLevelAsync(options.Value.StartingLevel, ct),
            clock.Today,
            new Random(),
            options.Value.PracticeSize);

        var session = QuizSession.StartPractice(plan, command.Topic, clock.UtcNow);
        db.QuizSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return await db.ToDtoAsync(session, ct);
    }
}

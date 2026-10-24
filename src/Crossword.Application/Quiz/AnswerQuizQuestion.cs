using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Domain.Entities;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Quiz;

/// <summary>Answers one question of a quiz and returns the correct option with its explanation.</summary>
public sealed record AnswerQuizQuestionCommand(Guid SessionId, int Position, int Choice) : ICommand<AnswerResultDto>;

public sealed record AnswerResultDto(
    bool Correct,
    int CorrectIndex,
    string Explanation,
    bool Completed,
    int CorrectCount,
    int AnsweredCount,
    int QuestionCount);

public sealed class AnswerQuizQuestionValidator : AbstractValidator<AnswerQuizQuestionCommand>
{
    public AnswerQuizQuestionValidator()
    {
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Choice).GreaterThanOrEqualTo(0);
    }
}

public sealed class AnswerQuizQuestionHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<AnswerQuizQuestionCommand, AnswerResultDto>
{
    public async ValueTask<AnswerResultDto> Handle(AnswerQuizQuestionCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var session = await db.LoadQuizSessionAsync(command.SessionId, ct);
        var questionId = session.QuestionIdAt(command.Position);
        var question = await db.QuizQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
                       ?? throw new NotFoundException("Soru", questionId);

        var correct = session.Answer(command.Position, command.Choice, question, now);

        var progress = await db.QuestionProgress.FirstOrDefaultAsync(p => p.QuestionId == question.Id, ct);
        if (progress is null)
        {
            progress = QuestionProgress.Create(question.Id);
            db.QuestionProgress.Add(progress);
        }

        progress.Record(correct, now, clock.Today);
        await db.SaveChangesAsync(ct);

        return new AnswerResultDto(
            correct,
            question.CorrectIndex,
            question.Explanation,
            session.IsCompleted,
            session.CorrectCount,
            session.AnsweredCount,
            session.QuestionIds.Length);
    }
}

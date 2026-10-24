using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Quiz;

/// <summary>Takes a question the player found wrong or ambiguous out of rotation until it is fixed in the bank.</summary>
public sealed record ReportQuizQuestionCommand(int QuestionId) : ICommand;

public sealed class ReportQuizQuestionHandler(IAppDbContext db, IClock clock) : ICommandHandler<ReportQuizQuestionCommand>
{
    public async ValueTask<Unit> Handle(ReportQuizQuestionCommand command, CancellationToken ct)
    {
        var question = await db.QuizQuestions.FirstOrDefaultAsync(q => q.Id == command.QuestionId, ct)
                       ?? throw new NotFoundException("Soru", command.QuestionId);
        question.Report(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

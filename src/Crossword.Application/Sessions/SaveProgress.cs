using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Sessions;

/// <summary>Autosave: stores the letters typed so far and the elapsed time.</summary>
public sealed record SaveProgressCommand(Guid SessionId, string Letters, int ElapsedSeconds) : ICommand;

public sealed class SaveProgressValidator : AbstractValidator<SaveProgressCommand>
{
    public SaveProgressValidator()
    {
        RuleFor(x => x.Letters).NotEmpty().MaximumLength(2500);
        RuleFor(x => x.ElapsedSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveProgressHandler(IAppDbContext db, IClock clock) : ICommandHandler<SaveProgressCommand>
{
    public async ValueTask<Unit> Handle(SaveProgressCommand command, CancellationToken ct)
    {
        var session = await db.GameSessions.FirstOrDefaultAsync(s => s.Id == command.SessionId, ct)
                      ?? throw new NotFoundException("Oyun", command.SessionId);

        session.SaveProgress(command.Letters, command.ElapsedSeconds, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

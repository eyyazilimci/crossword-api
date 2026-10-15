using Crossword.Application.Abstractions;
using FluentValidation;
using Mediator;

namespace Crossword.Application.Sessions;

/// <summary>Hint: fills the correct letters into the given cells.</summary>
public sealed record RevealCellsCommand(Guid SessionId, string Letters, int ElapsedSeconds, IReadOnlyList<int> Cells)
    : ICommand<RevealCellsResult>;

public sealed record RevealCellsResult(IReadOnlyDictionary<int, char> Revealed, string Letters, string RevealedMask);

public sealed class RevealCellsValidator : AbstractValidator<RevealCellsCommand>
{
    public RevealCellsValidator()
    {
        RuleFor(x => x.Letters).NotEmpty().MaximumLength(2500);
        RuleFor(x => x.Cells).NotEmpty().Must(c => c.Count <= 30).WithMessage("Tek seferde en fazla 30 harf açılabilir.");
        RuleFor(x => x.ElapsedSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class RevealCellsHandler(IAppDbContext db, IClock clock) : ICommandHandler<RevealCellsCommand, RevealCellsResult>
{
    public async ValueTask<RevealCellsResult> Handle(RevealCellsCommand command, CancellationToken ct)
    {
        var session = await db.LoadSessionAsync(command.SessionId, ct);
        session.SaveProgress(command.Letters, command.ElapsedSeconds, clock.UtcNow);

        var revealed = session.Reveal(command.Cells, session.Puzzle.BuildSolution());
        await db.SaveChangesAsync(ct);
        return new RevealCellsResult(revealed, session.Letters, session.RevealedMask);
    }
}

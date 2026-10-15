using Crossword.Application.Abstractions;
using FluentValidation;
using Mediator;

namespace Crossword.Application.Sessions;

/// <summary>Checks a cell, a word or the whole grid and reports which cells are wrong.</summary>
public sealed record CheckCellsCommand(Guid SessionId, string Letters, int ElapsedSeconds, IReadOnlyList<int> Cells)
    : ICommand<CheckCellsResult>;

public sealed record CheckCellsResult(IReadOnlyList<int> WrongCells, string MistakeMask);

public sealed class CheckCellsValidator : AbstractValidator<CheckCellsCommand>
{
    public CheckCellsValidator()
    {
        RuleFor(x => x.Letters).NotEmpty().MaximumLength(2500);
        RuleFor(x => x.Cells).NotEmpty();
        RuleFor(x => x.ElapsedSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class CheckCellsHandler(IAppDbContext db, IClock clock) : ICommandHandler<CheckCellsCommand, CheckCellsResult>
{
    public async ValueTask<CheckCellsResult> Handle(CheckCellsCommand command, CancellationToken ct)
    {
        var session = await db.LoadSessionAsync(command.SessionId, ct);
        session.SaveProgress(command.Letters, command.ElapsedSeconds, clock.UtcNow);

        var wrong = session.Check(command.Cells, session.Puzzle.BuildSolution());
        await db.SaveChangesAsync(ct);
        return new CheckCellsResult(wrong, session.MistakeMask);
    }
}

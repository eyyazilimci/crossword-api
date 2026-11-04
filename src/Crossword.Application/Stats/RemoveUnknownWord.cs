using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Domain;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Stats;

/// <summary>Takes a word the player no longer wants to review off their unknown list.</summary>
public sealed record RemoveUnknownWordCommand(int WordId, GameMode Mode) : ICommand;

public sealed class RemoveUnknownWordValidator : AbstractValidator<RemoveUnknownWordCommand>
{
    public RemoveUnknownWordValidator() => RuleFor(x => x.Mode).IsInEnum();
}

public sealed class RemoveUnknownWordHandler(IAppDbContext db) : ICommandHandler<RemoveUnknownWordCommand>
{
    public async ValueTask<Unit> Handle(RemoveUnknownWordCommand command, CancellationToken ct)
    {
        var mastery = await db.WordMasteries
                          .FirstOrDefaultAsync(m => m.WordId == command.WordId && m.Mode == command.Mode, ct)
                      ?? throw new NotFoundException("Kelime", command.WordId);

        mastery.RemoveFromUnknownList();
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

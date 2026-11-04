using Crossword.Application.Abstractions;
using Crossword.Domain;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Sessions;

/// <summary>Puts the words the player picked on the completion screen on their unknown list.</summary>
public sealed record MarkUnknownWordsCommand(Guid SessionId, IReadOnlyList<int> WordIds) : ICommand;

public sealed class MarkUnknownWordsValidator : AbstractValidator<MarkUnknownWordsCommand>
{
    public MarkUnknownWordsValidator() => RuleFor(x => x.WordIds).NotEmpty();
}

public sealed class MarkUnknownWordsHandler(IAppDbContext db) : ICommandHandler<MarkUnknownWordsCommand>
{
    public async ValueTask<Unit> Handle(MarkUnknownWordsCommand command, CancellationToken ct)
    {
        var session = await db.LoadSessionAsync(command.SessionId, ct);
        if (!session.IsCompleted)
            throw new DomainException("Bulmaca henüz tamamlanmadı.");

        var puzzle = session.Puzzle;
        var entries = puzzle.Entries
            .Where(e => command.WordIds.Contains(e.WordId))
            .ToDictionary(e => e.WordId);
        var wordIds = entries.Keys.ToList();

        var mastery = await db.WordMasteries
            .Where(m => m.Mode == puzzle.Mode && wordIds.Contains(m.WordId))
            .ToListAsync(ct);

        foreach (var m in mastery)
            m.MarkUnknown(countedCorrect: !session.WasMissed(entries[m.WordId], puzzle.Width));

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

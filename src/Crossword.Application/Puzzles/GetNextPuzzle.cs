using Crossword.Application.Abstractions;
using Crossword.Domain;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

/// <summary>
/// Picks the next never-played library puzzle for the mode and difficulty,
/// or builds a new one when the library has run out.
/// </summary>
public sealed record GetNextPuzzleCommand(GameMode Mode, Difficulty Difficulty) : ICommand<PuzzleSummaryDto>;

public sealed class GetNextPuzzleValidator : AbstractValidator<GetNextPuzzleCommand>
{
    public GetNextPuzzleValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.Difficulty).IsInEnum();
    }
}

public sealed class GetNextPuzzleHandler(IAppDbContext db, PuzzleFactory factory)
    : ICommandHandler<GetNextPuzzleCommand, PuzzleSummaryDto>
{
    public async ValueTask<PuzzleSummaryDto> Handle(GetNextPuzzleCommand command, CancellationToken ct)
    {
        var unplayed = await db.Puzzles.AsNoTracking()
            .Where(p => p.Mode == command.Mode && p.Difficulty == command.Difficulty && p.DailyDate == null)
            .Where(p => !db.GameSessions.Any(s => s.PuzzleId == p.Id))
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PuzzleSummaryDto(p.Id, p.Mode, p.Difficulty, p.Width, p.Height, p.Entries.Count))
            .FirstOrDefaultAsync(ct);

        if (unplayed is not null)
            return unplayed;

        var puzzle = await factory.CreateAsync(command.Mode, command.Difficulty, Random.Shared.Next(), null, ct);
        return new PuzzleSummaryDto(puzzle.Id, puzzle.Mode, puzzle.Difficulty, puzzle.Width, puzzle.Height, puzzle.Entries.Count);
    }
}

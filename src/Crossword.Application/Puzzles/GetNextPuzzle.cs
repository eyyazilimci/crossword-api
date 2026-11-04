using Crossword.Application.Abstractions;
using Crossword.Domain;
using Crossword.Domain.Crosswords;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

/// <summary>
/// Picks the never-played library puzzle with the most new words for the mode and difficulty,
/// or builds a new one when none of them has enough.
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

public sealed class GetNextPuzzleHandler(IAppDbContext db, IClock clock, PuzzleFactory factory)
    : ICommandHandler<GetNextPuzzleCommand, PuzzleSummaryDto>
{
    public async ValueTask<PuzzleSummaryDto> Handle(GetNextPuzzleCommand command, CancellationToken ct)
    {
        var unplayed = await db.Puzzles.AsNoTracking()
            .Where(p => p.Mode == command.Mode && p.Difficulty == command.Difficulty && p.DailyDate == null)
            .Where(p => !db.GameSessions.Any(s => s.PuzzleId == p.Id))
            .OrderBy(p => p.CreatedAt)
            .Select(p => new
            {
                Summary = new PuzzleSummaryDto(p.Id, p.Mode, p.Difficulty, p.Width, p.Height, p.Entries.Count),
                WordIds = p.Entries.Select(e => e.WordId).ToList(),
            })
            .ToListAsync(ct);

        var mastery = await db.WordMasteries.AsNoTracking()
            .Where(m => m.Mode == command.Mode)
            .ToDictionaryAsync(m => m.WordId, ct);
        var reviewWords = WordSelection.PickReviewWords(mastery.Values, clock.UtcNow);

        // Library puzzles were built without knowing what has been played, so take the one with the most to offer.
        var best = unplayed
            .Select(p => (p.Summary, Share: WordSelection.FreshShare(p.WordIds, mastery, reviewWords)))
            .Where(p => p.Share >= WordSelection.MinFreshShare)
            .OrderByDescending(p => p.Share)
            .FirstOrDefault();

        if (best.Summary is not null)
            return best.Summary;

        var puzzle = await factory.CreateAsync(command.Mode, command.Difficulty, Random.Shared.Next(), null, ct);
        return new PuzzleSummaryDto(puzzle.Id, puzzle.Mode, puzzle.Difficulty, puzzle.Width, puzzle.Height, puzzle.Entries.Count);
    }
}

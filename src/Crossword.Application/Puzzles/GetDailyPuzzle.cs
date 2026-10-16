using Crossword.Application.Abstractions;
using Crossword.Domain;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

/// <summary>
/// Returns today's puzzle for the mode, creating it on first request. Its difficulty adapts to how
/// the previous daily puzzle went (see <see cref="Domain.Gamification.DailyDifficultyPolicy"/>).
/// </summary>
public sealed record GetDailyPuzzleCommand(GameMode Mode) : ICommand<PuzzleSummaryDto>;

public sealed class GetDailyPuzzleValidator : AbstractValidator<GetDailyPuzzleCommand>
{
    public GetDailyPuzzleValidator() => RuleFor(x => x.Mode).IsInEnum();
}

public sealed class GetDailyPuzzleHandler(IAppDbContext db, IClock clock, PuzzleFactory factory)
    : ICommandHandler<GetDailyPuzzleCommand, PuzzleSummaryDto>
{
    public async ValueTask<PuzzleSummaryDto> Handle(GetDailyPuzzleCommand command, CancellationToken ct)
    {
        var today = clock.Today;
        var existing = await db.Puzzles.AsNoTracking()
            .Where(p => p.Mode == command.Mode && p.DailyDate == today)
            .Select(p => new PuzzleSummaryDto(p.Id, p.Mode, p.Difficulty, p.Width, p.Height, p.Entries.Count))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

        var decision = await db.DecideAsync(command.Mode, today, ct);
        var puzzle = await factory.CreateAsync(
            command.Mode, decision.Difficulty, PuzzleFactory.DailySeed(today, command.Mode), today, ct);
        return new PuzzleSummaryDto(puzzle.Id, puzzle.Mode, puzzle.Difficulty, puzzle.Width, puzzle.Height, puzzle.Entries.Count);
    }
}

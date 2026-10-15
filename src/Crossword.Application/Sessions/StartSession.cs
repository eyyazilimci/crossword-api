using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Application.Puzzles;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Sessions;

/// <summary>
/// Opens a puzzle for play. Resumes the unfinished session if there is one; otherwise shows the last
/// finished session, unless <see cref="Restart"/> asks for a new attempt.
/// </summary>
public sealed record StartSessionCommand(Guid PuzzleId, bool Restart = false) : ICommand<GameDto>;

public sealed class StartSessionHandler(IAppDbContext db, IClock clock) : ICommandHandler<StartSessionCommand, GameDto>
{
    public async ValueTask<GameDto> Handle(StartSessionCommand command, CancellationToken ct)
    {
        var puzzle = await db.Puzzles.WithEntries().FirstOrDefaultAsync(p => p.Id == command.PuzzleId, ct)
                     ?? throw new NotFoundException("Bulmaca", command.PuzzleId);

        var sessions = await db.GameSessions
            .Where(s => s.PuzzleId == puzzle.Id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

        var current = sessions.FirstOrDefault(s => s.Status == SessionStatus.InProgress);
        if (current is null && !command.Restart)
            current = sessions.FirstOrDefault();

        if (current is null)
        {
            current = GameSession.Start(puzzle, clock.UtcNow);
            db.GameSessions.Add(current);
            await db.SaveChangesAsync(ct);
        }

        return new GameDto(PuzzleDto.From(puzzle), SessionDto.From(current));
    }
}

using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Application.Puzzles;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Sessions;

public sealed record SessionDto(
    Guid Id,
    Guid PuzzleId,
    SessionStatus Status,
    string Letters,
    string RevealedMask,
    string MistakeMask,
    int ElapsedSeconds,
    int ChecksUsed,
    int RevealsUsed,
    int Score,
    int XpEarned,
    DateTime StartedAt,
    DateTime? CompletedAt)
{
    public static SessionDto From(GameSession s) => new(
        s.Id, s.PuzzleId, s.Status, s.Letters, s.RevealedMask, s.MistakeMask, s.ElapsedSeconds,
        s.ChecksUsed, s.RevealsUsed, s.Score, s.XpEarned, s.StartedAt, s.CompletedAt);
}

public sealed record GameDto(PuzzleDto Puzzle, SessionDto Session)
{
    public static GameDto From(GameSession session) => new(PuzzleDto.From(session.Puzzle), SessionDto.From(session));
}

internal static class SessionLoading
{
    public static async Task<GameSession> LoadSessionAsync(this IAppDbContext db, Guid id, CancellationToken ct) =>
        await db.GameSessions
            .Include(s => s.Puzzle).ThenInclude(p => p.Entries).ThenInclude(e => e.Word)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException("Oyun", id);
}

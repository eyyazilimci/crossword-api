using Crossword.Application.Abstractions;
using Crossword.Application.Common;
using Crossword.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

public sealed record GetPuzzleQuery(Guid Id) : IQuery<PuzzleDto>;

public sealed class GetPuzzleHandler(IAppDbContext db) : IQueryHandler<GetPuzzleQuery, PuzzleDto>
{
    public async ValueTask<PuzzleDto> Handle(GetPuzzleQuery query, CancellationToken ct)
    {
        var puzzle = await db.Puzzles.AsNoTracking().WithEntries().FirstOrDefaultAsync(p => p.Id == query.Id, ct)
                     ?? throw new NotFoundException("Bulmaca", query.Id);
        return PuzzleDto.From(puzzle);
    }
}

internal static class PuzzleQueryExtensions
{
    public static IQueryable<Puzzle> WithEntries(this IQueryable<Puzzle> puzzles) =>
        puzzles.Include(p => p.Entries).ThenInclude(e => e.Word);
}

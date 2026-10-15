using Crossword.Application.Abstractions;
using Crossword.Domain;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Puzzles;

public enum PlayStatus
{
    New = 1,
    InProgress = 2,
    Completed = 3,
}

public sealed record LibraryItemDto(
    Guid Id,
    GameMode Mode,
    Difficulty Difficulty,
    int Width,
    int Height,
    int WordCount,
    PlayStatus Status,
    int? BestScore,
    DateTime CreatedAt);

public sealed record CoverageDto(GameMode Mode, int TotalWords, int CoveredWords, int PuzzleCount);

public sealed record LibraryDto(IReadOnlyList<CoverageDto> Coverage, IReadOnlyList<LibraryItemDto> Items, int TotalCount);

public sealed record GetLibraryQuery(GameMode? Mode, Difficulty? Difficulty, PlayStatus? Status, int Page = 1, int PageSize = 24)
    : IQuery<LibraryDto>;

public sealed class GetLibraryValidator : AbstractValidator<GetLibraryQuery>
{
    public GetLibraryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetLibraryHandler(IAppDbContext db, PuzzleFactory factory) : IQueryHandler<GetLibraryQuery, LibraryDto>
{
    public async ValueTask<LibraryDto> Handle(GetLibraryQuery query, CancellationToken ct)
    {
        var coverage = new List<CoverageDto>();
        foreach (var mode in Enum.GetValues<GameMode>())
        {
            var total = (await factory.LoadCandidatesAsync(mode, ct)).Count;
            var covered = await db.PuzzleEntries.Where(e => e.Puzzle.Mode == mode).Select(e => e.WordId).Distinct().CountAsync(ct);
            var puzzles = await db.Puzzles.CountAsync(p => p.Mode == mode && p.DailyDate == null, ct);
            coverage.Add(new CoverageDto(mode, total, covered, puzzles));
        }

        var puzzlesQuery = db.Puzzles.AsNoTracking().Where(p => p.DailyDate == null);
        if (query.Mode is { } m) puzzlesQuery = puzzlesQuery.Where(p => p.Mode == m);
        if (query.Difficulty is { } d) puzzlesQuery = puzzlesQuery.Where(p => p.Difficulty == d);

        var rows = puzzlesQuery.Select(p => new
        {
            p.Id,
            p.Mode,
            p.Difficulty,
            p.Width,
            p.Height,
            WordCount = p.Entries.Count,
            p.CreatedAt,
            Completed = db.GameSessions.Any(s => s.PuzzleId == p.Id && s.Status == SessionStatus.Completed),
            InProgress = db.GameSessions.Any(s => s.PuzzleId == p.Id && s.Status == SessionStatus.InProgress),
            BestScore = db.GameSessions
                .Where(s => s.PuzzleId == p.Id && s.Status == SessionStatus.Completed)
                .Max(s => (int?)s.Score),
        });

        rows = query.Status switch
        {
            PlayStatus.New => rows.Where(r => !r.Completed && !r.InProgress),
            PlayStatus.InProgress => rows.Where(r => r.InProgress),
            PlayStatus.Completed => rows.Where(r => r.Completed),
            _ => rows,
        };

        var totalCount = await rows.CountAsync(ct);
        var page = await rows
            .OrderBy(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = page
            .Select(r => new LibraryItemDto(
                r.Id, r.Mode, r.Difficulty, r.Width, r.Height, r.WordCount,
                r.Completed ? PlayStatus.Completed : r.InProgress ? PlayStatus.InProgress : PlayStatus.New,
                r.BestScore, r.CreatedAt))
            .ToList();

        return new LibraryDto(coverage, items, totalCount);
    }
}

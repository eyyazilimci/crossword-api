using Crossword.Application.Puzzles;
using Crossword.Domain;
using Mediator;

namespace Crossword.Api.Endpoints;

internal static class PuzzleEndpoints
{
    public sealed record NextPuzzleRequest(GameMode Mode, Difficulty Difficulty);

    public sealed record DailyPuzzleRequest(GameMode Mode);

    public sealed record GenerateLibraryRequest(GameMode Mode, int ExtraPuzzles = 0);

    public static void MapPuzzleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/puzzles").WithTags("Puzzles");

        group.MapGet("/", async (GameMode? mode, Difficulty? difficulty, PlayStatus? status, int? page, int? pageSize,
                ISender sender, CancellationToken ct) =>
            await sender.Send(new GetLibraryQuery(mode, difficulty, status, page ?? 1, pageSize ?? 24), ct));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            await sender.Send(new GetPuzzleQuery(id), ct));

        group.MapPost("/next", async (NextPuzzleRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new GetNextPuzzleCommand(request.Mode, request.Difficulty), ct));

        group.MapPost("/daily", async (DailyPuzzleRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new GetDailyPuzzleCommand(request.Mode), ct));

        group.MapPost("/library", async (GenerateLibraryRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new GenerateLibraryCommand(request.Mode, request.ExtraPuzzles), ct));
    }
}

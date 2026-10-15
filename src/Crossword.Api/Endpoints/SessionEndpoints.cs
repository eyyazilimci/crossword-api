using Crossword.Application.Sessions;
using Mediator;

namespace Crossword.Api.Endpoints;

internal static class SessionEndpoints
{
    public sealed record StartRequest(Guid PuzzleId, bool Restart = false);

    public sealed record ProgressRequest(string Letters, int ElapsedSeconds);

    public sealed record CellsRequest(string Letters, int ElapsedSeconds, IReadOnlyList<int> Cells);

    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sessions").WithTags("Sessions");

        group.MapPost("/", async (StartRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new StartSessionCommand(request.PuzzleId, request.Restart), ct));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            await sender.Send(new GetSessionQuery(id), ct));

        group.MapPut("/{id:guid}/progress", async (Guid id, ProgressRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SaveProgressCommand(id, request.Letters, request.ElapsedSeconds), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/check", async (Guid id, CellsRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new CheckCellsCommand(id, request.Letters, request.ElapsedSeconds, request.Cells), ct));

        group.MapPost("/{id:guid}/reveal", async (Guid id, CellsRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new RevealCellsCommand(id, request.Letters, request.ElapsedSeconds, request.Cells), ct));

        group.MapPost("/{id:guid}/submit", async (Guid id, ProgressRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new SubmitSessionCommand(id, request.Letters, request.ElapsedSeconds), ct));
    }
}

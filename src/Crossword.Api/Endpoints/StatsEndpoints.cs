using Crossword.Application.Stats;
using Crossword.Domain;
using Crossword.Infrastructure.Persistence;
using Mediator;

namespace Crossword.Api.Endpoints;

internal static class StatsEndpoints
{
    public static void MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new GetDashboardQuery(), ct)).WithTags("Stats");

        app.MapGet("/stats", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new GetStatsQuery(), ct)).WithTags("Stats");

        app.MapDelete("/unknown-words/{mode}/{wordId:int}", async (GameMode mode, int wordId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RemoveUnknownWordCommand(wordId, mode), ct);
            return Results.NoContent();
        }).WithTags("Stats");

        app.MapGet("/achievements", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new GetAchievementsQuery(), ct)).WithTags("Stats");
    }

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
        {
            var database = await db.Database.CanConnectAsync(ct);
            var words = database ? db.Words.Count() : 0;
            return Results.Ok(new { status = database ? "ok" : "database-unreachable", words });
        }).WithTags("Health");
}

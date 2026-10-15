using Crossword.Application.Stats;
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

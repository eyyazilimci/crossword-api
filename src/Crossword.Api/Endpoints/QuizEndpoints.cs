using Crossword.Application.Quiz;
using Mediator;

namespace Crossword.Api.Endpoints;

internal static class QuizEndpoints
{
    public sealed record PracticeRequest(string Topic);

    public sealed record AnswerRequest(int Position, int Choice);

    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/quiz").WithTags("Quiz");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new GetQuizOverviewQuery(), ct));

        group.MapPost("/daily", async (ISender sender, CancellationToken ct) =>
            await sender.Send(new StartDailyQuizCommand(), ct));

        group.MapPost("/practice", async (PracticeRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new StartPracticeQuizCommand(request.Topic), ct));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            await sender.Send(new GetQuizSessionQuery(id), ct));

        group.MapPost("/{id:guid}/answers", async (Guid id, AnswerRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new AnswerQuizQuestionCommand(id, request.Position, request.Choice), ct));

        group.MapPost("/questions/{id:int}/report", async (int id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReportQuizQuestionCommand(id), ct);
            return Results.NoContent();
        });
    }
}

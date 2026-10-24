using Crossword.Application.Abstractions;
using Mediator;

namespace Crossword.Application.Quiz;

public sealed record GetQuizSessionQuery(Guid SessionId) : IQuery<QuizSessionDto>;

public sealed class GetQuizSessionHandler(IAppDbContext db) : IQueryHandler<GetQuizSessionQuery, QuizSessionDto>
{
    public async ValueTask<QuizSessionDto> Handle(GetQuizSessionQuery query, CancellationToken ct) =>
        await db.ToDtoAsync(await db.LoadQuizSessionAsync(query.SessionId, ct), ct);
}

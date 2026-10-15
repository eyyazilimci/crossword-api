using Crossword.Application.Abstractions;
using Mediator;

namespace Crossword.Application.Sessions;

public sealed record GetSessionQuery(Guid SessionId) : IQuery<GameDto>;

public sealed class GetSessionHandler(IAppDbContext db) : IQueryHandler<GetSessionQuery, GameDto>
{
    public async ValueTask<GameDto> Handle(GetSessionQuery query, CancellationToken ct) =>
        GameDto.From(await db.LoadSessionAsync(query.SessionId, ct));
}

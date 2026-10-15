using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Crossword.Application.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TMessage).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await next(message, cancellationToken);
        }
        finally
        {
            logger.LogInformation("{Message} handled in {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
        }
    }
}

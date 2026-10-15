using Crossword.Application.Behaviors;
using Crossword.Application.Puzzles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Crossword.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<PuzzleFactory>();
        return services;
    }
}

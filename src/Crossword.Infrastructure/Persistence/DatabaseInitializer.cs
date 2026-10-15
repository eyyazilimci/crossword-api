using Crossword.Application.Puzzles;
using Crossword.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crossword.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Apply EF Core migrations on startup. Off by default because the schema is created by hand (db/schema.sql).</summary>
    public bool AutoMigrate { get; init; }

    /// <summary>Generate puzzles on startup until every word has been used at least once.</summary>
    public bool GenerateLibraryOnStartup { get; init; } = true;

    /// <summary>Library size target per difficulty and mode, reached on startup.</summary>
    public int MinPuzzlesPerDifficulty { get; init; } = 30;
}

/// <summary>Seeds the word list and fills the puzzle library in the background after startup.</summary>
internal sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (options.Value.AutoMigrate)
                await db.Database.MigrateAsync(stoppingToken);

            await scope.ServiceProvider.GetRequiredService<WordSeeder>().SeedAsync(stoppingToken);

            if (!options.Value.GenerateLibraryOnStartup)
                return;

            var factory = scope.ServiceProvider.GetRequiredService<PuzzleFactory>();
            foreach (var mode in Enum.GetValues<GameMode>())
                await factory.GenerateLibraryAsync(mode, options.Value.MinPuzzlesPerDifficulty, extraPuzzles: 0, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialisation failed. Is the schema created (db/schema.sql) and the connection string set?");
        }
    }
}

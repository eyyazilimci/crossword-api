using Crossword.Domain;
using FluentValidation;
using Mediator;

namespace Crossword.Application.Puzzles;

/// <summary>
/// Fills the puzzle library so every word appears at least once, then adds <see cref="ExtraPuzzles"/> more.
/// </summary>
public sealed record GenerateLibraryCommand(GameMode Mode, int ExtraPuzzles = 0) : ICommand<GenerateLibraryResult>;

public sealed record GenerateLibraryResult(GameMode Mode, int PuzzlesCreated);

public sealed class GenerateLibraryValidator : AbstractValidator<GenerateLibraryCommand>
{
    public GenerateLibraryValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.ExtraPuzzles).InclusiveBetween(0, 200);
    }
}

public sealed class GenerateLibraryHandler(PuzzleFactory factory)
    : ICommandHandler<GenerateLibraryCommand, GenerateLibraryResult>
{
    public async ValueTask<GenerateLibraryResult> Handle(GenerateLibraryCommand command, CancellationToken ct)
    {
        var created = await factory.GenerateLibraryAsync(command.Mode, minPerDifficulty: 0, command.ExtraPuzzles, ct);
        return new GenerateLibraryResult(command.Mode, created);
    }
}

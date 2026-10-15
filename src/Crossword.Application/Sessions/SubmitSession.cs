using Crossword.Application.Abstractions;
using Crossword.Application.Gamification;
using Crossword.Domain;
using Crossword.Domain.Entities;
using Crossword.Domain.Gamification;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Crossword.Application.Sessions;

/// <summary>Hands in the filled grid. When every letter is right the puzzle is scored and progress is updated.</summary>
public sealed record SubmitSessionCommand(Guid SessionId, string Letters, int ElapsedSeconds) : ICommand<SubmitResult>;

public sealed record WordResultDto(int Number, Orientation Orientation, string Clue, string Answer, bool Correct);

public sealed record CompletionDto(
    ScoreBreakdown Score,
    int XpEarned,
    LevelDto LevelBefore,
    LevelDto LevelAfter,
    int CurrentStreak,
    IReadOnlyList<AchievementDto> NewAchievements,
    IReadOnlyList<WordResultDto> Words);

public sealed record SubmitResult(bool IsSolved, int WrongOrEmptyCells, CompletionDto? Completion);

public sealed class SubmitSessionValidator : AbstractValidator<SubmitSessionCommand>
{
    public SubmitSessionValidator()
    {
        RuleFor(x => x.Letters).NotEmpty().MaximumLength(2500);
        RuleFor(x => x.ElapsedSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class SubmitSessionHandler(IAppDbContext db, IClock clock) : ICommandHandler<SubmitSessionCommand, SubmitResult>
{
    public async ValueTask<SubmitResult> Handle(SubmitSessionCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var session = await db.LoadSessionAsync(command.SessionId, ct);
        var puzzle = session.Puzzle;

        session.SaveProgress(command.Letters, command.ElapsedSeconds, now);
        var wrong = session.CountWrongOrEmpty(puzzle.BuildSolution());
        if (wrong > 0)
        {
            session.RegisterFailedSubmit();
            await db.SaveChangesAsync(ct);
            return new SubmitResult(false, wrong, null);
        }

        var score = ScoreCalculator.Calculate(puzzle, session);
        var xp = ScoreCalculator.XpFor(score.Total);
        session.Complete(score.Total, xp, now);

        var words = await RecordMasteryAsync(session, now, ct);

        var profile = await db.GetProfileAsync(ct);
        var levelBefore = LevelDto.FromXp(profile.TotalXp);
        profile.RegisterCompletion(xp, clock.Today);
        await db.SaveChangesAsync(ct);

        var achievements = await db.UnlockAchievementsAsync(profile, session, clock.Today, now, ct);
        await db.SaveChangesAsync(ct);

        var completion = new CompletionDto(
            score,
            xp,
            levelBefore,
            LevelDto.FromXp(profile.TotalXp),
            profile.ActiveStreak(clock.Today),
            achievements,
            words);
        return new SubmitResult(true, 0, completion);
    }

    private async Task<List<WordResultDto>> RecordMasteryAsync(GameSession session, DateTime now, CancellationToken ct)
    {
        var puzzle = session.Puzzle;
        var wordIds = puzzle.Entries.Select(e => e.WordId).ToList();
        var mastery = await db.WordMasteries
            .Where(m => m.Mode == puzzle.Mode && wordIds.Contains(m.WordId))
            .ToDictionaryAsync(m => m.WordId, ct);

        var results = new List<WordResultDto>();
        foreach (var entry in puzzle.Entries.OrderBy(e => e.Orientation).ThenBy(e => e.Number))
        {
            if (!mastery.TryGetValue(entry.WordId, out var m))
            {
                m = WordMastery.Create(entry.WordId, puzzle.Mode, now);
                mastery[entry.WordId] = m;
                db.WordMasteries.Add(m);
            }

            var correct = !session.WasMissed(entry, puzzle.Width);
            m.Record(correct, now);
            results.Add(new WordResultDto(entry.Number, entry.Orientation, entry.Clue, entry.Answer, correct));
        }

        return results;
    }
}

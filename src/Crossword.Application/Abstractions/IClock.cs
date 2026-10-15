namespace Crossword.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Calendar day in the player's time zone; used for streaks and the daily puzzle.</summary>
    DateOnly Today { get; }
}

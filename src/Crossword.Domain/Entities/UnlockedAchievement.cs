namespace Crossword.Domain.Entities;

public sealed class UnlockedAchievement
{
    private UnlockedAchievement()
    {
    }

    public int Id { get; private set; }

    /// <summary>Matches <see cref="Gamification.AchievementDefinition.Code"/>.</summary>
    public string Code { get; private set; } = null!;

    public DateTime UnlockedAt { get; private set; }

    public static UnlockedAchievement Create(string code, DateTime now) => new() { Code = code, UnlockedAt = now };
}

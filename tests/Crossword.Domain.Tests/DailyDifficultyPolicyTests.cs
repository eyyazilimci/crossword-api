using Crossword.Domain.Gamification;

namespace Crossword.Domain.Tests;

public class DailyDifficultyPolicyTests
{
    [Fact]
    public void First_daily_puzzle_starts_easy() =>
        Assert.Equal(Difficulty.Easy, DailyDifficultyPolicy.Next(null, null));

    [Theory]
    [InlineData(Difficulty.Easy, 1.0, Difficulty.Medium)]
    [InlineData(Difficulty.Easy, 0.90, Difficulty.Medium)]
    [InlineData(Difficulty.Medium, 0.95, Difficulty.Hard)]
    [InlineData(Difficulty.Hard, 0.92, Difficulty.Expert)]
    [InlineData(Difficulty.Expert, 1.0, Difficulty.Expert)]
    public void Good_result_moves_up(Difficulty previous, double accuracy, Difficulty expected) =>
        Assert.Equal(expected, DailyDifficultyPolicy.Next(previous, accuracy));

    [Theory]
    [InlineData(Difficulty.Medium, 0.5, Difficulty.Easy)]
    [InlineData(Difficulty.Expert, 0.69, Difficulty.Hard)]
    [InlineData(Difficulty.Easy, 0.2, Difficulty.Easy)]
    public void Poor_result_moves_down(Difficulty previous, double accuracy, Difficulty expected) =>
        Assert.Equal(expected, DailyDifficultyPolicy.Next(previous, accuracy));

    [Theory]
    [InlineData(Difficulty.Medium, 0.70)]
    [InlineData(Difficulty.Medium, 0.89)]
    public void Middling_result_keeps_level(Difficulty previous, double accuracy) =>
        Assert.Equal(previous, DailyDifficultyPolicy.Next(previous, accuracy));

    [Fact]
    public void Unfinished_puzzle_keeps_level() =>
        Assert.Equal(Difficulty.Hard, DailyDifficultyPolicy.Next(Difficulty.Hard, null));
}

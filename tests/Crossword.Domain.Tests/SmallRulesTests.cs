using Crossword.Domain.Gamification;
using Crossword.Domain.Text;

namespace Crossword.Domain.Tests;

public class SmallRulesTests
{
    [Theory]
    [InlineData("ışık", "IŞIK")]
    [InlineData("istemek", "İSTEMEK")]
    [InlineData("kâğıt", "KAĞIT")]
    [InlineData("çiçek", "ÇİÇEK")]
    [InlineData("akşam yemeği", "AKŞAMYEMEĞİ")]
    [InlineData("e-posta", "EPOSTA")]
    public void Turkish_answers_use_turkish_casing(string word, string expected)
    {
        Assert.Equal(expected, AnswerNormalizer.ToAnswer(word, GameMode.EnToTr));
        Assert.True(AnswerNormalizer.IsValidAnswer(expected, GameMode.EnToTr));
    }

    [Fact]
    public void English_answers_use_invariant_casing()
    {
        Assert.Equal("LIGHT", AnswerNormalizer.ToAnswer("light", GameMode.TrToEn));
        Assert.Equal("GIVEUP", AnswerNormalizer.ToAnswer("give up", GameMode.TrToEn));
        Assert.Equal("YOUREWELCOME", AnswerNormalizer.ToAnswer("you're welcome", GameMode.TrToEn));
        Assert.Equal("YANINAKARKALMAK", AnswerNormalizer.ToAnswer("yanına kâr kalmak", GameMode.EnToTr));
        Assert.False(AnswerNormalizer.IsValidAnswer("ÇAY", GameMode.TrToEn));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(249, 2)]
    [InlineData(250, 3)]
    [InlineData(450, 4)]
    public void Level_grows_with_xp(int xp, int level) => Assert.Equal(level, LevelProgression.LevelForXp(xp));

    [Theory]
    [InlineData(1, CefrLevel.A1)]
    [InlineData(350, CefrLevel.A1)]
    [InlineData(351, CefrLevel.A2)]
    [InlineData(1401, CefrLevel.C1)]
    [InlineData(2118, CefrLevel.C2)]
    public void Level_follows_frequency_rank(int rank, CefrLevel level) =>
        Assert.Equal(level, Crosswords.DifficultyBands.LevelForRank(rank));

    [Fact]
    public void Achievement_codes_are_unique() =>
        Assert.Equal(Achievements.All.Count, Achievements.All.Select(a => a.Code).Distinct().Count());
}

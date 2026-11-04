using Crossword.Domain.Crosswords;
using Crossword.Domain.Entities;

namespace Crossword.Domain.Tests;

public class WordSelectionTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private static WordMastery Seen(int wordId, int daysAgo, bool correct = true)
    {
        var mastery = WordMastery.Create(wordId, GameMode.TrToEn, Now);
        mastery.RecordSeen(correct, Now.AddDays(-daysAgo));
        return mastery;
    }

    private static WordMastery Unknown(int wordId, int daysAgo)
    {
        var mastery = Seen(wordId, daysAgo, correct: false);
        mastery.MarkUnknown(countedCorrect: false);
        return mastery;
    }

    [Fact]
    public void A_missed_word_does_not_join_the_unknown_list_by_itself()
    {
        var mastery = Seen(1, 0, correct: false);

        Assert.Equal(0, mastery.WrongCount);
        Assert.Equal(0, mastery.ReviewWeight);
    }

    [Fact]
    public void Marking_a_word_unknown_takes_back_the_correct_count_of_that_puzzle()
    {
        var mastery = Seen(1, 0);

        mastery.MarkUnknown(countedCorrect: true);

        Assert.Equal(0, mastery.CorrectCount);
        Assert.Equal(1, mastery.WrongCount);
        Assert.False(mastery.IsLearned);
        Assert.True(mastery.ReviewWeight > 0);
    }

    [Fact]
    public void New_words_outrank_every_seen_word_even_after_a_long_time()
    {
        var newWord = WordSelection.Priority(null, false, Now);

        Assert.True(newWord > WordSelection.Priority(Seen(1, 365), false, Now));
        Assert.True(newWord > WordSelection.Priority(Unknown(2, 365), false, Now));
    }

    [Fact]
    public void A_word_seen_longer_ago_is_preferred_over_one_just_played()
    {
        Assert.True(WordSelection.Priority(Seen(1, 20), false, Now) > WordSelection.Priority(Seen(2, 0), false, Now));
    }

    [Fact]
    public void Review_words_are_capped_rest_after_being_seen_and_longest_unseen_go_first()
    {
        var mastery = new[]
        {
            Unknown(1, 1),
            Unknown(2, 5),
            Unknown(3, 6),
            Unknown(4, 7),
            Unknown(5, 8),
            Unknown(6, 9),
            Seen(7, 100),
        };

        var picked = WordSelection.PickReviewWords(mastery, Now);

        Assert.Equal(WordSelection.MaxReviewWordsPerPuzzle, picked.Count);
        Assert.Equal(new HashSet<int> { 3, 4, 5, 6 }, picked);
    }

    [Fact]
    public void Fresh_share_counts_new_and_review_words_but_not_other_seen_words()
    {
        var mastery = new[] { Seen(1, 1), Unknown(2, 9) }.ToDictionary(m => m.WordId);
        var review = WordSelection.PickReviewWords(mastery.Values, Now);

        Assert.Equal(0.75, WordSelection.FreshShare([1, 2, 3, 4], mastery, review));
        Assert.Equal(0, WordSelection.FreshShare([1], mastery, review));
    }
}

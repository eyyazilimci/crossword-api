using Crossword.Domain.Entities;

namespace Crossword.Domain.Crosswords;

/// <summary>
/// Decides which words a new puzzle should favour: never-seen words first, a handful of words the player
/// put on the unknown list, and otherwise whatever was seen longest ago.
/// </summary>
public static class WordSelection
{
    public const int MaxReviewWordsPerPuzzle = 4;

    /// <summary>A library puzzle with fewer wanted words than this is skipped in favour of a newly built one.</summary>
    public const double MinFreshShare = 0.7;

    private const double ReviewPriority = 12;
    private const double NewWordPriority = 10;
    private const double MaxSeenPriority = 4;

    private static readonly TimeSpan ReviewCooldown = TimeSpan.FromDays(3);
    private static readonly TimeSpan SeenRecovery = TimeSpan.FromDays(30);

    /// <summary>The unknown-list words due for another look, longest unseen first.</summary>
    public static IReadOnlySet<int> PickReviewWords(IEnumerable<WordMastery> mastery, DateTime now) =>
        mastery
            .Where(m => m.ReviewWeight > 0 && now - m.LastSeenAt >= ReviewCooldown)
            .OrderBy(m => m.LastSeenAt)
            .Take(MaxReviewWordsPerPuzzle)
            .Select(m => m.WordId)
            .ToHashSet();

    /// <summary>Seen words stay below new ones and recover slowly, so a word just played does not return soon.</summary>
    public static double Priority(WordMastery? mastery, bool isReviewPick, DateTime now)
    {
        if (isReviewPick)
            return ReviewPriority;
        if (mastery is null)
            return NewWordPriority;

        var recovered = (now - mastery.LastSeenAt) / SeenRecovery;
        return Math.Clamp(recovered, 0, 1) * MaxSeenPriority;
    }

    /// <summary>Share of a puzzle's words that are new or picked for review.</summary>
    public static double FreshShare(
        IReadOnlyCollection<int> wordIds,
        IReadOnlyDictionary<int, WordMastery> mastery,
        IReadOnlySet<int> reviewWords)
    {
        if (wordIds.Count == 0)
            return 0;

        var wanted = wordIds.Count(id => !mastery.ContainsKey(id) || reviewWords.Contains(id));
        return (double)wanted / wordIds.Count;
    }
}

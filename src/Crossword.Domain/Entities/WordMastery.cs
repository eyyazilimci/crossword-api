namespace Crossword.Domain.Entities;

/// <summary>How well the player knows a word in one direction. Feeds the "weak words come back" logic.</summary>
public sealed class WordMastery
{
    private WordMastery()
    {
    }

    public int WordId { get; private set; }
    public Word Word { get; private set; } = null!;
    public GameMode Mode { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    public bool IsLearned => CorrectCount > 0 && CorrectCount > WrongCount;

    public static WordMastery Create(int wordId, GameMode mode, DateTime now) =>
        new() { WordId = wordId, Mode = mode, LastSeenAt = now };

    /// <summary>Notes that the word was played. A miss adds nothing to the unknown list; only the player does that.</summary>
    public void RecordSeen(bool correct, DateTime now)
    {
        if (correct) CorrectCount++;
        LastSeenAt = now;
    }

    /// <summary>The player put the word on the unknown list after a puzzle, taking back that puzzle's correct count if it got one.</summary>
    public void MarkUnknown(bool countedCorrect)
    {
        if (countedCorrect && CorrectCount > 0) CorrectCount--;
        WrongCount++;
    }

    /// <summary>Above zero while the word is still on the player's unknown list and not yet made up for.</summary>
    public double ReviewWeight => Math.Max(0, WrongCount - CorrectCount * 0.5);
}

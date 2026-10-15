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

    public void Record(bool correct, DateTime now)
    {
        if (correct) CorrectCount++;
        else WrongCount++;
        LastSeenAt = now;
    }

    /// <summary>Extra generation priority for words the player keeps missing.</summary>
    public double ReviewWeight => Math.Max(0, WrongCount - CorrectCount * 0.5);
}

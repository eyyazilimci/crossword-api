namespace Crossword.Domain.Entities;

/// <summary>How the player has done on one quiz question. Feeds the "weak topics and missed questions come back" logic.</summary>
public sealed class QuestionProgress
{
    private QuestionProgress()
    {
    }

    public int QuestionId { get; private set; }
    public QuizQuestion Question { get; private set; } = null!;
    public int SeenCount { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public bool LastCorrect { get; private set; }
    public DateTime LastAnsweredAt { get; private set; }

    /// <summary>Calendar day of the last answer in the player's time zone; questions answered today are not asked again today.</summary>
    public DateOnly LastAnsweredOn { get; private set; }

    public static QuestionProgress Create(int questionId) => new() { QuestionId = questionId };

    public void Record(bool correct, DateTime now, DateOnly today)
    {
        SeenCount++;
        if (correct) CorrectCount++;
        else WrongCount++;
        LastCorrect = correct;
        LastAnsweredAt = now;
        LastAnsweredOn = today;
    }
}

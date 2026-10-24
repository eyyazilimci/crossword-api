using Crossword.Domain.Quiz;

namespace Crossword.Domain.Entities;

/// <summary>One quiz play-through. <see cref="Choices"/> runs parallel to <see cref="QuestionIds"/>.</summary>
public sealed class QuizSession
{
    public const int Unanswered = -1;

    private QuizSession()
    {
    }

    public Guid Id { get; private set; }
    public QuizKind Kind { get; private set; }

    /// <summary>Set for <see cref="QuizKind.Daily"/>; one daily quiz per day.</summary>
    public DateOnly? DailyDate { get; private set; }

    /// <summary>Set for <see cref="QuizKind.Practice"/>: the practised <see cref="GrammarTopic.Code"/>.</summary>
    public string? Topic { get; private set; }

    public int[] QuestionIds { get; private set; } = [];

    /// <summary>Chosen option per question, <see cref="Unanswered"/> until answered.</summary>
    public int[] Choices { get; private set; } = [];

    public SessionStatus Status { get; private set; }
    public int CorrectCount { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public bool IsCompleted => Status == SessionStatus.Completed;

    public int AnsweredCount => Choices.Count(c => c != Unanswered);

    public static QuizSession StartDaily(IReadOnlyList<int> questionIds, DateOnly date, DateTime now) =>
        Start(QuizKind.Daily, questionIds, date, null, now);

    public static QuizSession StartPractice(IReadOnlyList<int> questionIds, string topic, DateTime now)
    {
        if (!GrammarTopics.Exists(topic))
            throw new DomainException("Bilinmeyen konu.");
        return Start(QuizKind.Practice, questionIds, null, topic, now);
    }

    public int QuestionIdAt(int position) =>
        position >= 0 && position < QuestionIds.Length
            ? QuestionIds[position]
            : throw new DomainException("Soru bu teste ait değil.");

    /// <summary>Records the answer to the question at <paramref name="position"/> and returns whether it was right.</summary>
    public bool Answer(int position, int choice, QuizQuestion question, DateTime now)
    {
        if (IsCompleted)
            throw new DomainException("Bu test zaten tamamlandı.");
        if (QuestionIdAt(position) != question.Id)
            throw new DomainException("Soru bu teste ait değil.");
        if (Choices[position] != Unanswered)
            throw new DomainException("Bu soru zaten cevaplandı.");
        if (choice < 0 || choice >= question.Options.Length)
            throw new DomainException("Geçersiz şık.");

        var choices = Choices.ToArray();
        choices[position] = choice;
        Choices = choices;

        var correct = question.IsCorrect(choice);
        if (correct)
            CorrectCount++;

        UpdatedAt = now;
        if (AnsweredCount == QuestionIds.Length)
        {
            Status = SessionStatus.Completed;
            CompletedAt = now;
        }

        return correct;
    }

    private static QuizSession Start(QuizKind kind, IReadOnlyList<int> questionIds, DateOnly? date, string? topic, DateTime now)
    {
        if (questionIds.Count == 0)
            throw new DomainException("Bu test için soru bulunamadı.");
        if (questionIds.Distinct().Count() != questionIds.Count)
            throw new DomainException("Bir testte aynı soru iki kez olamaz.");

        return new QuizSession
        {
            Id = Guid.CreateVersion7(),
            Kind = kind,
            DailyDate = date,
            Topic = topic,
            QuestionIds = questionIds.ToArray(),
            Choices = Enumerable.Repeat(Unanswered, questionIds.Count).ToArray(),
            Status = SessionStatus.InProgress,
            StartedAt = now,
            UpdatedAt = now,
        };
    }
}

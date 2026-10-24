using Crossword.Domain.Quiz;

namespace Crossword.Domain.Entities;

/// <summary>A multiple-choice grammar question. <see cref="Key"/> ties it to its line in the question bank.</summary>
public sealed class QuizQuestion
{
    private QuizQuestion()
    {
    }

    public int Id { get; private set; }
    public string Key { get; private set; } = null!;

    /// <summary>A <see cref="GrammarTopic.Code"/>.</summary>
    public string Topic { get; private set; } = null!;

    public CefrLevel Level { get; private set; }

    /// <summary>Sentence with the gap marked as <see cref="QuestionBankParser.Blank"/>.</summary>
    public string Prompt { get; private set; } = null!;

    public string[] Options { get; private set; } = [];
    public int CorrectIndex { get; private set; }

    /// <summary>Turkish explanation shown after answering.</summary>
    public string Explanation { get; private set; } = null!;

    /// <summary>False once the player reported it as wrong; reported questions are not asked again.</summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ReportedAt { get; private set; }

    public static QuizQuestion Create(SeedQuestion q, DateTime now)
    {
        var question = new QuizQuestion { Key = q.Key, IsActive = true, CreatedAt = now };
        question.CopyFrom(q);
        return question;
    }

    /// <summary>
    /// Applies the bank's current text. A reported question that was edited in the bank counts as fixed and is asked again.
    /// </summary>
    public void UpdateFrom(SeedQuestion q)
    {
        var changed = Topic != q.Topic
                      || Level != q.Level
                      || Prompt != q.Prompt
                      || !Options.SequenceEqual(q.Options)
                      || CorrectIndex != q.CorrectIndex
                      || Explanation != q.Explanation;
        if (!changed)
            return;

        CopyFrom(q);
        IsActive = true;
        ReportedAt = null;
    }

    public bool IsCorrect(int choice) => choice == CorrectIndex;

    public void Report(DateTime now)
    {
        IsActive = false;
        ReportedAt = now;
    }

    private void CopyFrom(SeedQuestion q)
    {
        if (q.Options.Count != QuestionBankParser.OptionCount || q.CorrectIndex < 0 || q.CorrectIndex >= q.Options.Count)
            throw new DomainException("Soru dört şıklı olmalı ve doğru cevap şıklardan biri olmalı.");

        Topic = q.Topic;
        Level = q.Level;
        Prompt = q.Prompt;
        Options = q.Options.ToArray();
        CorrectIndex = q.CorrectIndex;
        Explanation = q.Explanation;
    }
}

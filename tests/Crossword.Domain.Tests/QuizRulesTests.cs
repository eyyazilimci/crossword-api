using Crossword.Domain.Entities;
using Crossword.Domain.Quiz;

namespace Crossword.Domain.Tests;

public class QuizRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

    private static SeedQuestion Seed(string key = "t-001", int correct = 1) =>
        new(key, "present-perfect", CefrLevel.B1, "I ___ him since 2010.", ["know", "have known", "knew", "am knowing"], correct, "since → Present Perfect.");

    /// <summary>Questions with ids 1..count; EF assigns ids in the app, here they are set through the backing property.</summary>
    private static List<QuizQuestion> Questions(int count) =>
        Enumerable.Range(1, count).Select(i =>
        {
            var q = QuizQuestion.Create(Seed($"t-{i:000}"), Now);
            typeof(QuizQuestion).GetProperty(nameof(QuizQuestion.Id))!.SetValue(q, i);
            return q;
        }).ToList();

    [Fact]
    public void Answering_every_question_completes_the_quiz()
    {
        var questions = Questions(3);
        var session = QuizSession.StartDaily(questions.Select(q => q.Id).ToList(), DateOnly.FromDateTime(Now), Now);

        Assert.True(session.Answer(0, 1, questions[0], Now));
        Assert.False(session.Answer(1, 0, questions[1], Now));
        Assert.False(session.IsCompleted);
        Assert.True(session.Answer(2, 1, questions[2], Now));

        Assert.True(session.IsCompleted);
        Assert.Equal(2, session.CorrectCount);
        Assert.Equal([1, 0, 1], session.Choices);
    }

    [Fact]
    public void A_question_cannot_be_answered_twice()
    {
        var questions = Questions(2);
        var session = QuizSession.StartDaily(questions.Select(q => q.Id).ToList(), DateOnly.FromDateTime(Now), Now);
        session.Answer(0, 2, questions[0], Now);

        Assert.Throws<DomainException>(() => session.Answer(0, 1, questions[0], Now));
    }

    [Fact]
    public void Answer_must_match_the_question_at_that_position()
    {
        var questions = Questions(2);
        var session = QuizSession.StartDaily(questions.Select(q => q.Id).ToList(), DateOnly.FromDateTime(Now), Now);

        Assert.Throws<DomainException>(() => session.Answer(0, 1, questions[1], Now));
        Assert.Throws<DomainException>(() => session.Answer(5, 1, questions[0], Now));
        Assert.Throws<DomainException>(() => session.Answer(0, 4, questions[0], Now));
    }

    [Fact]
    public void Quiz_needs_distinct_questions() =>
        Assert.Throws<DomainException>(() => QuizSession.StartDaily([1, 1], DateOnly.FromDateTime(Now), Now));

    [Fact]
    public void Practice_needs_a_known_topic() =>
        Assert.Throws<DomainException>(() => QuizSession.StartPractice([1], "no-such-topic", Now));

    [Fact]
    public void Reported_question_comes_back_when_it_is_fixed_in_the_bank()
    {
        var question = QuizQuestion.Create(Seed(correct: 0), Now);
        question.Report(Now);
        Assert.False(question.IsActive);

        question.UpdateFrom(Seed(correct: 0));
        Assert.False(question.IsActive);

        question.UpdateFrom(Seed(correct: 1));
        Assert.True(question.IsActive);
        Assert.Null(question.ReportedAt);
        Assert.Equal(1, question.CorrectIndex);
    }

    [Fact]
    public void Progress_remembers_the_last_answer()
    {
        var progress = QuestionProgress.Create(1);
        var today = DateOnly.FromDateTime(Now);
        progress.Record(false, Now, today);
        progress.Record(true, Now.AddDays(1), today.AddDays(1));

        Assert.Equal(2, progress.SeenCount);
        Assert.Equal(1, progress.CorrectCount);
        Assert.Equal(1, progress.WrongCount);
        Assert.True(progress.LastCorrect);
        Assert.Equal(today.AddDays(1), progress.LastAnsweredOn);
    }

    [Fact]
    public void Target_level_starts_at_b1() => Assert.Equal(CefrLevel.B1, QuizLevelPolicy.Target([]));

    [Fact]
    public void Passing_a_level_targets_the_next_one() =>
        Assert.Equal(CefrLevel.C1, QuizLevelPolicy.Target([new(CefrLevel.B1, 10, 9), new(CefrLevel.B2, 10, 8)]));

    [Fact]
    public void Too_few_answers_do_not_count() =>
        Assert.Equal(CefrLevel.B1, QuizLevelPolicy.Target([new(CefrLevel.B2, 5, 5)]));

    [Fact]
    public void Starting_level_can_be_raised() =>
        Assert.Equal(CefrLevel.B2, QuizLevelPolicy.Target([new(CefrLevel.A2, 10, 10)], CefrLevel.B2));

    [Fact]
    public void Target_level_never_goes_past_c2() =>
        Assert.Equal(CefrLevel.C2, QuizLevelPolicy.Target([new(CefrLevel.C2, 10, 10)]));

    [Fact]
    public void Streak_counts_consecutive_days()
    {
        var today = new DateOnly(2026, 10, 24);
        DateOnly[] days = [today.AddDays(-1), today.AddDays(-2), today.AddDays(-4)];

        Assert.Equal(2, QuizStreak.Current(days, today));
        Assert.Equal(3, QuizStreak.Current([.. days, today], today));
        Assert.Equal(0, QuizStreak.Current([today.AddDays(-2)], today));
        Assert.Equal(2, QuizStreak.Longest(days));
    }
}

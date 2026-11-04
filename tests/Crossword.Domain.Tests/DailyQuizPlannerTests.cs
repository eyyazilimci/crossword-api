using Crossword.Domain.Quiz;

namespace Crossword.Domain.Tests;

public class DailyQuizPlannerTests
{
    private static readonly DateOnly Today = new(2026, 10, 24);

    private static readonly string[] Topics = GrammarTopics.All.Take(8).Select(t => t.Code).ToArray();

    /// <summary>Eight topics with ten new B1–B2 questions each, ids 1..80.</summary>
    private static List<QuizCandidate> Bank() =>
        Topics.SelectMany((topic, t) => Enumerable.Range(1, 10).Select(i =>
                new QuizCandidate(t * 10 + i, topic, i % 2 == 0 ? CefrLevel.B1 : CefrLevel.B2, 0, 0, false, null)))
            .ToList();

    private static List<QuizCandidate> With(List<QuizCandidate> bank, params QuizCandidate[] changed) =>
        bank.Select(c => changed.FirstOrDefault(x => x.QuestionId == c.QuestionId) ?? c).ToList();

    private static QuizCandidate Missed(QuizCandidate c, int daysAgo) =>
        c with { SeenCount = 1, CorrectCount = 0, LastCorrect = false, LastAnsweredOn = Today.AddDays(-daysAgo) };

    private static QuizCandidate Solved(QuizCandidate c, int daysAgo) =>
        c with { SeenCount = 1, CorrectCount = 1, LastCorrect = true, LastAnsweredOn = Today.AddDays(-daysAgo) };

    [Fact]
    public void Daily_quiz_has_ten_distinct_questions()
    {
        var plan = DailyQuizPlanner.PlanDaily(Bank(), CefrLevel.B1, Today, new Random(1));

        Assert.Equal(10, plan.Count);
        Assert.Equal(10, plan.Distinct().Count());
    }

    [Fact]
    public void Missed_questions_from_earlier_days_come_back_oldest_first()
    {
        var bank = Bank();
        bank = With(bank, Missed(bank[0], 1), Missed(bank[11], 5), Missed(bank[22], 3), Missed(bank[33], 9));

        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(1));

        // Three reviews: the three oldest misses.
        Assert.Contains(bank[33].QuestionId, plan);
        Assert.Contains(bank[11].QuestionId, plan);
        Assert.Contains(bank[22].QuestionId, plan);
    }

    [Fact]
    public void Questions_answered_today_are_not_asked_again()
    {
        var bank = Bank();
        var answeredToday = bank.Take(30).Select(c => Missed(c, 0)).ToArray();
        bank = With(bank, answeredToday);

        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(4));

        Assert.DoesNotContain(plan, id => answeredToday.Any(c => c.QuestionId == id));
    }

    [Fact]
    public void Weakest_topic_gets_questions()
    {
        var bank = Bank();
        var weak = Topics[5];
        // Five wrong answers in the weak topic (answered long ago so they are not "today"), everything else unseen.
        var history = bank.Where(c => c.Topic == weak).Take(5).Select(c => Missed(c, 30)).ToArray();
        bank = With(bank, history);

        var options = new QuizPlanOptions { Reviews = 0 };
        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(2), options);

        var newInWeakTopic = bank.Where(c => c.Topic == weak && c.IsNew).Select(c => c.QuestionId).ToHashSet();
        Assert.True(plan.Count(newInWeakTopic.Contains) >= 2);
    }

    [Fact]
    public void Focus_topics_count_as_weak_before_they_are_played()
    {
        var bank = Bank();
        var focus = Topics[6];
        var options = new QuizPlanOptions { FocusTopics = [focus], WeakTopics = 1 };

        for (var seed = 0; seed < 20; seed++)
        {
            var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(seed), options);
            Assert.True(plan.Count(id => bank.Single(c => c.QuestionId == id).Topic == focus) >= 2);
        }
    }

    [Fact]
    public void Strong_topic_is_not_preferred_over_a_weak_one()
    {
        var bank = Bank();
        var strong = bank.Where(c => c.Topic == Topics[0]).Take(8).Select(c => Solved(c, 20)).ToArray();
        var weak = bank.Where(c => c.Topic == Topics[1]).Take(4).Select(c => Missed(c, 20)).ToArray();
        bank = With(bank, [.. strong, .. weak]);

        var options = new QuizPlanOptions { Reviews = 0, WeakTopics = 1, Stretch = 0 };
        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(3), options);
        var topicOf = bank.ToDictionary(c => c.QuestionId, c => c.Topic);

        Assert.True(plan.Count(id => topicOf[id] == Topics[1]) >= plan.Count(id => topicOf[id] == Topics[0]));
    }

    [Fact]
    public void One_question_is_above_the_target_level()
    {
        var bank = Bank();
        bank.Add(new QuizCandidate(1000, Topics[0], CefrLevel.C1, 0, 0, false, null));

        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B2, Today, new Random(5));

        Assert.Contains(1000, plan);
    }

    [Fact]
    public void A_topic_gets_at_most_three_questions_when_the_bank_is_big_enough()
    {
        var plan = DailyQuizPlanner.PlanDaily(Bank(), CefrLevel.B1, Today, new Random(6));
        var topicOf = Bank().ToDictionary(c => c.QuestionId, c => c.Topic);

        Assert.All(plan.GroupBy(id => topicOf[id]), g => Assert.True(g.Count() <= 3));
    }

    [Fact]
    public void Small_bank_still_fills_the_quiz()
    {
        var bank = Bank().Where(c => c.Topic == Topics[0]).Take(7).ToList();

        var plan = DailyQuizPlanner.PlanDaily(bank, CefrLevel.B1, Today, new Random(7));

        Assert.Equal(7, plan.Count);
    }

    [Fact]
    public void Same_seed_gives_the_same_plan()
    {
        var a = DailyQuizPlanner.PlanDaily(Bank(), CefrLevel.B1, Today, new Random(42));
        var b = DailyQuizPlanner.PlanDaily(Bank(), CefrLevel.B1, Today, new Random(42));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Practice_starts_with_missed_questions_of_the_topic()
    {
        var topic = Bank().Where(c => c.Topic == Topics[2]).ToList();
        topic = With(topic, Missed(topic[3], 0), Missed(topic[7], 2));

        var plan = DailyQuizPlanner.PlanPractice(topic, CefrLevel.B1, Today, new Random(8), size: 5);

        Assert.Equal(5, plan.Count);
        Assert.Contains(topic[3].QuestionId, plan);
        Assert.Contains(topic[7].QuestionId, plan);
    }

    [Theory]
    [InlineData(0, 0, 0.5)]
    [InlineData(8, 8, 0.9)]
    [InlineData(8, 0, 0.1)]
    public void Topic_strength_is_smoothed_accuracy(int seen, int correct, double expected) =>
        Assert.Equal(expected, DailyQuizPlanner.TopicStrength(seen, correct), 3);
}

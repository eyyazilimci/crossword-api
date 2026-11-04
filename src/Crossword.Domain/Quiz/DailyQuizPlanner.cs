namespace Crossword.Domain.Quiz;

/// <summary>An active question plus how the player has done on it so far.</summary>
public sealed record QuizCandidate(
    int QuestionId,
    string Topic,
    CefrLevel Level,
    int SeenCount,
    int CorrectCount,
    bool LastCorrect,
    DateOnly? LastAnsweredOn)
{
    public bool IsNew => SeenCount == 0;

    /// <summary>Answered before and the last answer was wrong.</summary>
    public bool NeedsReview => SeenCount > 0 && !LastCorrect;
}

public sealed class QuizPlanOptions
{
    public int Size { get; init; } = 10;

    /// <summary>Questions missed on earlier days that come back first.</summary>
    public int Reviews { get; init; } = 3;

    /// <summary>How many of the weakest topics get extra questions, and how many each.</summary>
    public int WeakTopics { get; init; } = 2;

    public int PerWeakTopic { get; init; } = 2;

    /// <summary>New questions above the player's level.</summary>
    public int Stretch { get; init; } = 1;

    /// <summary>Keeps the daily mix varied; relaxed only when the bank is too small.</summary>
    public int MaxPerTopic { get; init; } = 3;

    /// <summary>The player's starting profile: these topics count as weak until they have <see cref="FocusMinAnswers"/> answers.</summary>
    public IReadOnlyCollection<string> FocusTopics { get; init; } = [];

    public int FocusMinAnswers { get; init; } = 6;
}

/// <summary>
/// Picks the questions of a quiz. Daily: missed questions from earlier days, then new questions from the
/// weakest topics, one question above the player's level, and a varied fill. Practice: one topic, missed first.
/// </summary>
public static class DailyQuizPlanner
{
    /// <summary>Focus topics without enough answers are ranked at most this strong.</summary>
    public const double FocusStrength = 0.25;

    /// <summary>Smoothed accuracy (0–1) used to rank topics; an unseen topic is 0.5.</summary>
    public static double TopicStrength(int seen, int correct) => (correct + 1.0) / (seen + 2.0);

    public static IReadOnlyList<int> PlanDaily(
        IReadOnlyList<QuizCandidate> candidates,
        CefrLevel targetLevel,
        DateOnly today,
        Random random,
        QuizPlanOptions? options = null)
    {
        options ??= new QuizPlanOptions();
        var jitter = candidates.ToDictionary(c => c.QuestionId, _ => random.NextDouble());
        var plan = new Plan(options.Size, options.MaxPerTopic);

        plan.TakeFrom(
            candidates.Where(c => c.NeedsReview && c.LastAnsweredOn < today)
                .OrderBy(c => c.LastAnsweredOn)
                .ThenBy(c => jitter[c.QuestionId]),
            options.Reviews);

        foreach (var topic in WeakestTopics(candidates, options, today, random).Take(options.WeakTopics))
            plan.TakeFrom(Fresh(candidates.Where(c => c.Topic == topic), targetLevel, today, jitter), options.PerWeakTopic);

        plan.TakeFrom(
            candidates.Where(c => c.IsNew && c.Level > targetLevel)
                .OrderBy(c => c.Level)
                .ThenBy(c => jitter[c.QuestionId]),
            options.Stretch);

        plan.TakeFrom(Fresh(candidates, targetLevel, today, jitter), options.Size);

        // A small bank can run out of topics under the per-topic limit; fill from anything left.
        plan.TakeFrom(Fresh(candidates, targetLevel, today, jitter), options.Size, ignoreTopicLimit: true);
        plan.TakeFrom(candidates.OrderBy(c => jitter[c.QuestionId]), options.Size, ignoreTopicLimit: true);

        return plan.Shuffled(jitter);
    }

    /// <param name="candidates">Questions of the practised topic.</param>
    public static IReadOnlyList<int> PlanPractice(
        IReadOnlyList<QuizCandidate> candidates,
        CefrLevel targetLevel,
        DateOnly today,
        Random random,
        int size = 10)
    {
        var jitter = candidates.ToDictionary(c => c.QuestionId, _ => random.NextDouble());
        var plan = new Plan(size, int.MaxValue);

        plan.TakeFrom(
            candidates.Where(c => c.NeedsReview)
                .OrderBy(c => c.LastAnsweredOn)
                .ThenBy(c => jitter[c.QuestionId]),
            size / 2);
        plan.TakeFrom(Fresh(candidates, targetLevel, today, jitter), size);
        plan.TakeFrom(candidates.OrderBy(c => jitter[c.QuestionId]), size);

        return plan.Shuffled(jitter);
    }

    /// <summary>Topics with fresh questions, weakest first.</summary>
    private static IEnumerable<string> WeakestTopics(
        IReadOnlyList<QuizCandidate> candidates, QuizPlanOptions options, DateOnly today, Random random)
    {
        var focus = options.FocusTopics.ToHashSet();
        return candidates
            .GroupBy(c => c.Topic)
            .Where(g => g.Any(c => c.LastAnsweredOn != today))
            .Select(g =>
            {
                var seen = g.Sum(c => c.SeenCount);
                var strength = TopicStrength(seen, g.Sum(c => c.CorrectCount));
                if (focus.Contains(g.Key) && seen < options.FocusMinAnswers)
                    strength = Math.Min(strength, FocusStrength);
                return (Topic: g.Key, Strength: strength);
            })
            .OrderBy(t => t.Topic, StringComparer.Ordinal)
            .Select(t => (t.Topic, t.Strength, Tie: random.NextDouble()))
            .OrderBy(t => t.Strength)
            .ThenBy(t => t.Tie)
            .Select(t => t.Topic)
            .ToList();
    }

    /// <summary>Not answered today; new questions first, then closest to the target level, then longest unseen.</summary>
    private static IEnumerable<QuizCandidate> Fresh(
        IEnumerable<QuizCandidate> candidates, CefrLevel targetLevel, DateOnly today, Dictionary<int, double> jitter) =>
        candidates
            .Where(c => c.LastAnsweredOn != today)
            .OrderBy(c => c.IsNew ? 0 : 1)
            .ThenBy(c => Math.Abs(c.Level - targetLevel))
            .ThenBy(c => c.LastAnsweredOn ?? DateOnly.MinValue)
            .ThenBy(c => jitter[c.QuestionId]);

    private sealed class Plan(int size, int maxPerTopic)
    {
        private readonly List<int> _picked = [];
        private readonly HashSet<int> _used = [];
        private readonly Dictionary<string, int> _perTopic = [];

        public void TakeFrom(IEnumerable<QuizCandidate> source, int count, bool ignoreTopicLimit = false)
        {
            foreach (var c in source)
            {
                if (count <= 0 || _picked.Count >= size)
                    return;
                if (_used.Contains(c.QuestionId))
                    continue;
                if (!ignoreTopicLimit && _perTopic.GetValueOrDefault(c.Topic) >= maxPerTopic)
                    continue;

                _picked.Add(c.QuestionId);
                _used.Add(c.QuestionId);
                _perTopic[c.Topic] = _perTopic.GetValueOrDefault(c.Topic) + 1;
                count--;
            }
        }

        public IReadOnlyList<int> Shuffled(Dictionary<int, double> jitter) => _picked.OrderBy(id => jitter[id]).ToList();
    }
}

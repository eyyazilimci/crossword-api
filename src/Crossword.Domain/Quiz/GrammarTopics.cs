namespace Crossword.Domain.Quiz;

public enum GrammarCategory
{
    Tenses = 1,
    Conditionals = 2,
    Subjunctive = 3,
    Modals = 4,
    VerbPatterns = 5,
    PassiveCausative = 6,
    ReportedSpeech = 7,
    RelativeClauses = 8,
    InversionEmphasis = 9,
    Linkers = 10,
    Comparison = 11,
    TagsAgreement = 12,
    ArticlesPrepositions = 13,
}

/// <summary>A grammar sub-topic. <see cref="Code"/> is what the question bank and the database store.</summary>
public sealed record GrammarTopic(string Code, GrammarCategory Category, string Title);

/// <summary>The fixed category → topic catalogue every quiz question is tagged with.</summary>
public static class GrammarTopics
{
    public static IReadOnlyDictionary<GrammarCategory, string> CategoryTitles { get; } = new Dictionary<GrammarCategory, string>
    {
        [GrammarCategory.Tenses] = "Tenses",
        [GrammarCategory.Conditionals] = "Conditionals & Unreal",
        [GrammarCategory.Subjunctive] = "Subjunctive",
        [GrammarCategory.Modals] = "Modals",
        [GrammarCategory.VerbPatterns] = "Verb Patterns",
        [GrammarCategory.PassiveCausative] = "Passive & Causative",
        [GrammarCategory.ReportedSpeech] = "Reported Speech",
        [GrammarCategory.RelativeClauses] = "Relative Clauses",
        [GrammarCategory.InversionEmphasis] = "Inversion & Emphasis",
        [GrammarCategory.Linkers] = "Bağlaçlar",
        [GrammarCategory.Comparison] = "Karşılaştırma",
        [GrammarCategory.TagsAgreement] = "Question Tags & Agreement",
        [GrammarCategory.ArticlesPrepositions] = "Articles, Prepositions, Quantifiers",
    };

    public static IReadOnlyList<GrammarTopic> All { get; } =
    [
        new("present-simple-continuous", GrammarCategory.Tenses, "Present Simple & Continuous"),
        new("present-perfect", GrammarCategory.Tenses, "Present Perfect"),
        new("present-perfect-continuous", GrammarCategory.Tenses, "Present Perfect Continuous"),
        new("past-simple-continuous", GrammarCategory.Tenses, "Past Simple & Continuous"),
        new("past-perfect", GrammarCategory.Tenses, "Past Perfect"),
        new("future-forms", GrammarCategory.Tenses, "Future Forms (will, going to)"),
        new("future-perfect", GrammarCategory.Tenses, "Future Perfect & Continuous"),

        new("conditionals", GrammarCategory.Conditionals, "Conditionals (0–3)"),
        new("mixed-conditionals", GrammarCategory.Conditionals, "Mixed Conditionals"),
        new("wish-if-only", GrammarCategory.Conditionals, "Wish & If only"),
        new("unreal-past", GrammarCategory.Conditionals, "It's time, I'd rather, as if, suppose"),

        new("subjunctive", GrammarCategory.Subjunctive, "Suggest / Insist / It's essential that"),

        new("modals", GrammarCategory.Modals, "Modals"),
        new("modals-past", GrammarCategory.Modals, "Modal + have + V3"),

        new("gerund-infinitive", GrammarCategory.VerbPatterns, "-ing / to V / yalın fiil"),
        new("verb-meaning-change", GrammarCategory.VerbPatterns, "Remember, stop, regret… ve edat olan to"),

        new("passive", GrammarCategory.PassiveCausative, "Passive Voice"),
        new("causative", GrammarCategory.PassiveCausative, "Causative & It is said that"),

        new("reported-speech", GrammarCategory.ReportedSpeech, "Reported Speech"),

        new("relative-clauses", GrammarCategory.RelativeClauses, "Relative Clauses"),

        new("inversion", GrammarCategory.InversionEmphasis, "Inversion"),
        new("cleft-sentences", GrammarCategory.InversionEmphasis, "Cleft Sentences (It was… that)"),

        new("linkers", GrammarCategory.Linkers, "Although, despite, however…"),

        new("comparison", GrammarCategory.Comparison, "Comparatives & Superlatives"),

        new("tags-agreement", GrammarCategory.TagsAgreement, "Question Tags & So do I"),

        new("articles", GrammarCategory.ArticlesPrepositions, "Articles"),
        new("prepositions", GrammarCategory.ArticlesPrepositions, "Prepositions"),
        new("quantifiers", GrammarCategory.ArticlesPrepositions, "Quantifiers"),
    ];

    private static readonly Dictionary<string, GrammarTopic> ByCode = All.ToDictionary(t => t.Code);

    public static GrammarTopic? Find(string code) => ByCode.GetValueOrDefault(code);

    public static bool Exists(string code) => ByCode.ContainsKey(code);
}

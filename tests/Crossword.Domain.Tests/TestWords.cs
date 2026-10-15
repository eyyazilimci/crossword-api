using Crossword.Domain;
using Crossword.Domain.Crosswords;

namespace Crossword.Domain.Tests;

internal static class TestWords
{
    private static readonly string[] Common =
    [
        "time", "people", "year", "house", "water", "school", "family", "money", "friend", "number",
        "world", "country", "question", "student", "teacher", "morning", "evening", "window", "kitchen", "garden",
        "animal", "flower", "river", "mountain", "summer", "winter", "market", "street", "village", "city",
        "doctor", "hospital", "computer", "picture", "letter", "language", "history", "music", "story", "answer",
        "happy", "strong", "beautiful", "important", "different", "quick", "slow", "early", "simple", "clean",
        "travel", "write", "listen", "remember", "believe", "understand", "change", "follow", "create", "decide",
        "open", "close", "begin", "finish", "carry", "build", "teach", "learn", "speak", "think",
        "table", "chair", "paper", "pencil", "bread", "cheese", "apple", "orange", "lemon", "sugar",
        "coffee", "dinner", "lunch", "breakfast", "office", "company", "market", "price", "power", "energy",
        "nature", "forest", "island", "ocean", "weather", "season", "holiday", "birthday", "party", "dance",
        "player", "game", "sport", "football", "winner", "team", "result", "problem", "reason", "example",
    ];

    public static List<WordCandidate> Candidates(int count = int.MaxValue) =>
        Common.Distinct()
            .Take(count)
            .Select((w, i) => new WordCandidate(i + 1, w.ToUpperInvariant(), "clue-" + w, PartOfSpeech.Noun,
                DifficultyBands.LevelForRank(i * 20 + 1)))
            .ToList();
}

namespace Crossword.Domain.Gamification;

public sealed record AchievementDefinition(string Code, string Title, string Description, string Icon);

/// <summary>Everything the achievement rules look at after a puzzle is finished.</summary>
public sealed record AchievementContext(
    int PuzzlesCompleted,
    int CurrentStreak,
    int Level,
    int LearnedWords,
    bool CompletedBothModes,
    bool LastWasFlawless,
    int LastElapsedSeconds,
    Difficulty LastDifficulty,
    bool LastWasDaily);

public static class Achievements
{
    private sealed record Rule(AchievementDefinition Definition, Func<AchievementContext, bool> IsMet);

    private static readonly Rule[] Rules =
    [
        new(new("first_puzzle", "İlk Adım", "İlk bulmacanı tamamla.", "🎯"), c => c.PuzzlesCompleted >= 1),
        new(new("puzzles_10", "Isınma Turu", "10 bulmaca tamamla.", "🔟"), c => c.PuzzlesCompleted >= 10),
        new(new("puzzles_50", "Maratoncu", "50 bulmaca tamamla.", "🏃"), c => c.PuzzlesCompleted >= 50),
        new(new("puzzles_100", "Yüzler Kulübü", "100 bulmaca tamamla.", "💯"), c => c.PuzzlesCompleted >= 100),
        new(new("flawless", "Kusursuz", "Hiç kontrol, ipucu ya da hata olmadan bir bulmaca bitir.", "💎"), c => c.LastWasFlawless),
        new(new("speedy", "Şimşek", "Bir bulmacayı 10 dakikanın altında bitir.", "⚡"), c => c.LastElapsedSeconds < 600),
        new(new("brave", "Cesur", "Zor seviyede bir bulmaca bitir.", "🦁"), c => c.LastDifficulty == Difficulty.Hard),
        new(new("daily", "Günlük Rutin", "Günün bulmacasını çöz.", "📅"), c => c.LastWasDaily),
        new(new("bilingual", "İki Dilli", "Her iki modda da bulmaca bitir.", "🌍"), c => c.CompletedBothModes),
        new(new("streak_3", "Alışkanlık", "3 gün üst üste oyna.", "🔥"), c => c.CurrentStreak >= 3),
        new(new("streak_7", "Bir Hafta", "7 gün üst üste oyna.", "🗓️"), c => c.CurrentStreak >= 7),
        new(new("streak_30", "Demir İrade", "30 gün üst üste oyna.", "🏆"), c => c.CurrentStreak >= 30),
        new(new("words_100", "Kelime Dağarcığı", "100 kelime öğren.", "📗"), c => c.LearnedWords >= 100),
        new(new("words_500", "Kitap Kurdu", "500 kelime öğren.", "📚"), c => c.LearnedWords >= 500),
        new(new("words_1000", "Yürüyen Sözlük", "1000 kelime öğren.", "🧠"), c => c.LearnedWords >= 1000),
        new(new("level_5", "Yükselen Yıldız", "5. seviyeye ulaş.", "⭐"), c => c.Level >= 5),
        new(new("level_10", "Usta", "10. seviyeye ulaş.", "🌟"), c => c.Level >= 10),
    ];

    public static IReadOnlyList<AchievementDefinition> All { get; } = Rules.Select(r => r.Definition).ToList();

    public static AchievementDefinition? Find(string code) => All.FirstOrDefault(a => a.Code == code);

    public static IEnumerable<AchievementDefinition> Earned(AchievementContext context) =>
        Rules.Where(r => r.IsMet(context)).Select(r => r.Definition);
}

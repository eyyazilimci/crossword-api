using Crossword.Domain;
using Crossword.Domain.Crosswords;
using Crossword.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crossword.Infrastructure.Persistence;

/// <summary>
/// Loads the embedded word list (Seed/words-*.txt) into the database. Line order is the frequency rank.
/// Existing words are updated, so editing a translation in the file and restarting is enough.
/// </summary>
public sealed class WordSeeder(AppDbContext db, ILogger<WordSeeder> logger)
{
    /// <param name="Level">Explicit level from the file (phrases), otherwise derived from <paramref name="Rank"/>.</param>
    public sealed record SeedWord(string English, string Turkish, PartOfSpeech PartOfSpeech, int Rank, CefrLevel Level);

    public static IReadOnlyList<SeedWord> ReadEmbeddedList()
    {
        var assembly = typeof(WordSeeder).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Seed.words-", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        var words = new List<SeedWord>();
        var seen = new HashSet<string>();
        foreach (var name in resources)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 3 || !seen.Add(parts[0].Trim().ToLowerInvariant()))
                    continue;

                var rank = words.Count + 1;
                var level = parts.Length > 3 && Enum.TryParse<CefrLevel>(parts[3].Trim(), true, out var explicitLevel)
                    ? explicitLevel
                    : DifficultyBands.LevelForRank(rank);
                words.Add(new SeedWord(parts[0].Trim(), parts[1].Trim(), ParsePartOfSpeech(parts[2]), rank, level));
            }
        }

        return words;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        var list = ReadEmbeddedList();
        var existing = await db.Words.ToDictionaryAsync(w => w.English, ct);
        var added = 0;

        foreach (var w in list)
        {
            if (existing.TryGetValue(w.English, out var word))
            {
                word.Update(w.Turkish, w.PartOfSpeech, w.Level, w.Rank);
            }
            else
            {
                db.Words.Add(Word.Create(w.English, w.Turkish, w.PartOfSpeech, w.Level, w.Rank));
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Word list ready: {Total} words ({Added} new)", list.Count, added);
    }

    private static PartOfSpeech ParsePartOfSpeech(string value) => value.Trim() switch
    {
        "n" => PartOfSpeech.Noun,
        "v" => PartOfSpeech.Verb,
        "adj" => PartOfSpeech.Adjective,
        "adv" => PartOfSpeech.Adverb,
        "phr" => PartOfSpeech.Phrase,
        _ => PartOfSpeech.Other,
    };
}

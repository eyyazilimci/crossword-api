namespace Crossword.Domain.Entities;

/// <summary>An English–Turkish word pair from the frequency list.</summary>
public sealed class Word
{
    private Word()
    {
    }

    public int Id { get; private set; }
    public string English { get; private set; } = null!;
    public string Turkish { get; private set; } = null!;
    public PartOfSpeech PartOfSpeech { get; private set; }
    public CefrLevel Level { get; private set; }

    /// <summary>1 = most frequent word.</summary>
    public int FrequencyRank { get; private set; }

    public static Word Create(string english, string turkish, PartOfSpeech partOfSpeech, CefrLevel level, int frequencyRank)
    {
        if (string.IsNullOrWhiteSpace(english) || string.IsNullOrWhiteSpace(turkish))
            throw new DomainException("Kelime ve çevirisi boş olamaz.");

        return new Word
        {
            English = english.Trim().ToLowerInvariant(),
            Turkish = turkish.Trim(),
            PartOfSpeech = partOfSpeech,
            Level = level,
            FrequencyRank = frequencyRank,
        };
    }

    public void Update(string turkish, PartOfSpeech partOfSpeech, CefrLevel level, int frequencyRank)
    {
        Turkish = turkish.Trim();
        PartOfSpeech = partOfSpeech;
        Level = level;
        FrequencyRank = frequencyRank;
    }

    public string ClueFor(GameMode mode) => mode == GameMode.TrToEn ? Turkish : English;

    public string AnswerSourceFor(GameMode mode) => mode == GameMode.TrToEn ? English : Turkish;
}

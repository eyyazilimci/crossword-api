using System.Globalization;
using System.Text;

namespace Crossword.Domain.Text;

/// <summary>
/// Converts words to the upper-case form written into the grid.
/// Turkish answers use Turkish casing rules (i → İ, ı → I).
/// </summary>
public static class AnswerNormalizer
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public const string EnglishAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string TurkishAlphabet = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

    public static string ToAnswer(string word, GameMode mode) =>
        mode == GameMode.TrToEn ? Normalize(word, english: true) : Normalize(word, english: false);

    /// <summary>
    /// Multi-word phrases are written into the grid without separators: "akşam yemeği" → AKŞAMYEMEĞİ,
    /// "e-posta" → EPOSTA. The clue keeps the original spelling.
    /// </summary>
    public static string Normalize(string word, bool english)
    {
        var joined = new string(word.Where(ch => !char.IsWhiteSpace(ch) && ch is not ('-' or '\'' or '’')).ToArray());
        if (english)
            return joined.ToUpperInvariant();

        // Circumflex letters (kâğıt, hâlâ) are written without the hat in puzzles.
        var sb = new StringBuilder(joined.Length);
        foreach (var ch in joined)
        {
            sb.Append(ch switch
            {
                'â' => 'a',
                'î' => 'i',
                'û' => 'u',
                'Â' => 'A',
                'Î' => 'İ',
                'Û' => 'U',
                _ => ch,
            });
        }

        return sb.ToString().ToUpper(Turkish);
    }

    public static bool IsValidAnswer(string answer, GameMode mode)
    {
        var alphabet = mode == GameMode.TrToEn ? EnglishAlphabet : TurkishAlphabet;
        return answer.Length > 0 && answer.All(alphabet.Contains);
    }
}

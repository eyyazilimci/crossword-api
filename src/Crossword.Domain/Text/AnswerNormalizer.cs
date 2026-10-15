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

    public static string Normalize(string word, bool english)
    {
        var trimmed = word.Trim();
        if (english)
            return trimmed.ToUpperInvariant();

        // Circumflex letters (kâğıt, hâlâ) are written without the hat in puzzles.
        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
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

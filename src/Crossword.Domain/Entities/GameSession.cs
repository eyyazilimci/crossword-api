namespace Crossword.Domain.Entities;

/// <summary>One play-through of a puzzle. Letters and masks are row-major strings the size of the grid.</summary>
public sealed class GameSession
{
    private const char Off = '0';
    private const char On = '1';

    private GameSession()
    {
    }

    public Guid Id { get; private set; }
    public Guid PuzzleId { get; private set; }
    public Puzzle Puzzle { get; private set; } = null!;
    public SessionStatus Status { get; private set; }

    /// <summary>Player's letters; <see cref="Puzzle.EmptyCell"/> for empty or blocked cells.</summary>
    public string Letters { get; private set; } = null!;

    /// <summary>'1' where the letter was revealed by a hint.</summary>
    public string RevealedMask { get; private set; } = null!;

    /// <summary>'1' where a check ever found a wrong letter.</summary>
    public string MistakeMask { get; private set; } = null!;

    public int ElapsedSeconds { get; private set; }
    public int ChecksUsed { get; private set; }
    public int RevealsUsed { get; private set; }
    public int FailedSubmits { get; private set; }
    public int Score { get; private set; }
    public int XpEarned { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public bool IsCompleted => Status == SessionStatus.Completed;

    /// <summary>Played without checks, reveals or wrong submits.</summary>
    public bool IsFlawless => ChecksUsed == 0 && RevealsUsed == 0 && FailedSubmits == 0;

    public static GameSession Start(Puzzle puzzle, DateTime now) => new()
    {
        Id = Guid.CreateVersion7(),
        PuzzleId = puzzle.Id,
        Puzzle = puzzle,
        Status = SessionStatus.InProgress,
        Letters = new string(Puzzle.EmptyCell, puzzle.CellCount),
        RevealedMask = new string(Off, puzzle.CellCount),
        MistakeMask = new string(Off, puzzle.CellCount),
        StartedAt = now,
        UpdatedAt = now,
    };

    public void SaveProgress(string letters, int elapsedSeconds, DateTime now)
    {
        EnsureInProgress();
        Letters = Sanitize(letters);
        ElapsedSeconds = Math.Max(ElapsedSeconds, elapsedSeconds);
        UpdatedAt = now;
    }

    /// <summary>Checks the given cells and returns the ones that hold a wrong letter.</summary>
    public IReadOnlyList<int> Check(IEnumerable<int> cells, string solution)
    {
        EnsureInProgress();
        var wrong = cells
            .Distinct()
            .Where(i => i >= 0 && i < solution.Length && solution[i] != Puzzle.BlockedCell)
            .Where(i => Letters[i] != Puzzle.EmptyCell && Letters[i] != solution[i])
            .OrderBy(i => i)
            .ToList();

        ChecksUsed++;
        MistakeMask = SetBits(MistakeMask, wrong);
        return wrong;
    }

    /// <summary>Writes the correct letter into the given cells and returns them.</summary>
    public IReadOnlyDictionary<int, char> Reveal(IEnumerable<int> cells, string solution)
    {
        EnsureInProgress();
        var targets = cells
            .Distinct()
            .Where(i => i >= 0 && i < solution.Length && solution[i] != Puzzle.BlockedCell)
            .Where(i => Letters[i] != solution[i] || RevealedMask[i] == On)
            .ToList();

        var letters = Letters.ToCharArray();
        var revealed = new Dictionary<int, char>();
        foreach (var i in targets)
        {
            letters[i] = solution[i];
            revealed[i] = solution[i];
        }

        var newlyRevealed = targets.Count(i => RevealedMask[i] != On);
        RevealsUsed += newlyRevealed;
        Letters = new string(letters);
        RevealedMask = SetBits(RevealedMask, targets);
        return revealed;
    }

    public int CountWrongOrEmpty(string solution) =>
        solution.Where((c, i) => c != Puzzle.BlockedCell && Letters[i] != c).Count();

    public void RegisterFailedSubmit() => FailedSubmits++;

    /// <summary>True when any cell of the entry was revealed or found wrong.</summary>
    public bool WasMissed(PuzzleEntry entry, int width) =>
        entry.CellIndexes(width).Any(i => RevealedMask[i] == On || MistakeMask[i] == On);

    /// <summary>Share of words solved without a reveal or a wrong check. Needs <see cref="Puzzle"/> with its entries.</summary>
    public double WordAccuracy() =>
        Puzzle.Entries.Count == 0
            ? 0
            : (double)Puzzle.Entries.Count(e => !WasMissed(e, Puzzle.Width)) / Puzzle.Entries.Count;

    public bool HadRevealIn(PuzzleEntry entry, int width) =>
        entry.CellIndexes(width).Any(i => RevealedMask[i] == On);

    public void Complete(int score, int xp, DateTime now)
    {
        EnsureInProgress();
        Status = SessionStatus.Completed;
        Score = score;
        XpEarned = xp;
        CompletedAt = now;
        UpdatedAt = now;
    }

    private void EnsureInProgress()
    {
        if (IsCompleted)
            throw new DomainException("Bu bulmaca zaten tamamlandı.");
    }

    private string Sanitize(string letters)
    {
        if (letters.Length != Letters.Length)
            throw new DomainException("Harf dizisi bulmaca boyutuyla uyuşmuyor.");

        var chars = letters.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetter(chars[i]))
                chars[i] = Puzzle.EmptyCell;
        }

        return new string(chars);
    }

    private static string SetBits(string mask, IEnumerable<int> indexes)
    {
        var chars = mask.ToCharArray();
        foreach (var i in indexes)
            chars[i] = On;
        return new string(chars);
    }
}

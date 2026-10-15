using Crossword.Domain;
using Crossword.Domain.Crosswords;

namespace Crossword.Domain.Tests;

internal static class GridAssertions
{
    /// <summary>
    /// Every horizontal/vertical run of 2+ letters must be exactly one entry, letters must agree
    /// where words cross, and all words must be connected.
    /// </summary>
    public static void AssertValid(GeneratedPuzzle puzzle)
    {
        var grid = new char[puzzle.Height, puzzle.Width];
        foreach (var entry in puzzle.Entries)
        {
            var i = 0;
            foreach (var (r, c) in entry.Cells())
            {
                Assert.InRange(r, 0, puzzle.Height - 1);
                Assert.InRange(c, 0, puzzle.Width - 1);
                var letter = entry.Word.Answer[i++];
                Assert.True(grid[r, c] == '\0' || grid[r, c] == letter, $"Conflict at {r},{c}");
                grid[r, c] = letter;
            }
        }

        var runs = new HashSet<(int, int, Orientation, int)>();
        for (var r = 0; r < puzzle.Height; r++)
        {
            for (var c = 0; c < puzzle.Width; c++)
            {
                if (grid[r, c] == '\0') continue;

                if ((c == 0 || grid[r, c - 1] == '\0') && c + 1 < puzzle.Width && grid[r, c + 1] != '\0')
                {
                    var len = 0;
                    while (c + len < puzzle.Width && grid[r, c + len] != '\0') len++;
                    runs.Add((r, c, Orientation.Across, len));
                }

                if ((r == 0 || grid[r - 1, c] == '\0') && r + 1 < puzzle.Height && grid[r + 1, c] != '\0')
                {
                    var len = 0;
                    while (r + len < puzzle.Height && grid[r + len, c] != '\0') len++;
                    runs.Add((r, c, Orientation.Down, len));
                }
            }
        }

        var entries = puzzle.Entries.Select(e => (e.Row, e.Column, e.Orientation, e.Length)).ToHashSet();
        Assert.Equal(entries.Count, puzzle.Entries.Count);
        Assert.True(runs.SetEquals(entries), "Grid contains letter runs that are not entries (or vice versa).");

        AssertConnected(puzzle, grid);
    }

    private static void AssertConnected(GeneratedPuzzle puzzle, char[,] grid)
    {
        var start = puzzle.Entries[0];
        var seen = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        queue.Enqueue((start.Row, start.Column));
        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            if (r < 0 || c < 0 || r >= puzzle.Height || c >= puzzle.Width || grid[r, c] == '\0' || !seen.Add((r, c)))
                continue;
            queue.Enqueue((r + 1, c));
            queue.Enqueue((r - 1, c));
            queue.Enqueue((r, c + 1));
            queue.Enqueue((r, c - 1));
        }

        var letterCount = 0;
        foreach (var ch in grid)
            if (ch != '\0') letterCount++;

        Assert.Equal(letterCount, seen.Count);
    }
}

using Crossword.Domain;
using Crossword.Domain.Entities;

namespace Crossword.Application.Puzzles;

public sealed record ClueDto(
    int Id,
    int Number,
    Orientation Orientation,
    int Row,
    int Column,
    int Length,
    string Clue,
    PartOfSpeech PartOfSpeech);

/// <summary>A puzzle as sent to the player: the shape and clues, never the answers.</summary>
public sealed record PuzzleDto(
    Guid Id,
    GameMode Mode,
    Difficulty Difficulty,
    int Width,
    int Height,
    string Layout,
    DateOnly? DailyDate,
    IReadOnlyList<ClueDto> Clues)
{
    public static PuzzleDto From(Puzzle puzzle) => new(
        puzzle.Id,
        puzzle.Mode,
        puzzle.Difficulty,
        puzzle.Width,
        puzzle.Height,
        puzzle.BuildLayout(),
        puzzle.DailyDate,
        puzzle.Entries
            .OrderBy(e => e.Orientation)
            .ThenBy(e => e.Number)
            .Select(e => new ClueDto(e.Id, e.Number, e.Orientation, e.Row, e.Column, e.Answer.Length, e.Clue, e.Word.PartOfSpeech))
            .ToList());
}

public sealed record PuzzleSummaryDto(Guid Id, GameMode Mode, Difficulty Difficulty, int Width, int Height, int WordCount);

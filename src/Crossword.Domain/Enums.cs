namespace Crossword.Domain;

/// <summary>Which language is shown as the clue and which one is written into the grid.</summary>
public enum GameMode
{
    /// <summary>Clue is Turkish, answer is English.</summary>
    TrToEn = 1,

    /// <summary>Clue is English, answer is Turkish.</summary>
    EnToTr = 2,
}

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4,
}

public enum CefrLevel
{
    A1 = 1,
    A2 = 2,
    B1 = 3,
    B2 = 4,
    C1 = 5,
    C2 = 6,
}

public enum PartOfSpeech
{
    Noun = 1,
    Verb = 2,
    Adjective = 3,
    Adverb = 4,
    Other = 5,
}

public enum Orientation
{
    Across = 1,
    Down = 2,
}

public enum SessionStatus
{
    InProgress = 1,
    Completed = 2,
}

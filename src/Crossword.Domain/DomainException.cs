namespace Crossword.Domain;

/// <summary>Raised when an operation would break a domain rule.</summary>
public sealed class DomainException(string message) : Exception(message);

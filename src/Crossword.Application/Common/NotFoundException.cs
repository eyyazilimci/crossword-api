namespace Crossword.Application.Common;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} bulunamadı ({key}).");

using Crossword.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Crossword.Infrastructure;

internal sealed class SystemClock(TimeProvider timeProvider, IConfiguration configuration) : IClock
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(configuration["App:TimeZone"] ?? "Europe/Istanbul");

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _zone));
}

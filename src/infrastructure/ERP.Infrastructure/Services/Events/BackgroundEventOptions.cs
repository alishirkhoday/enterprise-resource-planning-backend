namespace ERP.Infrastructure.Services.Events;

/// <summary>How often the outbox is drained, and how much of it at a time.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// How long to wait between passes. Short enough that a fact reaches its handler while it
    /// still matters, long enough that an idle system is not querying for nothing all day.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// How many messages one pass takes. Bounded on purpose: a backlog built up during an outage
    /// should be worked through steadily rather than loaded into memory in one go.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// How long to wait after a pass that threw, rather than failing at full speed.
    /// </summary>
    public int BackoffSecondsAfterFailure { get; set; } = 60;

    public bool Enabled { get; set; } = true;
}

/// <summary>When the books are scanned for the facts nothing announces about itself.</summary>
public sealed class ObservedEventScanOptions
{
    public const string SectionName = "ObservedEventScan";

    /// <summary>
    /// Once a day is the natural rhythm: every one of these is about a date arriving - a cheque
    /// falling due, a batch nearing expiry - so scanning more often would find the same facts
    /// again without any of them having changed.
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Local hour to run at. Early, so a list of what needs attention is waiting when people
    /// arrive rather than landing halfway through their day.
    /// </summary>
    public int RunAtHour { get; set; } = 6;

    public bool Enabled { get; set; } = true;
}

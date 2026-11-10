namespace Bit.Belemetry;

/// <summary>One provider's snapshot, sanitized: its data and what the sanitizer did to it.</summary>
public sealed class BelemetryDiagnosticSnapshot
{
    /// <summary>The provider's name.</summary>
    public required string Name { get; init; }

    /// <summary>The label it was recorded under (<see cref="IBelemetryDiagnostics.RecordSnapshotAsync"/>), or null for one taken by a capture.</summary>
    public string? Label { get; init; }

    /// <summary>What became of it.</summary>
    public BelemetrySnapshotStatus Status { get; init; }

    /// <summary>When it was taken.</summary>
    public DateTimeOffset Time { get; init; }

    /// <summary>
    /// The sanitized data: strings, numbers, booleans, null, and nested <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// and <see cref="IReadOnlyList{T}"/> of those. Null unless <see cref="Status"/> is <see cref="BelemetrySnapshotStatus.Captured"/>.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Data { get; init; }

    /// <summary>How many values were redacted (by key, pattern or the app's sanitizer).</summary>
    public int Redactions { get; init; }

    /// <summary>How many values were cut or left out at a limit (depth, items, length, size).</summary>
    public int Truncations { get; init; }

    /// <summary>How many values were left out because their type is not supported or a sanitizer failed.</summary>
    public int Omissions { get; init; }

    /// <summary>The exception type of a failed provider (its message is not kept: it may hold the state).</summary>
    public string? Error { get; init; }

    /// <summary>The estimated size of <see cref="Data"/>, in bytes.</summary>
    public int Bytes { get; init; }

    internal BelemetryDiagnosticSnapshot WithStatus(BelemetrySnapshotStatus status) => new()
    {
        Name = Name,
        Label = Label,
        Status = status,
        Time = Time,
        Data = status == BelemetrySnapshotStatus.Captured ? Data : null,
        Redactions = Redactions,
        Truncations = Truncations,
        Omissions = Omissions,
        Error = Error,
        Bytes = status == BelemetrySnapshotStatus.Captured ? Bytes : 0,
    };
}

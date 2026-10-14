using System.Text.Json.Serialization;

namespace Bit.Bonboarding;

/// <summary>
/// The persisted footprint of one tour for one user: whether it finished, and where the user got
/// to if it did not.
/// </summary>
public sealed record BonboardingRecord
{
    /// <summary><see cref="BonboardingTourDefinition.Id"/>.</summary>
    public required string TourId { get; init; }

    /// <summary>
    /// The <see cref="BonboardingTourDefinition.Version"/> this record was written by. A record from an older
    /// version is discarded rather than resumed into a step that has since changed meaning.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>Id of the step the user was last on.</summary>
    public string? CurrentStepId { get; init; }

    /// <summary>Index of the step the user was last on, as a fallback when ids change.</summary>
    public int CurrentIndex { get; init; }

    /// <summary>True once the tour was run to the end.</summary>
    public bool Completed { get; init; }

    /// <summary>True when the user skipped or closed the tour instead of finishing it.</summary>
    public bool Dismissed { get; init; }

    /// <summary>Ids of the steps actually visited, in order. Restores branching history on resume.</summary>
    public IReadOnlyList<string> VisitedStepIds { get; init; } = [];

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Application-defined values round-tripped with the record.</summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }

    /// <summary>True when the tour is finished with, however it ended.</summary>
    [JsonIgnore]
    public bool IsClosed => Completed || Dismissed;
}

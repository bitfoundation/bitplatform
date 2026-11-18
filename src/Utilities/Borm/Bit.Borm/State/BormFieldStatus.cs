namespace Bit.Borm;

/// <summary>A field's validity in one word. See <see cref="BormFieldState.Status"/>.</summary>
public enum BormFieldStatus
{
    /// <summary>No validation pass has judged the current value yet.</summary>
    Unvalidated,

    /// <summary>An asynchronous rule is checking it right now.</summary>
    Pending,

    /// <summary>Judged, and carries an error.</summary>
    Invalid,

    /// <summary>Judged, and nothing is wrong with it.</summary>
    Valid
}

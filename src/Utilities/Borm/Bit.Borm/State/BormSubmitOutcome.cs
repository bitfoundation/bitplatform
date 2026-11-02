namespace Bit.Borm;

/// <summary>How a submit ended. See <see cref="BormState.SubmitOutcome"/>.</summary>
public enum BormSubmitOutcome
{
    /// <summary>No submit has finished since the form was created or last reset.</summary>
    None,

    /// <summary>The form failed its own validation, so the submit handler was never called.</summary>
    Invalid,

    /// <summary>
    /// The handler ran and recorded errors — a server that refused the request and said why, put on the
    /// fields with <c>SetServerErrors</c>. Nothing was saved, and the user has something to fix.
    /// </summary>
    Rejected,

    /// <summary>The handler threw. The exception is in <see cref="BormState.SubmitError"/>.</summary>
    Failed,

    /// <summary>The handler stopped because the submit was cancelled (<see cref="BormState.CancelSubmit"/>).</summary>
    Cancelled,

    /// <summary>The form was valid and the handler returned without recording an error.</summary>
    Succeeded
}

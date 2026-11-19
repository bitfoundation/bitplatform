namespace Bit.Blex;

/// <summary>How a mutation's latest call went.</summary>
public enum BlexMutationStatus
{
    /// <summary>Never called, or <see cref="BlexMutation{TArg, TResult}.Reset"/> since.</summary>
    Idle,

    /// <summary>The latest call is running.</summary>
    Pending,

    /// <summary>The latest call failed.</summary>
    Error,

    /// <summary>The latest call succeeded.</summary>
    Success,
}

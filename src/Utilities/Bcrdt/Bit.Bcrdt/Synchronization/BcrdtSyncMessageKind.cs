namespace Bit.Bcrdt;

/// <summary>The kinds of message peers exchange.</summary>
/// <remarks>These values are on the wire; never renumber an existing entry.</remarks>
public enum BcrdtSyncMessageKind : byte
{
    /// <summary>"Here is everything I have; send me what I am missing." Opens a handshake.</summary>
    SyncStep1 = 0,
    /// <summary>"Here is what you were missing."</summary>
    SyncStep2 = 1,
    /// <summary>An incremental change.</summary>
    Update = 2,
    /// <summary>Ephemeral presence state.</summary>
    AwarenessUpdate = 3,
    /// <summary>"Tell me who is here."</summary>
    AwarenessQuery = 4,
    /// <summary>An authentication or authorization result.</summary>
    Auth = 5,
    /// <summary>A liveness probe.</summary>
    Ping = 6,
    /// <summary>A liveness response.</summary>
    Pong = 7,
    /// <summary>
    /// "Having answered your <see cref="SyncStep1"/>, here is what I have." Answered with
    /// <see cref="SyncStep2"/> only, which is what makes the exchange terminate.
    /// </summary>
    SyncStep1Reply = 8,
}

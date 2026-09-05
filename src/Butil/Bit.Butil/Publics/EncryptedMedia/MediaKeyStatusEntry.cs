namespace Bit.Butil;

/// <summary>
/// One key in a session and what it can do, an entry of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/keyStatuses">MediaKeySession.keyStatuses</see>.
/// </summary>
/// <param name="KeyId">
/// The key id as lowercase hex. Key ids are opaque binary in the browser; hex is how licence servers,
/// manifests and logs refer to them, so it is what crosses the boundary.
/// </param>
/// <param name="Status">What this key can currently decrypt, if anything.</param>
public record MediaKeyStatusEntry(string KeyId, MediaKeyStatus Status);

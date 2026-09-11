namespace Bit.Butil;

/// <summary>
/// The initialization data a media element found in its stream, from the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/encrypted_event">encrypted</see>
/// event.
/// </summary>
/// <remarks>
/// This is the trigger of the whole EME dance: the element hit encrypted content, and these bytes -
/// a PSSH box, a key-id list - are what <see cref="MediaKeySessionHandle.GenerateRequest(EncryptedMediaInitData)"/> turns
/// into a licence request. The same stream can raise it more than once (one per PSSH box), so a
/// player normally ignores repeats for key ids it already has a licence for.
/// </remarks>
/// <param name="InitDataType">The format of the data, e.g. <c>"cenc"</c>, <c>"keyids"</c> or <c>"webm"</c>.</param>
/// <param name="InitData">The initialization data itself, to be passed on unchanged.</param>
public record EncryptedMediaInitData(string InitDataType, byte[] InitData);

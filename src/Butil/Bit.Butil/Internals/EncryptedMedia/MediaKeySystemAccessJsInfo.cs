namespace Bit.Butil;

/// <summary>
/// What JS reports about a granted <c>MediaKeySystemAccess</c>: the key system that answered, and the
/// configuration it resolved. The access object itself stays in JS - it is not serializable, and
/// everything a caller can do with it is exposed through <see cref="MediaKeysHandle"/>.
/// </summary>
internal class MediaKeySystemAccessJsInfo
{
    public string KeySystem { get; set; } = string.Empty;

    public MediaKeySystemJsConfiguration? Configuration { get; set; }
}

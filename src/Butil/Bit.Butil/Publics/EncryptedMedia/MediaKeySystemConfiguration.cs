using System.Linq;

namespace Bit.Butil;

/// <summary>
/// What a key system has to be able to do for the app, mirroring the configuration
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/requestMediaKeySystemAccess">Navigator.requestMediaKeySystemAccess()</see>
/// takes.
/// </summary>
/// <remarks>
/// Configurations are offered in preference order: the browser takes the first one it can satisfy,
/// drops the capabilities it can't, and hands back what is left - which is why
/// <see cref="EncryptedMedia.IsKeySystemSupported"/> is worth calling for its answer rather than for
/// a yes/no. At least one of <see cref="AudioCapabilities"/> and <see cref="VideoCapabilities"/> has
/// to be non-empty, or nothing matches.
/// </remarks>
public class MediaKeySystemConfiguration
{
    /// <summary>A name for this configuration, echoed back in the resolved one so you can tell which offer was accepted.</summary>
    public string? Label { get; set; }

    /// <summary>The initialization-data formats the content uses, e.g. <c>cenc</c>, <c>keyids</c> or <c>webm</c>.</summary>
    public string[]? InitDataTypes { get; set; }

    /// <summary>The audio codecs and robustness levels that are acceptable, most preferred first.</summary>
    public MediaKeySystemMediaCapability[]? AudioCapabilities { get; set; }

    /// <summary>The video codecs and robustness levels that are acceptable, most preferred first.</summary>
    public MediaKeySystemMediaCapability[]? VideoCapabilities { get; set; }

    /// <summary>
    /// Whether the key system may identify this device to the licence server. Defaults to
    /// <see cref="MediaKeysRequirement.Optional"/> in the browser; asking for
    /// <see cref="MediaKeysRequirement.Required"/> can trigger a user consent prompt.
    /// </summary>
    public MediaKeysRequirement? DistinctiveIdentifier { get; set; }

    /// <summary>
    /// Whether the key system may store state (licences, records of released keys) on the device.
    /// Has to be <see cref="MediaKeysRequirement.Required"/> for
    /// <see cref="MediaKeySessionType.PersistentLicense"/> sessions.
    /// </summary>
    public MediaKeysRequirement? PersistentState { get; set; }

    /// <summary>The session types the app intends to create. Defaults to temporary sessions only.</summary>
    public MediaKeySessionType[]? SessionTypes { get; set; }

    internal MediaKeySystemJsConfiguration ToJsObject() => new()
    {
        Label = Label,
        InitDataTypes = InitDataTypes,
        AudioCapabilities = AudioCapabilities,
        VideoCapabilities = VideoCapabilities,
        DistinctiveIdentifier = ToName(DistinctiveIdentifier),
        PersistentState = ToName(PersistentState),
        SessionTypes = SessionTypes?.Select(ToName).ToArray()
    };

    internal static string ToName(MediaKeySessionType type)
        => type == MediaKeySessionType.PersistentLicense ? "persistent-license" : "temporary";

    private static string? ToName(MediaKeysRequirement? requirement) => requirement switch
    {
        MediaKeysRequirement.Required => "required",
        MediaKeysRequirement.Optional => "optional",
        MediaKeysRequirement.NotAllowed => "not-allowed",
        _ => null
    };

    internal static MediaKeySystemConfiguration FromJsObject(MediaKeySystemJsConfiguration source) => new()
    {
        Label = source.Label,
        InitDataTypes = source.InitDataTypes,
        AudioCapabilities = source.AudioCapabilities,
        VideoCapabilities = source.VideoCapabilities,
        DistinctiveIdentifier = ToRequirement(source.DistinctiveIdentifier),
        PersistentState = ToRequirement(source.PersistentState),
        SessionTypes = source.SessionTypes?.Select(ToSessionType).ToArray()
    };

    private static MediaKeysRequirement? ToRequirement(string? raw) => raw switch
    {
        "required" => MediaKeysRequirement.Required,
        "optional" => MediaKeysRequirement.Optional,
        "not-allowed" => MediaKeysRequirement.NotAllowed,
        _ => null
    };

    private static MediaKeySessionType ToSessionType(string? raw)
        => raw == "persistent-license" ? MediaKeySessionType.PersistentLicense : MediaKeySessionType.Temporary;
}

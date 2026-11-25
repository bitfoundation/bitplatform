using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitVirtualKeyboardJsRuntimeExtensions
{
    // The payloads are only ever constructed (never read) from C#, so without these hints the trimmer strips their getters
    // and the reflection-based interop serialization sends empty objects in trimmed builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitVirtualKeyboardJsOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitVirtualKeyboardJsKey))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitVirtualKeyboardJsStrings))]
    public static ValueTask<IJSObjectReference> BitVirtualKeyboardSetup(this IJSRuntime jsRuntime,
                                                                        ElementReference root,
                                                                        DotNetObjectReference<BitVirtualKeyboard> dotnetObj,
                                                                        BitVirtualKeyboardJsOptions options,
                                                                        ElementReference? target)
    {
        return jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.BitVirtualKeyboard.setup", root, dotnetObj, options, target);
    }
}

// What the script of a BitVirtualKeyboard is told (BitVirtualKeyboard.ts, BitVirtualKeyboardOptions), serialized to
// camelCase by the interop serializer.
internal sealed record BitVirtualKeyboardJsOptions
{
    public int Version { get; init; }
    public int KeysVersion { get; init; }
    public string LayoutId { get; init; } = "";
    public string BaseLayer { get; init; } = "";
    public string? ShiftLayer { get; init; }
    public string? CapsLayer { get; init; }
    public string? AltGrLayer { get; init; }
    public string? ShiftAltGrLayer { get; init; }
    public BitVirtualKeyboardJsKey[] Keys { get; init; } = [];
    public IReadOnlyDictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = new Dictionary<string, Dictionary<string, string>>();
    public bool Docked { get; init; }
    public bool Open { get; init; }
    public bool ValueMode { get; init; }
    public bool FollowFocus { get; init; }
    public string? TargetSelector { get; init; }
    public string EnterBehavior { get; init; } = "";
    public string? AllowedCharacters { get; init; }
    public int? MaxLength { get; init; }
    public bool Secure { get; init; }
    public bool ShowKeyPreview { get; init; }
    public bool Sound { get; init; }
    public bool Haptics { get; init; }
    public bool HasKeyPressHandler { get; init; }
    public bool HasInputHandler { get; init; }
    public int LongPressDelay { get; init; }
    public int MultiTapTimeout { get; init; }
    public int RepeatDelay { get; init; }
    public int RepeatInterval { get; init; }
    public bool AutoForm { get; init; }
    public bool Compact { get; init; }
    public double CompactBreakpoint { get; init; }
    public bool ReserveSpace { get; init; }
    public bool Disabled { get; init; }
    public bool SetValue { get; init; }
    public string? Value { get; init; }
    public long ValueSeq { get; init; }
    public string? ActiveKeyClass { get; init; }
    public string? ActiveKeyStyle { get; init; }
    public string? PopupKeyClass { get; init; }
    public string? PopupKeyStyle { get; init; }
    public BitVirtualKeyboardJsStrings Strings { get; init; } = new();
}

// A key, by its index in the layout (data-bit-vkb-k): its id, the layer it is on, its action and what it acts with.
internal sealed record BitVirtualKeyboardJsKey
{
    public string Id { get; init; } = "";
    public string Ly { get; init; } = "";
    public string A { get; init; } = "";
    public string? O { get; init; }
    public string? L { get; init; }
    public string? Nl { get; init; }
    public string? Dk { get; init; }
    public string? C { get; init; }
    public IReadOnlyList<string>? Lp { get; init; }
    public string? Lpd { get; init; }
    public IReadOnlyList<string>? Mt { get; init; }
    public bool R { get; init; }
    public bool P { get; init; }
}

internal sealed record BitVirtualKeyboardJsStrings
{
    public string Alternates { get; init; } = "";
    public string LayerAnnouncement { get; init; } = "";
    public string ShiftOn { get; init; } = "";
    public string ShiftLocked { get; init; } = "";
    public string ShiftOff { get; init; } = "";
    public string CapsLockOn { get; init; } = "";
    public string CapsLockOff { get; init; } = "";
    public Dictionary<string, string> LayerNames { get; init; } = [];
}

// What the script tells of the input it attached to.
internal sealed record BitVirtualKeyboardJsTargetInfo
{
    public string? Id { get; init; }
    public string? Type { get; init; }
    public string? InputMode { get; init; }
    public string? EnterKeyHint { get; init; }
    public string? Purpose { get; init; }
    public bool Password { get; init; }
}

using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// Everything a view is set to: data state, theme, culture, sign-in, device, store backdrop, design option, switches and look.
/// It round-trips through the query string, which is how links, previews and screenshots carry it.
/// </summary>
public sealed partial record WorkbenchScenario
{
    public const int MaxCaptionLength = 120;

    /// <summary>The query-string prefix of page switches, as in sw-amount=few.</summary>
    public const string SwitchQueryPrefix = "sw-";

    /// <summary>The text sizes a view can be set to, in percent.</summary>
    public static readonly int[] TextScales = [100, 130, 200];

    public WorkbenchDataState Data { get; init; } = WorkbenchDataState.Live;

    public WorkbenchThemeMode Theme { get; init; } = WorkbenchThemeMode.Light;

    public string? Culture { get; init; }

    /// <summary>Whether the app's navigation, header and bottom bar are drawn around the page.</summary>
    public bool Chrome { get; init; } = true;

    public WorkbenchAuthState Auth { get; init; } = WorkbenchAuthState.MultiAccount;

    /// <summary>The index of the signed-in demo user in <see cref="WorkbenchDemoUsers.All"/>.</summary>
    public int User { get; init; }

    public string Device { get; init; } = DevicePresets.DefaultKey;

    /// <summary>The custom device's width; used only when <see cref="Device"/> is the custom key.</summary>
    public int Width { get; init; }

    /// <summary>The custom device's height; used only when <see cref="Device"/> is the custom key.</summary>
    public int Height { get; init; }

    public bool Rotated { get; init; }

    /// <summary>Whether the device is drawn around the screen.</summary>
    public bool Frame { get; init; } = true;

    /// <summary>The background of a store screenshot; null shows the page without one.</summary>
    public WorkbenchBackdrop? Backdrop { get; init; }

    /// <summary>A caption typed for the store screenshot, shown as written in every language.</summary>
    public string? Caption { get; init; }

    /// <summary>The key of the page's design option.</summary>
    public string? Variant { get; init; }

    /// <summary>The page switches, as key=value pairs sorted by key and separated by semicolons.</summary>
    public string? Switches { get; init; }

    /// <summary>Another primary color to try on this view only, as #rrggbb.</summary>
    public string? Accent { get; init; }

    public int TextScale { get; init; } = 100;

    public bool ReducedMotion { get; init; }

    public WorkbenchVision? Vision { get; init; }

    [JsonIgnore] public DevicePreset DevicePreset => Device == DevicePresets.CustomKey ? DevicePresets.Custom(Width, Height) : DevicePresets.Find(Device);

    [JsonIgnore] public bool IsRtl => Culture is not null && CultureInfoManager.GetCultureInfo(Culture)?.TextInfo.IsRightToLeft is true;

    public string? GetSwitch(string key) => ReadSwitches(Switches).GetValueOrDefault(key);

    /// <summary>A copy with the switch set, or removed when the value is null.</summary>
    public WorkbenchScenario WithSwitch(string key, string? value)
    {
        var switches = ReadSwitches(Switches);

        if (value is null)
        {
            switches.Remove(key);
        }
        else
        {
            switches[key] = value;
        }

        return this with { Switches = WriteSwitches(switches) };
    }

    /// <summary>A copy on a custom device of this size, clamped to the custom size limits.</summary>
    public WorkbenchScenario WithSize(int width, int height)
    {
        var custom = DevicePresets.Custom(width, height);

        return this with { Device = DevicePresets.CustomKey, Width = custom.Width, Height = custom.Height, Rotated = false };
    }

    /// <summary>Reads a scenario from a query string; a value that is missing or invalid keeps its default.</summary>
    public static WorkbenchScenario Parse(string? query)
    {
        var values = QueryHelpers.ParseQuery(query);
        var defaults = new WorkbenchScenario();

        string? Read(string key) => values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false ? value.ToString() : null;

        // An enum value given by its member's name, in any case; a number or a list of names names none.
        T? Member<T>(string key) where T : struct, Enum => Enum.GetNames<T>().FirstOrDefault(name => string.Equals(name, Read(key), StringComparison.OrdinalIgnoreCase)) is { } name ? Enum.Parse<T>(name) : null;

        var caption = Read("caption")?.Trim();
        var device = Read("device");
        var (width, height) = ReadSize(Read("size"));

        var switches = values.Where(pair => pair.Key.StartsWith(SwitchQueryPrefix, StringComparison.Ordinal))
                             .ToDictionary(pair => pair.Key[SwitchQueryPrefix.Length..], pair => pair.Value.ToString());

        var scenario = new WorkbenchScenario
        {
            Data = Member<WorkbenchDataState>("data") ?? defaults.Data,
            Theme = Member<WorkbenchThemeMode>("theme") ?? defaults.Theme,
            Culture = CultureInfoManager.GetCultureInfo(Read("culture"))?.Name,
            Chrome = bool.TryParse(Read("chrome"), out var chrome) ? chrome : defaults.Chrome,
            Auth = Member<WorkbenchAuthState>("auth") ?? defaults.Auth,
            User = int.TryParse(Read("user"), NumberStyles.None, CultureInfo.InvariantCulture, out var user) && user < WorkbenchDemoUsers.All.Count ? user : defaults.User,
            Device = string.Equals(device, DevicePresets.CustomKey, StringComparison.OrdinalIgnoreCase) ? DevicePresets.CustomKey : DevicePresets.Find(device).Key,
            Rotated = bool.TryParse(Read("rotated"), out var rotated) ? rotated : defaults.Rotated,
            Frame = bool.TryParse(Read("frame"), out var frame) ? frame : defaults.Frame,
            Backdrop = Member<WorkbenchBackdrop>("backdrop"),
            Caption = caption is null ? null : caption[..Math.Min(caption.Length, MaxCaptionLength)],
            Variant = Read("variant") is { } variant && TokenRegex().IsMatch(variant) ? variant : null,
            Switches = WriteSwitches(switches),
            Accent = ReadAccent(Read("accent")),
            TextScale = int.TryParse(Read("text"), NumberStyles.None, CultureInfo.InvariantCulture, out var text) && TextScales.Contains(text) ? text : defaults.TextScale,
            ReducedMotion = Read("motion") is "reduce",
            Vision = Member<WorkbenchVision>("vision")
        };

        // A custom device takes its size from the size key and keeps its rotation.
        return scenario.Device == DevicePresets.CustomKey ? scenario.WithSize(width, height) with { Rotated = scenario.Rotated } : scenario;
    }

    /// <summary>Writes only the values that differ from the defaults, so the default view has an empty query string.</summary>
    public string ToQueryString()
    {
        var defaults = new WorkbenchScenario();
        var values = new Dictionary<string, string?>();

        if (Data != defaults.Data) values["data"] = Data.ToString().ToLowerInvariant();
        if (Theme != defaults.Theme) values["theme"] = Theme.ToString().ToLowerInvariant();
        if (Culture is not null) values["culture"] = Culture;
        if (Chrome != defaults.Chrome) values["chrome"] = Chrome.ToString().ToLowerInvariant();
        if (Auth != defaults.Auth) values["auth"] = Auth.ToString().ToLowerInvariant();
        if (User != defaults.User) values["user"] = User.ToString(CultureInfo.InvariantCulture);
        if (Device != defaults.Device) values["device"] = Device;
        if (Device == DevicePresets.CustomKey) values["size"] = FormattableString.Invariant($"{DevicePreset.Width}x{DevicePreset.Height}");
        if (Rotated != defaults.Rotated) values["rotated"] = Rotated.ToString().ToLowerInvariant();
        if (Frame != defaults.Frame) values["frame"] = Frame.ToString().ToLowerInvariant();
        if (Backdrop is not null) values["backdrop"] = Backdrop.Value.ToString().ToLowerInvariant();
        if (Backdrop is not null && string.IsNullOrWhiteSpace(Caption) is false) values["caption"] = Caption;
        if (Variant is not null) values["variant"] = Variant;

        foreach (var (key, value) in ReadSwitches(Switches))
        {
            values[$"{SwitchQueryPrefix}{key}"] = value;
        }

        if (Accent is not null) values["accent"] = Accent.TrimStart('#');
        if (TextScale != defaults.TextScale) values["text"] = TextScale.ToString(CultureInfo.InvariantCulture);
        if (ReducedMotion) values["motion"] = "reduce";
        if (Vision is not null) values["vision"] = Vision.Value.ToString().ToLowerInvariant();

        return QueryHelpers.AddQueryString(string.Empty, values);
    }

    /// <summary>Turns a hex color, with or without #, into #rrggbb, or null when it is not one.</summary>
    public static string? ReadAccent(string? value)
    {
        var hex = value?.Trim().TrimStart('#');

        return hex is not null && HexColorRegex().IsMatch(hex) ? $"#{hex.ToLowerInvariant()}" : null;
    }

    private static (int Width, int Height) ReadSize(string? value)
    {
        var parts = value?.Split('x', 2) ?? [];

        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height)
            ? (width, height)
            : (0, 0);
    }

    private static Dictionary<string, string> ReadSwitches(string? switches)
    {
        return (switches ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(pair => pair.Split('=', 2))
                                         .Where(pair => pair.Length == 2)
                                         .ToDictionary(pair => pair[0], pair => pair[1]);
    }

    /// <summary>Keeps keys and values that are short lowercase tokens, sorted so equal switches give equal strings.</summary>
    private static string? WriteSwitches(Dictionary<string, string> switches)
    {
        var valid = switches.Where(pair => TokenRegex().IsMatch(pair.Key) && TokenRegex().IsMatch(pair.Value))
                            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => $"{pair.Key}={pair.Value}")
                            .ToArray();

        return valid.Length == 0 ? null : string.Join(';', valid);
    }

    [GeneratedRegex("^[a-z0-9-]{1,32}$")]
    private static partial Regex TokenRegex();

    [GeneratedRegex("^[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorRegex();
}

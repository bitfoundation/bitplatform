using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed partial record WorkbenchScenario
{
    public const int MaxCaptionLength = 120;

    public const string SwitchQueryPrefix = "sw-";

    public static readonly int[] TextScales = [100, 130, 200];

    public WorkbenchDataState Data { get; init; } = WorkbenchDataState.Live;

    public WorkbenchThemeMode Theme { get; init; } = WorkbenchThemeMode.Light;

    public string? Culture { get; init; }

    public bool Chrome { get; init; } = true;

    public WorkbenchAuthState Auth { get; init; } = WorkbenchAuthState.SignedIn;

    public int User { get; init; }

    public string Device { get; init; } = DevicePresets.DefaultKey;

    public int Width { get; init; }

    public int Height { get; init; }

    public bool Rotated { get; init; }

    public bool Frame { get; init; } = true;

    public WorkbenchBackdrop? Backdrop { get; init; }

    public string? Caption { get; init; }

    public string? Variant { get; init; }

    public string? Switches { get; init; }

    public string? Accent { get; init; }

    public int TextScale { get; init; } = 100;

    public bool ReducedMotion { get; init; }

    public WorkbenchVision? Vision { get; init; }

    [JsonIgnore] public DevicePreset DevicePreset => Device == DevicePresets.CustomKey ? DevicePresets.Custom(Width, Height) : DevicePresets.Find(Device);

    [JsonIgnore] public bool IsRtl => Culture is not null && CultureInfoManager.GetCultureInfo(Culture)?.TextInfo.IsRightToLeft is true;

    [JsonIgnore] public bool HasLightBackdrop => Backdrop is WorkbenchBackdrop.Paper;

    public string? GetSwitch(string key) => ReadSwitches(Switches).GetValueOrDefault(key);

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

    public WorkbenchScenario WithSize(int width, int height)
    {
        var custom = DevicePresets.Custom(width, height);

        return this with { Device = DevicePresets.CustomKey, Width = custom.Width, Height = custom.Height, Rotated = false };
    }

    public static WorkbenchScenario Parse(string? query)
    {
        var values = QueryHelpers.ParseQuery(query);
        var defaults = new WorkbenchScenario();

        string? Read(string key) => values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false ? value.ToString() : null;

        var caption = Read("caption")?.Trim();
        var device = Read("device");
        var (width, height) = ReadSize(Read("size"));

        var switches = values.Where(pair => pair.Key.StartsWith(SwitchQueryPrefix, StringComparison.Ordinal))
                             .ToDictionary(pair => pair.Key[SwitchQueryPrefix.Length..], pair => pair.Value.ToString());

        var scenario = new WorkbenchScenario
        {
            Data = Enum.TryParse<WorkbenchDataState>(Read("data"), ignoreCase: true, out var data) ? data : defaults.Data,
            Theme = Enum.TryParse<WorkbenchThemeMode>(Read("theme"), ignoreCase: true, out var theme) ? theme : defaults.Theme,
            Culture = CultureInfoManager.GetCultureInfo(Read("culture"))?.Name,
            Chrome = bool.TryParse(Read("chrome"), out var chrome) ? chrome : defaults.Chrome,
            Auth = Enum.TryParse<WorkbenchAuthState>(Read("auth"), ignoreCase: true, out var auth) ? auth : defaults.Auth,
            User = int.TryParse(Read("user"), NumberStyles.None, CultureInfo.InvariantCulture, out var user) && user < WorkbenchDemoUsers.All.Count ? user : defaults.User,
            Device = string.Equals(device, DevicePresets.CustomKey, StringComparison.OrdinalIgnoreCase) ? DevicePresets.CustomKey : DevicePresets.Find(device).Key,
            Rotated = bool.TryParse(Read("rotated"), out var rotated) ? rotated : defaults.Rotated,
            Frame = bool.TryParse(Read("frame"), out var frame) ? frame : defaults.Frame,
            Backdrop = Enum.TryParse<WorkbenchBackdrop>(Read("backdrop"), ignoreCase: true, out var backdrop) ? backdrop : null,
            Caption = caption is null ? null : caption[..Math.Min(caption.Length, MaxCaptionLength)],
            Variant = Read("variant") is { } variant && TokenRegex().IsMatch(variant) ? variant : null,
            Switches = WriteSwitches(switches),
            Accent = ReadAccent(Read("accent")),
            TextScale = int.TryParse(Read("text"), NumberStyles.None, CultureInfo.InvariantCulture, out var text) && TextScales.Contains(text) ? text : defaults.TextScale,
            ReducedMotion = Read("motion") is "reduce",
            Vision = Enum.TryParse<WorkbenchVision>(Read("vision"), ignoreCase: true, out var vision) ? vision : null
        };

        return scenario.Device == DevicePresets.CustomKey ? scenario.WithSize(width, height) with { Rotated = scenario.Rotated } : scenario;
    }

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

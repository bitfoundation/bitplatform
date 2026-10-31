using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchScenario
{
    public const int MaxCaptionLength = 120;

    public WorkbenchDataState Data { get; init; } = WorkbenchDataState.Live;

    public WorkbenchThemeMode Theme { get; init; } = WorkbenchThemeMode.Light;

    public string? Culture { get; init; }

    public bool Chrome { get; init; } = true;

    public WorkbenchAuthState Auth { get; init; } = WorkbenchAuthState.SignedIn;

    public string Device { get; init; } = DevicePresets.DefaultKey;

    public bool Rotated { get; init; }

    public bool Frame { get; init; } = true;

    public WorkbenchBackdrop? Backdrop { get; init; }

    public string? Caption { get; init; }

    [JsonIgnore] public DevicePreset DevicePreset => DevicePresets.Find(Device);

    [JsonIgnore] public bool IsRtl => Culture is not null && CultureInfoManager.GetCultureInfo(Culture)?.TextInfo.IsRightToLeft is true;

    [JsonIgnore] public bool HasLightBackdrop => Backdrop is WorkbenchBackdrop.Paper;

    public static WorkbenchScenario Parse(string? query)
    {
        var values = QueryHelpers.ParseQuery(query);
        var defaults = new WorkbenchScenario();

        string? Read(string key) => values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false ? value.ToString() : null;

        var caption = Read("caption")?.Trim();

        return new()
        {
            Data = Enum.TryParse<WorkbenchDataState>(Read("data"), ignoreCase: true, out var data) ? data : defaults.Data,
            Theme = Enum.TryParse<WorkbenchThemeMode>(Read("theme"), ignoreCase: true, out var theme) ? theme : defaults.Theme,
            Culture = CultureInfoManager.GetCultureInfo(Read("culture"))?.Name,
            Chrome = bool.TryParse(Read("chrome"), out var chrome) ? chrome : defaults.Chrome,
            Auth = Enum.TryParse<WorkbenchAuthState>(Read("auth"), ignoreCase: true, out var auth) ? auth : defaults.Auth,
            Device = DevicePresets.Find(Read("device")).Key,
            Rotated = bool.TryParse(Read("rotated"), out var rotated) ? rotated : defaults.Rotated,
            Frame = bool.TryParse(Read("frame"), out var frame) ? frame : defaults.Frame,
            Backdrop = Enum.TryParse<WorkbenchBackdrop>(Read("backdrop"), ignoreCase: true, out var backdrop) ? backdrop : null,
            Caption = caption is null ? null : caption[..Math.Min(caption.Length, MaxCaptionLength)]
        };
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
        if (Device != defaults.Device) values["device"] = Device;
        if (Rotated != defaults.Rotated) values["rotated"] = Rotated.ToString().ToLowerInvariant();
        if (Frame != defaults.Frame) values["frame"] = Frame.ToString().ToLowerInvariant();
        if (Backdrop is not null) values["backdrop"] = Backdrop.Value.ToString().ToLowerInvariant();
        if (Backdrop is not null && string.IsNullOrWhiteSpace(Caption) is false) values["caption"] = Caption;

        return QueryHelpers.AddQueryString(string.Empty, values);
    }
}

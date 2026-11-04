namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class DeviceFrame
{
    [Parameter] public LandingDeviceKind Kind { get; set; }

    [Parameter] public string? Address { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}

public enum LandingDeviceKind
{
    Phone,
    Browser
}

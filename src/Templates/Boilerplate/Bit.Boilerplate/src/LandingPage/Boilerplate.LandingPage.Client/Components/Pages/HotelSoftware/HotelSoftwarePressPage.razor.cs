using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwarePressPage
{
    private static readonly (string Label, string Value)[] facts =
    [
        (nameof(HotelSoftwareStrings.Fact1Label), nameof(HotelSoftwareStrings.Fact1Value)),
        (nameof(HotelSoftwareStrings.Fact2Label), nameof(HotelSoftwareStrings.Fact2Value)),
        (nameof(HotelSoftwareStrings.Fact3Label), nameof(HotelSoftwareStrings.Fact3Value)),
        (nameof(HotelSoftwareStrings.Fact4Label), nameof(HotelSoftwareStrings.Fact4Value)),
        (nameof(HotelSoftwareStrings.Fact5Label), nameof(HotelSoftwareStrings.Fact5Value)),
        (nameof(HotelSoftwareStrings.Fact6Label), nameof(HotelSoftwareStrings.Fact6Value))
    ];

    private static readonly (string File, string Name, string Note, bool IsDark)[] logos =
    [
        ("lodgeboard-logo.svg", nameof(HotelSoftwareStrings.LogoFull), nameof(HotelSoftwareStrings.LogoFullNote), false),
        ("lodgeboard-logo-light.svg", nameof(HotelSoftwareStrings.LogoLight), nameof(HotelSoftwareStrings.LogoLightNote), true),
        ("lodgeboard-mark.svg", nameof(HotelSoftwareStrings.LogoMark), nameof(HotelSoftwareStrings.LogoMarkNote), false)
    ];

    private static readonly (string Hex, string Name)[] colors =
    [
        ("#1E2124", nameof(HotelSoftwareStrings.ColorCharcoal)),
        ("#E0B54A", nameof(HotelSoftwareStrings.ColorBrass)),
        ("#F3F1EC", nameof(HotelSoftwareStrings.ColorStone))
    ];

    private static readonly (string Name, string Role, string Bio)[] founders =
    [
        (nameof(HotelSoftwareStrings.Founder1Name), nameof(HotelSoftwareStrings.Founder1Role), nameof(HotelSoftwareStrings.Founder1Bio)),
        (nameof(HotelSoftwareStrings.Founder2Name), nameof(HotelSoftwareStrings.Founder2Role), nameof(HotelSoftwareStrings.Founder2Bio))
    ];

    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private bool copied;

    private async Task CopyText()
    {
        await clipboard.WriteText(H[nameof(HotelSoftwareStrings.PressAboutText)]);
        copied = true;
    }

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}

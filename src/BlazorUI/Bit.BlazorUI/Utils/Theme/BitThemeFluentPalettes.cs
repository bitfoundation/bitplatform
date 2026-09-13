using System;

namespace Bit.BlazorUI;

/// <summary>
/// The packaged Fluent palettes, verbatim, as the template <see cref="BitThemeSeedPalette"/> transforms.
/// </summary>
/// <remarks>
/// <para>
/// Generating a whole theme from one color means answering "what should this token be" for two
/// hundred tokens. The packaged palettes already answer it - they are hand-solved against the WCAG
/// floors documented in their own stylesheets - so the seed factory transforms THEM rather than
/// re-deriving a palette from step constants. That is what makes seeding the packaged primary
/// reproduce the packaged palettes byte for byte instead of approximately, and it means every other
/// seed inherits relationships that were solved by hand rather than fitted by formula.
/// </para>
/// <para>
/// This file is generated from Styles/Fluent/*.scss and must not be hand-edited;
/// BitThemeFluentPaletteContractTests re-parses those stylesheets and fails if the two ever drift.
/// Slot order matches BitThemeColorVariants / BitThemeGeneralColorVariants exactly - see the
/// RoleSlots and TierSlots comments below.
/// </para>
/// </remarks>
internal static class BitThemeFluentPalettes
{
    // main, hover, active | dark, dark-hover, dark-active | light, light-hover, light-active | text, dis, dis-text
    internal const int RoleSlots = 12;

    // As above, minus the on-color: the neutral families have no '-text' slot of their own.
    internal const int TierSlots = 11;

    // ── Light ──────────────────────────────────────────────────────────────────────────────

    internal static readonly string[] LightPri =
    [
        "#1276C6", "#0B6AB4", "#0460A5", "#005391",
        "#00487F", "#003E70", "#85C1FF", "#74B4F6",
        "#66A9EE", "#FFFFFF", "#DDEDFF", "#71869B",
    ];
    internal static readonly string[] LightSec =
    [
        "#FD7F36", "#F27832", "#E9732E", "#DD6C2A",
        "#D56726", "#CD6323", "#FFC7AC", "#FFB490",
        "#FFA375", "#141414", "#FDE6DA", "#987C6F",
    ];
    internal static readonly string[] LightTer =
    [
        "#424242", "#383838", "#2F2F2F", "#242424",
        "#1B1B1B", "#131313", "#9E9E9E", "#929292",
        "#878787", "#FFFFFF", "#EBEBEB", "#838383",
    ];
    internal static readonly string[] LightInf =
    [
        "#6B737C", "#60686F", "#575E65", "#4B5158",
        "#41464C", "#383D42", "#B6BCC3", "#A9B0B7",
        "#9EA5AC", "#FFFFFF", "#E3ECF7", "#7C848D",
    ];
    internal static readonly string[] LightSuc =
    [
        "#228422", "#1B771C", "#156B16", "#0D5D0F",
        "#065108", "#014604", "#8CCD88", "#7CC178",
        "#6FB66B", "#FFFFFF", "#E0F1DF", "#768A74",
    ];
    internal static readonly string[] LightWrn =
    [
        "#EDAE12", "#DDA206", "#D09700", "#BF8B00",
        "#B48300", "#A97A00", "#FFDC9D", "#FECC6B",
        "#F7BF4E", "#141414", "#F5EAD5", "#8F8168",
    ];
    internal static readonly string[] LightSwr =
    [
        "#CE4207", "#BC3A01", "#AC3400", "#972D00",
        "#852600", "#762100", "#FFA488", "#FE8F6D",
        "#F6825E", "#FFFFFF", "#FEE5DD", "#997B72",
    ];
    internal static readonly string[] LightErr =
    [
        "#D2393B", "#C03234", "#B02B2D", "#9D2225",
        "#8B1A1E", "#7D1418", "#FFA29B", "#FF8D85",
        "#FA7C75", "#FFFFFF", "#FFE4E1", "#9A7A77",
    ];
    internal static readonly string[] LightFgPri =
    [
        "#1A1A1A", "#121212", "#0A0A0A", "#0D0D0D",
        "#080808", "#000000", "#2E2E2E", "#262626",
        "#1F1F1F", "#EBEBEB", "#858585",
    ];
    internal static readonly string[] LightFgSec =
    [
        "#525252", "#454545", "#383838", "#3D3D3D",
        "#303030", "#242424", "#6B6B6B", "#5E5E5E",
        "#4D4D4D", "#EBEBEB", "#858585",
    ];
    internal static readonly string[] LightFgTer =
    [
        "#707070", "#636363", "#575757", "#5C5C5C",
        "#4F4F4F", "#424242", "#8A8A8A", "#7D7D7D",
        "#696969", "#EBEBEB", "#858585",
    ];
    internal static readonly string[] LightBgPri =
    [
        "#FFFFFF", "#F5F5F5", "#EBEBEB", "#EDEDED",
        "#E3E3E3", "#D9D9D9", "#FFFFFF", "#F5F5F5",
        "#EBEBEB", "#F7F7F7", "#8C8C8C",
    ];
    internal static readonly string[] LightBgSec =
    [
        "#F5F5F5", "#EDEDED", "#E6E6E6", "#E8E8E8",
        "#E0E0E0", "#D9D9D9", "#FCFCFC", "#F7F7F7",
        "#F0F0F0", "#EDEDED", "#878787",
    ];
    internal static readonly string[] LightBgTer =
    [
        "#EBEBEB", "#E3E3E3", "#DBDBDB", "#DEDEDE",
        "#D6D6D6", "#CFCFCF", "#F5F5F5", "#F0F0F0",
        "#E6E6E6", "#E3E3E3", "#808080",
    ];
    internal static readonly string[] LightBrdPri =
    [
        "#707070", "#616161", "#525252", "#595959",
        "#4A4A4A", "#3B3B3B", "#8C8C8C", "#7D7D7D",
        "#696969", "#EBEBEB", "#858585",
    ];
    internal static readonly string[] LightBrdSec =
    [
        "#ADADAD", "#9E9E9E", "#8F8F8F", "#969696",
        "#878787", "#787878", "#C7C7C7", "#B8B8B8",
        "#A6A6A6", "#EBEBEB", "#858585",
    ];
    internal static readonly string[] LightBrdTer =
    [
        "#E0E0E0", "#D1D1D1", "#C2C2C2", "#C9C9C9",
        "#BABABA", "#ABABAB", "#F2F2F2", "#E8E8E8",
        "#D9D9D9", "#EBEBEB", "#858585",
    ];
    internal const string LightFgDisabled = "#8F8F8F";
    internal const string LightBgDisabled = "#F0F0F0";
    internal const string LightBrdDisabled = "#E0E0E0";
    internal const string LightRequired = "#A4262C";
    internal const string LightOverlay = "rgba(0, 0, 0, 0.4)";

    // ── Dark ──────────────────────────────────────────────────────────────────────────────

    internal static readonly string[] DarkPri =
    [
        "#4FA3F4", "#61AEFB", "#6EB6FF", "#3E87CE",
        "#4492DD", "#499AE7", "#89C3FF", "#9FCEFF",
        "#AED5FF", "#141414", "#222F3C", "#697D92",
    ];
    internal static readonly string[] DarkSec =
    [
        "#F49666", "#FBA377", "#FFAE86", "#D07D53",
        "#DE875A", "#E88D60", "#FFC1A3", "#FFD1BB",
        "#FFDCCC", "#141414", "#3B2921", "#8F7466",
    ];
    internal static readonly string[] DarkTer =
    [
        "#DEDEDE", "#E4E4E4", "#E8E8E8", "#C1C1C1",
        "#CCCCCC", "#D4D4D4", "#ECECEC", "#F2F2F2",
        "#F7F7F7", "#141414", "#2E2E2E", "#7A7A7A",
    ];
    internal static readonly string[] DarkInf =
    [
        "#A4ACB4", "#B0B7BE", "#B8BEC6", "#8A9097",
        "#949BA2", "#9BA2AA", "#C5CBD2", "#D1D6DC",
        "#D9DFE4", "#141414", "#282F35", "#747B83",
    ];
    internal static readonly string[] DarkSuc =
    [
        "#78B774", "#86C182", "#90C98C", "#62995F",
        "#6BA567", "#71AD6D", "#A0D49D", "#AFDEAB",
        "#B9E6B5", "#141414", "#253224", "#6D816B",
    ];
    internal static readonly string[] DarkWrn =
    [
        "#F0BB52", "#F7C86D", "#FCD17F", "#CE9F41",
        "#DBAA48", "#E4B24C", "#FFDA97", "#FFE8C0",
        "#FFF2DB", "#141414", "#352C1C", "#867860",
    ];
    internal static readonly string[] DarkSwr =
    [
        "#F79273", "#FEA083", "#FFAD93", "#D27A5F",
        "#E18366", "#EB8A6C", "#FFC0AC", "#FFD0C2",
        "#FFDCD1", "#141414", "#3B2923", "#907369",
    ];
    internal static readonly string[] DarkErr =
    [
        "#E67A73", "#EF8880", "#F4918A", "#C1635D",
        "#CF6C66", "#DA726C", "#FEA199", "#FFB2AB",
        "#FFBEB8", "#141414", "#3C2826", "#91726E",
    ];
    internal static readonly string[] DarkFgPri =
    [
        "#E6ECF2", "#EDF2F8", "#F2F7FD", "#D3D8DE",
        "#DBE0E6", "#E1E7ED", "#F4F9FF", "#F9FCFF",
        "#FEFEFF", "#2B2B2C", "#747577",
    ];
    internal static readonly string[] DarkFgSec =
    [
        "#B9BEC4", "#C3C8CE", "#CBD0D6", "#A3A8AE",
        "#ACB2B7", "#B4BABF", "#CFD5DB", "#D9DFE5",
        "#E1E7ED", "#2B2B2C", "#747577",
    ];
    internal static readonly string[] DarkFgTer =
    [
        "#8E9398", "#979CA1", "#9EA4A9", "#797E83",
        "#82878C", "#898E94", "#A3A8AE", "#ACB2B7",
        "#B4BABF", "#2B2B2C", "#747577",
    ];
    internal static readonly string[] DarkBgPri =
    [
        "#0F1318", "#151A1F", "#1A1F24", "#070B0F",
        "#0D1116", "#11151A", "#181C21", "#1F2329",
        "#23282D", "#0C0C0D", "#5E5F60",
    ];
    internal static readonly string[] DarkBgSec =
    [
        "#1B2025", "#22272C", "#272C31", "#12171B",
        "#191D22", "#1D2227", "#25292F", "#2C3136",
        "#31363B", "#181819", "#656667",
    ];
    internal static readonly string[] DarkBgTer =
    [
        "#272C31", "#2E3339", "#33393E", "#1D2227",
        "#25292F", "#292E34", "#31363B", "#383E43",
        "#3E4349", "#232425", "#6D6F70",
    ];
    internal static readonly string[] DarkBrdPri =
    [
        "#757B81", "#848A90", "#90969C", "#646A70",
        "#72787E", "#7E848A", "#878D93", "#969CA3",
        "#A2A8AF", "#2B2B2C", "#747577",
    ];
    internal static readonly string[] DarkBrdSec =
    [
        "#53595F", "#61676D", "#6C7278", "#43484E",
        "#50565C", "#5B6167", "#646A70", "#72787E",
        "#7E848A", "#2B2B2C", "#747577",
    ];
    internal static readonly string[] DarkBrdTer =
    [
        "#383E43", "#43484E", "#4B5056", "#292E34",
        "#33393E", "#3B4046", "#53595F", "#5E646A",
        "#676C73", "#2B2B2C", "#747577",
    ];
    internal const string DarkFgDisabled = "#5F6469";
    internal const string DarkBgDisabled = "#161B20";
    internal const string DarkBrdDisabled = "#2C3136";
    internal const string DarkRequired = "#E56167";
    internal const string DarkOverlay = "rgba(0, 0, 0, 0.6)";

    // The neutral ramp is declared once for both schemes (neutrals.fluent.scss targets :root).
    internal static readonly string[] Neutrals =
    [
        "#FFFFFF", "#000000", "#FAF9F8", "#F3F2F1",
        "#EDEBE9", "#E1DFDD", "#D2D0CE", "#C8C6C4",
        "#BEBBB8", "#B3B0AD", "#A19F9D", "#979593",
        "#8A8886", "#797775", "#605E5C", "#484644",
        "#3B3A39", "#323130", "#292827", "#252423",
        "#201F1E", "#1B1A19", "#161514", "#11100F",
    ];
}

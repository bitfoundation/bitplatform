namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.MediaQuery;

public partial class BitMediaQueryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "CascadingTheme",
            Type = "BitTheme?",
            DefaultValue = "null",
            Description = "The theme cascaded from an enclosing BitThemeProvider. Only its breakpoints are read, and only to resolve a ScreenQuery. " +
                          "They take precedence over the --bit-bp-* CSS variables of the rendered element, which is what keeps a scoped theme reachable " +
                          "when there is no element of the component's own to read them from (NoWrapper, or a usage with no content at all).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the element to render if the specified query is matched.",
        },
        new()
        {
            Name = "DefaultMatched",
            Type = "bool",
            DefaultValue = "false",
            Description = "The initial matched state to render with until the actual result of the query arrives from the browser. " +
                          "Useful to avoid a flash of the wrong content during prerendering, where the query cannot be evaluated yet. " +
                          "Ignored when IsMatched is bound, since the value handed over is then the initial state already.",
        },
        new()
        {
            Name = "IsMatched",
            Type = "bool",
            DefaultValue = "false",
            Description = "The current matched state of the provided query. This is an output of the component rather than an input: the browser owns " +
                          "the state, and the component writes the latest result it reports here. Set one way (without a Changed callback beside it) " +
                          "the value belongs to the page, which freezes the state; use DefaultMatched to seed it instead. (two-way bound)",
        },
        new()
        {
            Name = "Matched",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to be rendered if the provided query is matched (an alias for ChildContent).",
        },
        new()
        {
            Name = "NotMatched",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to be rendered if the provided query is not matched.",
        },
        new()
        {
            Name = "NoWrapper",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the active content directly, without the wrapping root element. " +
                          "Since no element is rendered, everything that describes one (class, style, id, dir, ...) is ignored.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The event callback to be called when the state of the media query has been changed. " +
                          "It is also called once with the initial matched state, right after the query gets evaluated by the browser for the first time.",
        },
        new()
        {
            Name = "Query",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the custom query to be matched. Any valid CSS media query is accepted, including non-viewport features " +
                          "such as orientation, pointer, or prefers-color-scheme. Takes precedence over ScreenQuery when both are provided.",
        },
        new()
        {
            Name = "ScreenQuery",
            Type = "BitScreenQuery?",
            DefaultValue = "null",
            Description = "Defines the screen query to be matched, amongst the predefined Bit screen media queries. " +
                          "The actual query is built at runtime from the live theme breakpoints (the --bit-bp-* CSS variables), " +
                          "so customized theme breakpoints are honored.",
            LinkType = LinkType.Link,
            Href = "#screen-query-enum"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "The content to be rendered for both states of the query, receiving the current matched state. Since it stays one fragment in " +
                          "one place of the render tree, the content is updated rather than built again when the query flips, so the state the components " +
                          "inside it hold survives the change. Takes precedence over Matched, ChildContent and NotMatched.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "screen-query-enum",
            Name = "BitScreenQuery",
            Description = "The predefined screen media queries in the bit BlazorUI. The actual query is built at runtime from the live theme breakpoints " +
                          "(the --bit-bp-* CSS variables), so customized theme breakpoints are honored; the pixel values below are the built-in defaults. " +
                          "The upper bound of a range sits a hundredth of a pixel below the next breakpoint, so no width falls between two neighboring members.",
            Items =
            [
                new()
                {
                    Name= "Xs",
                    Description="Extra small query: [@media screen and (max-width: 599.98px)]",
                    Value="0",
                },
                new()
                {
                    Name= "Sm",
                    Description="Small query: [@media screen and (min-width: 600px) and (max-width: 959.98px)]",
                    Value="1",
                },
                new()
                {
                    Name= "Md",
                    Description="Medium query: [@media screen and (min-width: 960px) and (max-width: 1279.98px)]",
                    Value="2",
                },
                new()
                {
                    Name= "Lg",
                    Description="Large query: [@media screen and (min-width: 1280px) and (max-width: 1919.98px)]",
                    Value="3",
                },
                new()
                {
                    Name= "Xl",
                    Description="Extra large query: [@media screen and (min-width: 1920px) and (max-width: 2559.98px)]",
                    Value="4",
                },
                new()
                {
                    Name= "Xxl",
                    Description="Extra extra large query: [@media screen and (min-width: 2560px)]",
                    Value="5",
                },
                new()
                {
                    Name= "LtSm",
                    Description="Less than small query: [@media screen and (max-width: 599.98px)]",
                    Value="6",
                },
                new()
                {
                    Name= "LtMd",
                    Description="Less than medium query: [@media screen and (max-width: 959.98px)]",
                    Value="7",
                },
                new()
                {
                    Name= "LtLg",
                    Description="Less than large query: [@media screen and (max-width: 1279.98px)]",
                    Value="8",
                },
                new()
                {
                    Name= "LtXl",
                    Description="Less than extra large query: [@media screen and (max-width: 1919.98px)]",
                    Value="9",
                },
                new()
                {
                    Name= "LtXxl",
                    Description="Less than extra extra large query: [@media screen and (max-width: 2559.98px)]",
                    Value="10",
                },
                new()
                {
                    Name= "GtXs",
                    Description="Greater than extra small query: [@media screen and (min-width: 600px)]",
                    Value="11",
                },
                new()
                {
                    Name= "GtSm",
                    Description="Greater than small query: [@media screen and (min-width: 960px)]",
                    Value="12",
                },
                new()
                {
                    Name= "GtMd",
                    Description="Greater than medium query: [@media screen and (min-width: 1280px)]",
                    Value="13",
                },
                new()
                {
                    Name= "GtLg",
                    Description="Greater than large query: [@media screen and (min-width: 1920px)]",
                    Value="14",
                },
                new()
                {
                    Name= "GtXl",
                    Description="Greater than extra large query: [@media screen and (min-width: 2560px)]",
                    Value="15",
                },
                new()
                {
                    Name= "SmToMd",
                    Description="Small through medium query: [@media screen and (min-width: 600px) and (max-width: 1279.98px)]",
                    Value="16",
                },
                new()
                {
                    Name= "SmToLg",
                    Description="Small through large query: [@media screen and (min-width: 600px) and (max-width: 1919.98px)]",
                    Value="17",
                },
                new()
                {
                    Name= "SmToXl",
                    Description="Small through extra large query: [@media screen and (min-width: 600px) and (max-width: 2559.98px)]",
                    Value="18",
                },
                new()
                {
                    Name= "MdToLg",
                    Description="Medium through large query: [@media screen and (min-width: 960px) and (max-width: 1919.98px)]",
                    Value="19",
                },
                new()
                {
                    Name= "MdToXl",
                    Description="Medium through extra large query: [@media screen and (min-width: 960px) and (max-width: 2559.98px)]",
                    Value="20",
                },
                new()
                {
                    Name= "LgToXl",
                    Description="Large through extra large query: [@media screen and (min-width: 1280px) and (max-width: 2559.98px)]",
                    Value="21",
                },
            ]
        }
    ];



    private bool isMatched;
    private int changeCount;
    private bool isSmallScreen;
    private BitMediaQuery? mediaQueryRef;
    private readonly BitTheme breakpointsTheme = new()
    {
        Layout = { Breakpoints = { Md = "700px", Lg = "900px" } }
    };

    private void HandleOnChange(bool value)
    {
        isMatched = value;
        changeCount++;
    }



    private string example1RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xs"">This is <b>Xs</b> (Extra Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Sm"">This is <b>Sm</b> (Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">This is <b>Md</b> (Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Lg"">This is <b>Lg</b> (Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xl"">This is <b>Xl</b> (Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xxl"">This is <b>Xxl</b> (Extra Extra Large).</BitMediaQuery>

<BitMediaQuery ScreenQuery=""BitScreenQuery.LtSm"">This is <b>LtSm</b> (Less Than Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"">This is <b>LtMd</b> (Less Than Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtLg"">This is <b>LtLg</b> (Less Than Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtXl"">This is <b>LtXl</b> (Less Than Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtXxl"">This is <b>LtXxl</b> (Less Than Extra Extra Large).</BitMediaQuery>

<BitMediaQuery ScreenQuery=""BitScreenQuery.GtXs"">This is <b>GtXs</b> (Greater Than Extra Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtSm"">This is <b>GtSm</b> (Greater Than Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtMd"">This is <b>GtMd</b> (Greater Than Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtLg"">This is <b>GtLg</b> (Greater Than Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtXl"">This is <b>GtXl</b> (Greater Than Extra Large).</BitMediaQuery>

<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToMd"">This is <b>SmToMd</b> (Small through Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToLg"">This is <b>SmToLg</b> (Small through Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToXl"">This is <b>SmToXl</b> (Small through Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.MdToLg"">This is <b>MdToLg</b> (Medium through Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.MdToXl"">This is <b>MdToXl</b> (Medium through Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LgToXl"">This is <b>LgToXl</b> (Large through Extra Large).</BitMediaQuery>";

    private string example2RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
    <Matched>
        This is <b>Matched</b> (BitScreenQuery.Md).
    </Matched>
    <NotMatched>
        [BitScreenQuery.Md] <b>NotMatched!</b>.
    </NotMatched>
</BitMediaQuery>";

    private string example3RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtSm"">
    <Template Context=""matched"">
        <BitButton Size=""@(matched ? BitSize.Large : BitSize.Small)""
                   Variant=""@(matched ? BitVariant.Fill : BitVariant.Outline)""
                   IconName=""@(matched ? BitIconName.TVMonitor : BitIconName.CellPhone)"">
            @(matched ? ""A large filled button above Sm"" : ""A small outlined one below it"")
        </BitButton>
    </Template>
</BitMediaQuery>";

    private string example4RazorCode = @"
<div><b>Document breakpoints</b> (Md: 960px to 1279.98px):</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
    <Matched>Md is <b>matched</b>.</Matched>
    <NotMatched>Md is <b>not matched</b>.</NotMatched>
</BitMediaQuery>

<div><b>Customized breakpoints</b> (Md: 700px to 899.98px):</div>
<BitThemeProvider Theme=""breakpointsTheme"">
    <BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
        <Matched>Md is <b>matched</b>.</Matched>
        <NotMatched>Md is <b>not matched</b>.</NotMatched>
    </BitMediaQuery>
</BitThemeProvider>";
    private string example4CsharpCode = @"
private readonly BitTheme breakpointsTheme = new()
{
    Layout = { Breakpoints = { Md = ""700px"", Lg = ""900px"" } }
};";

    private string example5RazorCode = @"
<BitMediaQuery Query=""screen and (max-width: 999px)"">
    <Matched>
        This is <b>screen and (max-width: 999px)</b>.
    </Matched>
    <NotMatched>
        Not matched yet!
    </NotMatched>
</BitMediaQuery>

<BitMediaQuery Query=""(400px <= width <= 700px)"">
    <Matched>
        The width is <b>between 400px and 700px</b> (range syntax).
    </Matched>
    <NotMatched>
        The width is <b>outside</b> the 400px to 700px range.
    </NotMatched>
</BitMediaQuery>";

    private string example6RazorCode = @"
<BitMediaQuery Query=""(orientation: landscape)"">
    <Matched>The screen is in <b>landscape</b> orientation.</Matched>
    <NotMatched>The screen is in <b>portrait</b> orientation.</NotMatched>
</BitMediaQuery>

<BitMediaQuery Query=""(prefers-color-scheme: dark)"">
    <Matched>The system prefers a <b>dark</b> color scheme.</Matched>
    <NotMatched>The system prefers a <b>light</b> color scheme.</NotMatched>
</BitMediaQuery>

<BitMediaQuery Query=""(pointer: fine)"">
    <Matched>The primary pointing device is <b>precise</b> (e.g. a mouse).</Matched>
    <NotMatched>The primary pointing device is <b>coarse</b> or absent (e.g. a touchscreen).</NotMatched>
</BitMediaQuery>

<BitMediaQuery Query=""(prefers-reduced-motion: reduce)"">
    <Matched>Reduced motion is <b>requested</b> by the system.</Matched>
    <NotMatched>Reduced motion is <b>not requested</b> by the system.</NotMatched>
</BitMediaQuery>";

    private string example7RazorCode = @"
<BitMediaQuery NoWrapper ScreenQuery=""BitScreenQuery.GtSm"">
    <Matched>This content renders <b>without</b> a wrapping element (BitScreenQuery.GtSm).</Matched>
    <NotMatched>[BitScreenQuery.GtSm] <b>NotMatched!</b> (still no wrapping element)</NotMatched>
</BitMediaQuery>";

    private string example8RazorCode = @"
<BitMediaQuery DefaultMatched ScreenQuery=""BitScreenQuery.GtSm"">
    <Matched>This is <b>Matched</b> (BitScreenQuery.GtSm), also rendered before the query gets evaluated.</Matched>
    <NotMatched>[BitScreenQuery.GtSm] <b>NotMatched!</b>.</NotMatched>
</BitMediaQuery>";

    private string example9RazorCode = @"
<BitMediaQuery @bind-IsMatched=""isSmallScreen"" ScreenQuery=""BitScreenQuery.LtMd"" />
<div>The screen is currently <b>@(isSmallScreen ? ""small"" : ""wide"")</b> (BitScreenQuery.LtMd).</div>
<BitButton FullWidth=""isSmallScreen"" Variant=""@(isSmallScreen ? BitVariant.Fill : BitVariant.Outline)"">
    A button driven by the bound value
</BitButton>";
    private string example9CsharpCode = @"
private bool isSmallScreen;";

    private string example10RazorCode = @"
<BitMediaQuery @ref=""mediaQueryRef"" ScreenQuery=""BitScreenQuery.Md"" OnChange=""HandleOnChange"" />
<div>[BitScreenQuery.Md] IsMatched?: <b>@isMatched</b></div>
<div>[BitScreenQuery.Md] via the IsMatched property: <b>@(mediaQueryRef?.IsMatched ?? false)</b></div>
<div>OnChange call count: <b>@changeCount</b></div>";
    private string example10CsharpCode = @"
private bool isMatched;
private int changeCount;
private BitMediaQuery? mediaQueryRef;

private void HandleOnChange(bool value)
{
    isMatched = value;
    changeCount++;
}";

    private string example11RazorCode = @"
<BitMediaQuery Style=""color: tomato; font-weight: bold;"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>Styled through the Style parameter (GtXs).</Matched>
    <NotMatched>Styled through the Style parameter, not matched (GtXs).</NotMatched>
</BitMediaQuery>

<BitMediaQuery Class=""custom-class"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>Classed through the Class parameter (GtXs).</Matched>
    <NotMatched>Classed through the Class parameter, not matched (GtXs).</NotMatched>
</BitMediaQuery>";

    private string example12RazorCode = @"
<BitMediaQuery Dir=""BitDir.Rtl"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>این محتوا در صفحه‌های بزرگ‌تر از <b>Xs</b> نمایش داده می‌شود.</Matched>
    <NotMatched>عرض صفحه کمتر از حد <b>GtXs</b> است.</NotMatched>
</BitMediaQuery>";
}

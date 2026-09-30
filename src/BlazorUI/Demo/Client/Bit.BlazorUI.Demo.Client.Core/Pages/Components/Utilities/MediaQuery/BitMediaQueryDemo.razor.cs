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
            Description = "The theme of an enclosing BitThemeProvider. Only its breakpoints are read, to resolve a ScreenQuery; they win over the --bit-bp-* CSS variables.",
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
            Description = "The matched state to render with until the browser answers the query, to avoid a flash of the wrong content while prerendering. Ignored when IsMatched is bound.",
        },
        new()
        {
            Name = "IsMatched",
            Type = "bool",
            DefaultValue = "false",
            Description = "The current matched state of the query. An output: the browser owns it, so bind it rather than setting it one way, which freezes it; seed it with DefaultMatched. (two-way bound)",
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
            Description = "Renders the active content without the wrapping root element, so what describes an element (class, style, id, dir, aria-label, ...) is ignored.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback for every change of the matched state, also called once with the first answer of the browser.",
        },
        new()
        {
            Name = "Query",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom media query to be matched, verbatim: any valid CSS media query, including the features other than the width. Takes precedence over ScreenQuery.",
        },
        new()
        {
            Name = "ScreenQuery",
            Type = "BitScreenQuery?",
            DefaultValue = "null",
            Description = "The predefined screen query to be matched, built from the live theme breakpoints (the --bit-bp-* CSS variables).",
            LinkType = LinkType.Link,
            Href = "#screen-query-enum"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "The content for both states, receiving the matched state. It is updated rather than rebuilt when the query flips, so the state inside it survives. Takes precedence over Matched, ChildContent and NotMatched.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "screen-query-enum",
            Name = "BitScreenQuery",
            Description = "The predefined screen queries, built from the live theme breakpoints (the --bit-bp-* CSS variables); the values below are the defaults. " +
                          "An upper bound sits 0.02px below the next breakpoint, so no width falls between two neighboring bands.",
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-bp-xs", DefaultValue = "0", Description = "The start of the Xs band, which a ScreenQuery is built from." },
        new() { Name = "--bit-bp-sm", DefaultValue = "600px", Description = "The start of the Sm band, which a ScreenQuery is built from." },
        new() { Name = "--bit-bp-md", DefaultValue = "960px", Description = "The start of the Md band, which a ScreenQuery is built from." },
        new() { Name = "--bit-bp-lg", DefaultValue = "1280px", Description = "The start of the Lg band, which a ScreenQuery is built from." },
        new() { Name = "--bit-bp-xl", DefaultValue = "1920px", Description = "The start of the Xl band, which a ScreenQuery is built from." },
        new() { Name = "--bit-bp-xxl", DefaultValue = "2560px", Description = "The start of the Xxl band, which a ScreenQuery is built from." },
    ];



    private int changeCount;
    private bool isSmallScreen;
    private readonly BitTheme breakpointsTheme = new()
    {
        Layout = { Breakpoints = { Md = "700px", Lg = "900px" } }
    };

    private void HandleOnChange(bool value)
    {
        changeCount++;
    }

    private readonly BitMediaQueryParams[] mediaQueryParams =
    [
        new()
        {
            ScreenQuery = BitScreenQuery.GtSm,
            NoWrapper = true,
        }
    ];
}

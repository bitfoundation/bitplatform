namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.MediaQuery;

public partial class BitMediaQueryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the element to render if the specified query is matched.",
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
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The event callback to be called when the state of the media query has been changed.",
        },
        new()
        {
            Name = "Query",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the custom query to be matched.",
        },
        new()
        {
            Name = "ScreenQuery",
            Type = "BitScreenQuery?",
            DefaultValue = "null",
            Description = "Defines the screen query to be matched, amongst the predefined Bit screen media queries.",
            LinkType = LinkType.Link,
            Href = "#screen-query-enum"
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "screen-query-enum",
            Name = "BitScreenQuery",
            Description = "The predefined screen media queries in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Xs",
                    Description="Extra small query: [@media screen and (max-width: 600px)]",
                    Value="0",
                },
                new()
                {
                    Name= "Sm",
                    Description="Small query: [@media screen and (min-width: 601px) and (max-width: 960px)]",
                    Value="1",
                },
                new()
                {
                    Name= "Md",
                    Description="Medium query: [@media screen and (min-width: 961px) and (max-width: 1280px)]",
                    Value="2",
                },
                new()
                {
                    Name= "Lg",
                    Description="Large query: [@media screen and (min-width: 1281px) and (max-width: 1920px)]",
                    Value="3",
                },
                new()
                {
                    Name= "Xl",
                    Description="Extra large query: [@media screen and (min-width: 1921px) and (max-width: 2560px)]",
                    Value="4",
                },
                new()
                {
                    Name= "Xxl",
                    Description="Extra extra large query: [@media screen and (min-width: 2561px)]",
                    Value="5",
                },
                new()
                {
                    Name= "LtSm",
                    Description="Less than small query: [@media screen and (max-width: 600px)]",
                    Value="6",
                },
                new()
                {
                    Name= "LtMd",
                    Description="Less than medium query: [@media screen and (max-width: 960px)]",
                    Value="7",
                },
                new()
                {
                    Name= "LtLg",
                    Description="Less than large query: [@media screen and (max-width: 1280px)]",
                    Value="8",
                },
                new()
                {
                    Name= "LtXl",
                    Description="Less than extra large query: [@media screen and (max-width: 1920px)]",
                    Value="9",
                },
                new()
                {
                    Name= "LtXxl",
                    Description="Less than extra extra large query: [@media screen and (max-width: 2560px)]",
                    Value="10",
                },
                new()
                {
                    Name= "GtXs",
                    Description="Greater than extra small query: [@media screen and (min-width: 601px)]",
                    Value="11",
                },
                new()
                {
                    Name= "GtSm",
                    Description="Greater than extra small query: [@media screen and (min-width: 601px)]",
                    Value="12",
                },
                new()
                {
                    Name= "GtMd",
                    Description="Greater than medium query: [@media screen and (min-width: 1281px)]",
                    Value="13",
                },
                new()
                {
                    Name= "GtLg",
                    Description="Greater than large query: [@media screen and (min-width: 1921px)]",
                    Value="14",
                },
                new()
                {
                    Name= "GtXl",
                    Description="Greater than extra large query: [@media screen and (min-width: 2561px)]",
                    Value="15",
                },
            ]
        }
    ];



    private bool isMatched;



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
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtXl"">This is <b>GtXl</b> (Greater Than Extra Large).</BitMediaQuery>";

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
<BitMediaQuery Query=""screen and (max-width: 999px)"">
    <Matched>
        This is <b>screen and (max-width: 999px)</b>.
    </Matched>
    <NotMatched>
        Not matched yet!
    </NotMatched>
</BitMediaQuery>";

    private string example4RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"" OnChange=""v => isMatched = v"" />
<div>[BitScreenQuery.Md] IsMatched?: <b>@isMatched</b></div>";
    private string example4CsharpCode = @"
private bool isMatched;";
}

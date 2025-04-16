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
            Description = "The content of the element.",
        },
        new()
        {
            Name = "Query",
            Type = "BitScreenQuery?",
            DefaultValue = "null",
            Description = "Specifies the media query to match for the child content.",
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
<div>IsMatched: @isMatched</div>";
    private string example4CsharpCode = @"
private bool isMatched;";
}

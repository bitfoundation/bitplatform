namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.MediaQuery;

public partial class BitMediaQueryDemo
{
    private readonly string example1RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtSm"">The screen is <b>wider</b> than the Sm band (GtSm).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"">The screen is <b>narrower</b> than the Md band (LtMd).</BitMediaQuery>";

    private readonly string example2RazorCode = @"
<div><b>Bands</b>:</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xs"">This is <b>Xs</b> (Extra Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Sm"">This is <b>Sm</b> (Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">This is <b>Md</b> (Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Lg"">This is <b>Lg</b> (Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xl"">This is <b>Xl</b> (Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Xxl"">This is <b>Xxl</b> (Extra Extra Large).</BitMediaQuery>
<br />
<div><b>Less than</b>:</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtSm"">This is <b>LtSm</b> (Less Than Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"">This is <b>LtMd</b> (Less Than Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtLg"">This is <b>LtLg</b> (Less Than Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtXl"">This is <b>LtXl</b> (Less Than Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtXxl"">This is <b>LtXxl</b> (Less Than Extra Extra Large).</BitMediaQuery>
<br />
<div><b>Greater than</b>:</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtXs"">This is <b>GtXs</b> (Greater Than Extra Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtSm"">This is <b>GtSm</b> (Greater Than Small).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtMd"">This is <b>GtMd</b> (Greater Than Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtLg"">This is <b>GtLg</b> (Greater Than Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtXl"">This is <b>GtXl</b> (Greater Than Extra Large).</BitMediaQuery>
<br />
<div><b>Spans</b>:</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToMd"">This is <b>SmToMd</b> (Small through Medium).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToLg"">This is <b>SmToLg</b> (Small through Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.SmToXl"">This is <b>SmToXl</b> (Small through Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.MdToLg"">This is <b>MdToLg</b> (Medium through Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.MdToXl"">This is <b>MdToXl</b> (Medium through Extra Large).</BitMediaQuery>
<BitMediaQuery ScreenQuery=""BitScreenQuery.LgToXl"">This is <b>LgToXl</b> (Large through Extra Large).</BitMediaQuery>";

    private readonly string example3RazorCode = @"
<style>
    .nav-links {
        gap: 1rem;
        display: flex;
    }
</style>

<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"">
    <Matched>
        <BitButton IconName=""@BitIconName.GlobalNavButton"" AriaLabel=""Menu"" Variant=""BitVariant.Text"" />
    </Matched>
    <NotMatched>
        <div class=""nav-links"">
            <BitLink Href=""#example3"">Home</BitLink>
            <BitLink Href=""#example3"">Docs</BitLink>
            <BitLink Href=""#example3"">Blog</BitLink>
        </div>
    </NotMatched>
</BitMediaQuery>";

    private readonly string example4RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.GtSm"">
    <Template Context=""wide"">
        <BitTextField Label=""Search""
                      Placeholder=""Type, then resize the window""
                      Size=""@(wide ? BitSize.Large : BitSize.Small)""
                      Style=""@(wide ? ""width: 24rem"" : ""width: 100%"")"" />
    </Template>
</BitMediaQuery>";

    private readonly string example5RazorCode = @"
<BitMediaQuery Query=""(400px <= width <= 700px)"">
    <Matched>The width is <b>between 400px and 700px</b>.</Matched>
    <NotMatched>The width is <b>outside</b> 400px to 700px.</NotMatched>
</BitMediaQuery>
<BitMediaQuery Query=""(orientation: landscape)"">
    <Matched>The screen is in <b>landscape</b> orientation.</Matched>
    <NotMatched>The screen is in <b>portrait</b> orientation.</NotMatched>
</BitMediaQuery>
<BitMediaQuery Query=""(prefers-color-scheme: dark)"">
    <Matched>The system prefers a <b>dark</b> color scheme.</Matched>
    <NotMatched>The system prefers a <b>light</b> color scheme.</NotMatched>
</BitMediaQuery>
<BitMediaQuery Query=""(pointer: fine)"">
    <Matched>The primary pointer is <b>precise</b> (a mouse).</Matched>
    <NotMatched>The primary pointer is <b>coarse</b> (a finger) or absent.</NotMatched>
</BitMediaQuery>
<BitMediaQuery Query=""(prefers-reduced-motion: reduce)"">
    <Matched>Reduced motion is <b>requested</b>.</Matched>
    <NotMatched>Reduced motion is <b>not requested</b>.</NotMatched>
</BitMediaQuery>";

    private readonly string example6RazorCode = @"
<div><b>Document breakpoints</b> (960px to 1279.98px):</div>
<BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
    <Matched>Md is <b>matched</b>.</Matched>
    <NotMatched>Md is <b>not matched</b>.</NotMatched>
</BitMediaQuery>

<div><b>BitThemeProvider</b> (700px to 899.98px):</div>
<BitThemeProvider Theme=""breakpointsTheme"">
    <BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
        <Matched>Md is <b>matched</b>.</Matched>
        <NotMatched>Md is <b>not matched</b>.</NotMatched>
    </BitMediaQuery>
</BitThemeProvider>

<div><b>CSS variables</b> (1100px to 1499.98px):</div>
<div style=""--bit-bp-md: 1100px; --bit-bp-lg: 1500px;"">
    <BitMediaQuery ScreenQuery=""BitScreenQuery.Md"">
        <Matched>Md is <b>matched</b>.</Matched>
        <NotMatched>Md is <b>not matched</b>.</NotMatched>
    </BitMediaQuery>
</div>";
    private readonly string example6CsharpCode = @"
private readonly BitTheme breakpointsTheme = new()
{
    Layout = { Breakpoints = { Md = ""700px"", Lg = ""900px"" } }
};";

    private readonly string example7RazorCode = @"
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }
</style>

<p>
    Your order ships
    <BitMediaQuery Element=""span"" ScreenQuery=""BitScreenQuery.GtSm"">
        <Matched><b>Wednesday, October 7</b></Matched>
        <NotMatched><b>Oct 7</b></NotMatched>
    </BitMediaQuery>
    by express courier.
</p>

<div class=""toolbar"">
    <BitButton IconName=""@BitIconName.Add"">New</BitButton>
    <BitMediaQuery NoWrapper ScreenQuery=""BitScreenQuery.GtSm"">
        <BitButton IconName=""@BitIconName.Share"" Variant=""BitVariant.Outline"">Share</BitButton>
        <BitButton IconName=""@BitIconName.Download"" Variant=""BitVariant.Outline"">Export</BitButton>
    </BitMediaQuery>
    <BitButton IconName=""@BitIconName.Settings"" Variant=""BitVariant.Text"">Settings</BitButton>
</div>";

    private readonly string example8RazorCode = @"
<BitMediaQuery DefaultMatched ScreenQuery=""BitScreenQuery.GtSm"">
    <Matched>Wide content, rendered before the query is answered too.</Matched>
    <NotMatched>Narrow content.</NotMatched>
</BitMediaQuery>";

    private readonly string example9RazorCode = @"
<BitMediaQuery @bind-IsMatched=""isSmallScreen"" ScreenQuery=""BitScreenQuery.LtMd"" OnChange=""HandleOnChange"" />
<div>The screen is <b>@(isSmallScreen ? ""small"" : ""wide"")</b> (LtMd). OnChange calls: <b>@changeCount</b></div>
<BitButton FullWidth=""isSmallScreen"" Variant=""@(isSmallScreen ? BitVariant.Fill : BitVariant.Outline)"">
    A button driven by the bound value
</BitButton>";
    private readonly string example9CsharpCode = @"
private int changeCount;
private bool isSmallScreen;

private void HandleOnChange(bool value)
{
    changeCount++;
}";

    private readonly string example10RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"" AriaLabel=""Checkout actions"">
    <Matched>
        <BitButton Id=""checkout-pay"" IconName=""@BitIconName.Money"" AriaLabel=""Pay now"" />
        <BitButton Id=""checkout-cart"" IconName=""@BitIconName.ShoppingCart"" AriaLabel=""View cart"" Variant=""BitVariant.Outline"" />
    </Matched>
    <NotMatched>
        <BitButton Id=""checkout-pay"" IconName=""@BitIconName.Money"">Pay now</BitButton>
        <BitButton Id=""checkout-cart"" IconName=""@BitIconName.ShoppingCart"" Variant=""BitVariant.Outline"">View cart</BitButton>
    </NotMatched>
</BitMediaQuery>";

    private readonly string example11RazorCode = @"
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }
</style>

<BitParams Parameters=""mediaQueryParams"">
    <div class=""toolbar"">
        <BitButton IconName=""@BitIconName.Edit"" AriaLabel=""Edit"">
            <BitMediaQuery>Edit</BitMediaQuery>
        </BitButton>
        <BitButton IconName=""@BitIconName.Copy"" AriaLabel=""Copy"">
            <BitMediaQuery>Copy</BitMediaQuery>
        </BitButton>
        <BitButton IconName=""@BitIconName.Delete"" AriaLabel=""Delete"" Color=""BitColor.Error"">
            <BitMediaQuery ScreenQuery=""BitScreenQuery.GtXs"">Delete</BitMediaQuery>
        </BitButton>
    </div>
</BitParams>";
    private readonly string example11CsharpCode = @"
private readonly BitMediaQueryParams[] mediaQueryParams =
[
    new()
    {
        ScreenQuery = BitScreenQuery.GtSm,
        Element = ""span"",
    }
];";

    private readonly string example12RazorCode = @"
<BitMediaQuery Style=""color: tomato; font-weight: bold;"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>Styled through the Style parameter (GtXs).</Matched>
    <NotMatched>Styled through the Style parameter, not matched (GtXs).</NotMatched>
</BitMediaQuery>
<BitMediaQuery Class=""custom-class"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>Classed through the Class parameter (GtXs).</Matched>
    <NotMatched>Classed through the Class parameter, not matched (GtXs).</NotMatched>
</BitMediaQuery>";

    private readonly string example13RazorCode = @"
<BitMediaQuery Dir=""BitDir.Rtl"" ScreenQuery=""BitScreenQuery.GtXs"">
    <Matched>این محتوا در صفحه‌های بزرگ‌تر از <b>Xs</b> نمایش داده می‌شود.</Matched>
    <NotMatched>عرض صفحه کمتر از حد <b>GtXs</b> است.</NotMatched>
</BitMediaQuery>";
}

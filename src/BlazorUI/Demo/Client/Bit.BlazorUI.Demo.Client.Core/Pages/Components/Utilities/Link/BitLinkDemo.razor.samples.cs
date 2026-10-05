namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Link;

public partial class BitLinkDemo
{
    private readonly string example1RazorCode = @"
<div>Read the <BitLink Href=""https://github.com/bitfoundation/bitplatform"">bit platform source</BitLink> on GitHub.</div>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"">Basic link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Disabled>Disabled link</BitLink>";

    private readonly string example2RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"">Underlined on hover (default)</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Underlined>Always underlined</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" NoUnderline>Never underlined</BitLink>";

    private readonly string example3RazorCode = @"
<BitLink IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"">Leading icon</BitLink>
<BitLink IconName=""@BitIconName.ChevronRight"" IconPosition=""BitIconPosition.End"" Href=""https://github.com/bitfoundation/bitplatform"">Trailing icon</BitLink>";

    private readonly string example4RazorCode = @"
<BitLink Target=""@BitLinkTarget.Blank"" Href=""https://github.com/bitfoundation/bitplatform"">Opens in a new tab</BitLink>

<BitLink Target=""@BitLinkTarget.Blank"" NewTabHint=""(در زبانه جدید باز می‌شود)"" Href=""https://github.com/bitfoundation/bitplatform"">Announced with a translated hint</BitLink>

<BitLink Target=""@BitLinkTarget.Blank"" NoNewTabHint IconName=""@BitIconName.OpenInNewWindow"" IconPosition=""BitIconPosition.End"" Href=""https://github.com/bitfoundation/bitplatform"">
    GitHub (opens in a new tab)
</BitLink>";

    private readonly string example5RazorCode = @"
<BitLink Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Download="""">Download the bit logo</BitLink>
<BitLink IconName=""@BitIconName.Download"" Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Download=""bit-platform-logo.svg"">Download with a custom file name</BitLink>";

    private readonly string example6RazorCode = @"
<BitLink Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" Href=""https://github.com/bitfoundation/bitplatform"">nofollow noreferrer</BitLink>
<BitLink Rel=""BitLinkRels.Sponsored | BitLinkRels.Ugc"" Href=""https://github.com/bitfoundation/bitplatform"">sponsored ugc</BitLink>
<BitLink Rel=""BitLinkRels.Opener"" Target=""@BitLinkTarget.Blank"" Href=""https://github.com/bitfoundation/bitplatform"">opener (no automatic noopener)</BitLink>";

    private readonly string example7RazorCode = @"
<BitLink OnClick=""() => buttonClickCount++"">A button link (clicked @buttonClickCount times)</BitLink>

<BitLink OnClick=""() => anchorClickCount++"" Target=""@BitLinkTarget.Blank"" Href=""https://github.com/bitfoundation/bitplatform"">
    An anchor with OnClick (clicked @anchorClickCount times)
</BitLink>

<div class=""clickable-container"" @onclick=""() => containerClickCount++"">
    A clickable container (clicked @containerClickCount times):
    <BitLink StopPropagation OnClick=""() => innerClickCount++"">StopPropagation (clicked @innerClickCount times)</BitLink>
</div>

<BitLink PreventDefault OnClick=""HandleGuardedClick"" Href=""https://github.com/bitfoundation/bitplatform"">Ask before leaving</BitLink>
<div>@guardMessage</div>";
    private readonly string example7CsharpCode = @"
private int buttonClickCount;
private int anchorClickCount;
private int innerClickCount;
private int containerClickCount;
private string? guardMessage;

private void HandleGuardedClick()
{
    // The browser did not navigate, so what happens next is entirely up to this handler:
    // confirm, save a draft, track the click, and then navigate from here if it should happen.
    guardMessage = ""The navigation was suppressed. This is where a confirmation would go."";
}";

    private readonly string example8RazorCode = @"
<BitLink Href=""#article-end"">Go to the end of the article</BitLink>

<div class=""article"">
    <p id=""article-start"" style=""scroll-margin: 110px"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions and
        dreams. These placeholder words stand for the beginning, a moment where everything is still to come.
    </p>
    <p>
        Soon these lines will turn into narratives that provoke thought and resonate with those who read them.
        Until then they are the scaffolding of something remarkable, a bridge between the empty page of now
        and the stories of tomorrow.
    </p>
    <p id=""article-end"" style=""scroll-margin: 110px"">
        In this space potential reigns: each word can still become something extraordinary. Whether it is a
        tale of adventure, a reflection of truth or an idea that sparks change, these lines are yours to fill.
    </p>
</div>

<BitLink Href=""#article-start"">Back to the start of the article</BitLink>";

    private readonly string example9RazorCode = @"
<nav aria-label=""Components"">
    <BitLink Match=""BitNavMatch.Prefix"" Href=""/components"">Components</BitLink>
    <BitLink Match=""BitNavMatch.Exact"" Href=""/components/button"">Button</BitLink>
    <BitLink Match=""BitNavMatch.Exact"" Href=""/components/link"">Link</BitLink>
    <BitLink Match=""BitNavMatch.Exact"" Href=""/components/image"">Image</BitLink>
</nav>


<ol aria-label=""Checkout"">
    @for (var i = 0; i < checkoutSteps.Length; i++)
    {
        var step = i;
        <li>
            <BitLink AriaCurrent=""@(currentStep == step ? BitNavAriaCurrent.Step : null)"" OnClick=""() => currentStep = step"">
                @checkoutSteps[step]
            </BitLink>
        </li>
    }
</ol>";
    private readonly string example9CsharpCode = @"
private int currentStep;
private readonly string[] checkoutSteps = [""Cart"", ""Shipping"", ""Payment""];";

    private readonly string example10RazorCode = @"
<BitLink Title=""github.com/bitfoundation/bitplatform"" Href=""https://github.com/bitfoundation/bitplatform"">Hover for the full address</BitLink>

<BitLink IconName=""@BitIconName.Download"" AriaDescription=""SVG, 12 kilobytes"" Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Download="""">
    Download the brand guidelines
</BitLink>


<BitLink Disabled Href=""https://github.com/bitfoundation/bitplatform"">Disabled (skipped by Tab)</BitLink>
<BitLink Disabled AllowDisabledFocus Href=""https://github.com/bitfoundation/bitplatform"">Disabled (still focusable)</BitLink>

<BitLink OnClick=""() => focusTargetRef.FocusAsync()"">Focus the link below</BitLink>
<BitLink @ref=""focusTargetRef"" Href=""https://github.com/bitfoundation/bitplatform"">The focus lands here</BitLink>";
    private readonly string example10CsharpCode = @"
private BitLink focusTargetRef = default!;";

    private readonly string example11RazorCode = @"
<div style=""--bit-Link-underline-offset: 0.25em; --bit-Link-underline-thickness: 2px; --bit-Link-underline-color: var(--bit-clr-sec); --bit-Link-visited-color: var(--bit-clr-ter);"">
    Set on the container: <BitLink Underlined Href=""https://github.com/bitfoundation/bitplatform"">an offset, thicker underline</BitLink>,
    and <BitLink Underlined Href=""/components/link"">a visited link</BitLink> in its own color.
</div>

<nav aria-label=""Components"" style=""--bit-Link-color: var(--bit-clr-fg-sec); --bit-Link-current-color: var(--bit-clr-fg-pri); --bit-Link-current-font-weight: 600;"">
    <BitLink Match=""BitNavMatch.Exact"" NoUnderline Href=""/components/button"">Button</BitLink>
    <BitLink Match=""BitNavMatch.Exact"" NoUnderline Href=""/components/link"">Link</BitLink>
    <BitLink Match=""BitNavMatch.Exact"" NoUnderline Href=""/components/image"">Image</BitLink>
</nav>

<BitLink Style=""--bit-Link-color: var(--bit-clr-fg-pri); --bit-Link-hover-color: var(--bit-clr-sec-fg); --bit-Link-font-weight: 600; --bit-Link-icon-gap: 8px;""
         IconName=""@BitIconName.ChevronRight""
         IconPosition=""BitIconPosition.End""
         Href=""https://github.com/bitfoundation/bitplatform"">
    Set on one link
</BitLink>";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""linkParams"">
    <BitLink Href=""https://bitplatform.dev"">bit platform</BitLink>
    <BitLink Href=""https://github.com/bitfoundation/bitplatform"">bit platform on GitHub</BitLink>
    <BitLink Underlined=""false"" Href=""https://github.com/bitfoundation/bitplatform/issues"">Its own Underlined, the cascaded rest</BitLink>
</BitParams>";
    private readonly string example12CsharpCode = @"
private readonly BitLinkParams[] linkParams =
[
    new()
    {
        Underlined = true,
        Target = BitLinkTarget.Blank,
        IconName = BitIconName.OpenInNewWindow,
        IconPosition = BitIconPosition.End,
    }
];";

    private readonly string example13RazorCode = @"
<BitLink Color=""BitColor.Primary"" Href=""https://github.com/bitfoundation/bitplatform"">Primary</BitLink>
<BitLink Color=""BitColor.Secondary"" Href=""https://github.com/bitfoundation/bitplatform"">Secondary</BitLink>
<BitLink Color=""BitColor.Tertiary"" Href=""https://github.com/bitfoundation/bitplatform"">Tertiary</BitLink>
<BitLink Color=""BitColor.Info"" Href=""https://github.com/bitfoundation/bitplatform"">Info</BitLink>
<BitLink Color=""BitColor.Success"" Href=""https://github.com/bitfoundation/bitplatform"">Success</BitLink>
<BitLink Color=""BitColor.Warning"" Href=""https://github.com/bitfoundation/bitplatform"">Warning</BitLink>
<BitLink Color=""BitColor.SevereWarning"" Href=""https://github.com/bitfoundation/bitplatform"">SevereWarning</BitLink>
<BitLink Color=""BitColor.Error"" Href=""https://github.com/bitfoundation/bitplatform"">Error</BitLink>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitLink Color=""BitColor.PrimaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBackground</BitLink>
    <BitLink Color=""BitColor.SecondaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBackground</BitLink>
    <BitLink Color=""BitColor.TertiaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBackground</BitLink>
</div>

<BitLink Color=""BitColor.PrimaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryForeground</BitLink>
<BitLink Color=""BitColor.SecondaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryForeground</BitLink>
<BitLink Color=""BitColor.TertiaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryForeground</BitLink>
<BitLink Color=""BitColor.PrimaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBorder</BitLink>
<BitLink Color=""BitColor.SecondaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBorder</BitLink>
<BitLink Color=""BitColor.TertiaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBorder</BitLink>


<BitLink Disabled Color=""BitColor.Primary"" Href=""https://github.com/bitfoundation/bitplatform"">Primary</BitLink>
<BitLink Disabled Color=""BitColor.Secondary"" Href=""https://github.com/bitfoundation/bitplatform"">Secondary</BitLink>
<BitLink Disabled Color=""BitColor.Warning"" Href=""https://github.com/bitfoundation/bitplatform"">Warning</BitLink>
<BitLink Disabled Color=""BitColor.Error"" Href=""https://github.com/bitfoundation/bitplatform"">Error</BitLink>
<BitLink Disabled Color=""BitColor.PrimaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryForeground</BitLink>


<BitLink NoColor Href=""https://github.com/bitfoundation/bitplatform"">
    <BitText Typography=""BitTypography.H6"">A card-like link</BitText>
    <BitText Typography=""BitTypography.Body2"">Its text keeps the colors BitText gives it.</BitText>
</BitLink>";

    private readonly string example14RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitLink Icon=""@BitIconInfo.Fa(""brands github"")"" Href=""https://github.com/bitfoundation/bitplatform"">FontAwesome</BitLink>
<BitLink Icon=""@BitIconInfo.Fa(""solid arrow-up-right-from-square"")"" IconPosition=""BitIconPosition.End"" Target=""@BitLinkTarget.Blank"" Href=""https://github.com/bitfoundation/bitplatform"">FontAwesome, trailing</BitLink>
<BitLink Icon=""@BitIconInfo.Bi(""github"")"" Href=""https://github.com/bitfoundation/bitplatform"">Bootstrap</BitLink>
<BitLink Icon=""@BitIconInfo.Bi(""box-arrow-up-right"")"" IconPosition=""BitIconPosition.End"" Target=""@BitLinkTarget.Blank"" Href=""https://github.com/bitfoundation/bitplatform"">Bootstrap, trailing</BitLink>";

    private readonly string example15RazorCode = @"
<BitLink Size=""BitSize.Small"" IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"">Small</BitLink>
<BitLink Size=""BitSize.Medium"" IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"">Medium</BitLink>
<BitLink Size=""BitSize.Large"" IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"">Large</BitLink>";

    private readonly string example16RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid red;
        max-width: max-content;
    }
</style>

<BitLink Style=""color: goldenrod; font-weight: bold"" Href=""https://github.com/bitfoundation/bitplatform"">Link with style</BitLink>
<BitLink Class=""custom-class"" Href=""https://github.com/bitfoundation/bitplatform"">Link with class</BitLink>";

    private readonly string example17RazorCode = @"
<div dir=""rtl"">
    <BitLink Dir=""BitDir.Rtl"" Href=""https://github.com/bitfoundation/bitplatform"">پیوند راست به چپ</BitLink>
    <BitLink Dir=""BitDir.Rtl"" IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"">پیوند راست به چپ با آیکن</BitLink>
</div>";
}

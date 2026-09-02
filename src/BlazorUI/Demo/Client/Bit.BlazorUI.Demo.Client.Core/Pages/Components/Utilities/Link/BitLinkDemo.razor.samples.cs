namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Link;

public partial class BitLinkDemo
{
    private readonly string example1RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"">Basic Link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"">Disabled Link</BitLink>
";

    private readonly string example2RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Underlined>Underlined link</BitLink>";

    private readonly string example3RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" NoUnderline>NoUnderline link</BitLink>";

    private readonly string example4RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""@BitLinkTarget.Blank"">Blank target link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""@BitLinkTarget.Parent"">Parent target link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""@BitLinkTarget.Self"">Self target link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""@BitLinkTarget.Top"">Top target link</BitLink>";

    private readonly string example5RazorCode = @"
<BitLink Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Download="""">Download the bit logo</BitLink>
<BitLink Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Download=""bit-platform-logo.svg"">Download with a custom file name</BitLink>";

    private readonly string example6RazorCode = @"
<BitLink OnClick=""HandleOnClick"">Click to navigate to the bit platform GitHub repo!</BitLink>

<BitLink OnClick=""() => clickCount++"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">Link with both Href and OnClick</BitLink>
<div>OnClick count: @clickCount</div>

<div class=""clickable-container"" @onclick=""() => containerClickCount++"">
    A clickable container (clicked @containerClickCount times):
    <BitLink StopPropagation OnClick=""() => linkClickCount++"">Link with StopPropagation (clicked @linkClickCount times)</BitLink>
</div>";
    private readonly string example6CsharpCode = @"
[Inject] private NavigationManager Navigation { get; set; } = default!;

private int clickCount;
private int linkClickCount;
private int containerClickCount;

private void HandleOnClick()
{
    Navigation.NavigateTo(""https://github.com/bitfoundation/bitplatform"");
}";

    private readonly string example7RazorCode = @"
<BitLink Style=""scroll-margin: 70px"" Id=""start-article"" Href=""#end-article"">Go To End of this Article</BitLink>
<br />
Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
<br />
Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
are boundless. This space is yours to craft, yours to shape, yours to bring to life.
<br />
In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
shaped into meaning, and the emotions ready to resonate with every reader.
<br />
In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
each word has the power to transform into something extraordinary. Here lies the start of something new-an
opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
begins here, in this quiet moment where everything is possible.
<br />
Imagine this space as a window into the future empty yet alive with the energy of endless possibilities.
These words stand as temporary guides, placeholders that whisper of what is to come.
They hold the promise of stories waiting to unfold, ideas eager to take shape, and
connections that will soon emerge to inspire and resonate. This is not an empty page;
it is a canvas, rich with potential and ready to transform into something meaningful.
<br />
For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
the foundation upon which your creativity will build. Soon, this space will hold your thoughts, your visions,
and your voice a reflection of who you are and what you wish to share with the world. Every sentence will carry
purpose, every word will invite others to connect, to think, to feel. So take a moment to dream, to imagine
what this blank slate can become. Whether it’s a story, an idea, or a message that matters, this is your
starting point. The possibilities are endless, and the journey begins now.
<br />
<BitLink Style=""scroll-margin: 70px"" Id=""end-article"" Href=""#start-article"">Go To Start of this Article</BitLink>";

    private readonly string example8RazorCode = @"
<BitLink Rel=""BitLinkRels.NoFollow"" Href=""https://github.com/bitfoundation/bitplatform"">Link with a rel attribute (nofollow)</BitLink>
<BitLink Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" Href=""https://github.com/bitfoundation/bitplatform"">Link with a rel attribute (nofollow & noreferrer)</BitLink>
<BitLink Rel=""BitLinkRels.Sponsored | BitLinkRels.Ugc"" Href=""https://github.com/bitfoundation/bitplatform"">Link with a rel attribute (sponsored & ugc)</BitLink>
<BitLink Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">Blank target link with an automatic noopener rel</BitLink>";

    private readonly string example9RazorCode = @"
<BitLink IsEnabled=""false"" Href=""https://github.com/bitfoundation/bitplatform"">Disabled link (skipped by Tab)</BitLink>
<BitLink IsEnabled=""false"" AllowDisabledFocus Href=""https://github.com/bitfoundation/bitplatform"">Disabled link with AllowDisabledFocus (focusable)</BitLink>";

    private readonly string example10RazorCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"">
    <BitText Typography=""BitTypography.H4"">Link with default color!</BitText>
    <BitText Typography=""BitTypography.Subtitle1"">this text color is coming from the link itself.</BitText>
</BitLink>

<BitLink Href=""https://github.com/bitfoundation/bitplatform"" NoColor>
    <BitText>Link with NoColor!</BitText>
</BitLink>";

    private readonly string example11RazorCode = @"
<BitLink Color=""BitColor.Primary"" Href=""https://github.com/bitfoundation/bitplatform"">Primary Color Link (default)</BitLink>
<BitLink Color=""BitColor.Secondary"" Href=""https://github.com/bitfoundation/bitplatform"">Secondary Color Link</BitLink>
<BitLink Color=""BitColor.Tertiary"" Href=""https://github.com/bitfoundation/bitplatform"">Tertiary Color Link</BitLink>
<BitLink Color=""BitColor.Info"" Href=""https://github.com/bitfoundation/bitplatform"">Info Color Link</BitLink>
<BitLink Color=""BitColor.Success"" Href=""https://github.com/bitfoundation/bitplatform"">Success Color Link</BitLink>
<BitLink Color=""BitColor.Warning"" Href=""https://github.com/bitfoundation/bitplatform"">Warning Color Link</BitLink>
<BitLink Color=""BitColor.SevereWarning"" Href=""https://github.com/bitfoundation/bitplatform"">SevereWarning Color Link</BitLink>
<BitLink Color=""BitColor.Error"" Href=""https://github.com/bitfoundation/bitplatform"">Error Color Link</BitLink>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitLink Color=""BitColor.PrimaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBackground Color Link</BitLink>
    <BitLink Color=""BitColor.SecondaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBackground Color Link</BitLink>
    <BitLink Color=""BitColor.TertiaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBackground Color Link</BitLink>
</div>

<BitLink Color=""BitColor.PrimaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryForeground Color Link</BitLink>
<BitLink Color=""BitColor.SecondaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryForeground Color Link</BitLink>
<BitLink Color=""BitColor.TertiaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryForeground Color Link</BitLink>

<BitLink Color=""BitColor.PrimaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBorder Color Link</BitLink>
<BitLink Color=""BitColor.SecondaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBorder Color Link</BitLink>
<BitLink Color=""BitColor.TertiaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBorder Color Link</BitLink>


<BitLink IsEnabled=""false"" Href=""https://github.com/bitfoundation/bitplatform"">Primary (default)</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Secondary"" Href=""https://github.com/bitfoundation/bitplatform"">Secondary</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Tertiary"" Href=""https://github.com/bitfoundation/bitplatform"">Tertiary</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Info"" Href=""https://github.com/bitfoundation/bitplatform"">Info</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Success"" Href=""https://github.com/bitfoundation/bitplatform"">Success</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Warning"" Href=""https://github.com/bitfoundation/bitplatform"">Warning</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.SevereWarning"" Href=""https://github.com/bitfoundation/bitplatform"">SevereWarning</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.Error"" Href=""https://github.com/bitfoundation/bitplatform"">Error</BitLink>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitLink IsEnabled=""false"" Color=""BitColor.PrimaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBackground</BitLink>
    <BitLink IsEnabled=""false"" Color=""BitColor.SecondaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBackground</BitLink>
    <BitLink IsEnabled=""false"" Color=""BitColor.TertiaryBackground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBackground</BitLink>
</div>

<BitLink IsEnabled=""false"" Color=""BitColor.PrimaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryForeground</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.SecondaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryForeground</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.TertiaryForeground"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryForeground</BitLink>

<BitLink IsEnabled=""false"" Color=""BitColor.PrimaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">PrimaryBorder</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.SecondaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">SecondaryBorder</BitLink>
<BitLink IsEnabled=""false"" Color=""BitColor.TertiaryBorder"" Href=""https://github.com/bitfoundation/bitplatform"">TertiaryBorder</BitLink>";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid red;
        max-width: max-content;
    }
</style>

<BitLink Style=""color: goldenrod; font-weight:bold"" Href=""https://github.com/bitfoundation/bitplatform"">Link with style</BitLink>
<BitLink Class=""custom-class"" Href=""https://github.com/bitfoundation/bitplatform"">Link with class</BitLink>";

    private readonly string example13RazorCode = @"
<BitLink Dir=""BitDir.Rtl"" Href=""https://github.com/bitfoundation/bitplatform"">پیوند راست به چپ</BitLink>";
}
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private readonly string example1RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" />
<BitIcon IconName=""@BitIconName.Bus"" />
<BitIcon IconName=""@BitIconName.Pinned"" />

<BitIcon IconName=""@BitIconName.Accept"" Disabled />
<BitIcon IconName=""@BitIconName.Bus"" Disabled />
<BitIcon IconName=""@BitIconName.Pinned"" Disabled />";

    private readonly string example2RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Text"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" Circular />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" Circular />

<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Text"" Disabled />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" Disabled />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" Disabled />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" Circular Disabled />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" Circular Disabled />";

    private readonly string example3RazorCode = @"
<BitIcon IconName=""@BitIconName.Up"" />
<BitIcon IconName=""@BitIconName.Up"" Rotate=""BitIconRotate.Rotate90"" />
<BitIcon IconName=""@BitIconName.Up"" Rotate=""BitIconRotate.Rotate180"" />
<BitIcon IconName=""@BitIconName.Up"" Rotate=""BitIconRotate.Rotate270"" />
<BitIcon IconName=""@BitIconName.Up"" RotateAngle=""45"" />
<BitIcon IconName=""@BitIconName.Up"" RotateAngle=""-30"" />

<BitIcon IconName=""@BitIconName.ReplyAlt"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Flip=""BitIconFlip.Horizontal"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Flip=""BitIconFlip.Vertical"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Flip=""BitIconFlip.Both"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Flip=""BitIconFlip.Horizontal"" RotateAngle=""45"" />


<div>
    <BitIcon IconName=""@BitIconName.Forward"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Back"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Clock"" />
</div>

<div dir=""rtl"">
    <BitIcon IconName=""@BitIconName.Forward"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Back"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Clock"" />
</div>";

    private readonly string example4RazorCode = @"
<BitIcon IconName=""@BitIconName.Sync"" Animation=""BitIconAnimation.Spin"" />
<BitIcon IconName=""@BitIconName.Sync"" Animation=""BitIconAnimation.SpinReverse"" />
<BitIcon IconName=""@BitIconName.ProgressRingDots"" Animation=""BitIconAnimation.Pulse"" />
<BitIcon IconName=""@BitIconName.Heart"" Animation=""BitIconAnimation.Beat"" />
<BitIcon IconName=""@BitIconName.StatusCircleInner"" Animation=""BitIconAnimation.Fade"" />
<BitIcon IconName=""@BitIconName.Ringer"" Animation=""BitIconAnimation.Shake"" />
<BitIcon IconName=""@BitIconName.Up"" Animation=""BitIconAnimation.Bounce"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Animation=""BitIconAnimation.BeatFade"" />

<BitIcon IconName=""@BitIconName.Sync"" Animation=""BitIconAnimation.Spin"" AnimationDuration=""4s"" />
<BitIcon IconName=""@BitIconName.Sync"" Animation=""BitIconAnimation.Spin"" AnimationDuration=""0.4s"" />
<BitIcon IconName=""@BitIconName.Send"" Animation=""BitIconAnimation.Beat"" RotateAngle=""45"" />

<BitIcon @key=""replayKey"" IconName=""@BitIconName.Ringer"" Animation=""BitIconAnimation.Shake"" AnimationIterationCount=""3"" />
<BitLink OnClick=""() => replayKey++"">Shake 3 times (replay)</BitLink>

<BitIcon IconName=""@BitIconName.CircleFill"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" AnimationDelay=""0.2s"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" AnimationDelay=""0.4s"" />";
    private readonly string example4CsharpCode = @"
// A new key renders a new element, which plays its animation from the start again.
private int replayKey;";

    private readonly string example5RazorCode = @"
<ul>
    <li><BitIcon IconName=""@BitIconName.Home"" /> Home</li>
    <li><BitIcon IconName=""@BitIconName.Settings"" /> Settings</li>
    <li><BitIcon IconName=""@BitIconName.Contact"" /> Profile</li>
    <li><BitIcon IconName=""@BitIconName.SignOut"" /> Sign out</li>
</ul>

<ul>
    <li><BitIcon IconName=""@BitIconName.Home"" FixedWidth /> Home</li>
    <li><BitIcon IconName=""@BitIconName.Settings"" FixedWidth /> Settings</li>
    <li><BitIcon IconName=""@BitIconName.Contact"" FixedWidth /> Profile</li>
    <li><BitIcon IconName=""@BitIconName.SignOut"" FixedWidth /> Sign out</li>
</ul>";

    private readonly string example6RazorCode = @"
<BitIcon IconName=""@(isStarred ? BitIconName.FavoriteStarFill : BitIconName.FavoriteStar)""
         Title=""Favorite""
         aria-pressed=""@(isStarred ? ""true"" : ""false"")""
         OnClick=""() => isStarred = !isStarred"" />

<BitIcon IconName=""@BitIconName.Refresh""
         Variant=""BitVariant.Outline""
         AriaLabel=""Refresh the list""
         OnClick=""() => clickCount++"" />

<BitIcon IconName=""@BitIconName.Delete""
         Variant=""BitVariant.Fill""
         Title=""Deleting is unavailable here""
         Disabled
         OnClick=""() => clickCount++"" />

<div>Refreshed @clickCount times.</div>";
    private readonly string example6CsharpCode = @"
private bool isStarred = true;
private int clickCount;";

    private readonly string example7RazorCode = @"
<BitIcon>
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M12 2 15.1 8.6 22 9.7l-5 4.9 1.2 7L12 18.3 5.8 21.6 7 14.6l-5-4.9 6.9-1.1z"" />
    </svg>
</BitIcon>

<BitIcon Variant=""BitVariant.Outline"">
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
    </svg>
</BitIcon>

<BitIcon Variant=""BitVariant.Fill"">
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M12 21.35 10.55 20C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54z"" />
    </svg>
</BitIcon>


<div>
    Aligned by its box
    <BitIcon>
        <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
            <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
        </svg>
    </BitIcon>
    and dropped onto the line with Inline
    <BitIcon Inline>
        <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
            <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
        </svg>
    </BitIcon>
</div>";

    private readonly string example8RazorCode = @"
<BitIcon IconName=""@BitIconName.CompletedSolid"" AriaLabel=""Succeeded"" />

<BitIcon IconName=""@BitIconName.ErrorBadge"" Title=""Failed on the last run"" />

<BitTooltip Text=""Failed runs are retried three times, a minute apart."">
    <BitIcon IconName=""@BitIconName.Info"" AriaLabel=""Retry policy"" TabIndex=""0"" />
</BitTooltip>

<span><BitIcon IconName=""@BitIconName.Attach"" /> Decorative, beside its own label</span>";

    private readonly string example9RazorCode = @"
<BitParams Parameters=""@iconParams"">
    <BitIcon IconName=""@BitIconName.Mail"" />
    <BitIcon IconName=""@BitIconName.Calendar"" />
    <BitIcon IconName=""@BitIconName.TaskManager"" />
    <BitIcon IconName=""@BitIconName.Settings"" Variant=""BitVariant.Fill"" />
</BitParams>";
    private readonly string example9CsharpCode = @"
private readonly BitIconParams[] iconParams =
[
    new()
    {
        Variant = BitVariant.Outline,
        Circular = true,
    }
];";

    private readonly string example10RazorCode = @"
<style>
    .on-dark {
        color: var(--bit-clr-bg-sec);
        background-color: var(--bit-clr-fg-sec);
    }
</style>

@foreach (var color in colors)
{
    <div class=""@(IsBackground(color) ? ""on-dark"" : """")"">
        <BitIcon IconName=""@BitIconName.Accept"" Color=""color"" />
        <BitIcon IconName=""@BitIconName.Accept"" Color=""color"" Variant=""BitVariant.Outline"" />
        <BitIcon IconName=""@BitIconName.Accept"" Color=""color"" Variant=""BitVariant.Fill"" />
        <BitIcon IconName=""@BitIconName.Accept"" Color=""color"" Variant=""BitVariant.Fill"" Disabled />
        <span>@color</span>
    </div>
}";
    private readonly string example10CsharpCode = @"
private readonly BitColor[] colors = Enum.GetValues<BitColor>();

// The background roles are the page's own surface colors, so they are shown on a dark panel to be seen at all.
private static bool IsBackground(BitColor color) =>
    color is BitColor.PrimaryBackground or BitColor.SecondaryBackground or BitColor.TertiaryBackground;";

    private readonly string example11RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />
<link rel=""stylesheet"" href=""https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined"" />


<BitIcon Icon=""@(""fa-solid fa-house"")"" />
<BitIcon Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" />
<BitIcon Icon=""@BitIconInfo.Fa(""solid rocket"")"" Color=""BitColor.Secondary"" />

<BitIcon Icon=""@(""bi bi-house-fill"")"" />
<BitIcon Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Bi(""github"")"" />
<BitIcon Icon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Secondary"" />

<BitIcon Icon=""@BitIconInfo.Ms(""home"")"" />
<BitIcon Icon=""@BitIconInfo.Ms(""favorite"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Ms(""settings"")"" Animation=""BitIconAnimation.Spin"" />
<BitIcon Icon=""@BitIconInfo.Ms(""rocket_launch"")"" Color=""BitColor.Secondary"" />

<BitIcon IconName=""house"" IconResolver=""@faResolver"" />
<BitIcon IconName=""heart"" IconResolver=""@faResolver"" Color=""BitColor.Error"" />
<BitIcon IconName=""rocket"" IconResolver=""@faResolver"" Color=""BitColor.Secondary"" />
<BitIcon IconName=""Accept"" IconResolver=""@faResolver"" Color=""BitColor.Success"" />";
    private readonly string example11CsharpCode = @"
// Every name this app writes is a FontAwesome one - except the ones FontAwesome does not have,
// which are left to the built-in set by answering with nothing.
private readonly Func<string, BitIconInfo?> faResolver =
    name => name is ""house"" or ""heart"" or ""rocket"" ? BitIconInfo.Fa($""solid {name}"") : null;

// The same resolver given to every icon of a subtree at once:
// <BitParams Parameters=""@([new BitIconParams { IconResolver = faResolver }])"">...</BitParams>";

    private readonly string example12RazorCode = @"
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />

<BitIcon FontSize=""1.5rem"" IconName=""@BitIconName.Accept"" />
<BitIcon FontSize=""2.5rem"" IconName=""@BitIconName.Accept"" />
<BitIcon FontSize=""4rem"" IconName=""@BitIconName.Accept"" />

<div style=""font-size:1.75rem"">
    Sized by the text around it <BitIcon FontSize=""inherit"" IconName=""@BitIconName.FavoriteStarFill"" />
</div>";

    private readonly string example13RazorCode = @"
<style>
    .icon-class {
        padding: 4px;
        font-size: 3rem;
        margin-left: 1rem;
        background-color: aquamarine;
    }
</style>

<BitIcon Size=""BitSize.Large""
         IconName=""@BitIconName.Accept""
         Style=""color: white; background-color: brown; border-radius: 4px"" />

<BitIcon Class=""icon-class""
         IconName=""@BitIconName.Accept"" />

<BitIcon IconName=""@BitIconName.Heart""
         Variant=""BitVariant.Fill""
         Style=""--bit-Icon-color: hotpink; --bit-Icon-radius: 50% 0;"" />";

    private readonly string example14RazorCode = @"
<div dir=""rtl"">
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Accept"" />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Bus"" Color=""BitColor.Info"" />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Forward"" FlipRtl />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Send"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" FlipRtl />
</div>";
}

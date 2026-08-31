namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private bool isStarred = true;
    private int clickCount;

    // Every name the FontAwesome example writes is a FontAwesome one - except the ones FontAwesome does
    // not have, which are left to the built-in set by answering with nothing.
    private readonly Func<string, BitIconInfo?> faResolver =
        name => name is "house" or "heart" or "rocket" ? BitIconInfo.Fa($"solid {name}") : null;



    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Animation",
            Type = "BitIconAnimation?",
            DefaultValue = "null",
            Description = "Specifies a looping animation to play on the icon. An animation is drawn with a transform of its own, so it replaces Rotate and Flip; Fade, which only changes opacity, combines with both.",
            LinkType = LinkType.Link,
            Href = "#animation-enum",
        },
        new()
        {
            Name = "AnimationDuration",
            Type = "string?",
            DefaultValue = "null",
            Description = "Overrides how long one cycle of the animation takes, as any CSS time. The reduced motion factor still multiplies it, so an animation asked to run fast still slows down for a reader who asked for less motion.",
        },
        new()
        {
            Name = "AnimationDelay",
            Type = "string?",
            DefaultValue = "null",
            Description = "Waits this long before the animation starts, as any CSS time - which is what turns a row of identical animated icons into a wave. The wait is not stretched under reduced motion the way the cycle is.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered inside the icon element, for an icon set that is neither a font nor a class - an inline svg, an image, a ligature of your own. The color, the size and the variant still apply around it.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the icon in a circle rather than in the rounded box of the design system, squaring the box off at the same time so a narrow glyph and a wide one are drawn in circles of the same size.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "Specifies the color theme of the icon. Default value is BitColor.Primary.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "FixedWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the icon in a box of a fixed width so that a column of icons of different widths lines up.",
        },
        new()
        {
            Name = "Flip",
            Type = "BitIconFlip?",
            DefaultValue = "null",
            Description = "Mirrors the icon on the horizontal axis, the vertical axis, or both.",
            LinkType = LinkType.Link,
            Href = "#flip-enum",
        },
        new()
        {
            Name = "FlipRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Mirrors the icon horizontally when it is rendered in a right-to-left direction. The direction is read off the rendered document, so it follows an ancestor's dir as well as the component's own Dir.",
        },
        new()
        {
            Name = "FontSize",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the font size of the icon, as any CSS length or the inherit keyword. Overrides Size when both are given.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Specifies the icon configuration for rendering icons from external icon libraries. Takes precedence over IconName when both name a glyph.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the name of the icon from the built-in Fluent UI icon library. This property is ignored when Icon names a glyph.",
            LinkType = LinkType.Link,
            Href = "/iconography",
        },
        new()
        {
            Name = "IconResolver",
            Type = "Func<string, BitIconInfo?>?",
            DefaultValue = "null",
            Description = "Names the icon set that IconName is a name in - name => BitIconInfo.Fa(name), BitIconInfo.Ms, or a lookup of your own. An Icon that names a glyph still wins over it, and a resolver that answers with null leaves the name to the built-in set. Cascades through BitParams to a whole subtree.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Drops the icon a quarter of an em below the baseline so that an inline svg or an image given as ChildContent sits centered on the line of text it is written in. A glyph of an icon font needs none of it.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "The callback for when the icon is clicked. An icon with a click handler joins the tab order, answers Enter and Space, and is announced as a button - so give it an AriaLabel or a Title.",
        },
        new()
        {
            Name = "Rotate",
            Type = "BitIconRotate?",
            DefaultValue = "null",
            Description = "Turns the icon by a quarter, a half, or three quarters of a turn.",
            LinkType = LinkType.Link,
            Href = "#rotate-enum",
        },
        new()
        {
            Name = "RotateAngle",
            Type = "int?",
            DefaultValue = "null",
            Description = "Turns the icon by an angle of your own, in degrees, negative for counter-clockwise. It replaces Rotate when both are given, and composes with Flip and FlipRtl.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Specifies the size of the icon. Default value is BitSize.Medium.",
            LinkType = LinkType.Link,
            Href = "#icon-size-enum",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text shown in the native tooltip when the pointer rests on the icon. It also names the icon for assistive technology, so an icon that carries one is announced rather than skipped.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "Specifies the visual styling variant of the icon. Default value is BitVariant.Text.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives focus to the icon element. Only an icon the browser can focus takes it: one with an OnClick handler, or one given a TabIndex of its own.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives focus to the icon element, leaving the page scrolled where it is instead of bringing the icon into view.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Names a glyph for any icon set. A class-based set (Fabric MDL2, FontAwesome, Bootstrap Icons) is described by BaseClass, Prefix and Name; a ligature-based set (Material Icons, Material Symbols) puts the family on BaseClass and the ligature on Content. The static factories build each of them: Bit(name), Fa(icons), Bi(name), Mi(name, style), Ms(name, style), Css(cssClasses), and From(icon, iconName) which resolves an Icon/IconName pair. A plain string converts implicitly and is taken as the complete class list.",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The name of the icon. For an external set this can be the complete CSS class list when BaseClass and Prefix are empty."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The base CSS class of the icon set - \"bit-icon\" for the built-in set, \"bi\" for Bootstrap Icons, \"material-symbols-outlined\" for Material Symbols. Leave it empty for a set that needs none."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The CSS class prefix written before the icon name - \"bit-icon--\" for the built-in set, \"bi-\" for Bootstrap Icons. Leave it empty for a set that uses none."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text rendered inside the icon element - the ligature of a ligature-based icon set such as Material Icons or Material Symbols. Class-based sets leave it null. Only a component that renders the icon's content puts it on the page, which BitIcon does; the glyphs the library draws inside its other controls are class-based, so a ligature set has to be given to a BitIcon."
               },
               new()
               {
                   Name = "IsEmpty",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "Whether this instance names no glyph at all - nothing to put in a class attribute, and nothing to write as the element's text. An empty instance is treated as no icon, so an IconName given beside it is still used."
               },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "icon-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "Display icon using small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "Display icon using medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "Display icon using large size.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "rotate-enum",
            Name = "BitIconRotate",
            Description = "The quarter turns an icon can be rendered at.",
            Items =
            [
                new()
                {
                    Name = "Rotate90",
                    Description = "A quarter turn clockwise.",
                    Value = "0",
                },
                new()
                {
                    Name = "Rotate180",
                    Description = "A half turn.",
                    Value = "1",
                },
                new()
                {
                    Name = "Rotate270",
                    Description = "A quarter turn counter-clockwise.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "flip-enum",
            Name = "BitIconFlip",
            Description = "The axes an icon can be mirrored on.",
            Items =
            [
                new()
                {
                    Name = "Horizontal",
                    Description = "Mirrored left to right.",
                    Value = "0",
                },
                new()
                {
                    Name = "Vertical",
                    Description = "Mirrored top to bottom.",
                    Value = "1",
                },
                new()
                {
                    Name = "Both",
                    Description = "Mirrored on both axes, which is the same as a half turn for an asymmetric glyph.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "animation-enum",
            Name = "BitIconAnimation",
            Description = "The looping animations an icon can play. All of them slow down rather than stop under reduced motion, and ForceAnimation restores their full speed. AnimationDuration replaces the length of one cycle, and a Rotate, RotateAngle or Flip is drawn beside the animation rather than under it.",
            Items =
            [
                new()
                {
                    Name = "Spin",
                    Description = "Turns continuously clockwise - the loading spinner.",
                    Value = "0",
                },
                new()
                {
                    Name = "SpinReverse",
                    Description = "Turns continuously counter-clockwise.",
                    Value = "1",
                },
                new()
                {
                    Name = "Pulse",
                    Description = "Turns clockwise in eight discrete steps, the way a segmented spinner ticks around.",
                    Value = "2",
                },
                new()
                {
                    Name = "Beat",
                    Description = "Scales up and back down, to draw the eye to something that just changed.",
                    Value = "3",
                },
                new()
                {
                    Name = "Fade",
                    Description = "Fades out and back in.",
                    Value = "4",
                },
                new()
                {
                    Name = "Shake",
                    Description = "Rocks back and forth, for something that needs attention now.",
                    Value = "5",
                },
                new()
                {
                    Name = "Bounce",
                    Description = "Jumps up and lands again, squashing on the way out and on the way back - the heaviest of these.",
                    Value = "6",
                },
                new()
                {
                    Name = "BeatFade",
                    Description = "Scales up and fades in together, which reads as a slower, softer Beat.",
                    Value = "7",
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" />
<BitIcon IconName=""@BitIconName.Bus"" />
<BitIcon IconName=""@BitIconName.Pinned"" />

<BitIcon IconName=""@BitIconName.Accept"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Bus"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Pinned"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Text"" />

<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Text"" IsEnabled=""false"" />

<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Circular />
<BitIcon IconName=""@BitIconName.Cancel"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Circular />
<BitIcon IconName=""@BitIconName.Info"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Size=""BitSize.Large"" Circular />";

    private readonly string example3RazorCode = @"
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" Rotate=""BitIconRotate.Rotate90"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" Rotate=""BitIconRotate.Rotate180"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" Rotate=""BitIconRotate.Rotate270"" />

<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" RotateAngle=""45"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" RotateAngle=""135"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" RotateAngle=""-30"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" RotateAngle=""200"" Flip=""BitIconFlip.Horizontal"" />

<BitIcon IconName=""@BitIconName.ReplyAlt"" Size=""BitSize.Large"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Size=""BitSize.Large"" Flip=""BitIconFlip.Horizontal"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Size=""BitSize.Large"" Flip=""BitIconFlip.Vertical"" />
<BitIcon IconName=""@BitIconName.ReplyAlt"" Size=""BitSize.Large"" Flip=""BitIconFlip.Both"" />

<div>
    <BitIcon IconName=""@BitIconName.Forward"" Size=""BitSize.Large"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Back"" Size=""BitSize.Large"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Clock"" Size=""BitSize.Large"" />
</div>

<div dir=""rtl"">
    <BitIcon IconName=""@BitIconName.Forward"" Size=""BitSize.Large"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Back"" Size=""BitSize.Large"" FlipRtl />
    <BitIcon IconName=""@BitIconName.Clock"" Size=""BitSize.Large"" />
</div>";

    private readonly string example4RazorCode = @"
<BitIcon IconName=""@BitIconName.Sync"" Size=""BitSize.Large"" Animation=""BitIconAnimation.Spin"" />
<BitIcon IconName=""@BitIconName.Sync"" Size=""BitSize.Large"" Animation=""BitIconAnimation.SpinReverse"" />
<BitIcon IconName=""@BitIconName.ProgressRingDots"" Size=""BitSize.Large"" Animation=""BitIconAnimation.Pulse"" />
<BitIcon IconName=""@BitIconName.Heart"" Size=""BitSize.Large"" Color=""BitColor.Error"" Animation=""BitIconAnimation.Beat"" />
<BitIcon IconName=""@BitIconName.StatusCircleInner"" Size=""BitSize.Large"" Color=""BitColor.Success"" Animation=""BitIconAnimation.Fade"" />
<BitIcon IconName=""@BitIconName.Ringer"" Size=""BitSize.Large"" Color=""BitColor.Warning"" Animation=""BitIconAnimation.Shake"" />
<BitIcon IconName=""@BitIconName.Up"" Size=""BitSize.Large"" Color=""BitColor.Info"" Animation=""BitIconAnimation.Bounce"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Size=""BitSize.Large"" Color=""BitColor.Error"" Animation=""BitIconAnimation.BeatFade"" />

<BitIcon IconName=""@BitIconName.Sync"" Size=""BitSize.Large"" Animation=""BitIconAnimation.Spin"" AnimationDuration=""4s"" />
<BitIcon IconName=""@BitIconName.Sync"" Size=""BitSize.Large"" Animation=""BitIconAnimation.Spin"" AnimationDuration=""0.4s"" />
<BitIcon IconName=""@BitIconName.Ringer"" Size=""BitSize.Large"" Color=""BitColor.Warning"" Animation=""BitIconAnimation.Shake"" AnimationDuration=""2s"" />
<BitIcon IconName=""@BitIconName.Send"" Size=""BitSize.Large"" Color=""BitColor.Success"" Animation=""BitIconAnimation.Beat"" RotateAngle=""45"" />

<BitIcon IconName=""@BitIconName.CircleFill"" Color=""BitColor.Info"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Color=""BitColor.Info"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" AnimationDelay=""0.2s"" />
<BitIcon IconName=""@BitIconName.CircleFill"" Color=""BitColor.Info"" Animation=""BitIconAnimation.Fade"" AnimationDuration=""1.2s"" AnimationDelay=""0.4s"" />";

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
<BitIcon IconName=""@BitIconName.FavoriteStarFill""
         Size=""BitSize.Large""
         Color=""@(isStarred ? BitColor.Warning : BitColor.TertiaryForeground)""
         Title=""@(isStarred ? ""Remove from favorites"" : ""Add to favorites"")""
         OnClick=""() => isStarred = !isStarred"" />

<BitIcon IconName=""@BitIconName.Refresh""
         Size=""BitSize.Large""
         Variant=""BitVariant.Outline""
         AriaLabel=""Refresh the list""
         OnClick=""() => clickCount++"" />

<BitIcon IconName=""@BitIconName.Delete""
         Size=""BitSize.Large""
         Color=""BitColor.Error""
         Title=""Deleting is unavailable here""
         IsEnabled=""false""
         OnClick=""() => clickCount++"" />

<div>Clicked @clickCount times.</div>";
    private readonly string example6CsharpCode = @"
private bool isStarred = true;
private int clickCount;";

    private readonly string example7RazorCode = @"
<BitIcon Color=""BitColor.Info"" Size=""BitSize.Large"">
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M12 2 15.1 8.6 22 9.7l-5 4.9 1.2 7L12 18.3 5.8 21.6 7 14.6l-5-4.9 6.9-1.1z"" />
    </svg>
</BitIcon>

<BitIcon Color=""BitColor.Success"" Variant=""BitVariant.Outline"">
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
    </svg>
</BitIcon>

<BitIcon Color=""BitColor.Error"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"">
    <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
        <path d=""M12 21.35 10.55 20C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54z"" />
    </svg>
</BitIcon>

<div style=""font-size:1.25rem"">
    Aligned by its box
    <BitIcon Color=""BitColor.Info"" FontSize=""inherit"">
        <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
            <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
        </svg>
    </BitIcon>
    and dropped onto the line with Inline
    <BitIcon Color=""BitColor.Info"" FontSize=""inherit"" Inline>
        <svg width=""1em"" height=""1em"" viewBox=""0 0 24 24"" fill=""currentColor"">
            <path d=""M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"" />
        </svg>
    </BitIcon>
</div>";

    private readonly string example8RazorCode = @"
<BitIcon IconName=""@BitIconName.CompletedSolid"" Color=""BitColor.Success"" AriaLabel=""Succeeded"" />

<BitIcon IconName=""@BitIconName.ErrorBadge"" Color=""BitColor.Error"" Title=""Failed on the last run"" />

<span><BitIcon IconName=""@BitIconName.Attach"" /> Attachment</span>";

    private readonly string example9RazorCode = @"
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Pinned"" />

<div style=""background:var(--bit-clr-fg-sec);padding:1rem;color:var(--bit-clr-bg-sec)"">
    <BitIcon Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Accept"" />
    <BitIcon Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Bus"" />
    <BitIcon Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Pinned"" />

    <BitIcon Color=""BitColor.SecondaryBackground"" IconName=""@BitIconName.Accept"" />
    <BitIcon Color=""BitColor.SecondaryBackground"" IconName=""@BitIconName.Bus"" />
    <BitIcon Color=""BitColor.SecondaryBackground"" IconName=""@BitIconName.Pinned"" />

    <BitIcon Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Accept"" />
    <BitIcon Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Bus"" />
    <BitIcon Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Pinned"" />
</div>

<BitIcon Color=""BitColor.PrimaryForeground"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.PrimaryForeground"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.PrimaryForeground"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.SecondaryForeground"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.SecondaryForeground"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.SecondaryForeground"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.TertiaryForeground"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.TertiaryForeground"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.TertiaryForeground"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.PrimaryBorder"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.PrimaryBorder"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.PrimaryBorder"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.SecondaryBorder"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.SecondaryBorder"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.SecondaryBorder"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.TertiaryBorder"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.TertiaryBorder"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.TertiaryBorder"" IconName=""@BitIconName.Pinned"" />";

    private readonly string example10RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitIcon Icon=""@(""fa-solid fa-house"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Fa(""solid rocket"")"" Color=""BitColor.Secondary"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitIcon Icon=""@(""bi bi-house-fill"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Bi(""github"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Secondary"" />


<link rel=""stylesheet"" href=""https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined"" />

<BitIcon Icon=""@BitIconInfo.Ms(""home"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Ms(""favorite"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Ms(""settings"")"" Size=""BitSize.Large"" Animation=""BitIconAnimation.Spin"" />
<BitIcon Icon=""@BitIconInfo.Ms(""rocket_launch"")"" Color=""BitColor.Secondary"" />


<BitIcon IconName=""house"" IconResolver=""@faResolver"" Size=""BitSize.Large"" />
<BitIcon IconName=""heart"" IconResolver=""@faResolver"" Color=""BitColor.Error"" />
<BitIcon IconName=""rocket"" IconResolver=""@faResolver"" Color=""BitColor.Secondary"" />
<BitIcon IconName=""Accept"" IconResolver=""@faResolver"" Color=""BitColor.Success"" />";
    private readonly string example10CsharpCode = @"
// Every name this app writes is a FontAwesome one - except the ones FontAwesome does not have,
// which are left to the built-in set by answering with nothing.
private readonly Func<string, BitIconInfo?> faResolver =
    name => name is ""house"" or ""heart"" or ""rocket"" ? BitIconInfo.Fa($""solid {name}"") : null;

// The same resolver given to every icon of a subtree at once:
// <BitParams Parameters=""@([new BitIconParams { IconResolver = faResolver }])"">...</BitParams>";

    private readonly string example11RazorCode = @"
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Pinned"" />

<BitIcon FontSize=""1.5rem"" IconName=""@BitIconName.Accept"" />
<BitIcon FontSize=""2.5rem"" IconName=""@BitIconName.Bus"" />
<BitIcon FontSize=""4rem"" IconName=""@BitIconName.Pinned"" />

<div style=""font-size:1.75rem"">
    Sized by the text around it <BitIcon FontSize=""inherit"" IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
</div>";

    private readonly string example12RazorCode = @"
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
         Style=""background-color: brown; border-radius: 4px"" />

<BitIcon Class=""icon-class""
         IconName=""@BitIconName.Accept"" />";

    private readonly string example13RazorCode = @"
<div dir=""rtl"">
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Accept"" />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Bus"" Color=""BitColor.Info"" />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Forward"" FlipRtl />
    <BitIcon Dir=""BitDir.Rtl"" IconName=""@BitIconName.Send"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" FlipRtl />
</div>";
}

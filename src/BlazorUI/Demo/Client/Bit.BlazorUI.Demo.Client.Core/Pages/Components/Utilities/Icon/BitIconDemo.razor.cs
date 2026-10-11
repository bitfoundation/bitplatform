namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private bool isStarred = true;
    private int clickCount;
    private int replayKey;

    // Every name the FontAwesome example writes is a FontAwesome one - except the ones FontAwesome does
    // not have, which are left to the built-in set by answering with nothing.
    private readonly Func<string, BitIconInfo?> faResolver =
        name => name is "house" or "heart" or "rocket" ? BitIconInfo.Fa($"solid {name}") : null;

    private readonly BitIconParams[] iconParams =
    [
        new()
        {
            Variant = BitVariant.Outline,
            Circular = true,
        }
    ];

    private readonly BitColor[] colors = Enum.GetValues<BitColor>();

    // The background roles are the page's own surface colors, so they are shown on a dark panel to be seen at all.
    private static bool IsBackground(BitColor color) =>
        color is BitColor.PrimaryBackground or BitColor.SecondaryBackground or BitColor.TertiaryBackground;



    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Animation",
            Type = "BitIconAnimation?",
            DefaultValue = "null",
            Description = "A looping animation to play on the icon. It composes with Rotate, RotateAngle and Flip: a mirrored arrow still spins.",
            LinkType = LinkType.Link,
            Href = "#animation-enum",
        },
        new()
        {
            Name = "AnimationDuration",
            Type = "string?",
            DefaultValue = "null",
            Description = "How long one cycle of the animation takes, as any CSS time. Still slowed down under reduced motion.",
        },
        new()
        {
            Name = "AnimationDelay",
            Type = "string?",
            DefaultValue = "null",
            Description = "How long to wait before the animation starts, as any CSS time.",
        },
        new()
        {
            Name = "AnimationIterationCount",
            Type = "int?",
            DefaultValue = "null",
            Description = "How many times the animation plays before the icon comes to rest. Unset, it loops. Beat and Fade run out and back as two iterations.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content rendered inside the icon element instead of a glyph - an inline svg, an image. The color, size and variant still apply.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the Fill or Outline box as a circle, the same size for narrow and wide glyphs.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "Specifies the color theme of the icon. An explicit value wins over the --bit-Icon-* color variables; left unset, the icon is primary unless they say otherwise.",
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
            Description = "Mirrors the icon horizontally when it renders right-to-left, whether by its own Dir or an ancestor's dir.",
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
            Description = "The name of a glyph in the built-in Fabric MDL2 set (or in another set, through IconResolver). Ignored when Icon names a glyph.",
            LinkType = LinkType.Link,
            Href = "/iconography",
        },
        new()
        {
            Name = "IconResolver",
            Type = "Func<string, BitIconInfo?>?",
            DefaultValue = "null",
            Description = "Maps an IconName to an icon of another set - name => BitIconInfo.Fa(name), or a lookup of your own. Icon wins over it; answering null leaves the name to the built-in set. Cascades through BitParams.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Drops an inline svg or image given as ChildContent onto the line of text it sits in.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "The callback for when the icon is clicked. The icon then becomes a button: a tab stop answering Enter and Space. Name it with an AriaLabel or a Title.",
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
            Description = "Specifies the size of the icon. An explicit value wins over --bit-Icon-size; left unset, the icon is medium unless it says otherwise. FontSize wins over both.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The native tooltip text. It also names the icon for assistive technology.",
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
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitSize(description: "The three icon sizes of the theme."),
        DemoSharedEnums.BitVariant(),
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
            Description = "The looping animations an icon can play. They slow down rather than stop under reduced motion; ForceAnimation restores their full speed, AnimationIterationCount stops them after a few cycles.",
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Icon-color",
            DefaultValue = "--bit-clr-pri",
            Description = "The icon's color: the glyph of a Text or an Outline icon, the box of a Fill one. The Color parameter wins over it. currentColor makes a Text icon follow the text around it - set it around Text icons only, since a Fill icon would paint its box in its own glyph color.",
        },
        new()
        {
            Name = "--bit-Icon-contrast-color",
            DefaultValue = "--bit-clr-pri-text",
            Description = "The glyph drawn over the box of a Fill icon, and of a clickable Outline icon under the pointer. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Icon-hover-color",
            DefaultValue = "a shade of --bit-Icon-color when it is set, otherwise --bit-clr-pri-hover",
            Description = "The color of a clickable icon under the pointer. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Icon-active-color",
            DefaultValue = "a shade of the hover color when either variable above is set, otherwise --bit-clr-pri-active",
            Description = "The color of a clickable icon while pressed. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Icon-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the keyboard focus ring of a clickable or otherwise focusable icon. The Color parameter wins over it. While it is unset, the focus ring is the library's own --bit-shd-focus-ring, so replacing that token re-shapes it too.",
        },
        new()
        {
            Name = "--bit-Icon-size",
            DefaultValue = "--bit-siz-icon-md",
            Description = "Font size, which is the size of the glyph. The Size parameter wins over it, and FontSize over both.",
        },
        new()
        {
            Name = "--bit-Icon-padding",
            DefaultValue = "half a spacing unit (4px)",
            Description = "Room around the glyph in a Fill or an Outline box.",
        },
        new()
        {
            Name = "--bit-Icon-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of a Fill or an Outline box. Circular wins over it.",
        },
        new()
        {
            Name = "--bit-Icon-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Border width of a Fill or an Outline box.",
        },
        new()
        {
            Name = "--bit-Icon-fixed-width",
            DefaultValue = "1.25em",
            Description = "Width of a FixedWidth icon, for a set whose widest glyph needs more (or less) room.",
        },
    ];
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Text;

public partial class BitTextDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Align",
            Type = "BitTextAlign?",
            DefaultValue = "null",
            Description = "Sets the horizontal alignment of the text content. Start and End follow the direction of the text, while Left and Right do not.",
            LinkType = LinkType.Link,
            Href = "#text-align-enum"
        },
        new()
        {
            Name = "AriaLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "Sets the level of the heading the text is announced as, without changing the rendered tag. On a tag that is not already a heading a heading role is written beside it.",
        },
        new()
        {
            Name = "Block",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the text as a block level element, which is what the inline variants need before they have a width to align inside or to truncate.",
        },
        new()
        {
            Name = "BreakWord",
            Type = "bool",
            DefaultValue = "false",
            Description = "Breaks a word that is too long for its line rather than letting it overflow, leaving the words that do fit alone.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the text. It is not rendered where Element names a void element.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the text.",
            LinkType = LinkType.Link,
            Href = "#color-enum"

        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. A value that is not a name a tag can have falls back to the tag of the typography variant.",
        },
        new()
        {
            Name = "ForceBreak",
            Type = "bool",
            DefaultValue = "false",
            Description = "Forces the text to always break at the end.",
        },
        new()
        {
            Name = "Foreground",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The kind of the foreground color of the text.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "Gradient",
            Type = "string?",
            DefaultValue = "null",
            Description = "Paints the glyphs of the text with a CSS gradient instead of with a flat color. The value is written as the background-image of the element and clipped to the text, and the fill is taken away by itself.",
        },
        new()
        {
            Name = "Gutter",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text will have a bottom margin, sized in em so that it follows the size of the variant.",
        },
        new()
        {
            Name = "Hyphenate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hyphenates the words that are broken across two lines, which needs a Lang the browser carries a dictionary for.",
        },
        new()
        {
            Name = "Italic",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the text in italics.",
        },
        new()
        {
            Name = "Lang",
            Type = "string?",
            DefaultValue = "null",
            Description = "The language of the text, written as the lang attribute of the rendered element.",
        },
        new()
        {
            Name = "LineClamp",
            Type = "int?",
            DefaultValue = "null",
            Description = "Truncates the text after the given number of lines with an ellipsis. A value below one leaves the text alone.",
        },
        new()
        {
            Name = "Monospace",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the text in the theme's monospaced family, so that every character is drawn at the same width and a column of them lines up.",
        },
        new()
        {
            Name = "NoSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the text from being selected.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.",
        },
        new()
        {
            Name = "Numeric",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the digits of the text at a single width, so that they line up across the lines.",
        },
        new()
        {
            Name = "PreserveWhitespace",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the line breaks and the runs of spaces of the content as they were written, while the lines still too wide for the box go on wrapping. NoWrap has the last word over it.",
        },
        new()
        {
            Name = "Strikethrough",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a line through the text. It combines with Underline.",
        },
        new()
        {
            Name = "Transform",
            Type = "BitTextTransform?",
            DefaultValue = "null",
            Description = "The capitalization of the text. The transform is visual only, so the characters in the document are the ones that were written.",
            LinkType = LinkType.Link,
            Href = "#text-transform-enum"
        },
        new()
        {
            Name = "Trim",
            Type = "BitTextTrim?",
            DefaultValue = "null",
            Description = "Trims the half-leading off the top, the bottom or both edges of the box the text draws in, so that the gap around it is the one that was written.",
            LinkType = LinkType.Link,
            Href = "#text-trim-enum"
        },
        new()
        {
            Name = "Typography",
            Type = "BitTypography?",
            DefaultValue = "null",
            Description = "The typography of the text.",
            LinkType = LinkType.Link,
            Href = "#typography-enum"
        },
        new()
        {
            Name = "Underline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Underlines the text. It combines with Strikethrough.",
        },
        new()
        {
            Name = "VisuallyHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the text from the page while keeping it available to assistive technologies.",
        },
        new()
        {
            Name = "Weight",
            Type = "BitFontWeight?",
            DefaultValue = "null",
            Description = "The font weight of the text. Left unset, the weight is the one the typography variant carries.",
            LinkType = LinkType.Link,
            Href = "#font-weight-enum"
        },
        new()
        {
            Name = "Wrap",
            Type = "BitTextWrap?",
            DefaultValue = "null",
            Description = "How the lines of the text are broken. NoWrap and LineClamp have the last word over it.",
            LinkType = LinkType.Link,
            Href = "#text-wrap-enum"
        }
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
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
            ]
        },
        new()
        {
            Id = "font-weight-enum",
            Name = "BitFontWeight",
            Description = "Defines the font weights of the typography ramp available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Light", Description = "The lightest step of the weight scale.", Value = "0" },
                new() { Name = "Regular", Description = "The weight of body copy, and the default of nearly every typography variant.", Value = "1" },
                new() { Name = "Medium", Description = "The step between the body copy and the titles.", Value = "2" },
                new() { Name = "Semibold", Description = "The weight of the titles and of the labels of the interactive controls.", Value = "3" },
                new() { Name = "Bold", Description = "The heaviest step of the weight scale.", Value = "4" },
            ]
        },
        new()
        {
            Id = "text-align-enum",
            Name = "BitTextAlign",
            Description = "Defines the horizontal alignment of a run of text. The values are the CSS text-align keywords.",
            Items =
            [
                new() { Name = "Start", Description = "Aligns to the leading edge of the text, whichever direction it runs in.", Value = "0" },
                new() { Name = "End", Description = "Aligns to the trailing edge of the text, whichever direction it runs in.", Value = "1" },
                new() { Name = "Left", Description = "Aligns to the left edge, whichever direction the text runs in.", Value = "2" },
                new() { Name = "Right", Description = "Aligns to the right edge, whichever direction the text runs in.", Value = "3" },
                new() { Name = "Center", Description = "Centers the lines inside the box.", Value = "4" },
                new() { Name = "Justify", Description = "Spaces the words of every line but the last so that both edges line up.", Value = "5" },
                new() { Name = "JustifyAll", Description = "Justifies the last line as well. No browser engine implements it yet.", Value = "6" },
                new() { Name = "MatchParent", Description = "Inherits the alignment, resolving a start or an end against the direction of the parent.", Value = "7" },
                new() { Name = "Inherit", Description = "Takes the alignment of the parent.", Value = "8" },
                new() { Name = "Initial", Description = "Takes the initial value of the property.", Value = "9" },
                new() { Name = "Revert", Description = "Reverts to the value the user agent or the user stylesheet sets.", Value = "10" },
                new() { Name = "RevertLayer", Description = "Reverts to the value of the previous cascade layer.", Value = "11" },
                new() { Name = "Unset", Description = "Inherits the alignment, or takes the initial value where it is not inherited.", Value = "12" },
            ]
        },
        new()
        {
            Id = "text-transform-enum",
            Name = "BitTextTransform",
            Description = "Defines the capitalization of a run of text in the bit BlazorUI.",
            Items =
            [
                new() { Name = "None", Description = "The text is rendered with the capitalization it was written in.", Value = "0" },
                new() { Name = "Uppercase", Description = "Every character is rendered in upper case.", Value = "1" },
                new() { Name = "Lowercase", Description = "Every character is rendered in lower case.", Value = "2" },
                new() { Name = "Capitalize", Description = "The first character of every word is rendered in upper case.", Value = "3" },
            ]
        },
        new()
        {
            Id = "text-trim-enum",
            Name = "BitTextTrim",
            Description = "Defines which of the two half-leadings of a run of text is trimmed away in the bit BlazorUI.",
            Items =
            [
                new() { Name = "None", Description = "Neither half-leading is trimmed, which is what a line box does of its own.", Value = "0" },
                new() { Name = "Start", Description = "The half-leading above the first line is trimmed, so that the top of the box is the cap height of the text.", Value = "1" },
                new() { Name = "End", Description = "The half-leading below the last line is trimmed, so that the bottom of the box is the alphabetic baseline.", Value = "2" },
                new() { Name = "Both", Description = "Both half-leadings are trimmed, so that the box is exactly as tall as the glyphs it draws.", Value = "3" },
            ]
        },
        new()
        {
            Id = "text-wrap-enum",
            Name = "BitTextWrap",
            Description = "Defines how the lines of a run of text are broken in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Wrap", Description = "The text is broken into lines the usual way.", Value = "0" },
                new() { Name = "NoWrap", Description = "The text is not broken into lines at all and overflows its container instead.", Value = "1" },
                new() { Name = "Balance", Description = "The lines are balanced so that they come out of a similar length. Engines only balance a short block.", Value = "2" },
                new() { Name = "Pretty", Description = "The break points avoid leaving a short last line. This is the one for body copy.", Value = "3" },
                new() { Name = "Stable", Description = "The lines already laid out keep their break points while the text after them is edited.", Value = "4" },
            ]
        },
        new()
        {
            Id = "typography-enum",
            Name = "BitTypography",
            Description = "Defines the steps of the theme's typography ramp, and the tag each of them renders on its own.",
            Items =
            [
                new() { Name = "H1", Description = "Renders an h1.", Value = "0" },
                new() { Name = "H2", Description = "Renders an h2.", Value = "1" },
                new() { Name = "H3", Description = "Renders an h3.", Value = "2" },
                new() { Name = "H4", Description = "Renders an h4.", Value = "3" },
                new() { Name = "H5", Description = "Renders an h5.", Value = "4" },
                new() { Name = "H6", Description = "Renders an h6.", Value = "5" },
                new() { Name = "Subtitle1", Description = "Renders an h6. The default variant.", Value = "6" },
                new() { Name = "Subtitle2", Description = "Renders an h6.", Value = "7" },
                new() { Name = "Body1", Description = "Renders a p.", Value = "8" },
                new() { Name = "Body2", Description = "Renders a p.", Value = "9" },
                new() { Name = "Button", Description = "Renders a span.", Value = "10" },
                new() { Name = "Caption1", Description = "Renders a span.", Value = "11" },
                new() { Name = "Caption2", Description = "Renders a span.", Value = "12" },
                new() { Name = "Overline", Description = "Renders a span.", Value = "13" },
                new() { Name = "Inherit", Description = "Renders a p, taking every typographic declaration from the element around it.", Value = "14" },
            ]
        }
    ];



    private string example1RazorCode = @"
<BitText>This is default (Subtitle1)</BitText>

<BitText Typography=""BitTypography.H1"">H1. Heading</BitText>
<BitText Typography=""BitTypography.H2"">H2. Heading</BitText>
<BitText Typography=""BitTypography.H3"">H3. Heading</BitText>
<BitText Typography=""BitTypography.H4"">H4. Heading</BitText>
<BitText Typography=""BitTypography.H5"">H5. Heading</BitText>
<BitText Typography=""BitTypography.H6"">H6. Heading</BitText>

<BitText Typography=""BitTypography.Subtitle1"">Subtitle1. Once upon a time</BitText>
<BitText Typography=""BitTypography.Subtitle2"">Subtitle2. Once upon a time</BitText>

<BitText Typography=""BitTypography.Body1"">Body1. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>
<BitText Typography=""BitTypography.Body2"">Body2. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>

<BitText Typography=""BitTypography.Button"">Button. Click Me</BitText>
<BitText Typography=""BitTypography.Caption1"">Caption1. Hello World!</BitText>
<BitText Typography=""BitTypography.Caption2"">Caption2. Hello World!</BitText>
<BitText Typography=""BitTypography.Overline"">Overline. this is overline text.</BitText>

<div style=""font-style:italic;font-size:1.25rem;color:tomato"">
    <BitText Typography=""BitTypography.Inherit"">Inherit. Takes the size, the weight and the family of the element around it.</BitText>
</div>";

    private string example2RazorCode = @"
<BitText Element=""h2"" Typography=""BitTypography.H4"">An h2 drawn at the size of an h4</BitText>
<BitText Element=""span"" Typography=""BitTypography.H4"">An h4 look with no heading semantics at all (span)</BitText>
<BitText Element=""strong"" Typography=""BitTypography.Body1"">Strongly emphasized body text</BitText>
<BitText Element=""blockquote"" Typography=""BitTypography.Body1"">A quotation, in a blockquote</BitText>
<BitText Element=""code"" Typography=""BitTypography.Body2"">var text = new BitText();</BitText>
<BitText Element=""not a tag name"" Typography=""BitTypography.Body2"">A tag name carrying whitespace falls back to the tag of the variant (p).</BitText>
<BitText Element=""hr"">A void element holds no content, so this text is not rendered.</BitText>";

    private string example3RazorCode = @"
<BitText Weight=""BitFontWeight.Light"">Light weight</BitText>
<BitText Weight=""BitFontWeight.Regular"">Regular weight</BitText>
<BitText Weight=""BitFontWeight.Medium"">Medium weight</BitText>
<BitText Weight=""BitFontWeight.Semibold"">Semibold weight</BitText>
<BitText Weight=""BitFontWeight.Bold"">Bold weight</BitText>

<BitText Italic>Italic text</BitText>
<BitText Underline>Underlined text</BitText>
<BitText Strikethrough>Struck through text</BitText>
<BitText Underline Strikethrough>Both underlined and struck through</BitText>

<BitText Transform=""BitTextTransform.Uppercase"">Uppercase transform</BitText>
<BitText Transform=""BitTextTransform.Lowercase"">Lowercase Transform</BitText>
<BitText Transform=""BitTextTransform.Capitalize"">capitalize transform</BitText>
<BitText Typography=""BitTypography.Overline"" Transform=""BitTextTransform.None"">None, undoing the uppercase of the overline variant</BitText>";

    private string example4RazorCode = @"
<BitText Typography=""BitTypography.Body1"">1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"">8,888.88</BitText>
<BitText Typography=""BitTypography.Body1"">1,234.56</BitText>

<BitText Typography=""BitTypography.Body1"" Numeric>1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"" Numeric>8,888.88</BitText>
<BitText Typography=""BitTypography.Body1"" Numeric>1,234.56</BitText>

<BitText Typography=""BitTypography.Body1"" Monospace>1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"" Monospace>8,888.88</BitText>
<BitText Typography=""BitTypography.Body1"" Monospace>1,234.56</BitText>

<BitText Element=""code"" Typography=""BitTypography.Body2"" Monospace>var text = new BitText { Monospace = true };</BitText>
<BitText Element=""samp"" Typography=""BitTypography.Body2"" Monospace>sha256:3f7a91c0b2ed48d5</BitText>";

    private string example5RazorCode = @"
<BitText Style=""width:250px"">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>

<BitText Style=""width:250px"" NoWrap>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>

<BitText Style=""width:250px"" Typography=""BitTypography.Caption2"" NoWrap Block>Once upon a time, stories wove connections between people, a symphony of voices.</BitText>

<BitText Style=""width:250px"" BreakWord>A path: /a/very/long/path/segment/that/never/breaks/on/its/own/anywhere.txt</BitText>

<BitText Style=""width:250px"" ForceBreak>1234567890123456789012345678901234567890123456789012345678901234567890</BitText>

<BitText Style=""width:250px"" Typography=""BitTypography.H5"" Wrap=""BitTextWrap.Balance"">A heading whose lines are balanced against each other</BitText>

<BitText Style=""width:250px"" Wrap=""BitTextWrap.Pretty"">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>

<BitText Style=""width:250px"" Lang=""en"" Hyphenate>An incomprehensibly complicated internationalization responsibility.</BitText>

<BitText Style=""width:250px"" PreserveWhitespace>@(@""Dear reader,

    Two blank lines and an indent survive,
    and a line this long is still wrapped."")</BitText>";

    private string example6RazorCode = @"
<BitText Style=""width:250px"" LineClamp=""1"">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams that outlasted every one of the nights they were told in.</BitText>

<BitText Style=""width:250px"" LineClamp=""2"">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams that outlasted every one of the nights they were told in.</BitText>

<BitText Style=""width:250px"" LineClamp=""3"">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams that outlasted every one of the nights they were told in.</BitText>";

    private string example7RazorCode = @"
<BitText Style=""width:250px"" Align=""BitTextAlign.Start"">Start</BitText>
<BitText Style=""width:250px"" Align=""BitTextAlign.Center"">Center</BitText>
<BitText Style=""width:250px"" Align=""BitTextAlign.End"">End</BitText>
<BitText Style=""width:250px"" Align=""BitTextAlign.Justify"">Justify. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>";

    private string example8RazorCode = @"
<BitText Typography=""BitTypography.H5"" Gutter>A heading with a gutter</BitText>
<BitText Typography=""BitTypography.Body1"" Gutter>A paragraph with a gutter, whose margin is smaller because the variant is.</BitText>
<BitText Typography=""BitTypography.Body1"">A paragraph with none.</BitText>";

    private string example9RazorCode = @"
<div class=""demo-boxed""><BitText Typography=""BitTypography.H4"">Handgloves</BitText></div>

<div class=""demo-boxed""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.Start"">Handgloves</BitText></div>

<div class=""demo-boxed""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.End"">Handgloves</BitText></div>

<div class=""demo-boxed""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.Both"">Handgloves</BitText></div>";

    private string example10RazorCode = @"
<BitText Element=""div"" Typography=""BitTypography.H5"" AriaLevel=""3"">A div announced as a level 3 heading</BitText>
<BitText Element=""h2"" Typography=""BitTypography.H5"" AriaLevel=""4"">An h2 announced as a level 4 heading</BitText>

<BitText VisuallyHidden>Read out by a screen reader, and drawn nowhere.</BitText>

<BitText NoSelect>Try to select this text - it will not be selected.</BitText>";

    private string example11RazorCode = @"
<BitText Visibility=""BitVisibility.Visible"">Visible text</BitText>
<BitText Visibility=""BitVisibility.Hidden"">Hidden text</BitText>
<BitText Visibility=""BitVisibility.Collapsed"">Collapsed text</BitText>

<BitText IsEnabled=""false"">A disabled run of text</BitText>
<BitText IsEnabled=""false"" Color=""BitColor.Error"">A disabled run of text, keeping its error color</BitText>";

    private string example12RazorCode = @"
<BitText Foreground=""BitColorKind.Primary"">Primary foreground</BitText>
<BitText Foreground=""BitColorKind.Secondary"">Secondary foreground</BitText>
<BitText Foreground=""BitColorKind.Tertiary"">Tertiary foreground</BitText>

<div style=""background:linear-gradient(blue, pink);background-clip:text;"">
    <BitText Foreground=""BitColorKind.Transparent"">Transparent foreground</BitText>
</div>";

    private string example13RazorCode = @"
<BitText Typography=""BitTypography.H3"" Gradient=""linear-gradient(90deg, #7c3aed, #06b6d4)"">A gradient headline</BitText>

<BitText Typography=""BitTypography.H4"" Gradient=""linear-gradient(45deg, #f43f5e, #f59e0b 50%, #22c55e)"">Three stops, on a diagonal</BitText>

<BitText Typography=""BitTypography.H4"" Weight=""BitFontWeight.Bold"" Gradient=""radial-gradient(circle at 30% 50%, #06b6d4, #7c3aed)"">A radial gradient, and a weight of its own</BitText>

<BitText Gradient=""linear-gradient(90deg, var(--bit-clr-fg-pri), transparent)"">A run of body text fading out into nothing</BitText>";

    private readonly BitTextParams[] textParams =
    [
        new()
        {
            Typography = BitTypography.Body1,
            Color = BitColor.Info,
            Transform = BitTextTransform.Uppercase,
        }
    ];

    private string example14RazorCode = @"
@* The params object carries a default down to every text under it, and never overwrites what one set itself. *@
<BitParams Parameters=""@textParams"">
    <BitText>Takes the variant, the color and the transform from the cascade</BitText>
    <BitText>So does this one, without repeating any of it</BitText>
    <BitText Color=""BitColor.Error"" Weight=""BitFontWeight.Bold"">Its own color and weight, the cascaded variant</BitText>
</BitParams>

<BitText>Outside the cascade, and back to the defaults</BitText>

@code {
    private readonly BitTextParams[] textParams =
    [
        new()
        {
            Typography = BitTypography.Body1,
            Color = BitColor.Info,
            Transform = BitTextTransform.Uppercase,
        }
    ];
}";

    private string example15RazorCode = @"
<BitText Color=""BitColor.Primary"">Primary color</BitText>
<BitText Color=""BitColor.Secondary"">Secondary color</BitText>
<BitText Color=""BitColor.Tertiary"">Tertiary color</BitText>

<BitText Color=""BitColor.Info"">Info color</BitText>
<BitText Color=""BitColor.Success"">Success color</BitText>
<BitText Color=""BitColor.Warning"">Warning color</BitText>
<BitText Color=""BitColor.SevereWarning"">SevereWarning color</BitText>
<BitText Color=""BitColor.Error"">Error color</BitText>

<BitText Color=""BitColor.PrimaryBackground"">PrimaryBackground color</BitText>
<BitText Color=""BitColor.SecondaryBackground"">SecondaryBackground color</BitText>
<BitText Color=""BitColor.TertiaryBackground"">TertiaryBackground color</BitText>

<BitText Color=""BitColor.PrimaryForeground"">PrimaryForeground color</BitText>
<BitText Color=""BitColor.SecondaryForeground"">SecondaryForeground color</BitText>
<BitText Color=""BitColor.TertiaryForeground"">TertiaryForeground color</BitText>

<BitText Color=""BitColor.PrimaryBorder"">PrimaryBorder color</BitText>
<BitText Color=""BitColor.SecondaryBorder"">SecondaryBorder color</BitText>
<BitText Color=""BitColor.TertiaryBorder"">TertiaryBorder color</BitText>";

    private string example16RazorCode = @"
<BitText Style=""color: tomato; font-weight: bold;"">Styled through the Style parameter</BitText>
<BitText Class=""demo-boxed"">Classed through the Class parameter</BitText>

<BitText Align=""BitTextAlign.Center""
         Style=""width:250px""
         @attributes=""@(new Dictionary<string, object> { [""class""] = ""demo-boxed"" })"">
    A splatted class, kept beside the class and the alignment the component builds
</BitText>";

    private string example17RazorCode = @"
<BitText Dir=""BitDir.Rtl"" Typography=""BitTypography.H5"">این یک عنوان راست‌چین است</BitText>
<BitText Dir=""BitDir.Rtl"" Align=""BitTextAlign.Start"">این متن از لبه‌ی آغازین چیده شده است.</BitText>
<BitText Dir=""BitDir.Rtl"" Align=""BitTextAlign.End"">این متن از لبه‌ی پایانی چیده شده است.</BitText>";
}

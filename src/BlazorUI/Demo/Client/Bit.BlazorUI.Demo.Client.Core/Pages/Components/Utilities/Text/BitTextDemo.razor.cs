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
            Description = "Breaks the text wherever the line runs out, even in the middle of a word.",
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
            Description = "Adds a bottom margin in em, so it follows the size of the variant. An inline variant needs Block for it to show.",
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
            Description = "Keeps the text on a single line and ends it with an ellipsis. Needs a box with a width, which Block gives an inline variant.",
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
            Description = "The typography variant: the size, weight, line height and tracking, and the tag rendered unless Element is set. Only the six heading variants render a heading tag. Defaults to Subtitle1.",
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
            Description = "Removes the text from the page while keeping it available to assistive technologies. It is drawn again while it holds the focus, so a skip link stays visible when tabbed to.",
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
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitColorKind(),
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
                new() { Name = "Subtitle1", Description = "Renders a div - not a heading. The default variant.", Value = "6" },
                new() { Name = "Subtitle2", Description = "Renders a div - not a heading.", Value = "7" },
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Text-color",
            DefaultValue = "currentcolor (the color around the text)",
            Description = "Text color when neither Color nor Foreground is set.",
        },
        new()
        {
            Name = "--bit-Text-font-family",
            DefaultValue = "--bit-tpg-font-family",
            Description = "Font family of every variant but Inherit, which keeps the family around it.",
        },
        new()
        {
            Name = "--bit-Text-heading-font-family",
            DefaultValue = "--bit-Text-font-family",
            Description = "Font family of the H1 to H6 variants, for a display face beside the text face.",
        },
        new()
        {
            Name = "--bit-Text-monospace-font-family",
            DefaultValue = "--bit-tpg-font-family-mono",
            Description = "Font family of a Monospace text.",
        },
        new()
        {
            Name = "--bit-Text-gutter",
            DefaultValue = "--bit-tpg-gutter-size",
            Description = "Bottom margin of a Gutter text.",
        },
        new()
        {
            Name = "--bit-Text-decoration-color",
            DefaultValue = "currentcolor",
            Description = "Color of the Underline and Strikethrough lines.",
        },
        new()
        {
            Name = "--bit-Text-decoration-thickness",
            DefaultValue = "auto",
            Description = "Thickness of the Underline and Strikethrough lines.",
        },
        new()
        {
            Name = "--bit-Text-underline-offset",
            DefaultValue = "auto",
            Description = "Distance between the baseline and the Underline line.",
        },
        new()
        {
            Name = "--bit-Text-disabled-opacity",
            DefaultValue = "--bit-opa-dis",
            Description = "Opacity of a text whose Disabled is true.",
        },
    ];



    private readonly BitTextParams[] textParams =
    [
        new()
        {
            Typography = BitTypography.Body1,
            Weight = BitFontWeight.Semibold,
            Transform = BitTextTransform.Uppercase,
        }
    ];
}

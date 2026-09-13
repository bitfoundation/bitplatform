namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Stack;

public partial class BitStackDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AlignContent",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets how the rows of a wrapping stack share out the room left over on the cross axis (the CSS align-content). It only says something once Wrap has produced more than one row and the stack is larger than those rows across. Baseline has no meaning for align-content on a flex container and is dropped.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets the alignment of the children of the stack on both axes at once. This is the shorthand of setting HorizontalAlign and VerticalAlign to the same value, and each of those takes precedence over it on its own axis. A specific alignment that means nothing on the axis it was given to steps aside for this shorthand rather than silencing it.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "AutoHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the height of the stack follow its content instead of filling its parent."
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes both the width and the height of the stack follow its content instead of filling its parent. This is the shorthand of setting AutoWidth and AutoHeight together."
        },
        new()
        {
            Name = "AutoWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the width of the stack follow its content instead of filling its parent."
        },
        new()
        {
            Name = "Basis",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the size this stack starts from before the leftover space of its own container is shared out (the CSS flex-basis), measured along the axis of that container. A basis of 0 takes the content of the stack out of the sum, so several stacks that also grow end up the same size as each other however much each of them holds."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the stack."
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the custom html element used for the root node, \"div\" by default. Only the tag name changes, and with it the semantics reported to assistive technologies, so a landmark element given here is worth pairing with an AriaLabel that names it. A stack rendered as a ul, an ol or a menu is also given an explicit list role, since some browsers drop the list semantics of those elements as soon as they are laid out as a flex container, and a role among the splatted html attributes still replaces it."
        },
        new()
        {
            Name = "EqualContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives every direct child of the stack an equal share of the axis they are laid out along, whatever each of them holds, so a row of three ends up in exact thirds. That is the difference from GrowContent, which lays the children out at their natural size first and only shares out what is left over."
        },
        new()
        {
            Name = "FillContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the direct children of the stack across the axis they are not laid out along: the full width in a column and the full height in a row. The size is forced, so a child that states a size of its own across that axis is overridden. A wrapping stack, and one whose direction changes with the width of the window, fills the row each child landed on instead, as a stretch that leaves a child that stated a size alone. Sharing the other axis out between the children is what GrowContent does, and the two combine."
        },
        new()
        {
            Name = "FitHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the height of the stack to fit its content. Unlike AutoHeight this also holds when the stack is a child of another flex container that would otherwise stretch it."
        },
        new()
        {
            Name = "FitSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width and height of the stack to fit its content. This is the shorthand of setting FitWidth and FitHeight together."
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width of the stack to fit its content. Unlike AutoWidth this also holds when the stack is a child of another flex container that would otherwise stretch it."
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack, using any CSS length value, 1rem by default. It takes one length for both axes, or two - the vertical one followed by the horizontal one - for a wrapping stack. HorizontalGap and VerticalGap each replace it on their own axis, GapXs to GapXxl each replace it from their own breakpoint upwards, and it takes precedence over the Size picked from the spacing scale of the theme."
        },
        new()
        {
            Name = "GapXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the extra small breakpoint (from 0px) upwards, using any CSS length value. The value keeps applying to every wider breakpoint until another one replaces it, and Gap is the base of that chain. This is the pair of HorizontalXs for the spacing rather than the direction, and the two are most often set together."
        },
        new()
        {
            Name = "GapSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "GapMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "GapLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "GapXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "GapXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack from the extra extra large breakpoint (from 2560px) upwards."
        },
        new()
        {
            Name = "Grow",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets how much of the leftover space of its own container this stack takes compared to its siblings (the CSS flex-grow factor). It describes the stack as a child of another flex container, so it needs a parent with room to share out to mean anything, and it takes precedence over Grows."
        },
        new()
        {
            Name = "GrowContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets every direct child of the stack grow into the room the stack has left over along the axis they are laid out on, so what is left after they have all been laid out at their natural size is shared out between them equally. A child that holds more therefore stays larger than one that holds less, which is where EqualContent differs. Expanding them across the other axis is what FillContent does, and the two combine."
        },
        new()
        {
            Name = "Grows",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the stack take the space its own container has left over (a CSS flex-grow factor of 1). This is Grow with the factor every layout reaches for first, and Grow replaces it when both are set."
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the children of the stack side by side in a row instead of stacked in a column. This is the direction of the stack at every width, and HorizontalXs to HorizontalXxl replace it from their own breakpoint upwards."
        },
        new()
        {
            Name = "HorizontalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets how the children of the stack are placed on the horizontal axis. In a horizontal stack that is the axis they are laid out along, so the space distributions apply and Baseline does not; in a vertical one it is the axis across them, so Baseline and Stretch apply and the space distributions do not. A value that means nothing on the axis it lands on steps aside for Alignment. Which of the two axes this is follows the direction each breakpoint gives, so a stack that changes direction with the width of the window is realigned along with it.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "HorizontalGap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis, using any CSS length value. This is the room between the columns, and it replaces Gap and Size on this axis alone. HorizontalGapXs to HorizontalGapXxl each replace it from their own breakpoint upwards."
        },
        new()
        {
            Name = "HorizontalGapXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and HorizontalGap is the base of that chain. A breakpoint this chain never reaches falls back to whatever the Gap chain resolves to there, so only the axis that needs its own answer has to be written out."
        },
        new()
        {
            Name = "HorizontalGapSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalGapMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalGapLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalGapXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalGapXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra extra large breakpoint (from 2560px) upwards."
        },
        new()
        {
            Name = "HorizontalXs",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the extra small breakpoint (from 0px) upwards: true lays the children out in a row and false in a column. The value keeps applying to every wider breakpoint until another one replaces it, and Horizontal is the base of that chain. HorizontalAlign and VerticalAlign follow the direction each breakpoint gives, so the stack is realigned along with it."
        },
        new()
        {
            Name = "HorizontalSm",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalMd",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalLg",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalXl",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "HorizontalXxl",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets the direction of the stack from the extra extra large breakpoint (from 2560px) upwards."
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the stack as an inline box, so it flows with the text around it instead of starting a line of its own and is sized by its content rather than by the box around it. Nothing about the layout of the children changes; only the outside of the stack does, and a size given through Style or through the Fit and Auto parameters still wins."
        },
        new()
        {
            Name = "NoShrink",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the stack at its natural size when its own container runs out of room, instead of letting it be squeezed, so the space has to come from its siblings instead. Shrink is the same thing with a factor of its own and takes precedence over it."
        },
        new()
        {
            Name = "NoShrinkContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps every direct child of the stack at its natural size when the stack runs out of room, so what no longer fits overflows the stack - or moves onto another row, if Wrap was asked for - rather than being crushed into it. This is NoShrink applied to the children rather than to the stack itself."
        },
        new()
        {
            Name = "Order",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the position of the stack among the children of its own container (the CSS order). Only the painted order changes: the order the content is read in by a screen reader and reached in by the keyboard stays the order it is written in."
        },
        new()
        {
            Name = "Padding",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the inner padding of the stack, using any CSS padding value: one length pads every side, two pad the block and then the inline axis, and four pad each side in turn. The stack is sized border-box, so the padding is taken out of the size it already has rather than added to it. A padding that answers to the width of the window is a fluid length such as clamp(0.5rem, 2vw, 2rem), since there is no per breakpoint chain for it."
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the children of the stack in the opposite direction (bottom-to-top in a vertical stack, and end-to-start in a horizontal one). Only the painted order is reversed: the order the children are read in by a screen reader and reached in by the keyboard stays the order they are written in."
        },
        new()
        {
            Name = "Self",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets how the stack itself is placed across the axis of its own container (the CSS align-self), overriding for this one child whatever that container aligns its children with. The three space distributions share room out between several children, which a single child cannot do, so they are ignored.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Shrink",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets how much of what its own container is short of this stack gives up compared to its siblings (the CSS flex-shrink factor). Every flex child answers for a factor of 1 by default, and NoShrink is this with the factor a layout reaches for most, zero."
        },
        new()
        {
            Name = "Shrinkable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the stack shrink below the size of what it holds when its own container runs out of room, by lifting the automatic minimum size a flex child has on both axes. That floor is why a nested stack refuses to shrink far enough for the text inside it to be truncated with an ellipsis or for a pane to scroll instead of the page growing. It is about how far the stack may be squeezed, where NoShrink is about whether it is squeezed at all."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack, picked from the spacing scale of the theme, so it follows the spacing unit and the density of the current theme instead of being hardcoded. Gap, HorizontalGap and VerticalGap each take precedence over it on the axis they target.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "VerticalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets how the children of the stack are placed on the vertical axis. In a vertical stack that is the axis they are laid out along, so the space distributions apply and Baseline does not; in a horizontal one it is the axis across them, so Baseline and Stretch apply and the space distributions do not. A value that means nothing on the axis it lands on steps aside for Alignment. Which of the two axes this is follows the direction each breakpoint gives, so a stack that changes direction with the width of the window is realigned along with it.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "VerticalGap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis, using any CSS length value. This is the room between the rows, and it replaces Gap and Size on this axis alone. VerticalGapXs to VerticalGapXxl each replace it from their own breakpoint upwards."
        },
        new()
        {
            Name = "VerticalGapXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and VerticalGap is the base of that chain. A breakpoint this chain never reaches falls back to whatever the Gap chain resolves to there, so only the axis that needs its own answer has to be written out."
        },
        new()
        {
            Name = "VerticalGapSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "VerticalGapMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "VerticalGapLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "VerticalGapXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "VerticalGapXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra extra large breakpoint (from 2560px) upwards."
        },
        new()
        {
            Name = "Wrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the children of the stack move onto more rows when they no longer fit on one. Without it they are squeezed to fit rather than moved. AlignContent is what places the rows a wrapping stack produces, and WrapXs to WrapXxl replace this from their own breakpoint upwards."
        },
        new()
        {
            Name = "WrapXs",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the extra small breakpoint (from 0px) upwards: true lets them wrap and false keeps them on one row. The value keeps applying to every wider breakpoint until another one replaces it, and Wrap is the base of that chain. WrapReverse is not part of it: it is the way every breakpoint that wraps at all wraps."
        },
        new()
        {
            Name = "WrapSm",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "WrapMd",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "WrapLg",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "WrapXl",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it."
        },
        new()
        {
            Name = "WrapXxl",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Gets or sets whether the children of the stack may move onto more rows, from the extra extra large breakpoint (from 2560px) upwards."
        },
        new()
        {
            Name = "WrapReverse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the children of the stack move onto more rows when they no longer fit on one, with the rows laid out from the far edge of the stack back towards the near one. It takes precedence over Wrap when both are set."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "Determines where the children of the stack are placed on an axis. The members that share room out between the children only mean something on the axis the children are laid out along, and Baseline only means something on the axis across them.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Description = "Packs the children against the edge the stack begins at.",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Description = "Packs the children against the edge the stack ends at.",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Description = "Centers the children on the axis.",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Description = "Puts the whole of the leftover room between the children, leaving none at either end.",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Description = "Gives each child an equal share of the leftover room on both of its sides, so the ends get half of what sits between two children.",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Description = "Makes every gap equal, the two at the ends included.",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Description = "Lines the children up on the first line of text of each of them.",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Description = "Grows the children until they fill the axis.",
                    Value = "7",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the spacing between the children of the stack, picked from the spacing scale of the theme.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        }
    ];



    private double gap = 1;

    private bool isReversed;
    private bool isHorizontal;
    private BitDir direction;
    private BitAlignment verticalAlign;
    private BitAlignment horizontalAlign;

    private double stackHeight = 15;
    private bool isWrapReversed;
    private BitAlignment alignContent;
}

namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// The tables of the enums the whole library shares - the placement, position, shape, line-style and
/// selection-mode vocabularies - written once and listed by every demo page whose component takes one of them,
/// instead of being pasted into each. A page adds the one it needs to its own sub-enum list and links to it by
/// the table's <see cref="ComponentSubEnum.Id"/>, which is therefore the same on every page.
/// </summary>
/// <remarks>
/// Which of the values a particular parameter honours belongs in that parameter's own description, not here:
/// these tables describe the type, and the MCP server hands them out as the type's documentation.
/// </remarks>
public static class SharedSubEnums
{
    public static readonly ComponentSubEnum BitPlacement = new()
    {
        Id = "placement-enum",
        Name = "BitPlacement",
        Description = "Where something is placed on a single axis: against one of the edges, in the middle, or - where a component can hold both edges at once - against a pair of them. Each parameter typed with it names the values it honours.",
        Items =
        [
            new()
            {
                Name = "Top",
                Value = "0",
                Description = "The top edge.",
            },
            new()
            {
                Name = "Bottom",
                Value = "1",
                Description = "The bottom edge.",
            },
            new()
            {
                Name = "Start",
                Value = "2",
                Description = "The edge the reading direction starts from - the left in LTR, the right in RTL. On the vertical axis, which does not turn around, it is the top.",
            },
            new()
            {
                Name = "End",
                Value = "3",
                Description = "The edge the reading direction ends at - the right in LTR, the left in RTL. On the vertical axis, which does not turn around, it is the bottom.",
            },
            new()
            {
                Name = "Left",
                Value = "4",
                Description = "The left edge, in both reading directions.",
            },
            new()
            {
                Name = "Right",
                Value = "5",
                Description = "The right edge, in both reading directions.",
            },
            new()
            {
                Name = "Center",
                Value = "6",
                Description = "The middle of the axis, against neither edge.",
            },
            new()
            {
                Name = "TopAndBottom",
                Value = "7",
                Description = "Both edges of the block axis at once.",
            },
            new()
            {
                Name = "StartAndEnd",
                Value = "8",
                Description = "Both edges of the inline axis at once, following the reading direction the way Start and End do.",
            }
        ]
    };

    public static readonly ComponentSubEnum BitPosition = new()
    {
        Id = "position-enum",
        Name = "BitPosition",
        Description = "A point on the three-by-three grid of the area a component is positioned in, used wherever a component sits somewhere inside a box rather than against one of its edges.",
        Items =
        [
            new()
            {
                Name = "TopLeft",
                Value = "0",
                Description = "The top left corner, in both reading directions.",
            },
            new()
            {
                Name = "TopCenter",
                Value = "1",
                Description = "The top edge, centered horizontally.",
            },
            new()
            {
                Name = "TopRight",
                Value = "2",
                Description = "The top right corner, in both reading directions.",
            },
            new()
            {
                Name = "TopStart",
                Value = "3",
                Description = "The top edge, on the side the reading direction starts from.",
            },
            new()
            {
                Name = "TopEnd",
                Value = "4",
                Description = "The top edge, on the side the reading direction ends at.",
            },
            new()
            {
                Name = "CenterLeft",
                Value = "5",
                Description = "The left edge, centered vertically, in both reading directions.",
            },
            new()
            {
                Name = "Center",
                Value = "6",
                Description = "Centered both ways.",
            },
            new()
            {
                Name = "CenterRight",
                Value = "7",
                Description = "The right edge, centered vertically, in both reading directions.",
            },
            new()
            {
                Name = "CenterStart",
                Value = "8",
                Description = "Centered vertically, on the side the reading direction starts from.",
            },
            new()
            {
                Name = "CenterEnd",
                Value = "9",
                Description = "Centered vertically, on the side the reading direction ends at.",
            },
            new()
            {
                Name = "BottomLeft",
                Value = "10",
                Description = "The bottom left corner, in both reading directions.",
            },
            new()
            {
                Name = "BottomCenter",
                Value = "11",
                Description = "The bottom edge, centered horizontally.",
            },
            new()
            {
                Name = "BottomRight",
                Value = "12",
                Description = "The bottom right corner, in both reading directions.",
            },
            new()
            {
                Name = "BottomStart",
                Value = "13",
                Description = "The bottom edge, on the side the reading direction starts from.",
            },
            new()
            {
                Name = "BottomEnd",
                Value = "14",
                Description = "The bottom edge, on the side the reading direction ends at.",
            }
        ]
    };

    public static readonly ComponentSubEnum BitShape = new()
    {
        Id = "shape-enum",
        Name = "BitShape",
        Description = "The outline a component draws itself with: how much its corners are rounded, and - for Circle alone - what proportions it takes. Each parameter typed with it names the values it honours.",
        Items =
        [
            new()
            {
                Name = "Rounded",
                Value = "0",
                Description = "The corner radius the current theme gives to this kind of surface.",
            },
            new()
            {
                Name = "Square",
                Value = "1",
                Description = "Sharp corners with no radius at all.",
            },
            new()
            {
                Name = "Pill",
                Value = "2",
                Description = "Fully rounded ends: a pill where the box is wider than it is tall, and a circle where the box is square.",
            },
            new()
            {
                Name = "Circle",
                Value = "3",
                Description = "A true circle, which takes its diameter from whichever of the height and the width is set.",
            }
        ]
    };

    public static readonly ComponentSubEnum BitLineStyle = new()
    {
        Id = "line-style-enum",
        Name = "BitLineStyle",
        Description = "How a line a component draws is stroked. Each parameter typed with it names the values it honours.",
        Items =
        [
            new()
            {
                Name = "Solid",
                Value = "0",
                Description = "A continuous line.",
            },
            new()
            {
                Name = "Dashed",
                Value = "1",
                Description = "A line of short dashes.",
            },
            new()
            {
                Name = "Dotted",
                Value = "2",
                Description = "A line of dots.",
            },
            new()
            {
                Name = "Double",
                Value = "3",
                Description = "Two parallel lines with a gap between them, which needs a line at least three pixels thick.",
            }
        ]
    };

    public static readonly ComponentSubEnum BitSelectionMode = new()
    {
        Id = "selection-mode-enum",
        Name = "BitSelectionMode",
        Description = "How many of a component's items can be selected at the same time.",
        Items =
        [
            new()
            {
                Name = "None",
                Value = "0",
                Description = "Nothing can be selected: the items act as plain content or as plain action buttons.",
            },
            new()
            {
                Name = "Single",
                Value = "1",
                Description = "At most one item can be selected at a time.",
            },
            new()
            {
                Name = "Multiple",
                Value = "2",
                Description = "Any number of items can be selected at the same time.",
            }
        ]
    };
}

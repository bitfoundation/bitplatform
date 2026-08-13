namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Grid;

public partial class BitGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AlignContent",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines how the rows of a wrapping grid share out the height left over (the CSS align-content). It only shows up in a grid that wrapped onto more than one row and is taller than those rows needed, and it is not part of the Alignment shorthand.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets the alignment of the children of the grid on both axes at once. HorizontalAlign and VerticalAlign each take precedence over it on their own axis.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Grid.",
        },
        new()
        {
            Name = "Columns",
            Type = "int",
            DefaultValue = "12",
            Description = "Defines the number of columns the width of the grid is divided into. It is the count of every breakpoint that is not overridden, and values below 1 are treated as 1.",
        },
        new()
        {
            Name = "ColumnsXs",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra small breakpoint (from 0px). The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "ColumnsSm",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the small breakpoint (from 600px). The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "ColumnsMd",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the medium breakpoint (from 960px). The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "ColumnsLg",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the large breakpoint (from 1280px). The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "ColumnsXl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra large breakpoint (from 1920px). The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "ColumnsXxl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra extra large breakpoint (from 2560px).",
        },
        new()
        {
            Name = "Container",
            Type = "bool",
            DefaultValue = "false",
            Description = "Measures the breakpoints of the grid against its own width instead of the width of the viewport, by turning the grid into a CSS query container. The breakpoints keep their usual widths, so a grid in a 700px panel lays itself out the way the same grid lays itself out in a 700px window.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"div\".",
        },
        new()
        {
            Name = "Grow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets every child of the grid grow into whatever width its row did not use, which is what fills out the last, short row of a grid of otherwise equal items. The items that are sized to their own content are left alone.",
        },
        new()
        {
            Name = "HorizontalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines the horizontal distribution of the children of the grid (the CSS justify-content). Baseline and Stretch distribute nothing, so those two act on the vertical axis instead.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "HorizontalSpacing",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid. It is taken out of the width of the row before the columns divide it up. Falls back to Spacing.",
        },
        new()
        {
            Name = "HorizontalSpacingXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingXs.",
        },
        new()
        {
            Name = "HorizontalSpacingSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingSm.",
        },
        new()
        {
            Name = "HorizontalSpacingMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingMd.",
        },
        new()
        {
            Name = "HorizontalSpacingLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingLg.",
        },
        new()
        {
            Name = "HorizontalSpacingXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingXl.",
        },
        new()
        {
            Name = "HorizontalSpacingXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between the children of the grid from the extra extra large breakpoint (from 2560px) upwards, and it takes precedence over SpacingXxl.",
        },
        new()
        {
            Name = "MinItemWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sizes the children of the grid to a width they may not go below instead of to a number of columns, and fits as many of them on a row as that width allows. Takes any CSS length, and a bare number is read as pixels. The items of a row share it equally, the last row included. The column tracks are left behind while it is set, except for the items that state Auto or Grow for themselves and except for the offsets, which are still counted in columns.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the children of the grid on a single row, shrinking them to fit instead of letting them wrap onto more rows.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the children of the grid in the opposite direction. Only the painted order is reversed; the reading and tab order stay as written.",
        },
        new()
        {
            Name = "Spacing",
            Type = "string",
            DefaultValue = "4px",
            Description = "Defines the spacing between the children of the grid on both axes. HorizontalSpacing and VerticalSpacing each replace it on their own axis, and it is the spacing of every breakpoint that is not overridden.",
        },
        new()
        {
            Name = "SpacingXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "SpacingSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "SpacingMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "SpacingLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "SpacingXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it.",
        },
        new()
        {
            Name = "SpacingXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between the children of the grid on both axes from the extra extra large breakpoint (from 2560px) upwards.",
        },
        new()
        {
            Name = "Span",
            Type = "int",
            DefaultValue = "1",
            Description = "Defines the number of columns the children of the grid fill by default, used by every item that does not set a ColumnSpan of its own.",
        },
        new()
        {
            Name = "VerticalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines the vertical alignment of the children of the grid within a row (the CSS align-items). The three space distributions have no meaning on this axis and are ignored.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "VerticalSpacing",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid. Unlike HorizontalSpacing it has no effect on the width of the items. Falls back to Spacing.",
        },
        new()
        {
            Name = "VerticalSpacingXs",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingXs.",
        },
        new()
        {
            Name = "VerticalSpacingSm",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingSm.",
        },
        new()
        {
            Name = "VerticalSpacingMd",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingMd.",
        },
        new()
        {
            Name = "VerticalSpacingLg",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingLg.",
        },
        new()
        {
            Name = "VerticalSpacingXl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence over SpacingXl.",
        },
        new()
        {
            Name = "VerticalSpacingXxl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between the rows of the grid from the extra extra large breakpoint (from 2560px) upwards, and it takes precedence over SpacingXxl.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-grid-item",
            Title = "BitGridItem",
            Description = "A cell of a BitGrid, holding the content of one area of the layout and stating how many columns of the grid that area covers.",
            Parameters =
            [
                new()
                {
                    Name = "AlignSelf",
                    Type = "BitAlignment?",
                    DefaultValue = "null",
                    Description = "Defines the vertical alignment of this item within its row, overriding the VerticalAlign of the grid for this item alone (the CSS align-self).",
                    LinkType = LinkType.Link,
                    Href = "#alignment-enum",
                },
                new()
                {
                    Name = "Auto",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Sizes the item to its own content instead of to a number of columns. Takes precedence over Grow and over every span of the item, and is the base of the mobile first chain the AutoXs to AutoXxl and GrowXs to GrowXxl parameters carry upwards.",
                },
                new()
                {
                    Name = "AutoXs",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoSm",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoMd",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoLg",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoXl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoXxl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Sizes the item to its own content from the extra extra large breakpoint (from 2560px) upwards, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "AutoOffset",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Pushes this item to the end edge of its row by turning every column left over before it into the offset. Takes precedence over Offset and over every per breakpoint offset, and follows the edge the row starts at exactly as Offset does.",
                },
                new()
                {
                    Name = "AutoOffsetXs",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "AutoOffsetSm",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "AutoOffsetMd",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "AutoOffsetLg",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "AutoOffsetXl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "AutoOffsetXxl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Pushes this item to the end edge of its row from the extra extra large breakpoint (from 2560px) upwards, and false hands the room back to Offset.",
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the Grid item.",
                },
                new()
                {
                    Name = "ColumnSpan",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill. A span of 0 is an item that is not shown at all. When not set, the item falls back to the Span of its grid.",
                },
                new()
                {
                    Name = "Element",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom html element used for the root node. The default is \"div\".",
                },
                new()
                {
                    Name = "Grow",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Lets the item grow into whatever width is left over in its row, sharing it equally with the other items that grow. It is the base of the mobile first chain the GrowXs to GrowXxl and AutoXs to AutoXxl parameters carry upwards.",
                },
                new()
                {
                    Name = "GrowXs",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the extra small breakpoint (from 0px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "GrowSm",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the small breakpoint (from 600px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "GrowMd",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the medium breakpoint (from 960px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "GrowLg",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the large breakpoint (from 1280px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "GrowXl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the extra large breakpoint (from 1920px) upwards. The value keeps applying to every wider breakpoint until another one replaces it, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "GrowXxl",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Lets the item grow into whatever width is left over in its row from the extra extra large breakpoint (from 2560px) upwards, and false gives the item its columns back.",
                },
                new()
                {
                    Name = "Offset",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item, measured in the columns of the grid with the gaps included. The room is left on the edge the row starts at, so it indents from the right in a right-to-left grid and from the other end of either once the grid is Reversed.",
                },
                new()
                {
                    Name = "OffsetXs",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the extra small breakpoint (from 0px).",
                },
                new()
                {
                    Name = "OffsetSm",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the small breakpoint (from 600px).",
                },
                new()
                {
                    Name = "OffsetMd",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the medium breakpoint (from 960px).",
                },
                new()
                {
                    Name = "OffsetLg",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the large breakpoint (from 1280px).",
                },
                new()
                {
                    Name = "OffsetXl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the extra large breakpoint (from 1920px).",
                },
                new()
                {
                    Name = "OffsetXxl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns to leave empty before this item in the extra extra large breakpoint (from 2560px).",
                },
                new()
                {
                    Name = "Order",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings, regardless of where it is written. The value can be negative, and only the painted order changes.",
                },
                new()
                {
                    Name = "OrderXs",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the extra small breakpoint (from 0px).",
                },
                new()
                {
                    Name = "OrderSm",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the small breakpoint (from 600px).",
                },
                new()
                {
                    Name = "OrderMd",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the medium breakpoint (from 960px).",
                },
                new()
                {
                    Name = "OrderLg",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the large breakpoint (from 1280px).",
                },
                new()
                {
                    Name = "OrderXl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the extra large breakpoint (from 1920px).",
                },
                new()
                {
                    Name = "OrderXxl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines the position of this item among its siblings in the extra extra large breakpoint (from 2560px).",
                },
                new()
                {
                    Name = "Xs",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the extra small breakpoint (from 0px). The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item from this breakpoint upwards.",
                },
                new()
                {
                    Name = "Sm",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the small breakpoint (from 600px). The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item from this breakpoint upwards.",
                },
                new()
                {
                    Name = "Md",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the medium breakpoint (from 960px). The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item from this breakpoint upwards.",
                },
                new()
                {
                    Name = "Lg",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the large breakpoint (from 1280px). The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item from this breakpoint upwards.",
                },
                new()
                {
                    Name = "Xl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the extra large breakpoint (from 1920px). The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item from this breakpoint upwards.",
                },
                new()
                {
                    Name = "Xxl",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Number of columns this item should fill in the extra extra large breakpoint (from 2560px). A value of 0 hides the item from this breakpoint upwards.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "Start, End, Center and the three space distributions apply to the horizontal axis, while Start, End, Center, Baseline and Stretch apply to the vertical one.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        }
    ];



    private double minItemWidth = 10;
    private double verticalSpacing = 0.5;
    private double horizontalSpacing = 0.5;

    private BitAlignment horizontalAlign = BitAlignment.Start;
    private BitAlignment verticalAlign = BitAlignment.Start;
    private BitAlignment alignContent = BitAlignment.Start;

    private readonly string[] fruits = ["Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew"];
}

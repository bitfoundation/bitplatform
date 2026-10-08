namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Progress;

public partial class BitProgressDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnnounceProgress",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces the progress to screen readers through a polite live region, once per AnnounceStep crossed and always at completion.",
        },
        new()
        {
            Name = "AnnounceStep",
            Type = "double",
            DefaultValue = "25",
            Description = "How far the progress has to advance, in percentage points, before it is announced again. The first value is recorded without announcement; zero or negative is treated as 25.",
        },
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value in words for screen readers (\"3 of 10 files\"), also what AnnounceProgress speaks.",
        },
        new()
        {
            Name = "BarColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The color of the bar as any CSS color, replacing the Color role. The ring, the buffer and the stripes follow it.",
        },
        new()
        {
            Name = "Buffer",
            Type = "double?",
            DefaultValue = "null",
            Description = "A fainter second value behind the bar, read on the same scale as Value (or as a percentage without one). Ignored while Indeterminate.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the progress as a ring instead of a bar. An indeterminate ring is a spinner.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitProgressClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProgress.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            LinkType = LinkType.Link,
            Href = "#color-enum",
            DefaultValue = "null",
            Description = "The general color of the BitProgress.",
        },
        new()
        {
            Name = "Delay",
            Type = "int",
            DefaultValue = "0",
            Description = "Milliseconds the progress stays hidden after it first renders, so a quick operation never flashes one. Its space is kept and it is hidden from assistive technology too. The window opens once, with the first render: giving a Delay to a progress already on screen does not hide it.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text describing or supplementing the operation.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for describing or supplementing the operation.",
        },
        new()
        {
            Name = "Diameter",
            Type = "int?",
            DefaultValue = "null",
            Description = "The exact diameter of the ring in pixels. Unset, it follows the Size, growing only when Thickness times Radius asks for more.",
        },
        new()
        {
            Name = "GapDegree",
            Type = "double",
            DefaultValue = "0",
            Description = "Cuts a gap of this many degrees (0 to 295) out of the ring, turning it into a gauge. No effect on a bar.",
        },
        new()
        {
            Name = "GapPlacement",
            Type = "BitPlacement",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
            DefaultValue = "BitPlacement.Bottom",
            Description = "Where the gauge gap sits. Reversed swaps Start and End.",
        },
        new()
        {
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports that something is running without saying how far: the bar sweeps and the ring spins. No value is published to assistive technology and the readout is hidden.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label to display above the BitProgress.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label template to display above the BitProgress.",
        },
        new()
        {
            Name = "Length",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of a Vertical bar, as a CSS length (10rem by default).",
        },
        new()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "100",
            Description = "The top of the range Value is read against. Ignored without a Value.",
        },
        new()
        {
            Name = "Meter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Exposes the indicator with the meter role - a reading within a range, such as disk usage - instead of progressbar. An Indeterminate one stays a progressbar.",
        },
        new()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "The bottom of the range Value is read against. Ignored without a Value.",
        },
        new()
        {
            Name = "Percent",
            Type = "double",
            DefaultValue = "0",
            Description = "The completeness as a percentage between 0 and 100. Ignored when Value is set.",
        },
        new()
        {
            Name = "PercentNumberFormat",
            Type = "string",
            DefaultValue = "{0:F0} %",
            Description = "The composite format string of the readout, applied to the percentage on the current culture.",
        },
        new()
        {
            Name = "PercentNumberPosition",
            Type = "BitProgressPercentPosition",
            LinkType = LinkType.Link,
            Href = "#percent-position-enum",
            DefaultValue = "BitProgressPercentPosition.End",
            Description = "Where the readout of a bar sits: under it (End, Start, Center), on the label's row (Top) or on the bar (Inside). A ring always shows it in its middle.",
        },
        new()
        {
            Name = "PercentNumberTemplate",
            Type = "RenderFragment<double>?",
            DefaultValue = "null",
            Description = "Custom markup for the readout, receiving the percentage as its context. Shows the readout even without ShowPercentNumber.",
        },
        new()
        {
            Name = "Radius",
            Type = "int",
            DefaultValue = "6",
            Description = "The multiplier applied to Thickness to size the ring when no Diameter is set.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Fills from the end towards the start and turns the ring counter-clockwise.",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pill-shaped ends on the bar and round caps on the ring.",
        },
        new()
        {
            Name = "SegmentGap",
            Type = "int",
            DefaultValue = "4",
            Description = "The gap between two Segments, in pixels.",
        },
        new()
        {
            Name = "Segments",
            Type = "int?",
            DefaultValue = "null",
            Description = "Cuts the bar into this many equal segments. The value still fills continuously. No effect on a ring.",
        },
        new()
        {
            Name = "ShowPercentNumber",
            Type = "bool",
            DefaultValue = "false",
            Description = "Writes the percentage beside the bar or in the middle of the ring. Hidden while Indeterminate.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            LinkType = LinkType.Link,
            Href = "#size-enum",
            DefaultValue = "null",
            Description = "The size of the BitProgress.",
        },
        new()
        {
            Name = "Striped",
            Type = "bool",
            DefaultValue = "false",
            Description = "Paints diagonal stripes over a determinate bar.",
        },
        new()
        {
            Name = "StripedAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the stripes of a Striped bar travel.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProgressClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS Styles for different parts of the BitProgress.",
        },
        new()
        {
            Name = "Thickness",
            Type = "int?",
            DefaultValue = "null",
            Description = "The height of the bar, the width of a Vertical one and the stroke of the ring, in pixels. Unset, it follows the Size.",
        },
        new()
        {
            Name = "TrackColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The color of the unfilled part as any CSS color: the track, the ring behind the stroke and the ends of the sweep.",
        },
        new()
        {
            Name = "Value",
            Type = "double?",
            DefaultValue = "null",
            Description = "The completeness in the operation's own unit, read against Min and Max. Takes the place of Percent and is what assistive technology is given.",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stands the bar on its end, filling from the bottom up (top down when Reversed). Its height comes from Length.",
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Progress-bar-color",
            DefaultValue = "The Color role's main color",
            Description = "Fill of the bar and stroke of the ring; the buffer and the stripes derive from it. BarColor wins over it.",
        },
        new()
        {
            Name = "--bit-Progress-bar-text-color",
            DefaultValue = "The Color role's text color",
            Description = "Color of a readout placed Inside the bar.",
        },
        new()
        {
            Name = "--bit-Progress-track-color",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "The unfilled part: the track, the ring behind the stroke and the ends of the sweep. TrackColor wins over it.",
        },
        new()
        {
            Name = "--bit-Progress-buffer-color",
            DefaultValue = "The bar color at 38%",
            Description = "The buffered second value, on the bar and on the ring.",
        },
        new()
        {
            Name = "--bit-Progress-stripe-color",
            DefaultValue = "--bit-clr-bg-pri at 25%",
            Description = "The stripes of a Striped bar.",
        },
        new()
        {
            Name = "--bit-Progress-stripe-size",
            DefaultValue = "spacing(2)",
            Description = "Pitch of the stripes, which is also how far they travel in one cycle.",
        },
        new()
        {
            Name = "--bit-Progress-thickness",
            DefaultValue = "Per Size: --bit-siz-track-sm / -md / -lg (bar), 1x / 2x / 4x --bit-siz-spinner-stroke (ring)",
            Description = "Height of the bar, width of a vertical one and stroke of the ring. Thickness wins over it.",
        },
        new()
        {
            Name = "--bit-Progress-radius",
            DefaultValue = "--bit-shp-radius-progress",
            Description = "Corner radius of the track and the bar. Rounded wins over it with a full radius.",
        },
        new()
        {
            Name = "--bit-Progress-diameter",
            DefaultValue = "Per Size: spacing(4) / spacing(6.25) / spacing(9)",
            Description = "Smallest diameter of the ring. Diameter wins over it.",
        },
        new()
        {
            Name = "--bit-Progress-length",
            DefaultValue = "spacing(20)",
            Description = "Height of a Vertical bar. Length wins over it.",
        },
        new()
        {
            Name = "--bit-Progress-transition-duration",
            DefaultValue = "--bit-mot-duration",
            Description = "How long the fill takes to follow a new value. Set it to 0s for a bar fed many times a second.",
        },
        new()
        {
            Name = "--bit-Progress-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-xs / -sm / -md",
            Description = "Text size of the label and of the readout beside the bar.",
        },
        new()
        {
            Name = "--bit-Progress-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the label.",
        },
        new()
        {
            Name = "--bit-Progress-label-font-weight",
            DefaultValue = "--bit-tpg-fw-regular",
            Description = "Weight of the label.",
        },
        new()
        {
            Name = "--bit-Progress-percent-color",
            DefaultValue = "--bit-clr-fg-pri (bar), --bit-clr-fg-sec (ring)",
            Description = "Color of the readout beside the bar or in the middle of the ring.",
        },
        new()
        {
            Name = "--bit-Progress-percent-font-size",
            DefaultValue = "A step of the type ramp per the size the ring is drawn at, --bit-tpg-fs-xs to --bit-tpg-fs-4xl",
            Description = "Text size of the readout in the middle of the ring.",
        },
        new()
        {
            Name = "--bit-Progress-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the description.",
        },
        new()
        {
            Name = "--bit-Progress-description-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-2xs (small, medium), --bit-tpg-fs-xs (large)",
            Description = "Text size of the description.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "progressBar-class-styles",
            Title = "BitProgressClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgress."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitProgress."
               },
               new()
               {
                   Name = "PercentNumber",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the percent number of the BitProgress."
               },
               new()
               {
                   Name = "BarContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar container of the BitProgress."
               },
               new()
               {
                   Name = "Track",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the track of the BitProgress."
               },
               new()
               {
                   Name = "Buffer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the buffer bar of the BitProgress."
               },
               new()
               {
                   Name = "Bar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar of the BitProgress."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitProgress."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitPlacement(),
        new()
        {
            Id = "percent-position-enum",
            Name = "BitProgressPercentPosition",
            Description = "Where the percentage readout of a linear BitProgress is placed.",
            Items =
            [
                new() { Name = "End", Description = "Under the bar, aligned to the end of it. This is the default.", Value = "0" },
                new() { Name = "Start", Description = "Under the bar, aligned to the start of it.", Value = "1" },
                new() { Name = "Center", Description = "Under the bar, in the middle of it.", Value = "2" },
                new() { Name = "Inside", Description = "On the bar itself rather than under it, which keeps the whole indicator to one line.", Value = "3" },
                new() { Name = "Top", Description = "Above the bar, on the same row as the label and aligned to the end of it. Without a label it is a line of its own above the bar.", Value = "4" }
            ]
        },
        DemoSharedEnums.BitSize()
    ];



    private bool isLoading;
    private double barThickness = 10;
    private double bufferPercent = 40;
    private double segmentedPercent = 45;
    private double announcedPercent = 20;
    private double gaugeValue = 65;
    private double meterValue = 62;

    private readonly BitProgressParams[] progressParams =
    [
        new()
        {
            Thickness = 8,
            Rounded = true,
            ShowPercentNumber = true,
            PercentNumberPosition = BitProgressPercentPosition.Top,
        }
    ];
}

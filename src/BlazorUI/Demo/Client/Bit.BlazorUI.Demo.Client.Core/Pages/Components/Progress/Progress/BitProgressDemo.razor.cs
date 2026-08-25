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
            Description = "Announces the progress to screen readers as it advances, through a live region of its own. The announcement is made once per AnnounceStep crossed rather than on every change, since a bar that speaks on every percent is a bar nobody can listen to.",
        },
        new()
        {
            Name = "AnnounceStep",
            Type = "double",
            DefaultValue = "25",
            Description = "How far the progress has to advance, in percentage points, before it is announced again. Completion is always announced, whatever the step divides into.",
        },
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text alternative of the progress status, used by screen readers for reading the value of the progress.",
        },
        new()
        {
            Name = "BarColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The color of the bar itself, as any CSS color. It replaces the palette the Color role would have given, and everything derived from it follows: the stroke of the ring, the faint tint of the Buffer and the fill of a Striped bar.",
        },
        new()
        {
            Name = "Buffer",
            Type = "double?",
            DefaultValue = "null",
            Description = "The secondary, buffered progress rendered behind the main bar, for an operation that loads ahead of what it has already played or processed. It is read on the same scale as Value (between Min and Max) when a Value is set, and as a percentage between 0 and 100 otherwise. Ignored while Indeterminate is true.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the progress as a ring instead of as a bar, which is the shape for a compact spot - inside a button, in a card corner, beside a row - where a full-width bar has nowhere to go. A circular indeterminate progress is what is usually called a spinner.",
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
            Description = "The diameter of the circular progress in pixels. When not set, the diameter falls back to the theme value of the current Size, growing beyond it only when Thickness multiplied by Radius asks for more room.",
        },
        new()
        {
            Name = "GapDegree",
            Type = "double",
            DefaultValue = "0",
            Description = "Cuts a gap of this many degrees out of the bottom of the circular progress, which turns the ring into a gauge. Between 0 (a closed ring, the default) and 295; a value of 180 leaves a half circle. Has no effect on the linear progress.",
        },
        new()
        {
            Name = "GapPosition",
            Type = "BitProgressGapPosition",
            LinkType = LinkType.Link,
            Href = "#gap-position-enum",
            DefaultValue = "BitProgressGapPosition.Bottom",
            Description = "Where the GapDegree gap sits, which is also where the stroke of the gauge begins and ends. Reversed mirrors the gauge, so it swaps a Start gap with an End one and leaves a Top or a Bottom one where it is.",
        },
        new()
        {
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports that something is running without saying how far along it is: the bar sweeps and the ring spins instead of filling. No value is published to assistive technology in this mode - which is what tells a screen reader the progress is indeterminate - and the percentage readout is hidden. Switch to a determinate value as soon as one exists.",
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
            Description = "How long a Vertical bar is, as a CSS length. A horizontal bar takes the width of whatever it is put in, so this has no effect there.",
        },
        new()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "100",
            Description = "The highest value of the range the Value is read against. It has no effect while Value is null, in which case Percent is already a percentage.",
        },
        new()
        {
            Name = "Meter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports the indicator as a meter rather than as a progress bar. A progress bar says how far along a task is and only ever moves forward; a meter is a reading taken within a known range - a disk that is 60% full, a temperature, a score - which can move either way and is never \"finished\". This is what the ARIA practices ask for when the number is a measurement rather than progress. An Indeterminate indicator stays a progress bar, since a meter always has a value.",
        },
        new()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "The lowest value of the range the Value is read against. It has no effect while Value is null, in which case Percent is already a percentage.",
        },
        new()
        {
            Name = "Percent",
            Type = "double",
            DefaultValue = "0",
            Description = "Percentage of the operation's completeness, numerically between 0 and 100. Ignored when Value is set.",
        },
        new()
        {
            Name = "PercentNumberFormat",
            Type = "string",
            DefaultValue = "{0:F0} %",
            Description = "The composite format string the percentage readout is written with, applied to the percentage itself - \"{0:F0} %\" by default. It is formatted on the current culture, since it is text the reader sees.",
        },
        new()
        {
            Name = "PercentNumberPosition",
            Type = "BitProgressPercentPosition",
            LinkType = LinkType.Link,
            Href = "#percent-position-enum",
            DefaultValue = "BitProgressPercentPosition.End",
            Description = "Where the percentage readout of a linear progress is placed: under the bar aligned to its end (the default), to its start, in the middle, or on the bar itself. The readout of a circular progress is always in the middle of the ring, so this has no effect there.",
        },
        new()
        {
            Name = "PercentNumberTemplate",
            Type = "RenderFragment<double>?",
            DefaultValue = "null",
            Description = "Custom template for the percentage display, receiving the current percentage as its context. It replaces the text that PercentNumberFormat would have produced.",
        },
        new()
        {
            Name = "Radius",
            Type = "int",
            DefaultValue = "6",
            Description = "The multiplier applied to the Thickness to size the circular progress. The resulting diameter never falls below the theme value of the current Size, and setting Diameter replaces this calculation altogether.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Fills the progress from the end of the container towards its start, mirroring the direction of the linear bar and turning the circular one counter-clockwise.",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Rounds the ends of the bar: a pill-shaped track and bar in linear mode, and a round stroke cap in circular mode.",
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
            Description = "Cuts the linear bar into this many equal segments, for an operation made of a known number of discrete steps. The bar still fills continuously - the segments are how far apart the steps are drawn, not how the value is rounded. Has no effect on the circular progress.",
        },
        new()
        {
            Name = "ShowPercentNumber",
            Type = "bool",
            DefaultValue = "false",
            Description = "Writes the percentage beside the bar, or in the middle of the ring. PercentNumberPosition says where it goes and PercentNumberFormat how it reads. It is hidden while Indeterminate is true, since there is no number to show.",
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
            Description = "Paints diagonal stripes over the linear bar, which is the conventional way of saying that the operation behind a determinate bar is still running. Has no effect on the circular or the indeterminate progress.",
        },
        new()
        {
            Name = "StripedAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Animates the stripes of a Striped bar so they travel along it.",
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
            Description = "How thick the indicator is drawn, in pixels: the height of a horizontal bar, the width of a Vertical one and the stroke of the ring. When not set it follows the Size, which is what keeps a page of indicators in step with each other and with the theme.",
        },
        new()
        {
            Name = "TrackColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The color of the unfilled part of the indicator, as any CSS color: the track behind the bar, the ring behind the stroke, and the two ends the indeterminate sweep fades into.",
        },
        new()
        {
            Name = "Value",
            Type = "double?",
            DefaultValue = "null",
            Description = "The completeness of the operation expressed in its own unit, read against Min and Max. When set, it takes the place of Percent and is what the screen reader is given, so an operation counted in files or in bytes is announced in files or in bytes.",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stands the linear bar on its end, filling it from the bottom up - or from the top down when it is also Reversed. A vertical bar has no width to take from its container, so its height comes from Length. Has no effect on the circular progress.",
        }
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
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        },
        new()
        {
            Id = "gap-position-enum",
            Name = "BitProgressGapPosition",
            Description = "Where the gap of a gauge-shaped BitProgress sits, which is also where its stroke begins and ends.",
            Items =
            [
                new() { Name = "Bottom", Description = "At the bottom of the ring, which is where a gauge is normally opened. This is the default.", Value = "0" },
                new() { Name = "Top", Description = "At the top of the ring.", Value = "1" },
                new() { Name = "Start", Description = "At the starting side of the ring - the left in a left-to-right context, the right in a right-to-left one.", Value = "2" },
                new() { Name = "End", Description = "At the ending side of the ring - the right in a left-to-right context, the left in a right-to-left one.", Value = "3" }
            ]
        },
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
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        }
    ];



    private double barThickness = 10;
    private double bufferPercent = 40;
    private double segmentedPercent = 45;
    private double announcedPercent = 0;
    private double gaugeValue = 65;
    private double meterValue = 62;
}

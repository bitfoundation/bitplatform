using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Slider;

public partial class BitSliderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaValueText",
            Type = "Func<double, string>?",
            DefaultValue = "null",
            Description = "A text description of the Slider number value for the benefit of screen readers. This should be used when the Slider number value is not accurately represented by a number. The returned text becomes the aria-valuetext of every thumb.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the slider automatically receives focus when the page renders.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSliderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitSlider.",
            LinkType = LinkType.Link,
            Href = "#slider-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the slider, applied to the filled part of the track and to the thumbs.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultLowerValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The default lower value of the ranged Slider, used when LowerValue is not bound.",
        },
        new()
        {
            Name = "DefaultUpperValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The default upper value of the ranged Slider, used when UpperValue is not bound.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "double",
            DefaultValue = "0",
            Description = "The default value of the Slider, used when Value is not bound.",
        },
        new()
        {
            Name = "DraggableTrack",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the filled band between the two thumbs of a ranged Slider be dragged bodily, so the whole range travels at once and keeps the width it had. Only the rail strictly between the thumbs takes the drag, so the thumbs themselves stay grabbable.",
        },
        new()
        {
            Name = "GetValueText",
            Type = "Func<double, string>?",
            DefaultValue = "null",
            Description = "Builds the text of every label the Slider draws from the value it stands for. Takes precedence over ValueFormat, and is what AriaValueText falls back to.",
        },
        new()
        {
            Name = "Inverted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Fills the track from the far end instead of from the near one. A ranged slider inverts into the two outer segments, leaving the span between the thumbs unfilled.",
        },
        new()
        {
            Name = "IsOriginFromZero",
            Type = "bool",
            DefaultValue = "false",
            Description = "Attaches the origin of the filled part of the track to zero. Shorthand for setting Origin to 0.",
        },
        new()
        {
            Name = "IsRanged",
            Type = "bool",
            DefaultValue = "false",
            Description = "If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected.",
        },
        new()
        {
            Name = "IsVertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render the slider vertically. Its length comes from the --bit-sld-length CSS variable, which defaults to 12rem.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Description label of the Slider, which also names it for assistive technologies when no AriaLabel is given.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the plain text Label of the Slider with custom content.",
        },
        new()
        {
            Name = "LargeStep",
            Type = "double?",
            DefaultValue = "null",
            Description = "The distance the larger keyboard jumps cover: Page Up and Page Down, and an arrow key held with Shift. It defaults to whatever the browser does on its own.",
        },
        new()
        {
            Name = "LowerAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the lower thumb of a ranged slider.",
        },
        new()
        {
            Name = "LowerValue",
            Type = "double",
            DefaultValue = "0",
            Description = "The lower value of the ranged Slider.",
        },
        new()
        {
            Name = "Marks",
            Type = "IEnumerable<BitSliderMark>?",
            DefaultValue = "null",
            Description = "The marks drawn along the track of the Slider, each optionally carrying a label. Takes precedence over ShowMarks.",
            LinkType = LinkType.Link,
            Href = "#slider-mark",
        },
        new()
        {
            Name = "MarkStep",
            Type = "double?",
            DefaultValue = "null",
            Description = "The interval between the marks generated by ShowMarks. Defaults to the Step.",
        },
        new()
        {
            Name = "MaxRange",
            Type = "double?",
            DefaultValue = "null",
            Description = "The largest distance the two thumbs of a ranged slider are allowed to be apart.",
        },
        new()
        {
            Name = "MinRange",
            Type = "double",
            DefaultValue = "0",
            Description = "The smallest distance the two thumbs of a ranged slider are allowed to be apart. Any value above zero also stops the thumbs from crossing.",
        },
        new()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "The min value of the Slider.",
        },
        new()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "10",
            Description = "The max value of the Slider.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the underlying range input, which is what makes the value readable by a plain form post.",
        },
        new()
        {
            Name = "NoFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the track unfilled, so the slider is a bare rail with a thumb on it rather than a bar that grows. It also turns off the highlighting of the marks the fill would otherwise have reached.",
        },
        new()
        {
            Name = "NoSwap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the two thumbs of a ranged slider from crossing over each other.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            Description = "Callback when the value of a single-value Slider changes. This is called on every individual step.",
        },
        new()
        {
            Name = "OnChangeEnd",
            Type = "EventCallback<double>",
            Description = "Callback for when the interaction that changes the value ends, rather than on every step along the way.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when a thumb of the Slider receives the focus.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when a thumb of the Slider loses the focus.",
        },
        new()
        {
            Name = "OnRangeChange",
            Type = "EventCallback<BitSliderRangeValue>",
            Description = "Callback when the range of a ranged Slider changes. This is called on every individual step.",
            LinkType = LinkType.Link,
            Href = "#slider-range-value",
        },
        new()
        {
            Name = "OnRangeChangeEnd",
            Type = "EventCallback<BitSliderRangeValue>",
            Description = "Callback for when the interaction that changes the range ends, rather than on every step along the way.",
            LinkType = LinkType.Link,
            Href = "#slider-range-value",
        },
        new()
        {
            Name = "Origin",
            Type = "double?",
            DefaultValue = "null",
            Description = "The value the filled part of the track grows out of, instead of the near end of the track. Has no effect on a ranged slider.",
        },
        new()
        {
            Name = "Pushable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a thumb of a ranged slider push the other one along instead of stopping against it. It has nothing to do without a MinRange to keep or a NoSwap ordering to hold, and it keeps the thumbs on their own sides since pushing and crossing are opposites.",
        },
        new()
        {
            Name = "RangeValue",
            Type = "BitSliderRangeValue?",
            DefaultValue = "null",
            Description = "The range value of the Slider. Use this parameter to get or set both the LowerValue and the UpperValue at once.",
            LinkType = LinkType.Link,
            Href = "#slider-range-value",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Slider read-only: still focusable and announced, but refusing every change.",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the Slider as required, which is announced through aria-required.",
        },
        new()
        {
            Name = "RestrictToMarks",
            Type = "bool",
            DefaultValue = "false",
            Description = "Restricts the values the user can pick to the marks alone, so the thumb jumps from one mark to the next instead of moving by the Step in between them.",
        },
        new()
        {
            Name = "ShowMarks",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a mark at every MarkStep (or every Step when that is not set) along the track.",
        },
        new()
        {
            Name = "ShowMarkLabels",
            Type = "bool",
            DefaultValue = "false",
            Description = "Labels the generated marks of ShowMarks with their own values.",
        },
        new()
        {
            Name = "ShowValue",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether to show the value beside the Slider.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Size of the Slider, which scales its track, thumbs and labels together.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "SliderBoxHtmlAttributes",
            Type = "Dictionary<string, object>?",
            DefaultValue = "null",
            Description = "Additional html attributes for the Slider box, the element the track and the inputs are laid inside of.",
        },
        new()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "The difference between the two adjacent values of the Slider. Values of zero or below fall back to 1.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSliderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitSlider.",
            LinkType = LinkType.Link,
            Href = "#slider-class-styles",
        },
        new()
        {
            Name = "ThumbLabel",
            Type = "BitSliderThumbLabel?",
            DefaultValue = "null",
            Description = "Decides when the floating label that rides along with the thumb is shown.",
            LinkType = LinkType.Link,
            Href = "#thumb-label-enum",
        },
        new()
        {
            Name = "ThumbLabelTemplate",
            Type = "RenderFragment<double>?",
            DefaultValue = "null",
            Description = "Replaces the text of the floating label that rides along with the thumb with custom content, built from the value that thumb stands for.",
        },
        new()
        {
            Name = "UpperAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the upper thumb of a ranged slider.",
        },
        new()
        {
            Name = "UpperValue",
            Type = "double",
            DefaultValue = "0",
            Description = "The upper value of the ranged Slider.",
        },
        new()
        {
            Name = "Value",
            Type = "double",
            DefaultValue = "0",
            Description = "The value of the Slider.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom format for the displayed value of the Slider, applied to the value labels, the mark labels and the floating thumb labels alike.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "slider-class-styles",
            Title = "BitSliderClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's root element."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's label."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's container."
                },
                new()
                {
                    Name = "SliderBox",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's box, the element the track and the inputs are laid inside of."
                },
                new()
                {
                    Name = "Track",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's track, the full-length rail the thumb travels along."
                },
                new()
                {
                    Name = "Fill",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's fill, the highlighted part of the track."
                },
                new()
                {
                    Name = "Thumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's thumbs, the handles that travel along the track."
                },
                new()
                {
                    Name = "LowerThumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the lower thumb of a ranged BitSlider, added on top of Thumb."
                },
                new()
                {
                    Name = "UpperThumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the upper thumb of a ranged BitSlider, added on top of Thumb."
                },
                new()
                {
                    Name = "LowerValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's lower value input."
                },
                new()
                {
                    Name = "UpperValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's upper value input."
                },
                new()
                {
                    Name = "ValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's value input."
                },
                new()
                {
                    Name = "TrackInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input that makes the filled band of a ranged BitSlider draggable, which is only rendered while DraggableTrack asks for it."
                },
                new()
                {
                    Name = "OriginFromZero",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's origin tick."
                },
                new()
                {
                    Name = "ValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's value label."
                },
                new()
                {
                    Name = "LowerValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's lower value label."
                },
                new()
                {
                    Name = "UpperValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's upper value label."
                },
                new()
                {
                    Name = "Mark",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's mark ticks."
                },
                new()
                {
                    Name = "MarkLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's mark labels."
                },
                new()
                {
                    Name = "ThumbLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's floating thumb labels."
                }
            ]
        },
        new()
        {
            Id = "slider-range-value",
            Title = "BitSliderRangeValue",
            Parameters =
            [
                new()
                {
                    Name = "Lower",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The lower value of the ranged Slider."
                },
                new()
                {
                    Name = "Upper",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The upper value of the ranged Slider."
                },
                new()
                {
                    Name = "Length",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The distance between the two ends of the range."
                }
            ]
        },
        new()
        {
            Id = "slider-mark",
            Title = "BitSliderMark",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The value the mark sits at. A mark outside the Min..Max range of the slider is not rendered."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text rendered under the mark. A mark without a label is drawn as a plain tick."
                },
                new()
                {
                    Name = "Class",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes for this mark, added to both its tick and its label."
                },
                new()
                {
                    Name = "Style",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS styles for this mark, added to both its tick and its label."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "thumb-label-enum",
            Name = "BitSliderThumbLabel",
            Description = "Decides when the BitSlider shows the floating label that rides along with its thumb.",
            Items =
            [
                new() { Name = "Off", Description = "Never show the floating label.", Value = "0" },
                new() { Name = "Auto", Description = "Show the floating label only while the slider is being hovered, dragged or focused.", Value = "1" },
                new() { Name = "On", Description = "Always show the floating label.", Value = "2" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the size of the slider.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the color kinds available in the bit BlazorUI.",
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
        }
    ];



    private BitSliderRangeValue ageBand = new(25, 45);

    private readonly List<BitSliderMark> qualityMarks =
    [
        new(0, "Draft"),
        new(1, "Low"),
        new(2, "Medium"),
        new(3, "High"),
        new(4, "Lossless")
    ];

    // An irregular scale: the gaps grow as the numbers do, which is exactly what RestrictToMarks is for.
    private readonly List<BitSliderMark> storageMarks =
    [
        new(0, "0"),
        new(64, "64"),
        new(128, "128"),
        new(256, "256"),
        new(512, "512"),
        new(1000, "1 TB"),
        new(2000, "2 TB")
    ];
    private double storageValue = 256;

    private BitSliderRangeValue freeRange = new(30, 70);
    private BitSliderRangeValue pushableRange = new(20, 40);
    private BitSliderRangeValue draggableRange = new(25, 55);

    private double oneWayBinding = 3;
    private double twoWayBinding = 5;
    private double boundLower = 25;
    private double boundUpper = 75;

    private int onChangeCount;
    private double? onChangeEndValue;
    private int onRangeChangeCount;
    private BitSliderRangeValue? onRangeChangeEndValue;

    private BitSlider? focusableSlider;

    private const string focusedText = "focused";
    private const string blurredText = "blurred";
    private string focusState = blurredText;

    public BitSliderDemoFormModel ValidationModel = new();
    public string? SuccessMessage;



    private static readonly string[] qualityWords = ["Draft", "Low", "Medium", "High", "Lossless"];

    private static string GetTimeText(double minutes)
    {
        return TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    private static string GetVolumeIcon(double value) => value switch
    {
        <= 0 => BitIconName.VolumeDisabled,
        < 34 => BitIconName.Volume1,
        < 67 => BitIconName.Volume2,
        _ => BitIconName.Volume3
    };

    private static string GetQualityText(double value)
    {
        var index = (int)Math.Clamp(value, 0, qualityWords.Length - 1);

        return qualityWords[index];
    }

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(2000);
        SuccessMessage = string.Empty;
        ValidationModel.Days = default;
        ValidationModel.Budget = new(40, 50);
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }
}

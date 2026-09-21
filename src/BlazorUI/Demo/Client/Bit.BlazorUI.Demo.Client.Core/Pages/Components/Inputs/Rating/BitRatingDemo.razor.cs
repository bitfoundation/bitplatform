namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Rating;

public partial class BitRatingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowClear",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the current value be cleared, by clicking the item that is already selected or by pressing Delete or Backspace. Clearing sets the value to 0, so it also makes 0 a reachable value the same way AllowZeroStars does.",
        },
        new()
        {
            Name = "AllowZeroStars",
            Type = "bool",
            DefaultValue = "false",
            Description = "Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard unless AllowClear is also set.",
        },
        new()
        {
            Name = "AriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers. Placeholder {0} is the current rating and placeholder {1} is the max. Without it an item is named by its ItemTitles tooltip, and failing that by its position in the scale.",
        },
        new()
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of an element that names the rating as a whole, for a name that is already written somewhere on the page. It wins over every other source of the name, including the visible Label.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the rating automatically receives focus when the page renders.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitRatingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitRating.",
            LinkType = LinkType.Link,
            Href = "#rating-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the rating, applied to the filled part of the items. The unfilled part stays neutral so it reads as \"not rated yet\" whichever color is picked.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The hint shown under the items and pointed at by aria-describedby, for the instruction a row of stars cannot give by itself. It describes the rating rather than naming it, so it is announced after the label.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the Description with custom content, which is still what describes the rating for assistive technologies.",
        },
        new()
        {
            Name = "GetAriaLabel",
            Type = "Func<double, double, string>?",
            DefaultValue = "null",
            Description = "Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if the AriaLabel parameter is not provided. The first argument is the current value and the second one is the max.",
        },
        new()
        {
            Name = "GetSelectedIcon",
            Type = "Func<int, BitIconInfo?>?",
            DefaultValue = "null",
            Description = "Chooses the selected (filled) icon of each rating item separately, from the one-based position of the item. Returning null falls back to SelectedIcon / SelectedIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GetUnselectedIcon",
            Type = "Func<int, BitIconInfo?>?",
            DefaultValue = "null",
            Description = "Chooses the unselected (empty) icon of each rating item separately, from the one-based position of the item. Returning null falls back to UnselectedIcon / UnselectedIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "HighlightSelectedOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Highlights only the item matching the current value instead of every item up to it, turning the rating into a scale of standalone choices rather than a cumulative one. A fractional value still fills its own item by the fraction it covers.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitRatingItemContext>?",
            DefaultValue = "null",
            Description = "Replaces the default pair of icons of every rating item with custom content.",
            LinkType = LinkType.Link,
            Href = "#rating-item-context",
        },
        new()
        {
            Name = "ItemTitles",
            Type = "IList<string>?",
            DefaultValue = "null",
            Description = "The native tooltips of the rating items, in order, shown when hovering over each one, and used as the accessible name of the item unless AriaLabelFormat overrides it. Items beyond the end of the list simply get no tooltip.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The visible label of the rating, which also becomes its accessible name: a row of stars carries no text of its own, so without a label - or an AriaLabel - the group is announced without saying what is being rated. A required rating marks its label with an asterisk.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "Where the label sits relative to the items: above them by default, and beside them with Start or End for the compact single-line row.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the Label with custom content, which still names the rating for assistive technologies the same way the plain label does.",
        },
        new()
        {
            Name = "Max",
            Type = "int",
            DefaultValue = "5",
            Description = "Maximum rating, which is also the number of rendered items. Values below 1 are treated as 1.",
        },
        new()
        {
            Name = "NoHoverPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the preview that follows the pointer over the items and shows the value that a click would commit.",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitRatingChangeArgs>",
            Description = "Callback invoked before the value of the rating changes, letting the change be cancelled by setting Cancel on the provided args.",
            LinkType = LinkType.Link,
            Href = "#rating-change-args",
        },
        new()
        {
            Name = "OnHoverChange",
            Type = "EventCallback<double?>",
            Description = "Callback for when the previewed value changes, which is the value a click would commit. It receives null when the pointer leaves the rating and the preview ends.",
        },
        new()
        {
            Name = "Precision",
            Type = "double",
            DefaultValue = "1",
            Description = "The smallest change of the value the user can make, as a fraction of a single item. The default of 1 only allows whole items, 0.5 adds halves, 0.1 makes every tenth selectable. It constrains what the user can pick, not what can be displayed.",
        },
        new()
        {
            Name = "SelectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for selected rating elements using external icon libraries (e.g. FontAwesome, Bootstrap Icons). Takes precedence over SelectedIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "SelectedIconName",
            Type = "string?",
            DefaultValue = "FavoriteStarFill",
            Description = "Custom icon name for selected rating elements (Fluent UI). For external icon libraries, use SelectedIcon instead.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Size of rating elements.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitRatingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitRating.",
            LinkType = LinkType.Link,
            Href = "#rating-class-styles",
        },
        new()
        {
            Name = "UnselectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for unselected rating elements using external icon libraries (e.g. FontAwesome, Bootstrap Icons). Takes precedence over UnselectedIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "UnselectedIconName",
            Type = "string?",
            DefaultValue = "FavoriteStar",
            Description = "Custom icon name for unselected rating elements (Fluent UI). For external icon libraries, use UnselectedIcon instead.",
        },
        new()
        {
            Name = "ValueTextFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the spoken form of the current value, where placeholder {0} is the value and placeholder {1} is the max. It is what the live region of an interactive rating announces for a value no radio can carry, and what a read-only rating falls back to when it is given no other label. The default is \"{0} of {1}\".",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stacks the rating items in a column instead of a row, filling from the bottom up so that \"more\" is up, the way the ArrowUp key means more.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "rating-class-styles",
            Title = "BitRatingClassStyles",
            Description = "The CSS classes and styles of the individual parts of the BitRating.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the rating.",
                },
                new()
                {
                    Name = "LabelContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the label of the rating.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the rating.",
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description of the rating.",
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the rating items.",
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating's button.",
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating icon container.",
                },
                new()
                {
                    Name = "SelectedIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating selected icon.",
                },
                new()
                {
                    Name = "UnselectedIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating unselected icon.",
                }
            ]
        },
        new()
        {
            Id = "rating-change-args",
            Title = "BitRatingChangeArgs",
            Description = "The arguments of the OnChanging callback, which runs before the value of the rating changes.",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "double",
                    Description = "The rating value the component is about to move to.",
                },
                new()
                {
                    Name = "OldValue",
                    Type = "double",
                    Description = "The rating value the component is moving away from.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the change and keep the current value of the rating.",
                }
            ]
        },
        new()
        {
            Id = "rating-item-context",
            Title = "BitRatingItemContext",
            Description = "The context passed to the ItemTemplate, describing the rating item being rendered.",
            Parameters =
            [
                new()
                {
                    Name = "Index",
                    Type = "int",
                    Description = "The one-based position of the item in the rating.",
                },
                new()
                {
                    Name = "Max",
                    Type = "int",
                    Description = "The number of items the rating renders.",
                },
                new()
                {
                    Name = "Percentage",
                    Type = "double",
                    Description = "How much of the item is filled, from 0 to 100. A partially filled item is the fractional part of the value.",
                },
                new()
                {
                    Name = "DisplayValue",
                    Type = "double",
                    Description = "The value the item is rendered from, which is the hovered value while a hover preview is active, and the committed value otherwise.",
                },
                new()
                {
                    Name = "Value",
                    Type = "double",
                    Description = "The committed value of the rating, regardless of any hover preview.",
                },
                new()
                {
                    Name = "IsSelected",
                    Type = "bool",
                    Description = "Whether the item is filled at all, meaning its Percentage is greater than zero.",
                },
                new()
                {
                    Name = "IsFull",
                    Type = "bool",
                    Description = "Whether the item is completely filled, meaning its Percentage is 100.",
                }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Represents icon information for rendering icons. Supports built-in Fluent UI icons and external icon libraries (FontAwesome, Bootstrap Icons, etc.). Use BitIconInfo.Css(\"fa-solid fa-star\"), BitIconInfo.Fa(\"solid star\"), or BitIconInfo.Bi(\"star-fill\") for external icons.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon.",
                },
                new()
                {
                    Name = "BaseClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the base CSS class for the icon. For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty.",
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class prefix used before the icon name. For external icon libraries, you might set this to \"fa-\" or leave empty.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the size of the rating items.",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "Display rating icon using small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "Display rating icon using medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "Display rating icon using large size.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "Determines where the label of the rating sits relative to its items.",
            Items =
            [
                new() { Name = "Top", Description = "The label sits above the items.", Value = "0" },
                new() { Name = "End", Description = "The label sits after the items, on the same line.", Value = "1" },
                new() { Name = "Bottom", Description = "The label sits below the items.", Value = "2" },
                new() { Name = "Start", Description = "The label sits before the items, on the same line.", Value = "3" }
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Rating-color",
            DefaultValue = "The Color role's main color",
            Description = "Color of the filled part of the items.",
        },
        new()
        {
            Name = "--bit-Rating-unselected-color",
            DefaultValue = "--bit-clr-fg-ter",
            Description = "Color of the unfilled part of the items, which stays neutral whatever the Color is so that it keeps reading as \"not rated yet\".",
        },
        new()
        {
            Name = "--bit-Rating-hover-color",
            DefaultValue = "The Color role's hover color",
            Description = "Color of the filled part while the pointer is previewing a value over the items (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Rating-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring of an item.",
        },
        new()
        {
            Name = "--bit-Rating-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Color of both parts of the items, and of the label, when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-Rating-invalid-color",
            DefaultValue = "--bit-clr-err (items), --bit-clr-err-focus (ring)",
            Description = "Color of both parts of the items, and of the focus ring, while the value is invalid.",
        },
        new()
        {
            Name = "--bit-Rating-size",
            DefaultValue = "Per size: --bit-siz-icon-sm / -md / -lg",
            Description = "Size of the item glyphs, which the Size parameter otherwise picks.",
        },
        new()
        {
            Name = "--bit-Rating-target-size",
            DefaultValue = "1.5rem",
            Description = "Smallest pointer target of an item on both axes, which the glyph is centred in - the 24px minimum of WCAG 2.2 (SC 2.5.8). Raise it for the roomier targets of a touch platform, or set it to 0 to shrink the items to the glyph and its padding, for a rating that has to sit inside a line of running text.",
        },
        new()
        {
            Name = "--bit-Rating-padding",
            DefaultValue = "spacing(0.25)",
            Description = "Padding of an item around its glyph, which only widens the item once it exceeds the target size.",
        },
        new()
        {
            Name = "--bit-Rating-gap",
            DefaultValue = "0",
            Description = "Extra room between the items, beyond their own padding.",
        },
        new()
        {
            Name = "--bit-Rating-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of an item and of its focus ring.",
        },
        new()
        {
            Name = "--bit-Rating-hover-scale",
            DefaultValue = "1.1",
            Description = "How much the item under the pointer grows, which is the affordance that says the items are there to be pressed. A value of 1 turns it off.",
        },
        new()
        {
            Name = "--bit-Rating-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the label.",
        },
        new()
        {
            Name = "--bit-Rating-label-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of the label.",
        },
        new()
        {
            Name = "--bit-Rating-label-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the label and the items, and between the items and the description.",
        },
        new()
        {
            Name = "--bit-Rating-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of the description.",
        },
        new()
        {
            Name = "--bit-Rating-description-font-size",
            DefaultValue = "--bit-tpg-fs-xs",
            Description = "Text size of the description.",
        }
    ];



    private double labelValue = 3;
    private double labelTemplateValue = 4;

    private double verticalValue = 3;
    private double verticalPrecisionValue = 3.5;

    private double halfPrecisionValue = 2.5;
    private double quarterPrecisionValue = 3.25;
    private double exactPrecisionValue = 3.7;

    private double noZeroValue;
    private double allowZeroValue;
    private double allowClearValue = 3;

    private double highlightValue = 3;

    private double hoverBoundValue = 3;
    private double? hoverPreviewValue;
    private readonly string[] ratingWords = ["Not rated yet", "Terrible", "Bad", "Normal", "Good", "Wonderful"];

    private double perItemIconValue = 4;
    private double faceValue = 3;
    private readonly string[] faceIcons =
    [
        BitIconName.EmojiDisappointed,
        BitIconName.Sad,
        BitIconName.EmojiNeutral,
        BitIconName.Emoji,
        BitIconName.Emoji2
    ];

    private double templateValue = 7;
    private double moodValue = 4;
    private readonly string[] moodFaces = ["😖", "😐", "🙂", "😀", "🤩"];

    private double oneWayBinding = 0;
    private double twoWayBinding = 3;

    private double onChangeValue;
    private double onChangingValue = 3;
    private bool changeRejected;

    private double accessibilityValue = 3;

    private double scoreValue = 2;
    private readonly string[] scoreWords = ["Unrated", "Poor", "Poor", "Okay", "Great", "Great"];

    private double cascadeValue = 3;
    private readonly BitRatingParams[] ratingParams =
    [
        new()
        {
            ReadOnly = true,
            Precision = 0.5,
            Size = BitSize.Small,
            Color = BitColor.Warning,
            SelectedIconName = BitIconName.HeartFill,
            UnselectedIconName = BitIconName.Heart,
            LabelPosition = BitLabelPosition.Start
        }
    ];

    public BitRatingDemoFormModel ValidationModel = new();
    public string? SuccessMessage;



    private void HandleOnChanging(BitRatingChangeArgs args)
    {
        changeRejected = args.Value < args.OldValue;

        args.Cancel = changeRejected;
    }

    private BitIconInfo GetFaceIcon(int index) => BitIconInfo.Bit(faceIcons[index - 1]);

    private BitColor GetScoreColor() => scoreValue switch
    {
        <= 2 => BitColor.Error,
        <= 3 => BitColor.Warning,
        _ => BitColor.Success
    };

    private BitIconInfo GetScoreIcon(int index) => BitIconInfo.Bit(scoreValue switch
    {
        <= 2 => BitIconName.Sad,
        <= 3 => BitIconName.EmojiNeutral,
        _ => BitIconName.Emoji2
    });

    private string GetRatingAriaLabel(double value, double max) => $"Rated {value} out of {max}";

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(2000);
        SuccessMessage = string.Empty;
        ValidationModel.Value = default;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }
}

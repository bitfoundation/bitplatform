using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnnouncementProvider",
            Type = "Func<BitSearchBoxAnnouncementArgs, string?>?",
            DefaultValue = "null",
            Description = "Builds the text that the screen reader announces through the live region of the search box whenever the suggest items change, in place of the built-in English announcements. Returning null or an empty string announces nothing.",
            LinkType = LinkType.Link,
            Href = "#announcement-args",
        },
        new()
        {
            Name = "AutoSelectSuggestItem",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically highlights the first suggest item as soon as the suggest list opens, so pressing enter selects it without pressing the arrow keys first.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The background color kind of the search box.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template rendered at the bottom of the suggest items callout.",
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template rendered at the top of the suggest items callout.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the search box.",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
        },
        new()
        {
            Name = "ClearButtonAriaLabel",
            Type = "string",
            DefaultValue = "Clear",
            Description = "The accessible label (aria-label) of the clear button.",
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display on the clear button using custom CSS classes for external icon libraries. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "Gets or sets the name of the icon to display on the clear button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "ClearButtonTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for clear button icon.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the search box, used for colored parts like icons.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DisableAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to animate the search box icon on focus.",
        },
        new()
        {
            Name = "EnterKeyHint",
            Type = "BitEnterKeyHint?",
            DefaultValue = "BitEnterKeyHint.Search",
            Description = "Sets the enterkeyhint html attribute of the input element, which tells virtual keyboards which action label to render on their enter key. It defaults to Search because pressing enter always runs a search here.",
            LinkType = LinkType.Link,
            Href = "#enter-key-hint-enum",
        },
        new()
        {
            Name = "FixedCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Forces the suggest callout width to be always fixed at the component's width.",
        },
        new()
        {
            Name = "FixedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to make the icon be always visible (it hides by default when the search box is focused).",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the search box to fill the available width of its container.",
        },
        new()
        {
            Name = "HideClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the clear button when the search box has value.",
        },
        new()
        {
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the icon is visible.",
        },
        new()
        {
            Name = "HighlightSuggestItems",
            Type = "bool",
            DefaultValue = "false",
            Description = "Highlights the part of each suggest item that matches the current search term.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "Search",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the input element.",
            LinkType = LinkType.Link,
            Href = "#input-mode",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the label of the search box, rendered as a real label element tied to the input.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the search box.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template rendered in place of the default spinner while the SuggestItemsProvider is resolving the suggest items.",
        },
        new()
        {
            Name = "LoadingText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered next to the loading indicator while the SuggestItemsProvider is resolving the suggest items.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Sets the maxlength html attribute of the input element. A negative value means no limit.",
        },
        new()
        {
            Name = "MaxSuggestCount",
            Type = "int",
            DefaultValue = "5",
            Description = "The maximum number of items or suggestions that will be displayed. A value of zero or less means no limit.",
        },
        new()
        {
            Name = "MinSuggestTriggerChars",
            Type = "int",
            DefaultValue = "3",
            Description = "The minimum character requirement for doing a search in suggest items. Setting it to zero also enables searching with an empty search term which is useful for showing default or recent items.",
        },
        new()
        {
            Name = "MinSuggestTriggerCharsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the hint the callout shows while the typed term is still shorter than MinSuggestTriggerChars, which receives the number of characters that are still missing, for example \"Type {0} more characters to search\". The hint is never shown while the field is empty, and it replaces the built-in English sentence announced to screen readers as well.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the overlay of suggest items callout.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the search box.",
        },
        new()
        {
            Name = "NoClearOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents clearing the value of the search box when the user presses the escape key.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template rendered in the callout when the search finds no suggest item.",
        },
        new()
        {
            Name = "NoResultsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered in the callout when the search finds no suggest item.",
        },
        new()
        {
            Name = "NoWrapNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the up and down arrows from cycling between the two ends of the suggest list, so that the highlight stops at the first and the last item instead of jumping from one to the other.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback executed when the user clears the search box by either clicking 'X' or hitting escape.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback executed when the user clicks on the input of the search box.",
        },
        new()
        {
            Name = "OnEscape",
            Type = "EventCallback",
            Description = "Callback executed when the user presses escape in the search box.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback executed when the input of the search box gets focused.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback executed when the input of the search box gets focused in.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback executed when the input of the search box loses focus.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback executed on each key down of the input of the search box.",
        },
        new()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback executed on each key up of the input of the search box.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "Callback executed when the user presses enter in the search box, clicks the search button, or picks one of the suggest items.",
        },
        new()
        {
            Name = "OnSuggestItemSelect",
            Type = "EventCallback<string>",
            Description = "Callback executed when the user selects one of the suggest items either by clicking on it or by pressing enter while it is highlighted.",
        },
        new()
        {
            Name = "OnSuggestItemsToggle",
            Type = "EventCallback<bool>",
            Description = "Callback executed with true when the suggest items callout opens and with false when it closes.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder for the search box.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prefix text displayed before the search box input. This is not included in the value.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the prefix of the search box.",
        },
        new()
        {
            Name = "SearchButtonAriaLabel",
            Type = "string",
            DefaultValue = "Search",
            Description = "The accessible label (aria-label) of the search button.",
        },
        new()
        {
            Name = "SearchButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display on the search button using custom CSS classes for external icon libraries. Takes precedence over SearchButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "SearchButtonIconName",
            Type = "string?",
            DefaultValue = "ChromeBackMirrored",
            Description = "Gets or sets the name of the icon to display on the search button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "SearchButtonTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for search button icon.",
        },
        new()
        {
            Name = "SelectTextOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Selects the text already in the search box whenever the input takes the focus, so that typing replaces the previous term instead of appending to it. It does nothing while the field is empty.",
        },
        new()
        {
            Name = "ShowSearchButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the search button.",
        },
        new()
        {
            Name = "ShowSuggestItemsOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the suggest items callout as soon as the input gets focused, without waiting for the user to type. Combine it with a zero MinSuggestTriggerChars to implement default or recent search items.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the search box.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "SpellCheck",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Sets the spellcheck html attribute of the input element. Leaving it null keeps the default behavior of the browser, setting it to false removes the red squiggles from search terms that are not real words.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
            Description = "Custom CSS styles for different parts of the search box.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix text displayed after the search box input. This is not included in the value.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the suffix of the search box.",
        },
        new()
        {
            Name = "SuggestFilterFunction",
            Type = "Func<string?, string?, bool>?",
            DefaultValue = "null",
            Description = "Custom search function to be used in place of the default search algorithm. The first argument is the current search term and the second one is the suggest item to examine.",
        },
        new()
        {
            Name = "SuggestIgnoreDiacritics",
            Type = "bool",
            DefaultValue = "false",
            Description = "Matches the search term against the suggest items with the diacritics of both removed, so that \"Jose\" finds \"José\" and \"Muller\" finds \"Müller\". The item text itself is left untouched, and so is the part of it that HighlightSuggestItems emphasizes. Ignored when a SuggestFilterFunction is provided, but still applied to the highlight.",
        },
        new()
        {
            Name = "SuggestItems",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The list of suggest items to display in the callout."
        },
        new()
        {
            Name = "SuggestItemsAriaLabel",
            Type = "string",
            DefaultValue = "Suggestions",
            Description = "The accessible label (aria-label) of the suggest items list.",
        },
        new()
        {
            Name = "SuggestItemsProvider",
            Type = "BitSearchBoxSuggestItemsProvider?",
            DefaultValue = "null",
            Description = "The item provider function providing suggest items.",
            LinkType = LinkType.Link,
            Href = "#suggest-items-provider-request",
        },
        new()
        {
            Name = "SuggestItemTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the suggest items of the search box.",
        },
        new()
        {
            Name = "Trim",
            Type = "bool",
            DefaultValue = "false",
            Description = "Trims the leading and trailing white-spaces of the value of the search box.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the search box is underlined.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "searchbox-class-styles",
            Title = "BitSearchBoxClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the search box.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focus state of the search box.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's label.",
                },
                new()
                {
                    Name = "Wrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the input container and the search button of the search box.",
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's input container.",
                },
                new()
                {
                    Name = "IconWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's icon wrapper.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search icon.",
                },
                new()
                {
                    Name = "PrefixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search prefix container.",
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search prefix.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's Input.",
                },
                new()
                {
                    Name = "SuffixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search suffix container.",
                },
                new()
                {
                    Name = "Suffix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search suffix.",
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button.",
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button icon.",
                },
                new()
                {
                    Name = "SearchButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search button.",
                },
                new()
                {
                    Name = "SearchButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search button icon.",
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's overlay.",
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's callout.",
                },
                new()
                {
                    Name = "CalloutHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the search box's callout.",
                },
                new()
                {
                    Name = "CalloutFooter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the search box's callout.",
                },
                new()
                {
                    Name = "Loading",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading container of the search box's callout.",
                },
                new()
                {
                    Name = "NoResults",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the no-results container of the search box's callout.",
                },
                new()
                {
                    Name = "Hint",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the too-short-term hint of the search box's callout.",
                },
                new()
                {
                    Name = "ScrollContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's scroll container.",
                },
                new()
                {
                    Name = "SuggestItemWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's suggest item wrapper.",
                },
                new()
                {
                    Name = "SuggestItemButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's suggest item button.",
                },
                new()
                {
                    Name = "SuggestItemText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's suggest item text.",
                },
                new()
                {
                    Name = "SuggestItemHighlight",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the highlighted part of the search box's suggest item text.",
                },
            ]
        },
        new()
        {
            Id = "suggest-items-provider-request",
            Title = "BitSearchBoxSuggestItemsProviderRequest",
            Description = "The context passed to the SuggestItemsProvider delegate on every search.",
            Parameters =
            [
                new()
                {
                    Name = "SearchTerm",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The current value of the search box that the suggest items must be resolved for.",
                },
                new()
                {
                    Name = "Take",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The value of the MaxSuggestCount parameter, so the provider can only fetch as many items as will be rendered.",
                },
                new()
                {
                    Name = "CancellationToken",
                    Type = "CancellationToken",
                    DefaultValue = "",
                    Description = "A token that is cancelled as soon as a newer search starts, so an outdated request can be aborted and can never overwrite a newer result.",
                },
            ]
        },
        new()
        {
            Id = "announcement-args",
            Title = "BitSearchBoxAnnouncementArgs",
            Description = "The state of the suggest items at the moment the screen reader announcement is built, passed to the AnnouncementProvider.",
            Parameters =
            [
                new()
                {
                    Name = "SearchTerm",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The current value of the search box that the suggest items were resolved for.",
                },
                new()
                {
                    Name = "SuggestItems",
                    Type = "IReadOnlyList<string>",
                    DefaultValue = "[]",
                    Description = "The suggest items that are about to be rendered in the callout.",
                },
                new()
                {
                    Name = "IsLoading",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether an asynchronous SuggestItemsProvider is still resolving the suggest items.",
                },
                new()
                {
                    Name = "IsSearchTermTooShort",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the search term is still shorter than the MinSuggestTriggerChars, so no search was performed at all.",
                },
                new()
                {
                    Name = "MinSuggestTriggerChars",
                    Type = "int",
                    DefaultValue = "3",
                    Description = "The value of the MinSuggestTriggerChars parameter.",
                },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "enter-key-hint-enum",
            Name = "BitEnterKeyHint",
            Description = "Tells the browser which action label (or icon) to present for the enter key of a virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "Enter",
                    Description="Typically inserting a new line.",
                    Value="0",
                },
                new()
                {
                    Name= "Done",
                    Description="Typically meaning there is nothing more to input and the input method editor will be closed.",
                    Value="1",
                },
                new()
                {
                    Name= "Go",
                    Description="Typically meaning to take the user to the target of the text they typed.",
                    Value="2",
                },
                new()
                {
                    Name= "Next",
                    Description="Typically taking the user to the next field that will accept text.",
                    Value="3",
                },
                new()
                {
                    Name= "Previous",
                    Description="Typically taking the user to the previous field that will accept text.",
                    Value="4",
                },
                new()
                {
                    Name= "Search",
                    Description="Typically taking the user to the results of searching for the text they have typed.",
                    Value="5",
                },
                new()
                {
                    Name= "Send",
                    Description="Typically delivering the text to its target.",
                    Value="6",
                }
            ]
        },
        new()
        {
            Id = "input-mode",
            Name = "BitInputMode",
            Description = "This allows a browser to display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The input expects text characters.",
                    Value="0",
                },
                new()
                {
                    Name= "Text",
                    Description="Standard input keyboard for the user's current locale.",
                    Value="1",
                },
                new()
                {
                    Name= "Decimal",
                    Description="Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.",
                    Value="2",
                },
                new()
                {
                    Name= "Numeric",
                    Description="Numeric input keyboard, but only requires the digits 0–9.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key",
                    Value="4",
                },
                new()
                {
                    Name= "Search",
                    Description="A virtual keyboard optimized for search input.",
                    Value="5",
                },
                new()
                {
                    Name= "Email",
                    Description="A virtual keyboard optimized for entering email addresses.",
                    Value="6",
                },
                new()
                {
                    Name= "Url",
                    Description="A keypad optimized for entering URLs.",
                    Value="7",
                }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitSearchBox.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitSearchBox.",
        },
        new()
        {
            Name = "IsSuggestItemsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the callout of the suggest items is currently open.",
        },
        new()
        {
            Name = "Clear",
            Type = "Task",
            Description = "Clears the value of the BitSearchBox and invokes the OnClear callback.",
        },
        new()
        {
            Name = "ShowSuggestItems",
            Type = "Task",
            Description = "Runs the suggest items search of the current value and opens the callout of the suggest items.",
        },
        new()
        {
            Name = "HideSuggestItems",
            Type = "Task",
            Description = "Closes the callout of the suggest items.",
        }
    ];



    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private string? maxLengthValue;
    private string? trimmedValue;

    private string? twoWaySearchValue;
    private string? immediateTwoWaySearchValue;
    private string? onChangeSearchValue;
    private string? onSearchValue;
    private string? uncontrolledValue;

    private readonly List<string> eventLogs = [];

    private string? searchValue;
    private string? searchValueWithSuggestFilterFunction;
    private string? searchValueWithSearchDelay;
    private string? searchValueWithMinSearchLength;
    private string? searchValueWithMaxSuggestedItems;
    private string? searchValueWithItemsProvider;
    private string? selectedSuggestItem;

    private string? announcedText;

    private bool isSuggestOpen;
    private BitSearchBox searchBoxRef = default!;

    private readonly ValidationSearchBoxModel validationModel = new();

    private string? AnnounceSuggestItems(BitSearchBoxAnnouncementArgs args)
    {
        announcedText = args switch
        {
            { IsLoading: true } => "Looking for matches...",
            { SearchTerm: null or "" } => null,
            { IsSearchTermTooShort: true } => $"Keep typing, {args.MinSuggestTriggerChars} characters are needed to search.",
            { SuggestItems.Count: 0 } => $"Nothing matches '{args.SearchTerm}'. Try another word.",
            { SuggestItems.Count: 1 } => $"One match for '{args.SearchTerm}': {args.SuggestItems[0]}. Press enter to pick it.",
            _ => $"{args.SuggestItems.Count} matches for '{args.SearchTerm}', from {args.SuggestItems[0]} to {args.SuggestItems[^1]}."
        };

        // The provider is called by the search box while it renders itself, which never re-renders
        // this page, so the announced text below the field has to ask for a render of its own.
        _ = InvokeAsync(StateHasChanged);

        return announcedText;
    }

    private void Log(string message)
    {
        eventLogs.Insert(0, message);

        if (eventLogs.Count > 20)
        {
            eventLogs.RemoveAt(eventLogs.Count - 1);
        }
    }

    private void HandleOnClick() => Log("OnClick");
    private void HandleOnFocusIn() => Log("OnFocusIn");
    private void HandleOnFocusOut() => Log("OnFocusOut");
    private void HandleOnEscape() => Log("OnEscape");
    private void HandleOnClear() => Log("OnClear");
    private void HandleOnSearch(string? value) => Log($"OnSearch: {value}");
    private void HandleOnKeyDown(KeyboardEventArgs args) => Log($"OnKeyDown: {args.Key}");

    private List<string> GetSuggestedItems() =>
    [
         "Apple",
         "Red Apple",
         "Blue Apple",
         "Green Apple",
         "Banana",
         "Orange",
         "Grape",
         "Broccoli",
         "Carrot",
         "Lettuce"
    ];

    private List<string> GetAccentedSuggestedItems() =>
    [
        "José Álvarez",
        "Jürgen Müller",
        "Renée Fauré",
        "Zoë Brontë",
        "Søren Kierkegaard"
    ];

    private List<string> GetRecentSearches() =>
    [
        "Wireless keyboard",
        "Noise cancelling headphones",
        "Mechanical switches",
        "USB-C hub"
    ];

    private List<string> GetPersianSuggestedItems() =>
    [
        "سیب",
        "سیب قرمز",
        "سیب سبز",
        "موز",
        "پرتقال",
        "انگور"
    ];

    private List<string> GetLongSuggestedItems() =>
    [
        "Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple",
        "Red Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple",
        "Blue Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple",
        "Green Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple Apple",
        "Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana Banana",
        "Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange Orange",
        "Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape Grape",
        "Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli Broccoli",
        "Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot Carrot",
        "Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce Lettuce"
    ];

    private Func<string?, string?, bool> SearchFunc = (string? searchText, string? itemText) =>
    {
        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(itemText)) return false;

        return itemText.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
    };

    private async ValueTask<IEnumerable<string>> LoadItemsSlowly(BitSearchBoxSuggestItemsProviderRequest request)
    {
        // an artificial delay to make the loading indicator of the callout observable.
        await Task.Delay(1500, request.CancellationToken);

        return await LoadItems(request);
    }

    private async ValueTask<IEnumerable<string>> LoadItems(BitSearchBoxSuggestItemsProviderRequest request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
            {
                { "$top", request.Take < 1 ? 5 : request.Take },
            };

            if (string.IsNullOrEmpty(request.SearchTerm) is false)
            {
                query.Add("$filter", $"contains(toupper(Name),'{request.SearchTerm.ToUpper()}')");
            }

            var url = NavManager.GetUriWithQueryParameters("api/Products/GetProducts", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto, request.CancellationToken);

            return data!.Items!.Select(i => i.Name)!;
        }
        catch
        {
            return [];
        }
    }
}

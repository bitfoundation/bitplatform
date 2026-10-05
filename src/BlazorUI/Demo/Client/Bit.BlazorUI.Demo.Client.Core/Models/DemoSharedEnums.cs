using System;
using System.Linq;

namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// The enum tables of the types the whole library shares - <c>BitColor</c>, <c>BitSize</c>,
/// <c>BitVariant</c> and the like - written once, for every demo page whose API section lists them.
/// <para>
/// Each page used to carry its own copy of these tables, and dozens of copies of the same table drift:
/// some lost members the enum gained, some kept placeholder descriptions, and the same type was
/// anchored under three different ids. A page now names the table in its <c>componentSubEnums</c>
/// list instead (<c>DemoSharedEnums.BitColor()</c>), which is also all the MCP server reads it from.
/// </para>
/// <para>
/// A table is only shared while what it says holds on every page that shows it. A page whose members
/// mean something of their own there - BitLoading's sizes are pixel sizes, BitNavBar's alignments
/// spread its own items - keeps its own table, under the same anchor id. What a page may still adjust
/// is the line above the table, through <c>description</c> (every factory takes one), and the members
/// it actually supports, through <see cref="Only"/>.
/// </para>
/// <para>
/// Every call builds a new instance, so nothing a page does to its list reaches another page.
/// </para>
/// </summary>
public static class DemoSharedEnums
{
    public static ComponentSubEnum BitAlignment(string? description = null) => new()
    {
        Id = "alignment-enum",
        Name = "BitAlignment",
        Description = description ?? "Defines how the content is placed, or the free space around it shared out, along an axis.",
        Items =
        [
            new() { Name = "Start", Value = "0", Description = "Packs the content at the start of the axis." },
            new() { Name = "End", Value = "1", Description = "Packs the content at the end of the axis." },
            new() { Name = "Center", Value = "2", Description = "Centers the content on the axis." },
            new() { Name = "SpaceBetween", Value = "3", Description = "Distributes the free space between the items, with no space at the two edges." },
            new() { Name = "SpaceAround", Value = "4", Description = "Distributes the free space around the items, so the edges get half of what sits between the items." },
            new() { Name = "SpaceEvenly", Value = "5", Description = "Distributes the free space evenly between the items and at the two edges." },
            new() { Name = "Baseline", Value = "6", Description = "Aligns the items on their baseline." },
            new() { Name = "Stretch", Value = "7", Description = "Stretches the items to fill the axis." },
        ]
    };

    public static ComponentSubEnum BitButtonType(string? description = null) => new()
    {
        Id = "button-type-enum",
        Name = "BitButtonType",
        Description = description ?? "Defines the type attribute of the rendered button element, which decides what clicking it does inside a form.",
        Items =
        [
            new() { Name = "Button", Value = "0", Description = "The button is a clickable button." },
            new() { Name = "Submit", Value = "1", Description = "The button is a submit button (submits form-data)." },
            new() { Name = "Reset", Value = "2", Description = "The button is a reset button (resets the form-data to its initial values)." },
        ]
    };

    public static ComponentSubEnum BitColor(string? description = null) => new()
    {
        Id = "color-enum",
        Name = "BitColor",
        Description = description ?? "Defines the general colors available in the bit BlazorUI.",
        Items =
        [
            new() { Name = "Primary", Value = "0", Description = "Primary general color." },
            new() { Name = "Secondary", Value = "1", Description = "Secondary general color." },
            new() { Name = "Tertiary", Value = "2", Description = "Tertiary general color." },
            new() { Name = "Info", Value = "3", Description = "Info general color." },
            new() { Name = "Success", Value = "4", Description = "Success general color." },
            new() { Name = "Warning", Value = "5", Description = "Warning general color." },
            new() { Name = "SevereWarning", Value = "6", Description = "SevereWarning general color." },
            new() { Name = "Error", Value = "7", Description = "Error general color." },
            new() { Name = "PrimaryBackground", Value = "8", Description = "Primary background color." },
            new() { Name = "SecondaryBackground", Value = "9", Description = "Secondary background color." },
            new() { Name = "TertiaryBackground", Value = "10", Description = "Tertiary background color." },
            new() { Name = "PrimaryForeground", Value = "11", Description = "Primary foreground color." },
            new() { Name = "SecondaryForeground", Value = "12", Description = "Secondary foreground color." },
            new() { Name = "TertiaryForeground", Value = "13", Description = "Tertiary foreground color." },
            new() { Name = "PrimaryBorder", Value = "14", Description = "Primary border color." },
            new() { Name = "SecondaryBorder", Value = "15", Description = "Secondary border color." },
            new() { Name = "TertiaryBorder", Value = "16", Description = "Tertiary border color." },
        ]
    };

    public static ComponentSubEnum BitColorKind(string? description = null) => new()
    {
        Id = "color-kind-enum",
        Name = "BitColorKind",
        Description = description ?? "Defines the color kinds available in the bit BlazorUI.",
        Items =
        [
            new() { Name = "Primary", Value = "0", Description = "The primary color kind." },
            new() { Name = "Secondary", Value = "1", Description = "The secondary color kind." },
            new() { Name = "Tertiary", Value = "2", Description = "The tertiary color kind." },
            new() { Name = "Transparent", Value = "3", Description = "The transparent color kind." },
        ]
    };

    public static ComponentSubEnum BitDropDirection(string? description = null) => new()
    {
        Id = "drop-direction-enum",
        Name = "BitDropDirection",
        Description = description ?? "Determines the allowed drop directions of the callout.",
        Items =
        [
            new() { Name = "All", Value = "0", Description = "The direction determined automatically based on the available spaces in all directions." },
            new() { Name = "TopAndBottom", Value = "1", Description = "The direction determined automatically based on the available spaces in only top and bottom directions." },
        ]
    };

    public static ComponentSubEnum BitEnterKeyHint(string? description = null) => new()
    {
        Id = "enter-key-hint-enum",
        Name = "BitEnterKeyHint",
        Description = description ?? "Tells the browser which action label (or icon) to present for the enter key of a virtual keyboard.",
        Items =
        [
            new() { Name = "Enter", Value = "0", Description = "Typically inserting a new line." },
            new() { Name = "Done", Value = "1", Description = "Typically meaning there is nothing more to input and the input method editor will be closed." },
            new() { Name = "Go", Value = "2", Description = "Typically meaning to take the user to the target of the text they typed." },
            new() { Name = "Next", Value = "3", Description = "Typically taking the user to the next field that will accept text." },
            new() { Name = "Previous", Value = "4", Description = "Typically taking the user to the previous field that will accept text." },
            new() { Name = "Search", Value = "5", Description = "Typically taking the user to the results of searching for the text they have typed." },
            new() { Name = "Send", Value = "6", Description = "Typically delivering the text to its target." },
        ]
    };

    public static ComponentSubEnum BitIconLocation(string? description = null) => new()
    {
        Id = "icon-location-enum",
        Name = "BitIconLocation",
        Description = description ?? "Defines the side of the input the icon is shown on.",
        Items =
        [
            new() { Name = "Left", Value = "0", Description = "Show the icon at the left side." },
            new() { Name = "Right", Value = "1", Description = "Show the icon at the right side." },
        ]
    };

    /// <remarks>
    /// Which end is the default differs from one component to the next, so the members do not say;
    /// the parameter table does.
    /// </remarks>
    public static ComponentSubEnum BitIconPosition(string? description = null) => new()
    {
        Id = "icon-position-enum",
        Name = "BitIconPosition",
        Description = description ?? "Describes the placement of an icon relative to other content.",
        Items =
        [
            new() { Name = "Start", Value = "0", Description = "Icon renders before the content." },
            new() { Name = "End", Value = "1", Description = "Icon renders after the content." },
        ]
    };

    public static ComponentSubEnum BitImageLoading(string? description = null) => new()
    {
        Id = "image-loading-enum",
        Name = "BitImageLoading",
        Description = description ?? "Represents the img loading attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading",
        Items =
        [
            new() { Name = "Eager", Value = "0", Description = "The default behavior, eager tells the browser to load the image as soon as the img element is processed." },
            new() { Name = "Lazy", Value = "1", Description = "Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently." },
        ]
    };

    public static ComponentSubEnum BitInputMode(string? description = null) => new()
    {
        Id = "input-mode-enum",
        Name = "BitInputMode",
        Description = description ?? "Defines the inputmode html attribute, which is what lets a browser display an appropriate virtual keyboard.",
        Items =
        [
            new() { Name = "None", Value = "0", Description = "No virtual keyboard. For when the page implements its own keyboard input control." },
            new() { Name = "Text", Value = "1", Description = "Standard input keyboard for the user's current locale." },
            new() { Name = "Decimal", Value = "2", Description = "Fractional numeric input keyboard containing the digits and decimal separator for the user's locale." },
            new() { Name = "Numeric", Value = "3", Description = "Numeric input keyboard, but only requires the digits 0–9." },
            new() { Name = "Tel", Value = "4", Description = "A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key." },
            new() { Name = "Search", Value = "5", Description = "A virtual keyboard optimized for search input." },
            new() { Name = "Email", Value = "6", Description = "A virtual keyboard optimized for entering email addresses." },
            new() { Name = "Url", Value = "7", Description = "A keypad optimized for entering URLs." },
        ]
    };

    public static ComponentSubEnum BitInputType(string? description = null) => new()
    {
        Id = "input-type-enum",
        Name = "BitInputType",
        Description = description ?? "Defines the type attribute of the rendered input element, which decides what it accepts and which virtual keyboard a browser offers for it.",
        Items =
        [
            new() { Name = "Text", Value = "0", Description = "The input expects text characters." },
            new() { Name = "Password", Value = "1", Description = "The input expects password characters." },
            new() { Name = "Number", Value = "2", Description = "The input expects number characters." },
            new() { Name = "Email", Value = "3", Description = "The input expects email characters." },
            new() { Name = "Tel", Value = "4", Description = "The input expects tel characters." },
            new() { Name = "Url", Value = "5", Description = "The input expects url characters." },
            new() { Name = "Search", Value = "6", Description = "The input expects a search term, which is what lets a browser offer the previous searches of the same field and show its own clear affordance." },
        ]
    };

    public static ComponentSubEnum BitLinkRels(string? description = null) => new()
    {
        Id = "link-rels-enum",
        Name = "BitLinkRels",
        Description = description ?? "The rel attribute defines the relationship between a linked resource and the current document.",
        Items =
        [
            new() { Name = "Alternate", Value = "1", Description = "Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)" },
            new() { Name = "Author", Value = "2", Description = "Provides a link to the author of the document." },
            new() { Name = "Bookmark", Value = "4", Description = "Permanent URL used for bookmarking." },
            new() { Name = "External", Value = "8", Description = "Indicates that the referenced document is not part of the same site as the current document." },
            new() { Name = "Help", Value = "16", Description = "Provides a link to a help document." },
            new() { Name = "License", Value = "32", Description = "Provides a link to licensing information for the document." },
            new() { Name = "Next", Value = "64", Description = "Provides a link to the next document in the series." },
            new() { Name = "NoFollow", Value = "128", Description = @"Links to an unendorsed document, like a paid link. (""NoFollow"" is used by Google, to specify that the Google search spider should not follow that link)" },
            new() { Name = "NoOpener", Value = "256", Description = "Requires that any browsing context created by following the hyperlink must not have an opener browsing context." },
            new() { Name = "NoReferrer", Value = "512", Description = "Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink." },
            new() { Name = "Prev", Value = "1024", Description = "The previous document in a selection." },
            new() { Name = "Search", Value = "2048", Description = "Links to a search tool for the document." },
            new() { Name = "Tag", Value = "4096", Description = "A tag (keyword) for the current document." },
            new() { Name = "Me", Value = "8192", Description = "Indicates that the linked document represents the person who owns the current content. (used for identity verification)" },
            new() { Name = "Opener", Value = "16384", Description = "Requires that any browsing context created by following the hyperlink keeps its opener browsing context. (reverses the implicit noopener modern browsers apply to _blank targets)" },
            new() { Name = "PrivacyPolicy", Value = "32768", Description = "Links to the privacy policy that applies to the current document. (rendered as privacy-policy)" },
            new() { Name = "Sponsored", Value = "65536", Description = "Marks the link as an advertisement or paid placement, so search engines do not count it as an organic endorsement." },
            new() { Name = "TermsOfService", Value = "131072", Description = "Links to the terms of service that apply to the current document. (rendered as terms-of-service)" },
            new() { Name = "Ugc", Value = "262144", Description = "Marks the link as user-generated content, like forum posts or comments, for search engines." },
        ]
    };

    public static ComponentSubEnum BitNavAriaCurrent(string? description = null) => new()
    {
        Id = "nav-aria-current-enum",
        Name = "BitNavAriaCurrent",
        Description = description ?? "Defines the value of the aria-current attribute reported by the current link of a set.",
        Items =
        [
            new() { Name = "Page", Value = "0", Description = "Represents the current page within a set of pages." },
            new() { Name = "Step", Value = "1", Description = "Represents the current step within a process." },
            new() { Name = "Location", Value = "2", Description = "Represents the current location within an environment or context." },
            new() { Name = "Date", Value = "3", Description = "Represents the current date within a collection of dates." },
            new() { Name = "Time", Value = "4", Description = "Represents the current time within a set of times." },
            new() { Name = "True", Value = "5", Description = "Represents the current item within a set, without saying which kind of set it is." },
        ]
    };

    public static ComponentSubEnum BitNavItemTemplateRenderMode(string? description = null) => new()
    {
        Id = "nav-item-template-render-mode-enum",
        Name = "BitNavItemTemplateRenderMode",
        Description = description ?? "Defines how the item template of a nav is rendered.",
        Items =
        [
            new() { Name = "Normal", Value = "0", Description = "Renders the template inside the button/anchor root element of the item." },
            new() { Name = "Replace", Value = "1", Description = "Replaces the button/anchor root element of the item." },
        ]
    };

    public static ComponentSubEnum BitNavMatch(string? description = null) => new()
    {
        Id = "nav-match-enum",
        Name = "BitNavMatch",
        Description = description ?? "Modifies the URL matching behavior of an item.",
        Items =
        [
            new() { Name = "Exact", Value = "0", Description = "Specifies that the item should be active when it matches exactly the current URL." },
            new() { Name = "Prefix", Value = "1", Description = "Specifies that the item should be active when it matches any prefix of the current URL." },
            new() { Name = "Regex", Value = "2", Description = "Specifies that the item should be active when its provided regex matches the current URL." },
            new() { Name = "Wildcard", Value = "3", Description = "Specifies that the item should be active when its provided wildcard matches the current URL." },
        ]
    };

    public static ComponentSubEnum BitNavMode(string? description = null) => new()
    {
        Id = "nav-mode-enum",
        Name = "BitNavMode",
        Description = description ?? "Defines whether the selection of the component follows the current URL or is driven by the app.",
        Items =
        [
            new() { Name = "Automatic", Value = "0", Description = "The component follows the browser: it selects the item whose URL points at the page the app currently sits on, and it re-selects on every navigation." },
            new() { Name = "Manual", Value = "1", Description = "The selection is driven by clicks and by the SelectedItem binding instead of by the current URL, which is what a component that switches between the panels of a single page needs." },
        ]
    };

    public static ComponentSubEnum BitNavRenderType(string? description = null) => new()
    {
        Id = "nav-render-type-enum",
        Name = "BitNavRenderType",
        Description = description ?? "Determines how the nav items are rendered visually.",
        Items =
        [
            new() { Name = "Normal", Value = "0", Description = "All items will be rendered normally only based on their own properties." },
            new() { Name = "Grouped", Value = "1", Description = "Root elements are rendered in a specific way that resembles a grouped list of items." },
        ]
    };

    public static ComponentSubEnum BitPanelPosition(string? description = null) => new()
    {
        Id = "panel-position-enum",
        Name = "BitPanelPosition",
        Description = description ?? "The edge of the screen the panel slides in from.",
        Items =
        [
            new() { Name = "Start", Value = "0", Description = "The start edge: the left in left-to-right, the right in right-to-left." },
            new() { Name = "End", Value = "1", Description = "The end edge: the right in left-to-right, the left in right-to-left." },
            new() { Name = "Top", Value = "2", Description = "The top edge." },
            new() { Name = "Bottom", Value = "3", Description = "The bottom edge." },
        ]
    };

    public static ComponentSubEnum BitPoliteness(string? description = null) => new()
    {
        Id = "politeness-enum",
        Name = "BitPoliteness",
        Description = description ?? "How urgently a live region interrupts a screen reader, which is what the aria-live attribute carries.",
        Items =
        [
            new() { Name = "Off", Value = "0", Description = "The region is not a live region: nothing in it is announced as it changes (aria-live=\"off\")." },
            new() { Name = "Polite", Value = "1", Description = "The change waits its turn and is announced once the screen reader has finished what it was saying (aria-live=\"polite\")." },
            new() { Name = "Assertive", Value = "2", Description = "The change interrupts the screen reader and is announced right away (aria-live=\"assertive\")." },
        ]
    };

    public static ComponentSubEnum BitPosition(string? description = null) => new()
    {
        Id = "position-enum",
        Name = "BitPosition",
        Description = description ?? "Defines where the content is placed. Start and End follow the text direction; Left and Right stay on their side.",
        Items =
        [
            new() { Name = "TopLeft", Value = "0", Description = "At the top, against the left edge." },
            new() { Name = "TopCenter", Value = "1", Description = "At the top, centered horizontally." },
            new() { Name = "TopRight", Value = "2", Description = "At the top, against the right edge." },
            new() { Name = "TopStart", Value = "3", Description = "At the top, against the start edge (the left in left-to-right)." },
            new() { Name = "TopEnd", Value = "4", Description = "At the top, against the end edge (the right in left-to-right)." },
            new() { Name = "CenterLeft", Value = "5", Description = "Centered vertically, against the left edge." },
            new() { Name = "Center", Value = "6", Description = "Centered on both axes." },
            new() { Name = "CenterRight", Value = "7", Description = "Centered vertically, against the right edge." },
            new() { Name = "CenterStart", Value = "8", Description = "Centered vertically, against the start edge (the left in left-to-right)." },
            new() { Name = "CenterEnd", Value = "9", Description = "Centered vertically, against the end edge (the right in left-to-right)." },
            new() { Name = "BottomLeft", Value = "10", Description = "At the bottom, against the left edge." },
            new() { Name = "BottomCenter", Value = "11", Description = "At the bottom, centered horizontally." },
            new() { Name = "BottomRight", Value = "12", Description = "At the bottom, against the right edge." },
            new() { Name = "BottomStart", Value = "13", Description = "At the bottom, against the start edge (the left in left-to-right)." },
            new() { Name = "BottomEnd", Value = "14", Description = "At the bottom, against the end edge (the right in left-to-right)." },
        ]
    };

    public static ComponentSubEnum BitSize(string? description = null) => new()
    {
        Id = "size-enum",
        Name = "BitSize",
        Description = description ?? "Defines the sizes available in the bit BlazorUI.",
        Items =
        [
            new() { Name = "Small", Value = "0", Description = "The small size." },
            new() { Name = "Medium", Value = "1", Description = "The medium size." },
            new() { Name = "Large", Value = "2", Description = "The large size." },
        ]
    };

    public static ComponentSubEnum BitTimeFormat(string? description = null) => new()
    {
        Id = "time-format-enum",
        Name = "BitTimeFormat",
        Description = description ?? "Defines the clock the time picker shows its hours in.",
        Items =
        [
            new() { Name = "TwentyFourHours", Value = "0", Description = "Show time pickers in 24 hours format." },
            new() { Name = "TwelveHours", Value = "1", Description = "Show time pickers in 12 hours format." },
        ]
    };

    public static ComponentSubEnum BitVariant(string? description = null) => new()
    {
        Id = "variant-enum",
        Name = "BitVariant",
        Description = description ?? "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
        Items =
        [
            new() { Name = "Fill", Value = "0", Description = "Fill styled variant." },
            new() { Name = "Outline", Value = "1", Description = "Outline styled variant." },
            new() { Name = "Text", Value = "2", Description = "Text styled variant." },
        ]
    };

    /// <summary>
    /// Narrows a shared table down to the members a component actually supports, in the table's own
    /// order - BitPagination renders only the eight general colors of <c>BitColor</c>. A name that is not
    /// a member of the table throws, so a typo or a renamed member cannot quietly empty a page's table.
    /// </summary>
    public static ComponentSubEnum Only(this ComponentSubEnum subEnum, params string[] names)
    {
        var unknown = names.Except(subEnum.Items.Select(i => i.Name)).ToArray();

        if (unknown.Length > 0)
            throw new ArgumentException($"{string.Join(", ", unknown)} is not a member of {subEnum.Name}.", nameof(names));

        subEnum.Items = [.. subEnum.Items.Where(i => names.Contains(i.Name))];

        return subEnum;
    }
}

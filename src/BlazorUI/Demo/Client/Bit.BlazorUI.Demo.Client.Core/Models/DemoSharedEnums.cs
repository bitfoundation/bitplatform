using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Library = Bit.BlazorUI;

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
/// The members and their values are read off the enum itself, so a table cannot list a member the enum
/// does not have, or number one wrongly; all a table writes is the prose. That prose is the enum's own
/// XML documentation - what IntelliSense and the MCP server's <c>GetBitBlazorUIType</c> say about the
/// same member - written out here because a WebAssembly page has no XML documentation to read it from,
/// and <c>DemoSharedEnumsTests</c> fails the moment the two say different things.
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
    public static ComponentSubEnum BitAlignment(string? description = null)
    {
        return Table<Library.BitAlignment>("alignment-enum", description ?? "Defines how the content is placed, or the free space around it shared out, along an axis.", new()
        {
            [Library.BitAlignment.Start] = "Packs the content at the start of the axis.",
            [Library.BitAlignment.End] = "Packs the content at the end of the axis.",
            [Library.BitAlignment.Center] = "Centers the content on the axis.",
            [Library.BitAlignment.SpaceBetween] = "Distributes the free space between the items, with no space at the two edges.",
            [Library.BitAlignment.SpaceAround] = "Distributes the free space around the items, so the edges get half of what sits between the items.",
            [Library.BitAlignment.SpaceEvenly] = "Distributes the free space evenly between the items and at the two edges.",
            [Library.BitAlignment.Baseline] = "Aligns the items on their baseline.",
            [Library.BitAlignment.Stretch] = "Stretches the items to fill the axis.",
        });
    }

    public static ComponentSubEnum BitButtonType(string? description = null)
    {
        return Table<Library.BitButtonType>("button-type-enum", description ?? "Defines the type attribute of the rendered button element, which decides what clicking it does inside a form.", new()
        {
            [Library.BitButtonType.Button] = "The button is a clickable button.",
            [Library.BitButtonType.Submit] = "The button is a submit button (submits form-data).",
            [Library.BitButtonType.Reset] = "The button is a reset button (resets the form-data to its initial values).",
        });
    }

    public static ComponentSubEnum BitColor(string? description = null)
    {
        return Table<Library.BitColor>("color-enum", description ?? "Defines the general colors available in the bit BlazorUI.", new()
        {
            [Library.BitColor.Primary] = "Primary general color.",
            [Library.BitColor.Secondary] = "Secondary general color.",
            [Library.BitColor.Tertiary] = "Tertiary general color.",
            [Library.BitColor.Info] = "Info general color.",
            [Library.BitColor.Success] = "Success general color.",
            [Library.BitColor.Warning] = "Warning general color.",
            [Library.BitColor.SevereWarning] = "SevereWarning general color.",
            [Library.BitColor.Error] = "Error general color.",
            [Library.BitColor.PrimaryBackground] = "Primary background color.",
            [Library.BitColor.SecondaryBackground] = "Secondary background color.",
            [Library.BitColor.TertiaryBackground] = "Tertiary background color.",
            [Library.BitColor.PrimaryForeground] = "Primary foreground color.",
            [Library.BitColor.SecondaryForeground] = "Secondary foreground color.",
            [Library.BitColor.TertiaryForeground] = "Tertiary foreground color.",
            [Library.BitColor.PrimaryBorder] = "Primary border color.",
            [Library.BitColor.SecondaryBorder] = "Secondary border color.",
            [Library.BitColor.TertiaryBorder] = "Tertiary border color.",
        });
    }

    public static ComponentSubEnum BitColorKind(string? description = null)
    {
        return Table<Library.BitColorKind>("color-kind-enum", description ?? "Defines the color kinds available in the bit BlazorUI.", new()
        {
            [Library.BitColorKind.Primary] = "The primary color kind.",
            [Library.BitColorKind.Secondary] = "The secondary color kind.",
            [Library.BitColorKind.Tertiary] = "The tertiary color kind.",
            [Library.BitColorKind.Transparent] = "The transparent color kind.",
        });
    }

    public static ComponentSubEnum BitDropDirection(string? description = null)
    {
        return Table<Library.BitDropDirection>("drop-direction-enum", description ?? "Determines the allowed drop directions of the callout.", new()
        {
            [Library.BitDropDirection.All] = "The direction determined automatically based on the available spaces in all directions.",
            [Library.BitDropDirection.TopAndBottom] = "The direction determined automatically based on the available spaces in only top and bottom directions.",
        });
    }

    public static ComponentSubEnum BitEnterKeyHint(string? description = null)
    {
        return Table<Library.BitEnterKeyHint>("enter-key-hint-enum", description ?? "Tells the browser which action label (or icon) to present for the enter key of a virtual keyboard.", new()
        {
            [Library.BitEnterKeyHint.Enter] = "Typically inserting a new line.",
            [Library.BitEnterKeyHint.Done] = "Typically meaning there is nothing more to input and the input method editor will be closed.",
            [Library.BitEnterKeyHint.Go] = "Typically meaning to take the user to the target of the text they typed.",
            [Library.BitEnterKeyHint.Next] = "Typically taking the user to the next field that will accept text.",
            [Library.BitEnterKeyHint.Previous] = "Typically taking the user to the previous field that will accept text.",
            [Library.BitEnterKeyHint.Search] = "Typically taking the user to the results of searching for the text they have typed.",
            [Library.BitEnterKeyHint.Send] = "Typically delivering the text to its target.",
        });
    }

    public static ComponentSubEnum BitIconLocation(string? description = null)
    {
        return Table<Library.BitIconLocation>("icon-location-enum", description ?? "Defines the side of the input the icon is shown on.", new()
        {
            [Library.BitIconLocation.Left] = "Show the icon at the left side.",
            [Library.BitIconLocation.Right] = "Show the icon at the right side.",
        });
    }

    /// <remarks>
    /// Which end is the default differs from one component to the next, so the members do not say;
    /// the parameter table does.
    /// </remarks>
    public static ComponentSubEnum BitIconPosition(string? description = null)
    {
        return Table<Library.BitIconPosition>("icon-position-enum", description ?? "Describes the placement of an icon relative to other content.", new()
        {
            [Library.BitIconPosition.Start] = "Icon renders before the content.",
            [Library.BitIconPosition.End] = "Icon renders after the content.",
        });
    }

    public static ComponentSubEnum BitImageLoading(string? description = null)
    {
        return Table<Library.BitImageLoading>("image-loading-enum", description ?? "Represents the img loading attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading", new()
        {
            [Library.BitImageLoading.Eager] = "Tells the browser to load the image as soon as the img element is processed, which is what a browser does with an img that has no loading attribute.",
            [Library.BitImageLoading.Lazy] = "Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently.",
        });
    }

    public static ComponentSubEnum BitInputMode(string? description = null)
    {
        return Table<Library.BitInputMode>("input-mode-enum", description ?? "Defines the inputmode html attribute, which is what lets a browser display an appropriate virtual keyboard.", new()
        {
            [Library.BitInputMode.None] = "No virtual keyboard. For when the page implements its own keyboard input control.",
            [Library.BitInputMode.Text] = "Standard input keyboard for the user's current locale.",
            [Library.BitInputMode.Decimal] = "Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.",
            [Library.BitInputMode.Numeric] = "Numeric input keyboard, but only requires the digits 0–9.",
            [Library.BitInputMode.Tel] = "A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key.",
            [Library.BitInputMode.Search] = "A virtual keyboard optimized for search input.",
            [Library.BitInputMode.Email] = "A virtual keyboard optimized for entering email addresses.",
            [Library.BitInputMode.Url] = "A keypad optimized for entering URLs.",
        });
    }

    public static ComponentSubEnum BitInputType(string? description = null)
    {
        return Table<Library.BitInputType>("input-type-enum", description ?? "Defines the type attribute of the rendered input element, which decides what it accepts and which virtual keyboard a browser offers for it.", new()
        {
            [Library.BitInputType.Text] = "The input expects text characters.",
            [Library.BitInputType.Password] = "The input expects password characters.",
            [Library.BitInputType.Number] = "The input expects number characters.",
            [Library.BitInputType.Email] = "The input expects email characters.",
            [Library.BitInputType.Tel] = "The input expects tel characters.",
            [Library.BitInputType.Url] = "The input expects url characters.",
            [Library.BitInputType.Search] = "The input expects a search term, which is what lets a browser offer the previous searches of the same field and show its own clear affordance.",
        });
    }

    public static ComponentSubEnum BitLinkRels(string? description = null)
    {
        return Table<Library.BitLinkRels>("link-rels-enum", description ?? "The rel attribute defines the relationship between a linked resource and the current document.", new()
        {
            [Library.BitLinkRels.Alternate] = "Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)",
            [Library.BitLinkRels.Author] = "Provides a link to the author of the document.",
            [Library.BitLinkRels.Bookmark] = "Permanent URL used for bookmarking.",
            [Library.BitLinkRels.External] = "Indicates that the referenced document is not part of the same site as the current document.",
            [Library.BitLinkRels.Help] = "Provides a link to a help document.",
            [Library.BitLinkRels.License] = "Provides a link to licensing information for the document.",
            [Library.BitLinkRels.Next] = "Provides a link to the next document in the series.",
            [Library.BitLinkRels.NoFollow] = "Links to an unendorsed document, like a paid link. (\"NoFollow\" is used by Google, to specify that the Google search spider should not follow that link)",
            [Library.BitLinkRels.NoOpener] = "Requires that any browsing context created by following the hyperlink must not have an opener browsing context.",
            [Library.BitLinkRels.NoReferrer] = "Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink.",
            [Library.BitLinkRels.Prev] = "The previous document in a selection.",
            [Library.BitLinkRels.Search] = "Links to a search tool for the document.",
            [Library.BitLinkRels.Tag] = "A tag (keyword) for the current document.",
            [Library.BitLinkRels.Me] = "Indicates that the linked document represents the person who owns the current content. (used for identity verification)",
            [Library.BitLinkRels.Opener] = "Requires that any browsing context created by following the hyperlink keeps its opener browsing context. (reverses the implicit noopener modern browsers apply to _blank targets)",
            [Library.BitLinkRels.PrivacyPolicy] = "Links to the privacy policy that applies to the current document. (rendered as privacy-policy)",
            [Library.BitLinkRels.Sponsored] = "Marks the link as an advertisement or paid placement, so search engines do not count it as an organic endorsement.",
            [Library.BitLinkRels.TermsOfService] = "Links to the terms of service that apply to the current document. (rendered as terms-of-service)",
            [Library.BitLinkRels.Ugc] = "Marks the link as user-generated content, like forum posts or comments, for search engines.",
        });
    }

    public static ComponentSubEnum BitNavAriaCurrent(string? description = null)
    {
        return Table<Library.BitNavAriaCurrent>("nav-aria-current-enum", description ?? "Defines the value of the aria-current attribute reported by the current link of a set.", new()
        {
            [Library.BitNavAriaCurrent.Page] = "Represents the current page within a set of pages.",
            [Library.BitNavAriaCurrent.Step] = "Represents the current step within a process.",
            [Library.BitNavAriaCurrent.Location] = "Represents the current location within an environment or context.",
            [Library.BitNavAriaCurrent.Date] = "Represents the current date within a collection of dates.",
            [Library.BitNavAriaCurrent.Time] = "Represents the current time within a set of times.",
            [Library.BitNavAriaCurrent.True] = "Represents the current item within a set, without saying which kind of set it is.",
        });
    }

    public static ComponentSubEnum BitNavItemTemplateRenderMode(string? description = null)
    {
        return Table<Library.BitNavItemTemplateRenderMode>("nav-item-template-render-mode-enum", description ?? "Defines how the item template of a nav is rendered.", new()
        {
            [Library.BitNavItemTemplateRenderMode.Normal] = "Renders the template inside the button/anchor root element of the item.",
            [Library.BitNavItemTemplateRenderMode.Replace] = "Replaces the button/anchor root element of the item.",
        });
    }

    public static ComponentSubEnum BitNavMatch(string? description = null)
    {
        return Table<Library.BitNavMatch>("nav-match-enum", description ?? "Modifies the URL matching behavior of an item.", new()
        {
            [Library.BitNavMatch.Exact] = "Specifies that the item should be active when it matches exactly the current URL.",
            [Library.BitNavMatch.Prefix] = "Specifies that the item should be active when it matches any prefix of the current URL.",
            [Library.BitNavMatch.Regex] = "Specifies that the item should be active when its provided regex matches the current URL.",
            [Library.BitNavMatch.Wildcard] = "Specifies that the item should be active when its provided wildcard matches the current URL.",
        });
    }

    public static ComponentSubEnum BitNavMode(string? description = null)
    {
        return Table<Library.BitNavMode>("nav-mode-enum", description ?? "Defines whether the selection of the component follows the current URL or is driven by the app.", new()
        {
            [Library.BitNavMode.Automatic] = "The component follows the browser: it selects the item whose URL points at the page the app currently sits on, and it re-selects on every navigation.",
            [Library.BitNavMode.Manual] = "The selection is driven by clicks and by the SelectedItem binding instead of by the current URL, which is what a component that switches between the panels of a single page needs.",
        });
    }

    public static ComponentSubEnum BitNavRenderType(string? description = null)
    {
        return Table<Library.BitNavRenderType>("nav-render-type-enum", description ?? "Determines how the nav items are rendered visually.", new()
        {
            [Library.BitNavRenderType.Normal] = "All items will be rendered normally only based on their own properties.",
            [Library.BitNavRenderType.Grouped] = "Root elements are rendered in a specific way that resembles a grouped list of items.",
        });
    }

    public static ComponentSubEnum BitPanelPosition(string? description = null)
    {
        return Table<Library.BitPanelPosition>("panel-position-enum", description ?? "The edge of the screen the panel slides in from.", new()
        {
            [Library.BitPanelPosition.Start] = "The start edge: the left in left-to-right, the right in right-to-left.",
            [Library.BitPanelPosition.End] = "The end edge: the right in left-to-right, the left in right-to-left.",
            [Library.BitPanelPosition.Top] = "The top edge.",
            [Library.BitPanelPosition.Bottom] = "The bottom edge.",
        });
    }

    public static ComponentSubEnum BitPoliteness(string? description = null)
    {
        return Table<Library.BitPoliteness>("politeness-enum", description ?? "How urgently a live region interrupts a screen reader, which is what the aria-live attribute carries.", new()
        {
            [Library.BitPoliteness.Off] = "The region is not a live region: nothing in it is announced as it changes (aria-live=\"off\").",
            [Library.BitPoliteness.Polite] = "The change waits its turn and is announced once the screen reader has finished what it was saying (aria-live=\"polite\").",
            [Library.BitPoliteness.Assertive] = "The change interrupts the screen reader and is announced right away (aria-live=\"assertive\").",
        });
    }

    public static ComponentSubEnum BitPosition(string? description = null)
    {
        return Table<Library.BitPosition>("position-enum", description ?? "Defines where the content is placed. Start and End follow the text direction; Left and Right stay on their side.", new()
        {
            [Library.BitPosition.TopLeft] = "At the top, against the left edge.",
            [Library.BitPosition.TopCenter] = "At the top, centered horizontally.",
            [Library.BitPosition.TopRight] = "At the top, against the right edge.",
            [Library.BitPosition.TopStart] = "At the top, against the start edge (the left in left-to-right).",
            [Library.BitPosition.TopEnd] = "At the top, against the end edge (the right in left-to-right).",
            [Library.BitPosition.CenterLeft] = "Centered vertically, against the left edge.",
            [Library.BitPosition.Center] = "Centered on both axes.",
            [Library.BitPosition.CenterRight] = "Centered vertically, against the right edge.",
            [Library.BitPosition.CenterStart] = "Centered vertically, against the start edge (the left in left-to-right).",
            [Library.BitPosition.CenterEnd] = "Centered vertically, against the end edge (the right in left-to-right).",
            [Library.BitPosition.BottomLeft] = "At the bottom, against the left edge.",
            [Library.BitPosition.BottomCenter] = "At the bottom, centered horizontally.",
            [Library.BitPosition.BottomRight] = "At the bottom, against the right edge.",
            [Library.BitPosition.BottomStart] = "At the bottom, against the start edge (the left in left-to-right).",
            [Library.BitPosition.BottomEnd] = "At the bottom, against the end edge (the right in left-to-right).",
        });
    }

    public static ComponentSubEnum BitSize(string? description = null)
    {
        return Table<Library.BitSize>("size-enum", description ?? "Defines the sizes available in the bit BlazorUI.", new()
        {
            [Library.BitSize.Small] = "The small size.",
            [Library.BitSize.Medium] = "The medium size.",
            [Library.BitSize.Large] = "The large size.",
        });
    }

    public static ComponentSubEnum BitTimeFormat(string? description = null)
    {
        return Table<Library.BitTimeFormat>("time-format-enum", description ?? "Defines the clock the time picker shows its hours in.", new()
        {
            [Library.BitTimeFormat.TwentyFourHours] = "Show time pickers in 24 hours format.",
            [Library.BitTimeFormat.TwelveHours] = "Show time pickers in 12 hours format.",
        });
    }

    public static ComponentSubEnum BitVariant(string? description = null)
    {
        return Table<Library.BitVariant>("variant-enum", description ?? "Determines the variant of the content that controls the rendered style of the corresponding element(s).", new()
        {
            [Library.BitVariant.Fill] = "Fill styled variant.",
            [Library.BitVariant.Outline] = "Outline styled variant.",
            [Library.BitVariant.Text] = "Text styled variant.",
        });
    }

    /// <summary>
    /// Narrows a shared table down to the members a component actually supports, in the table's own
    /// order - BitPagination renders only the eight general colors of <c>BitColor</c>. A name that is not
    /// a member of the table throws, and so does naming none at all, so a typo, a renamed member or an
    /// empty list cannot quietly empty a page's table.
    /// </summary>
    public static ComponentSubEnum Only(this ComponentSubEnum subEnum, params string[] names)
    {
        if (names.Length == 0)
            throw new ArgumentException($"Name at least one member of {subEnum.Name} to keep.", nameof(names));

        var unknown = names.Except(subEnum.Items.Select(i => i.Name)).ToArray();

        if (unknown.Length > 0)
            throw new ArgumentException($"{string.Join(", ", unknown)} is not a member of {subEnum.Name}.", nameof(names));

        subEnum.Items = [.. subEnum.Items.Where(i => names.Contains(i.Name))];

        return subEnum;
    }

    /// <summary>
    /// Builds the table of <typeparamref name="TEnum"/>: one row per member, in the enum's own order and
    /// with its own value, each described by its entry in <paramref name="members"/>. A member with no
    /// entry throws, so a member added to the enum is a failing test rather than a silently missing row.
    /// </summary>
    private static ComponentSubEnum Table<TEnum>(string id, string description, Dictionary<TEnum, string> members) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();

        var undescribed = values.Where(v => members.ContainsKey(v) is false).ToArray();

        if (undescribed.Length > 0)
            throw new InvalidOperationException($"{string.Join(", ", undescribed)} of {typeof(TEnum).Name} has no description in its shared table.");

        return new()
        {
            Id = id,
            Name = typeof(TEnum).Name,
            Description = description,
            Items =
            [
                .. values.Select(v => new ComponentEnumItem
                {
                    Name = v.ToString(),
                    Value = Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                    Description = members[v],
                })
            ]
        };
    }
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Badge;

public partial class BitBadgeDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a ring around the badge in the color of the page behind it, so it stays legible over a busy child such as an avatar or an image."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the badge will apply to. When it is not set the badge renders standalone, in the normal flow of the page."
        },
        new()
        {
            Name = "Classes",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the badge.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Content",
            Type = "object?",
            DefaultValue = "null",
            Description = "Content you want inside the badge. A number is capped by Max and hidden by ShowZero when it is zero, a string is rendered as it is, and any other value is rendered through its ToString(). A badge given no content, no icon and no template at all is not rendered."
        },
        new()
        {
            Name = "ContentTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render inside the badge, in place of Content. A template is content of its own, so neither Max nor ShowZero reads it, and it is markup rather than words, so a Live badge showing one needs a Description before its live region has anything to announce."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text alternative of the badge for assistive technologies, for example \"5 unread messages\". It is rendered into the badge visible only to assistive technologies, and hides the visual content from them so the two are not announced twice."
        },
        new()
        {
            Name = "Dot",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reduces the size of the badge and hide any of its content."
        },
        new()
        {
            Name = "Hidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "The visibility of the badge. A hidden badge is removed from the DOM while its child content keeps rendering."
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL the badge navigates to, which also turns the badge into a link: an anchor that is focusable, offers the context menu and the middle click, and is announced as a link. While IsEnabled is false the href is dropped and the badge leaves the tab order."
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
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the badge out next to its child content in the normal flow of the page instead of over it. Overlap stops applying and only the side of Position is read: the Start and Left families put the badge before the child content, every other one after it."
        },
        new()
        {
            Name = "Live",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces the badge to assistive technologies whenever its content changes, by turning it into a polite live region. The region is kept on the page whether or not the badge itself is, so a counter that appears, changes and disappears is announced every time. It reads out the Description when there is one and the counter itself otherwise."
        },
        new()
        {
            Name = "Max",
            Type = "int?",
            DefaultValue = "null",
            Description = "Max value to display when content is a number. A content above it renders as the max followed by a plus sign, for example 99+, and the badge carries the figure it shortened as its tooltip unless a Title of its own says something better."
        },
        new()
        {
            Name = "OffsetX",
            Type = "string?",
            DefaultValue = "null",
            Description = "Moves the badge along the horizontal axis by the given CSS length, on top of its Position. A positive value moves the badge to the right in both directions of writing."
        },
        new()
        {
            Name = "OffsetY",
            Type = "string?",
            DefaultValue = "null",
            Description = "Moves the badge along the vertical axis by the given CSS length, on top of its Position. A positive value moves the badge down."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "The click event of the badge, which also turns the badge into a keyboard-operable button."
        },
        new()
        {
            Name = "Overlap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Overlaps the badge on top of the child content."
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "The position of the badge. The Left/Right positions are physical, while the Start/End ones follow the direction of writing.",
            LinkType = LinkType.Link,
            Href = "#position-enum"
        },
        new()
        {
            Name = "Pulse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders an expanding ring around the badge to report that something is in progress."
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The relationship between the current document and the one the Href of the badge leads to. With no value of its own, a badge opening in a new browsing context gets rel=\"noopener\" automatically.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum"
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the direction flow of the content of the badge, which puts the icon after the content."
        },
        new()
        {
            Name = "Shape",
            Type = "BitBadgeShape?",
            DefaultValue = "null",
            Description = "The corner shape of the badge.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "ShowZero",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders the badge when its content is the number zero. Turn it off for a counter that should disappear once it is emptied. Only a numeric Content counts as zero, and a string is rendered as it is. An icon or a ContentTemplate is content of its own, so it keeps the badge on the page and only the emptied number is taken off it."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of badge, Possible values: Small | Medium | Large",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "The browsing context the Href of the badge is opened in, for example _blank."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the badge. It is rendered on the badge itself rather than on the child content underneath it. A badge whose Max has capped its count already spells that count out on hover, so this is only needed when there is something better to say than the figure itself. A title is not a text alternative, so what a screen reader should hear belongs in Description."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the badge.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
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
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size badge.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size badge.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size badge.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "shape-enum",
            Name = "BitBadgeShape",
            Description = "Determines the corner shape of the BitBadge.",
            Items =
            [
                new()
                {
                    Name= "Circular",
                    Description="Fully rounded corners, so a counter reads as a circle and a longer label as a pill.",
                    Value="0",
                },
                new()
                {
                    Name= "Rounded",
                    Description="The corner radius the current theme gives to its controls.",
                    Value="1",
                },
                new()
                {
                    Name= "Square",
                    Description="Square corners with no radius at all.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "TopLeft",
                    Value = "0"
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2"
                },
                new()
                {
                    Name = "TopStart",
                    Value = "3"
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "5"
                },
                new()
                {
                    Name = "Center",
                    Value = "6"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "7"
                },
                new()
                {
                    Name = "CenterStart",
                    Value = "8"
                },
                new()
                {
                    Name = "CenterEnd",
                    Value = "9"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "10"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "11"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "12"
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "13"
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "14"
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "link-rels-enum",
            Name = "BitLinkRels",
            Description = "The rel attribute defines the relationship between a linked resource and the current document.",
            Items =
            [
                new()
                {
                    Name = "Alternate",
                    Value = "1",
                    Description = "Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)"
                },
                new()
                {
                    Name = "Author",
                    Value = "2",
                    Description = "Provides a link to the author of the document."
                },
                new()
                {
                    Name = "Bookmark",
                    Value = "4",
                    Description = "Permanent URL used for bookmarking."
                },
                new()
                {
                    Name = "External",
                    Value = "8",
                    Description = "Indicates that the referenced document is not part of the same site as the current document."
                },
                new()
                {
                    Name = "Help",
                    Value = "16",
                    Description = "Provides a link to a help document."
                },
                new()
                {
                    Name = "License",
                    Value = "32",
                    Description = "Provides a link to licensing information for the document."
                },
                new()
                {
                    Name = "Next",
                    Value = "64",
                    Description = "Provides a link to the next document in the series."
                },
                new()
                {
                    Name = "NoFollow",
                    Value = "128",
                    Description = @"Links to an unendorsed document, like a paid link. (""NoFollow"" is used by Google, to specify that the Google search spider should not follow that link)"
                },
                new()
                {
                    Name = "NoOpener",
                    Value = "256",
                    Description = "Requires that any browsing context created by following the hyperlink must not have an opener browsing context."
                },
                new()
                {
                    Name = "NoReferrer",
                    Value = "512",
                    Description = "Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink."
                },
                new()
                {
                    Name = "Prev",
                    Value = "1024",
                    Description = "The previous document in a selection."
                },
                new()
                {
                    Name = "Search",
                    Value = "2048",
                    Description = "Links to a search tool for the document."
                },
                new()
                {
                    Name = "Tag",
                    Value = "4096",
                    Description = "A tag (keyword) for the current document."
                },
                new()
                {
                    Name = "Me",
                    Value = "8192",
                    Description = "Indicates that the linked document represents the person who owns the current content. (used for identity verification)"
                },
                new()
                {
                    Name = "Opener",
                    Value = "16384",
                    Description = "Requires that any browsing context created by following the hyperlink keeps its opener browsing context. (reverses the implicit noopener modern browsers apply to _blank targets)"
                },
                new()
                {
                    Name = "PrivacyPolicy",
                    Value = "32768",
                    Description = "Links to the privacy policy that applies to the current document. (rendered as privacy-policy)"
                },
                new()
                {
                    Name = "Sponsored",
                    Value = "65536",
                    Description = "Marks the link as an advertisement or paid placement, so search engines do not count it as an organic endorsement."
                },
                new()
                {
                    Name = "TermsOfService",
                    Value = "131072",
                    Description = "Links to the terms of service that apply to the current document. (rendered as terms-of-service)"
                },
                new()
                {
                    Name = "Ugc",
                    Value = "262144",
                    Description = "Marks the link as user-generated content, like forum posts or comments, for search engines."
                }
            ]
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "badge-class-styles",
            Title = "BitBadgeClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitBadge."
               },
               new()
               {
                   Name = "BadgeWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge wrapper of the BitBadge."
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge of the BitBadge."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitBadge."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitBadge."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the visually hidden description of the BitBadge."
               },
               new()
               {
                   Name = "LiveRegion",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the visually hidden live region of the BitBadge, rendered while Live is on and the badge is not a button."
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



    private bool hidden;
    private int zeroCount;
    private int unread = 3;
    private int counter;
    private BitPosition badgePosition;
    private List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues(typeof(BitPosition))
        .Cast<BitPosition>()
        .Select(enumValue => new BitDropdownItem<BitPosition>
        {
            Value = enumValue,
            Text = enumValue.ToString()
        })
        .ToList();



    private readonly string example1RazorCode = @"
<BitBadge Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example2RazorCode = @"
<BitBadge Content=""84"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""84"" Variant=""BitVariant.Fill"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example3RazorCode = @"
<BitBadge Content=""9"" Shape=""BitBadgeShape.Circular"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""9"" Shape=""BitBadgeShape.Rounded"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""9"" Shape=""BitBadgeShape.Square"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Circular"" Color=""BitColor.Info"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Rounded"" Color=""BitColor.Info"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Square"" Color=""BitColor.Info"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example4RazorCode = @"
<BitBadge Dot Size=""BitSize.Small"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Size=""BitSize.Medium"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Size=""BitSize.Large"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Color=""BitColor.Success"" Description=""Online"">
    <BitIcon IconName=""@BitIconName.Contact"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example5RazorCode = @"
<BitBadge Max=""63"" Content=""60"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Max=""63"" Content=""100"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Max=""99"" Content=""12345L"" Title=""12345 unread messages"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example6RazorCode = @"
<BitBadge Content=""zeroCount"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""zeroCount"" ShowZero=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => zeroCount--"" IsEnabled=""@(zeroCount > 0)"">Remove one</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => zeroCount++"">Add one</BitButton>";
    private readonly string example6CsharpCode = @"
private int zeroCount;";

    private readonly string example7RazorCode = @"
<BitBadge Content=""@(""Text"")"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge IconName=""@BitIconName.Ringer"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""@(""Sent"")"" IconName=""@BitIconName.CheckMark"" Color=""BitColor.Success"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""@(""Sent"")"" IconName=""@BitIconName.CheckMark"" Color=""BitColor.Success"" Reversed>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Color=""BitColor.Error"">
    <ContentTemplate>
        <b>99</b><span style=""opacity:0.75"">%</span>
    </ContentTemplate>
    <ChildContent>
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </ChildContent>
</BitBadge>";

    private readonly string example8RazorCode = @"
<BitBadge Content=""63"" Position=""badgePosition"">
    <BitButton Variant=""BitVariant.Outline"">Position</BitButton>
</BitBadge>

<BitDropdown Items=""badgePositionList"" @bind-Value=""badgePosition"" Style=""width: 8rem;"" />";
    private readonly string example8CsharpCode = @"
private BitPosition badgePosition;

private List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues(typeof(BitPosition))
    .Cast<BitPosition>()
    .Select(enumValue => new BitDropdownItem<BitPosition>
    {
        Value = enumValue,
        Text = enumValue.ToString()
    })
    .ToList();";

    private readonly string example9RazorCode = @"
<BitBadge Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""63"" Overlap>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example10RazorCode = @"
<BitBadge Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""63"" OffsetX=""-0.5rem"" OffsetY=""0.5rem"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Position=""BitPosition.BottomEnd"" OffsetX=""-2px"" OffsetY=""-2px"" Color=""BitColor.Success"">
    <BitIcon IconName=""@BitIconName.Contact"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example11RazorCode = @"
<BitBadge Dot Color=""BitColor.Success"" Position=""BitPosition.BottomEnd"" Overlap Description=""Online"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>

<BitBadge Dot Bordered Color=""BitColor.Success"" Position=""BitPosition.BottomEnd"" Overlap Description=""Online"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>

<BitBadge Content=""8"" Bordered Overlap>
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>";

    private readonly string example12RazorCode = @"
<BitBadge Dot Pulse Color=""BitColor.Success"" Description=""Connected"">
    <BitIcon IconName=""@BitIconName.Streaming"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Pulse Color=""BitColor.Warning"" Position=""BitPosition.BottomEnd"" Overlap Description=""Syncing"">
    <BitIcon IconName=""@BitIconName.Cloud"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""3"" Pulse Color=""BitColor.Error"">
    <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example13RazorCode = @"
<BitBadge Content=""@(""Draft"")"" Color=""BitColor.Tertiary"" />
<BitBadge Content=""@(""Active"")"" Color=""BitColor.Success"" />
<BitBadge Content=""@(""Failed"")"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitBadge Content=""@(""Beta"")"" Color=""BitColor.Info"" IconName=""@BitIconName.TestBeaker"" Shape=""BitBadgeShape.Rounded"" />
<BitBadge Dot Color=""BitColor.Warning"" Description=""Degraded"" />";

    private readonly string example14RazorCode = @"
<BitBadge Inline Content=""24"">
    <BitText Typography=""BitTypography.Body1"">Inbox</BitText>
</BitBadge>

<BitBadge Inline Content=""3"" Color=""BitColor.Error"" Position=""BitPosition.CenterStart"">
    <BitText Typography=""BitTypography.Body1"">Alerts</BitText>
</BitBadge>

<BitBadge Inline Dot Color=""BitColor.Success"" Position=""BitPosition.CenterStart"" Description=""Operational"">
    <BitText Typography=""BitTypography.Body1"">Build server</BitText>
</BitBadge>

<BitBadge Inline Content=""@(""Beta"")"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Shape=""BitBadgeShape.Rounded"">
    <BitText Typography=""BitTypography.Body1"">Reports</BitText>
</BitBadge>";

    private readonly string example15RazorCode = @"
<BitToggle @bind-Value=""hidden"" Label=""Hide the badge"" />

<BitBadge Hidden=""hidden"" Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";
    private readonly string example15CsharpCode = @"
private bool hidden;";

    private readonly string example16RazorCode = @"
<BitBadge Content=""counter"" OnClick=""() => counter++"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""counter"" OnClick=""() => counter++"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";
    private readonly string example16CsharpCode = @"
private int counter;";

    private readonly string example17RazorCode = @"
<BitBadge Inline Content=""7"" Href=""#example17"" AriaLabel=""7 new items"">
    <BitText Typography=""BitTypography.Body1"">Inbox</BitText>
</BitBadge>

<BitBadge Content=""@(""Docs"")"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank"" />

<BitBadge Content=""@(""Source"")"" Color=""BitColor.Secondary"" IconName=""@BitIconName.OpenInNewWindow""
          Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" />

<BitBadge Content=""@(""Disabled"")"" Href=""https://bitplatform.dev"" IsEnabled=""false"" />";

    private readonly string example18RazorCode = @"
<BitBadge Content=""unread"" Description=""@($""{unread} unread messages"")"" Live>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Color=""BitColor.Success"" Description=""Online"">
    <BitIcon IconName=""@BitIconName.Contact"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dot Color=""BitColor.Error"" Href=""#example18"" AriaLabel=""Open the alerts you have not read"">
    <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => unread++"">Receive a message</BitButton>";
    private readonly string example18CsharpCode = @"
private int unread = 3;";

    private readonly string example19RazorCode = @"
<BitBadge Content=""84"" Color=""BitColor.Primary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Secondary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Tertiary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Info"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Success"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Warning"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.SevereWarning"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Error"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<div><b>Backgrounds, foregrounds & borders</b>:</div>

<BitBadge Content=""84"" Color=""BitColor.PrimaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.PrimaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<div><b>Disabled</b>:</div>

<BitBadge Content=""84"" Color=""BitColor.Primary"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Secondary"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Tertiary"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Info"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Success"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Warning"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Error"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitBadge Content=""4"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""63"" Icon=""@BitIconInfo.Fa(""solid bell"")"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitBadge Content=""3"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Icon=""@BitIconInfo.Bi(""gear-fill"")"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example21RazorCode = @"
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example22RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-class div {
        padding: 0.5rem;
        color: blueviolet;
    }

    .custom-root {
        margin-left: 2rem;
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-wrapper {
        padding: 1rem;
    }

    .custom-badge {
        border-end-end-radius: 0.5rem;
        border-start-end-radius: unset;
        border-end-start-radius: unset;
        border-start-start-radius: 0.5rem;
    }

    .custom-icon {
        color: dodgerblue;
    }

    .custom-content {
        font-style: italic;
    }
</style>

<BitBadge Content=""84"" Style=""color: dodgerblue;"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Class=""custom-class"" Variant=""BitVariant.Outline"">
    <div>Anchor</div>
</BitBadge>


<BitBadge Content=""84"" Style=""--bit-bdg-clr: rebeccapurple; --bit-bdg-clr-txt: white;"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Inline Content=""84"" Style=""--bit-bdg-gap: 1rem;"">
    <BitText Typography=""BitTypography.Body1"">Wider gap</BitText>
</BitBadge>


<BitBadge Content=""84"" IconName=""@BitIconName.Info""
          Styles=""@(new() { Root = ""color: tomato;"",
                            Badge = ""border-radius: unset;"",
                            Icon = ""color: tomato;"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" IconName=""@BitIconName.Info""
          Variant=""BitVariant.Outline""
          Classes=""@(new() { Root = ""custom-root"",
                             BadgeWrapper = ""custom-wrapper"",
                             Badge = ""custom-badge"",
                             Icon = ""custom-icon"",
                             Content = ""custom-content"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example23RazorCode = @"
<BitBadge Dir=""BitDir.Rtl"" Content=""63"" Position=""BitPosition.TopEnd"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dir=""BitDir.Rtl"" Content=""63"" Position=""BitPosition.TopStart"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dir=""BitDir.Rtl"" Content=""@(""جدید"")"" IconName=""@BitIconName.CheckMark"" Color=""BitColor.Success"" Position=""BitPosition.BottomStart"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Dir=""BitDir.Rtl"" Content=""@(""پیش‌نویس"")"" Color=""BitColor.Tertiary"" />";
}

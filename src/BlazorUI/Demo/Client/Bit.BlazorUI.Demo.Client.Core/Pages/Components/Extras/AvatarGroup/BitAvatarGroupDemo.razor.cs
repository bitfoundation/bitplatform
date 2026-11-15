namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AvatarGroup;

public partial class BitAvatarGroupDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAvatarGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the avatar group.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<BitAvatarGroupItem>",
            DefaultValue = "[]",
            Description = "The people of the group, in the order their avatars are drawn.",
            LinkType = LinkType.Link,
            Href = "#avatar-group-item",
        },
        new()
        {
            Name = "MaxDisplayed",
            Type = "int?",
            DefaultValue = "null",
            Description = "The number of avatars drawn before the rest are counted by the overflow coin (\"+N\") instead. By default every item is drawn.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitAvatarGroupItem>",
            DefaultValue = "",
            Description = "The callback for when an avatar is clicked, which makes every avatar a button.",
        },
        new()
        {
            Name = "OnOverflowClick",
            Type = "EventCallback<IReadOnlyList<BitAvatarGroupItem>>",
            DefaultValue = "",
            Description = "The callback for when the overflow coin is clicked, given the items it stands in for. It makes the overflow coin a button.",
        },
        new()
        {
            Name = "OverflowAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the overflow coin to the screen readers, as a composite format string: {0} is the number of the people it hides and {1} their names. Default: \"{0} more: {1}\".",
        },
        new()
        {
            Name = "Size",
            Type = "BitPersonaSize",
            DefaultValue = "BitPersonaSize.Size32",
            Description = "The size of the avatars, from Size24 to Size120. Size8, which has no coin, is drawn as Size24.",
            LinkType = LinkType.Link,
            Href = "/components/persona#persona-size-enum",
        },
        new()
        {
            Name = "Spread",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the avatars out side by side with a gap between them, instead of overlapping each other.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAvatarGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the avatar group.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "avatar-group-item",
            Title = "BitAvatarGroupItem",
            Parameters =
            [
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the avatar of the item." },
                new() { Name = "CoinColor", Type = "BitColor?", DefaultValue = "null", Description = "The color of the coin of the item, which takes the place of the one picked for it from its name.", LinkType = LinkType.Link, Href = "#color-enum" },
                new() { Name = "Data", Type = "object?", DefaultValue = "null", Description = "Any data the app wants to keep with the item." },
                new() { Name = "Href", Type = "string?", DefaultValue = "null", Description = "The URL the avatar of the item links to, which makes it a link." },
                new() { Name = "ImageUrl", Type = "string?", DefaultValue = "null", Description = "The URL of the picture of the item. Without one, or while it loads, the coin shows the initials of the Text." },
                new() { Name = "Initials", Type = "string?", DefaultValue = "null", Description = "The initials the coin shows in place of the ones derived from the Text." },
                new() { Name = "IsDisabled", Type = "bool", DefaultValue = "false", Description = "Whether the avatar of the item is disabled, which is never clickable nor a link." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the item." },
                new() { Name = "Presence", Type = "BitPersonaPresence", DefaultValue = "BitPersonaPresence.None", Description = "The presence status of the item, drawn as a dot on the corner of its coin.", LinkType = LinkType.Link, Href = "/components/persona#presence-enum" },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom CSS styles of the avatar of the item." },
                new() { Name = "Target", Type = "string?", DefaultValue = "null", Description = "The target of the link of the item (_blank, _self, ...), for an item with an Href." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "The name of the item, which names its avatar to the screen readers and gives its coin its initials." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The tooltip of the avatar of the item. Default: the Text." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitAvatarGroupClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAvatarGroup." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each avatar of the BitAvatarGroup." },
                new() { Name = "Overflow", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the overflow coin of the BitAvatarGroup, the one that counts the hidden avatars." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AvatarGroup-overlap", DefaultValue = "0.2", Description = "Share of a coin each avatar covers of the one before it." },
        new() { Name = "--bit-AvatarGroup-gap", DefaultValue = "spacing(0.5)", Description = "Room between the avatars of a Spread group." },
        new() { Name = "--bit-AvatarGroup-ring-color", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Ring that cuts overlapping coins apart, the color of the surface the group sits on." },
        new() { Name = "--bit-AvatarGroup-ring-width", DefaultValue = "2px", Description = "Width of that ring." },
        new() { Name = "--bit-AvatarGroup-overflow-background", DefaultValue = "var(--bit-clr-bg-ter)", Description = "Fill of the overflow coin." },
        new() { Name = "--bit-AvatarGroup-overflow-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Count on the overflow coin." },
        new() { Name = "--bit-AvatarGroup-overflow-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Edge of the overflow coin, which keeps it a coin on any surface." },
    ];



    private const string PersonaImage = "/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png";

    private string? clickedPerson;
    private string? hiddenPeople;

    private void HandleOverflowClick(IReadOnlyList<BitAvatarGroupItem> hidden)
    {
        hiddenPeople = string.Join(", ", hidden.Select(p => p.Text));
    }

    private static readonly List<BitAvatarGroupItem> people =
    [
        new() { Key = "annie", Text = "Annie Lindqvist", ImageUrl = PersonaImage },
        new() { Key = "aaron", Text = "Aaron Reid" },
        new() { Key = "alex", Text = "Alex Lundberg" },
        new() { Key = "roko", Text = "Roko Kolar" },
        new() { Key = "christian", Text = "Christian Bergqvist" },
        new() { Key = "valentina", Text = "Valentina Lovric" },
        new() { Key = "maor", Text = "Maor Sharett" },
    ];

    private static readonly List<BitAvatarGroupItem> presentPeople =
    [
        new() { Text = "Annie Lindqvist", ImageUrl = PersonaImage, Presence = BitPersonaPresence.Online },
        new() { Text = "Aaron Reid", Presence = BitPersonaPresence.Away },
        new() { Text = "Alex Lundberg", Presence = BitPersonaPresence.Busy },
        new() { Text = "Roko Kolar", Presence = BitPersonaPresence.Offline },
    ];

    private static readonly List<BitAvatarGroupItem> linkedPeople =
    [
        new() { Text = "Annie Lindqvist", ImageUrl = PersonaImage, Href = "/components/persona" },
        new() { Text = "Aaron Reid", Href = "/components/persona" },
        new() { Text = "Alex Lundberg", Href = "/components/persona", Target = "_blank" },
    ];

    private static readonly List<BitAvatarGroupItem> partlyDisabledPeople =
    [
        new() { Text = "Annie Lindqvist", ImageUrl = PersonaImage },
        new() { Text = "Aaron Reid", IsDisabled = true },
        new() { Text = "Alex Lundberg" },
    ];

    private static readonly List<BitAvatarGroupItem> coloredPeople =
    [
        new() { Text = "Aaron Reid", CoinColor = BitColor.Primary },
        new() { Text = "Alex Lundberg", CoinColor = BitColor.Success },
        new() { Text = "Roko Kolar", CoinColor = BitColor.Warning },
        new() { Text = "Christian Bergqvist", CoinColor = BitColor.Error },
        new() { Text = "Valentina Lovric" },
    ];

    private static readonly List<BitAvatarGroupItem> rtlPeople =
    [
        new() { Text = "سارا نوری", ImageUrl = PersonaImage },
        new() { Text = "مریم احمدی" },
        new() { Text = "علی رضایی" },
        new() { Text = "زهرا کریمی" },
        new() { Text = "حسین محمدی" },
    ];
}

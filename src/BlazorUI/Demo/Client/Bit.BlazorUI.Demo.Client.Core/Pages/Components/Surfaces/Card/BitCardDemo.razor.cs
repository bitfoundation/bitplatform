namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Card;

public partial class BitCardDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered at the trailing edge of the header of the card, for whatever acts on the card as a whole. It is raised above the stretched link of a card that has an Href, so the controls in it stay clickable.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the card.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the card.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the card. It renders on its own inside the padding of the card, unless the card also has a cover, a header or a footer - then it renders as the body between them.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the card.",
            LinkType = LinkType.Link,
            Href = "#card-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the card. Setting it paints the card in one of the roles of the theme instead of in the neutral surface colors, in the way the Variant asks for.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Cover",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The full-bleed media at the head of the card, rendered outside the padding and clipped to the corner of the card. It takes precedence over ImageUrl.",
        },
        new()
        {
            Name = "CoverWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the cover of a horizontal card. The default is a third of the width of the card.",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "The download attribute of the stretched link of the card.",
        },
        new()
        {
            Name = "Elevation",
            Type = "int?",
            DefaultValue = "null",
            Description = "Sets the shadow elevation level of the card (0-24). Maps to theme shadow variables (--bit-shd-1 to --bit-shd-24), with 0 being a card with no shadow at all.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered under the body of the card, for the actions a reader is meant to take. It is raised above the stretched link of a card that has an Href, so the controls in it stay clickable.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card height 100% of its parent container.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width and height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width 100% of its parent container.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template rendered as the header of the card, in place of the icon, the title and the subtitle. Actions still renders beside it.",
        },
        new()
        {
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "The heading level the title of the card reports itself as (1-6). Leaving it unset keeps the title plain text. Values outside 1-6 are ignored.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the height of the card explicitly.",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the cover of the card beside its content instead of above it.",
        },
        new()
        {
            Name = "Hoverable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lifts the card while the pointer is over it. A clickable card or a linked one lifts on its own.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL the whole card leads to, rendered as an anchor stretched over the surface of the card. The anchor is named by the AriaLabel, or failing that by the Title or the Subtitle of the card.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The leading icon of the header of the card.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the leading icon of the header of the card.",
        },
        new()
        {
            Name = "ImageAlt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The alternate text of the cover image of the card. With no value it renders an empty alt, so the picture is skipped by assistive technologies.",
        },
        new()
        {
            Name = "ImageHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the cover image of the card. The image is cropped to fill it rather than stretched.",
        },
        new()
        {
            Name = "ImageUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL of the cover image at the head of the card.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stands the body of the card in with a placeholder while its content is being fetched, and reports the card as busy. The header keeps rendering.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom placeholder rendered in the body of the card while Loading is set.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the maximum height of the card.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the maximum width of the card.",
        },
        new()
        {
            Name = "MinHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the minimum height of the card.",
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the minimum width of the card.",
        },
        new()
        {
            Name = "NoPadding",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default padding of the card.",
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default shadow around the card.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback for when the card is clicked. Setting it turns the card into a button: it takes focus, it answers Enter and Space, and it reports itself as a control to assistive technologies.",
        },
        new()
        {
            Name = "Outlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the card with no shadow and a primary border. An explicit Border still wins over the border color it asks for.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The rel attribute of the stretched link of the card. With no value of its own, a card whose Target is _blank gets noopener.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum",
        },
        new()
        {
            Name = "Selected",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the card is currently selected. Binding it turns the card into a toggle: clicking it flips the value and the card reports its state through aria-pressed.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the card, which sets its padding, the gap between its parts and the type of its header.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Square",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border-radius from the card, rendering it with sharp corners.",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the propagation of the click event of the card.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the card.",
            LinkType = LinkType.Link,
            Href = "#card-class-styles",
        },
        new()
        {
            Name = "Subtitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The second line of the header of the card, under the title.",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "The target attribute of the stretched link of the card.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the card, rendered as the first line of its header. It also names the stretched link of a card that has an Href.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the card, which only takes effect while a Color is set. The default is Fill.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the width of the card explicitly.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the card, which only lands anywhere while the card is a control or has a tab index of its own."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "card-class-styles",
            Title = "BitCardClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCard."
                },
                new()
                {
                    Name = "Link",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the stretched link that covers a card with an Href."
                },
                new()
                {
                    Name = "Cover",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the full-bleed media area at the head of the BitCard."
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image rendered from the ImageUrl of the BitCard."
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element that holds the header, the body and the footer of the BitCard."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitCard."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the leading icon of the header of the BitCard."
                },
                new()
                {
                    Name = "HeaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element that holds the title and the subtitle of the BitCard."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitCard."
                },
                new()
                {
                    Name = "Subtitle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the subtitle of the BitCard."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions rendered at the trailing edge of the header of the BitCard."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitCard, which is what the ChildContent renders into."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitCard."
                },
                new()
                {
                    Name = "Selected",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCard while it is selected."
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
                    Name = "Primary",
                    Description = "Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
                },
                new()
                {
                    Name = "PrimaryBackground",
                    Description = "Primary background color.",
                    Value = "8",
                },
                new()
                {
                    Name = "SecondaryBackground",
                    Description = "Secondary background color.",
                    Value = "9",
                },
                new()
                {
                    Name = "TertiaryBackground",
                    Description = "Tertiary background color.",
                    Value = "10",
                },
                new()
                {
                    Name = "PrimaryForeground",
                    Description = "Primary foreground color.",
                    Value = "11",
                },
                new()
                {
                    Name = "SecondaryForeground",
                    Description = "Secondary foreground color.",
                    Value = "12",
                },
                new()
                {
                    Name = "TertiaryForeground",
                    Description = "Tertiary foreground color.",
                    Value = "13",
                },
                new()
                {
                    Name = "PrimaryBorder",
                    Description = "Primary border color.",
                    Value = "14",
                },
                new()
                {
                    Name = "SecondaryBorder",
                    Description = "Secondary border color.",
                    Value = "15",
                },
                new()
                {
                    Name = "TertiaryBorder",
                    Description = "Tertiary border color.",
                    Value = "16",
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
                    Name = "Small",
                    Description = "The small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
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
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
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
                }
            ]
        },
    ];



    private int size = 0;
    private int clickCount;
    private bool isPinned;
    private bool isLoading = true;
    private double elevation = 4;
    private double cardWidth = 300;
    private double cardHeight = 200;
    private bool isProSelected;
    private bool isBasicSelected = true;
    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;



    private readonly string example1RazorCode = @"
<BitCard>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example2RazorCode = @"
<BitCard Title=""bit BlazorUI"" Subtitle=""Native Blazor components"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">
        bit BlazorUI components are native, easy-to-customize, and ...
    </BitText>
</BitCard>

<BitCard Title=""Deployment"" Subtitle=""Succeeded 2 minutes ago"" HeadingLevel=""3""
         IconName=""@BitIconName.Rocket"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">
        A title given a heading level reports itself as one to assistive technologies.
    </BitText>
</BitCard>

<BitCard Width=""18rem"">
    <HeaderTemplate>
        <BitPersona Size=""BitPersonaSize.Size32""
                    PrimaryText=""Ada Lovelace""
                    SecondaryText=""Author""
                    ImageUrl=""/images/persona/persona-female.png"" />
    </HeaderTemplate>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            A HeaderTemplate takes the place of the icon, the title and the subtitle.
        </BitText>
    </ChildContent>
</BitCard>";

    private readonly string example3RazorCode = @"
<BitCard Title=""Mount Rainier"" Subtitle=""Washington, USA"" Width=""18rem""
         ImageUrl=""/images/carousel/img1.jpg"" ImageHeight=""9rem"">
    <BitText Typography=""BitTypography.Body2"">
        A cover runs edge to edge and is clipped to the corner of the card.
    </BitText>
</BitCard>

<BitCard Title=""Custom cover"" Subtitle=""Any markup you like"" Width=""18rem"">
    <Cover>
        <div class=""example-cover"">bit BlazorUI</div>
    </Cover>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            The Cover template takes the place of the image.
        </BitText>
    </ChildContent>
</BitCard>";

    private readonly string example4RazorCode = @"
<BitCard Title=""Weekly report"" Subtitle=""Updated an hour ago"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.More"" Title=""More"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            Actions sit beside the title; the footer sits under the body.
        </BitText>
    </ChildContent>
    <Footer>
        <BitButton Variant=""BitVariant.Fill"" Size=""BitSize.Small"">Open</BitButton>
        <BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Share</BitButton>
    </Footer>
</BitCard>";

    private readonly string example5RazorCode = @"
<BitCard Horizontal Width=""26rem""
         Title=""Mount Rainier"" Subtitle=""Washington, USA""
         ImageUrl=""/images/carousel/img2.jpg"">
    <BitText Typography=""BitTypography.Body2"">
        A horizontal card is a thumbnail and a summary on one row.
    </BitText>
</BitCard>

<BitCard Horizontal Width=""26rem"" CoverWidth=""8rem""
         Title=""Mount Rainier"" Subtitle=""A narrower cover""
         ImageUrl=""/images/carousel/img3.jpg"">
    <BitText Typography=""BitTypography.Body2"">
        CoverWidth pins the thumbnail to a fixed width.
    </BitText>
</BitCard>";

    private readonly string example6RazorCode = @"
<BitCard OnClick=""() => clickCount++"" Title=""Clickable card"" Width=""18rem"" AriaLabel=""Clickable card"">
    <BitText Typography=""BitTypography.Body2"">Clicked @clickCount times.</BitText>
</BitCard>

<BitCard OnClick=""() => clickCount++"" Title=""Disabled card"" Width=""18rem"" IsEnabled=""false"">
    <BitText Typography=""BitTypography.Body2"">This one answers nothing.</BitText>
</BitCard>";
    private readonly string example6CsharpCode = @"
private int clickCount;";

    private readonly string example7RazorCode = @"
<BitCard Href=""https://blazorui.bitplatform.dev"" Target=""_blank""
         Title=""bit BlazorUI"" Subtitle=""blazorui.bitplatform.dev""
         IconName=""@BitIconName.Globe"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Pinned"" Title=""Pin""
                   OnClick=""() => isPinned = !isPinned"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            The whole surface is the link; the pin button above it is not. Pinned: @isPinned
        </BitText>
    </ChildContent>
    <Footer>
        <BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"">GitHub</BitLink>
    </Footer>
</BitCard>";

    private readonly string example8RazorCode = @"
<BitCard @bind-Selected=""isBasicSelected"" Title=""Basic"" Subtitle=""Everything you need to start"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Selected: @isBasicSelected</BitText>
</BitCard>

<BitCard @bind-Selected=""isProSelected"" Title=""Pro"" Subtitle=""For teams that ship"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Selected: @isProSelected</BitText>
</BitCard>";
    private readonly string example8CsharpCode = @"
private bool isProSelected;
private bool isBasicSelected = true;";

    private readonly string example9RazorCode = @"
<BitCard Hoverable Title=""Hover me"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">The shadow answers the pointer.</BitText>
</BitCard>";

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""isLoading"" Label=""Loading"" />

<BitCard Loading=""isLoading"" Title=""Weekly report"" Subtitle=""Updated an hour ago"" Width=""20rem"">
    <BitText Typography=""BitTypography.Body2"">
        Once it is loaded, this is what the card had to say.
    </BitText>
</BitCard>

<BitCard Loading=""isLoading"" Title=""Custom placeholder"" Width=""20rem"">
    <LoadingTemplate>
        <BitShimmer Width=""100%"" Height=""4rem"" />
    </LoadingTemplate>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            And this one brought its own placeholder.
        </BitText>
    </ChildContent>
</BitCard>";
    private readonly string example10CsharpCode = @"
private bool isLoading = true;";

    private readonly string example11RazorCode = @"
<BitSlider @bind-Value=""elevation"" Min=""0"" Max=""24"" Step=""1"" Label=""Elevation"" />

<div style=""padding:5rem"">
    <BitCard Elevation=""(int)elevation"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example11CsharpCode = @"
private double elevation = 4;";

    private readonly string example12RazorCode = @"
<BitCard NoShadow>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example13RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray"">
    <BitCard Background=""backgroundColorKind"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example13CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;";

    private readonly string example14RazorCode = @"
<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitCard Border=""borderColorKind"">
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";
    private readonly string example14CsharpCode = @"
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example15RazorCode = @"
<BitCard Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example16RazorCode = @"
<BitCard Square Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example17RazorCode = @"
<BitCard NoPadding Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example18RazorCode = @"
<BitSlider @bind-Value=""cardWidth"" Min=""100"" Max=""600"" Step=""10"" Label=""Width (px)"" />
<BitSlider @bind-Value=""cardHeight"" Min=""100"" Max=""400"" Step=""10"" Label=""Height (px)"" />

<BitCard Width=""@($""{(int)cardWidth}px"")"" Height=""@($""{(int)cardHeight}px"")"" Outlined Style=""overflow:hidden"">
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>

<BitCard MinWidth=""12rem"" MaxWidth=""24rem"" Outlined>
    <BitText Typography=""BitTypography.Body2"">
        This one is bounded rather than sized: it never shrinks below 12rem and never grows past 24rem.
    </BitText>
</BitCard>";
    private readonly string example18CsharpCode = @"
private double cardWidth = 300;
private double cardHeight = 200;";

    private readonly string example19RazorCode = @"
<BitChoiceGroup @bind-Value=""size"" Horizontal
                TItem=""BitChoiceGroupOption<int>"" TValue=""int"">
    <BitChoiceGroupOption Text=""FullSize"" Value=""0"" />
    <BitChoiceGroupOption Text=""FullWidth"" Value=""1"" />
    <BitChoiceGroupOption Text=""FullHeight"" Value=""2"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray;height:500px"">
    <BitCard FullSize=""size == 0"" FullWidth=""size == 1"" FullHeight=""size == 2"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example19CsharpCode = @"
private int size = 0;";

    private readonly string example20RazorCode = @"
<BitCard AriaLabel=""Release notes"" Title=""Release notes"" HeadingLevel=""3"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">
        A named card is a group, and its title is a level-3 heading.
    </BitText>
</BitCard>

<BitCard Loading Title=""Still loading"" Width=""18rem"" />

<BitCard OnClick=""() => { }"" IsEnabled=""false"" Title=""Disabled control"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">
        Out of the tab order, off the pointer, marked disabled.
    </BitText>
</BitCard>";

    private readonly string example21RazorCode = @"
<BitCard Color=""BitColor.Primary"" Title=""Primary"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Primary"" Title=""Primary"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Primary"" Title=""Primary"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Secondary"" Title=""Secondary"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Secondary"" Title=""Secondary"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Secondary"" Title=""Secondary"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Tertiary"" Title=""Tertiary"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Tertiary"" Title=""Tertiary"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Tertiary"" Title=""Tertiary"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Info"" Title=""Info"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Info"" Title=""Info"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Info"" Title=""Info"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Success"" Title=""Success"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Success"" Title=""Success"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Success"" Title=""Success"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Warning"" Title=""Warning"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Warning"" Title=""Warning"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Warning"" Title=""Warning"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.SevereWarning"" Title=""SevereWarning"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.SevereWarning"" Title=""SevereWarning"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.SevereWarning"" Title=""SevereWarning"" Variant=""BitVariant.Text"" Width=""12rem"" />

<BitCard Color=""BitColor.Error"" Title=""Error"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard Color=""BitColor.Error"" Title=""Error"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard Color=""BitColor.Error"" Title=""Error"" Variant=""BitVariant.Text"" Width=""12rem"" />

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitCard Color=""BitColor.PrimaryBackground"" Title=""PrimaryBackground"" Variant=""BitVariant.Fill"" Width=""14rem"" />
    <BitCard Color=""BitColor.SecondaryBackground"" Title=""SecondaryBackground"" Variant=""BitVariant.Fill"" Width=""14rem"" />
    <BitCard Color=""BitColor.TertiaryBackground"" Title=""TertiaryBackground"" Variant=""BitVariant.Fill"" Width=""14rem"" />
</div>

<BitCard Color=""BitColor.PrimaryForeground"" Title=""PrimaryForeground"" Variant=""BitVariant.Fill"" Width=""14rem"" />
<BitCard Color=""BitColor.SecondaryForeground"" Title=""SecondaryForeground"" Variant=""BitVariant.Fill"" Width=""14rem"" />
<BitCard Color=""BitColor.TertiaryForeground"" Title=""TertiaryForeground"" Variant=""BitVariant.Fill"" Width=""14rem"" />

<BitCard Color=""BitColor.PrimaryBorder"" Title=""PrimaryBorder"" Variant=""BitVariant.Outline"" Width=""14rem"" />
<BitCard Color=""BitColor.SecondaryBorder"" Title=""SecondaryBorder"" Variant=""BitVariant.Outline"" Width=""14rem"" />
<BitCard Color=""BitColor.TertiaryBorder"" Title=""TertiaryBorder"" Variant=""BitVariant.Outline"" Width=""14rem"" />

<BitCard IsEnabled=""false"" Color=""BitColor.Primary"" Title=""Primary"" Variant=""BitVariant.Fill"" Width=""12rem"" />
<BitCard IsEnabled=""false"" Color=""BitColor.Success"" Title=""Success"" Variant=""BitVariant.Outline"" Width=""12rem"" />
<BitCard IsEnabled=""false"" Color=""BitColor.Error"" Title=""Error"" Variant=""BitVariant.Text"" Width=""12rem"" />";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCard Icon=""@(""fa-solid fa-house"")"" Title=""House"" Subtitle=""fa-solid fa-house"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Fa(""brands github"")"" Title=""GitHub"" Subtitle=""fa-brands fa-github"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Fa(""solid rocket"")"" Title=""Rocket"" Subtitle=""fa-solid fa-rocket"" Width=""16rem"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCard Icon=""@(""bi bi-house-fill"")"" Title=""House"" Subtitle=""bi bi-house-fill"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Bi(""github"")"" Title=""GitHub"" Subtitle=""bi bi-github"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Bi(""gear-fill"")"" Title=""Gear"" Subtitle=""bi bi-gear-fill"" Width=""16rem"" />";

    private readonly string example23RazorCode = @"
<BitCard Size=""BitSize.Small"" Title=""Small"" Subtitle=""A tight card"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Small padding, small type.</BitText>
</BitCard>

<BitCard Size=""BitSize.Medium"" Title=""Medium"" Subtitle=""The default"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Medium padding, medium type.</BitText>
</BitCard>

<BitCard Size=""BitSize.Large"" Title=""Large"" Subtitle=""A roomy card"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Large padding, large type.</BitText>
</BitCard>";

    private readonly string example24RazorCode = @"
<BitCard Style=""border: 2px solid mediumpurple; box-shadow: mediumpurple 0 0 0.5rem;"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Styled card</BitText>
</BitCard>

<BitCard Class=""custom-class"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Classed card</BitText>
</BitCard>


<BitCard Title=""Styles"" Subtitle=""Per-part inline styles"" IconName=""@BitIconName.Color"" Width=""18rem""
         Styles=""@(new() { Root = ""border: 1px solid darkcyan"",
                           Header = ""border-bottom: 1px solid darkcyan; padding-bottom: 0.5rem"",
                           Title = ""color: darkcyan"",
                           Icon = ""color: darkcyan"" })"">
    <BitText Typography=""BitTypography.Body2"">Every part can be styled on its own.</BitText>
</BitCard>

<BitCard Title=""Classes"" Subtitle=""Per-part CSS classes"" IconName=""@BitIconName.Color"" Width=""18rem""
         Classes=""@(new() { Root = ""custom-root"", Title = ""custom-title"", Icon = ""custom-icon"" })"">
    <BitText Typography=""BitTypography.Body2"">And every part can take a class.</BitText>
</BitCard>";

    private readonly string example25RazorCode = @"
<BitCard Dir=""BitDir.Rtl"" Title=""کارت"" Subtitle=""یک زیرعنوان"" IconName=""@BitIconName.Album"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.More"" Title=""بیشتر"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">
            بیت بلیزور یو آی، کامپوننت‌های بومی، قابل تنظیم و ...
        </BitText>
    </ChildContent>
    <Footer>
        <BitButton Variant=""BitVariant.Fill"" Size=""BitSize.Small"">باز کردن</BitButton>
    </Footer>
</BitCard>";
}

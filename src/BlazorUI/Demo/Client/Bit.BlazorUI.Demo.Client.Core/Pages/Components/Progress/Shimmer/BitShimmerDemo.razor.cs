namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Shimmer;

public partial class BitShimmerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The background color of the container of the shimmer.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content that will be shown when the Loaded parameter changes to true."
        },
        new()
        {
            Name = "Circle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the shimmer as circle instead of a rectangle."
        },
        new()
        {
            Name = "Classes",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the animated part of the shimmer.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent."
        },
        new()
        {
            Name = "Delay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation delay value in ms.",
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation duration value in ms.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer height value."
        },
        new()
        {
            Name = "Loaded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Controls when the shimmer is swapped with actual data through an animated transition."
        },
        new()
        {
            Name = "Pulse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Changes the animation type of the shimmer to pulse.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to replace the default shimmer container and animation."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer width value."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitShimmerClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitShimmer."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitShimmer."
               },
               new()
               {
                   Name = "ShimmerWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the shimmer wrapper of the BitShimmer."
               },
               new()
               {
                   Name = "Shimmer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the shimmer of the BitShimmer."
               },
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
    ];



    private bool isDataLoaded;

    private bool isContentLoaded;



    private readonly string example1RazorCode = @"
<BitShimmer />";

    private readonly string example2RazorCode = @"
<BitShimmer Height=""5rem"" />
<BitShimmer Width=""10rem"" />";

    private readonly string example3RazorCode = @"
<BitShimmer Circle Height=""3rem"" />
<BitShimmer Circle Width=""4rem"" />";

    private readonly string example4RazorCode = @"
<BitShimmer Height=""4rem"" Duration=""5000"" Delay=""1000"" />

<BitStack Horizontal>
    <BitShimmer Pulse Circle Height=""4rem"" Duration=""3000"" Delay=""1000"" />
    <BitShimmer Pulse Height=""4rem"" Width=""100%"" Duration=""3000"" Delay=""1000"" />
</BitStack>";

    private readonly string example5RazorCode = @"
<BitShimmer Loaded=""@isDataLoaded"" AriaLabel=""Loading content"" Height=""1.5rem"">
    Content loaded successfully.
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isDataLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example5CsharpCode = @"
private bool isDataLoaded;";

    private readonly string example6RazorCode = @"
<BitShimmer Loaded=""@isContentLoaded"" AriaLabel=""Loading content"" Width=""15rem"" Height=""unset"">
    <Content>
        <BitImage Height=""8rem"" Alt=""bit logo""
                  Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
        <br />
        <BitPersona PrimaryText=""Annie Lindqvist""
                    SecondaryText=""Software Engineer""
                    Size=""@BitPersonaSize.Size56""
                    Presence=""@BitPersonaPresence.Online""
                    ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
    </Content>
    <Template>
        <BitShimmer Height=""8rem"" />
        <br />
        <BitStack Horizontal>
            <BitShimmer Circle Height=""3.5rem"" />
            <BitStack>
                <BitShimmer Height=""1.25rem"" Width=""8.5rem"" />
                <BitShimmer Height=""0.75rem"" Width=""7rem"" />
            </BitStack>
        </BitStack>
    </Template>
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isContentLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example6CsharpCode = @"
private bool isContentLoaded;";

    private readonly string example7RazorCode = @"
<BitShimmer Height=""1rem"" Color=""BitColor.Primary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Secondary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Tertiary"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Info"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Success"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Warning"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SevereWarning"" />
<BitShimmer Height=""1rem"" Color=""BitColor.Error"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""1rem"" Color=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""1rem"" Color=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""1rem"" Color=""BitColor.TertiaryBorder"" />";

    private readonly string example8RazorCode = @"
<BitShimmer Height=""2rem"" Background=""BitColor.Primary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Secondary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Tertiary"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Info"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Success"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Warning"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SevereWarning"" />
<BitShimmer Height=""2rem"" Background=""BitColor.Error"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""2rem"" Background=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""2rem"" Background=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""2rem"" Background=""BitColor.TertiaryBorder"" />";

    private readonly string example9RazorCode = @"
<style>
    .custom-class {
        box-shadow: aqua 0 0 1rem 0.5rem;
    }

    .custom-root {
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-shimmer {
        background: linear-gradient(90deg, transparent, darkred, transparent);
    }

    .custom-wrapper {
        border: solid tomato;
        border-radius: 0.5rem;
    }
</style>


<BitShimmer Height=""2.7rem"" Style=""border:2px solid gray"" />
<BitShimmer Height=""2.7rem"" Class=""custom-class"" />

<BitShimmer Height=""2.7rem"" Styles=""@(new() { Shimmer=""background-color: darkgoldenrod;"",
                                                ShimmerWrapper = ""background-color: darkgoldenrod;"" })"" />
<BitShimmer Height=""2.7rem"" Classes=""@(new() { Root = ""custom-root"",
                                                 Shimmer=""custom-shimmer"",
                                                 ShimmerWrapper = ""custom-wrapper"" })"" />";
}

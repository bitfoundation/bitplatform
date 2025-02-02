namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Shimmer;

public partial class BitShimmerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnimationDelay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation delay value in ms.",
        },
        new()
        {
            Name = "AnimationDuration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The animation duration value in ms.",
        },
        new()
        {
            Name = "BackgroundColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the animated part of the shimmer.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
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
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the shimmer will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#shimmer-class-styles"
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
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer height value."
        },
        new()
        {
            Name = "IsDataLoaded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Controls when the shimmer is swapped with actual data through an animated transition."
        },
        new()
        {
            Name = "Circle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Changes the shape of the shimmer to circle."
        },
        new()
        {
            Name = "ShimmerTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of the shimmer."
        },
        new()
        {
            Name = "Styles",
            Type = "BitShimmerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitShimmer.",
            LinkType = LinkType.Link,
            Href = "#shimmer-class-styles"
        },
        new()
        {
            Name = "WaveColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the animated part of the shimmer.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
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
            Id = "shimmer-class-styles",
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
<BitShimmer Height=""1.5rem"" />";

    private readonly string example2RazorCode = @"
<BitShimmer Height=""3rem"" />
<BitShimmer Circle Height=""3rem"" />";

    private readonly string example3RazorCode = @"
<BitShimmer Height=""4rem"" AnimationDuration=""5000"" AnimationDelay=""1000"" />

<BitStack Horizontal>
    <BitShimmer Pulse Circle Height=""4rem"" AnimationDuration=""3000"" AnimationDelay=""1000"" />
    <BitShimmer Pulse Height=""4rem"" Width=""100%"" AnimationDuration=""3000"" AnimationDelay=""1000"" />
</BitStack>";

    private readonly string example4RazorCode = @"
<BitShimmer IsDataLoaded=""@isDataLoaded"" AriaLabel=""Loading content"" Height=""1.5rem"">
    Content loaded successfully.
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isDataLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example4CsharpCode = @"
private bool isDataLoaded;";

    private readonly string example5RazorCode = @"
<BitShimmer IsDataLoaded=""@isContentLoaded"" AriaLabel=""Loading content"" Width=""15.1rem"">
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
    <ShimmerTemplate>
        <BitShimmer Height=""8rem"" />
        <br />
        <BitStack Horizontal>
            <BitShimmer Circle Height=""3.5rem"" />
            <BitStack>
                <BitShimmer Height=""1.25rem"" Width=""8.5rem"" />
                <BitShimmer Height=""0.75rem"" Width=""7rem"" />
            </BitStack>
        </BitStack>
    </ShimmerTemplate>
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isContentLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example5CsharpCode = @"
private bool isContentLoaded;";

    private readonly string example6RazorCode = @"
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Primary"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Secondary"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Tertiary"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Info"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Success"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Warning"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.SevereWarning"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.Error"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""1rem"" WaveColor=""BitColor.TertiaryBorder"" />";

    private readonly string example7RazorCode = @"
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Primary"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Secondary"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Tertiary"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Info"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Success"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Warning"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.SevereWarning"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.Error"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.PrimaryBackground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.SecondaryBackground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.TertiaryBackground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.PrimaryForeground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.SecondaryForeground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.TertiaryForeground"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.PrimaryBorder"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.SecondaryBorder"" />
<BitShimmer Height=""2rem"" BackgroundColor=""BitColor.TertiaryBorder"" />";

    private readonly string example8RazorCode = @"
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

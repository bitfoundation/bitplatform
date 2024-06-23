namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Shimmer;

public partial class BitShimmerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Animation",
            Type = "BitShimmerAnimation",
            DefaultValue = "BitShimmerAnimation.Wave",
            Description = "The animation of the shimmer",
            LinkType = LinkType.Link,
            Href = "#shimmer-animation-enum",
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
            Name = "Shape",
            Type = "BitShimmerShape",
            DefaultValue = "BitShimmerShape.TopRight",
            Description = "The shape of the shimmer.",
            LinkType = LinkType.Link,
            Href = "#shimmer-shape-enum"
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
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The shimmer width value."
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "shimmer-animation-enum",
            Name = "BitShimmerAnimation",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Wave",
                    Value="0",
                },
                new()
                {
                    Name= "Pulse",
                    Value="1",
                }
            }
        },
        new()
        {
            Id = "shimmer-shape-enum",
            Name = "BitShimmerShape",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name = "Line",
                    Value = "0"
                },
                new()
                {
                    Name = "Circle",
                    Value = "1"
                },
                new()
                {
                    Name = "Rectangle",
                    Value = "2"
                }
            }
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "shimmer-class-styles",
            Title = "BitShimmerClassStyles",
            Parameters = new()
            {
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
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitShimmer Height=""1.5rem"" />";

    private readonly string example2RazorCode = @"
<BitShimmer Height=""1.5rem"" />

<BitShimmer Shape=""BitShimmerShape.Circle"" Height=""2.7rem"" />

<BitShimmer Shape=""BitShimmerShape.Rectangle"" Height=""2.7rem"" />";

    private readonly string example3RazorCode = @"
<BitShimmer Height=""1.5rem"" />

<BitShimmer Animation=""BitShimmerAnimation.Pulse"" Shape=""BitShimmerShape.Circle"" Height=""3.5rem"" />
<BitShimmer Animation=""BitShimmerAnimation.Pulse"" Shape=""BitShimmerShape.Rectangle"" Height=""2.7rem"" Width=""100%"" />";

    private readonly string example4RazorCode = @"
<BitShimmer IsDataLoaded=""@isDataLoaded"" AriaLabel=""Loading content"" Height=""1.5rem"">
    Content loaded successfully.
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isDataLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example4CsharpCode = @"
private bool isDataLoaded;";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        box-shadow: aqua 0 0 1rem;
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


<BitShimmer Height=""2.7rem""
            Class=""custom-class""
            Shape=""BitShimmerShape.Rectangle"" />


<BitShimmer Height=""2.7rem""
            Shape=""BitShimmerShape.Rectangle""
            Styles=""@(new() { Shimmer=""background-color: darkgoldenrod;"",
                              ShimmerWrapper = ""background-color: darkgoldenrod;"" })"" />

<BitShimmer Height=""2.7rem""
            Shape=""BitShimmerShape.Rectangle""
            Classes=""@(new() { Root = ""custom-root"",
                               Shimmer=""custom-shimmer"",
                               ShimmerWrapper = ""custom-wrapper"" })"" />";

    private readonly string example6RazorCode = @"
<style>
    .custom-content {
        gap: 1.7rem;
        width: 100%;
        display: flex;
        align-items: center;
    }

    .custom-content .column {
        gap: 0.5rem;
        flex-flow: column;
        align-items: start;
    }
</style>


<BitShimmer IsDataLoaded=""@isContentLoaded"" AriaLabel=""Loading content"" Width=""15.1rem"">
    <Content>
        <BitImage Height=""8rem""
                  Alt=""bit logo""
                  Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
        <br />
        <BitPersona PrimaryText=""Annie Lindqvist""
                    SecondaryText=""Software Engineer""
                    Size=""@BitPersonaSize.Size56""
                    Presence=""@BitPersonaPresence.Online""
                    ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
        <br />
    </Content>
    <ShimmerTemplate>
        <div>
            <BitShimmer Shape=""BitShimmerShape.Rectangle"" Height=""8rem"" />
            <br />
            <div class=""custom-content"">
                <BitShimmer Shape=""BitShimmerShape.Circle"" Height=""3.5rem"" />
                <div class=""custom-content column"">
                    <BitShimmer Height=""1.25rem"" Width=""8.5rem"" />
                    <BitShimmer Height=""0.75rem"" Width=""7rem"" />
                </div>
            </div>
            <br />
        </div>
    </ShimmerTemplate>
</BitShimmer>

<BitToggleButton @bind-IsChecked=""@isContentLoaded"" Text=""Toggle shimmer"" />";
    private readonly string example6CsharpCode = @"
private bool isContentLoaded;";
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AppShell;

public partial class BitAppShellDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "AutoGoToTop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto-scroll to the top of the main container on navigation.",
         },
         new()
         {
            Name = "CascadingValues",
            Type = "IEnumerable<BitCascadingValue>?",
            DefaultValue = "null",
            Description = "The cascading values to be provided for the children of the layout.",
            LinkType = LinkType.Link,
            Href = "#cascading-value"
         },
         new()
         {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the layout.",
         },
         new()
         {
            Name = "Classes",
            Type = "BitAppShellClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the layout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
         new()
         {
            Name = "Styles",
            Type = "BitAppShellClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the layout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "GoToTop",
            Type = "Func<BitScrollBehavior, Task>",
            DefaultValue = "",
            Description = "Scrolls the main container to top.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "cascading-value",
            Title = "BitCascadingValue",
            Description = "The cascading value to be provided using the BitCascadingValueProvider component.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value.",
                },
                new()
                {
                    Name = "Value",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "The value to be provided.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "null",
                    Description = "If true, indicates that Value will not change.",
                }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitAppShellClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root of the BitAppShell.",
                },
                new()
                {
                    Name = "Top",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top area of the BitAppShell.",
                },
                new()
                {
                    Name = "Center",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top center of the BitAppShell.",
                },
                new()
                {
                    Name = "Left",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top left of the BitAppShell.",
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main area of the BitAppShell.",
                },
                new()
                {
                    Name = "Right",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the right area of the BitAppShell.",
                },
                new()
                {
                    Name = "Bottom",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the bottom area of the BitAppShell.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "scroll-behavior-enum",
            Name = "BitScrollBehavior",
            Description = "Determines whether scrolling is instant or animates smoothly.",
            Items =
            [
                new()
                {
                    Name= "Smooth",
                    Description="Scrolling should animate smoothly.",
                    Value="0",
                },
                new()
                {
                    Name= "Instant",
                    Description="Scrolling should happen instantly in a single jump.",
                    Value="1",
                },
                new()
                {
                    Name= "Auto",
                    Description="Scroll behavior is determined by the computed value of scroll-behavior.",
                    Value="2",
                }
            ]
        },
    ];
}

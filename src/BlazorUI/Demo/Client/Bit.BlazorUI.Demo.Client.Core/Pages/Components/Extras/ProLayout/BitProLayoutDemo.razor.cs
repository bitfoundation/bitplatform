namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ProLayout;

public partial class BitProLayoutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Type = "BitProLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the layout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
         new()
         {
            Name = "Styles",
            Type = "BitProLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the layout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "class-styles",
            Title = "BitProLayoutClassStyles",
            Parameters=
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root of the BitProLayout.",
                },
                new()
                {
                    Name = "Top",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top area of the BitProLayout.",
                },
                new()
                {
                    Name = "Center",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top center of the BitProLayout.",
                },
                new()
                {
                    Name = "Left",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top left of the BitProLayout.",
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main area of the BitProLayout.",
                },
                new()
                {
                    Name = "Right",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the right area of the BitProLayout.",
                },
                new()
                {
                    Name = "Bottom",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the bottom area of the BitProLayout.",
                },
            ]
        }
    ];

}

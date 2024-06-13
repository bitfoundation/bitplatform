namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layout;

public partial class BitLayoutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitLayout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "FixedHeader",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables fixed positioning of the header at the top of the viewport.",
        },
        new()
        {
            Name = "FixedFooter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables fixed positioning of the footer at the bottom of the viewport.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the header section.",
        },
        new()
        {
            Name = "HeaderHeight",
            Type = "int",
            DefaultValue = "0",
            Description = "The height of the header to calculate heights and paddings.",
        },
        new()
        {
            Name = "Main",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the main section.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the footer section.",
        },
        new()
        {
            Name = "FooterHeight",
            Type = "int",
            DefaultValue = "0",
            Description = "The height of the footer to calculate heights and paddings.",
        },
        new()
        {
            Name = "StatusBarHeight",
            Type = "int",
            DefaultValue = "0",
            Description = "The height of the status bar on mobile devices to calculate heights and paddings.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitLayout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLayoutClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitLayout."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header section of the BitLayout."
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main section of the BitLayout."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer section of the BitLayout."
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<style>
    .header {
        display: flex;
        padding: 0.5rem;
        background-color: gray;
        justify-content: center;
    }

    .main {
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .footer {
        display: flex;
        padding: 0.5rem;
        background-color: gray;
        justify-content: center;
    }
</style>

<BitLayout>
    <Header>
        <div class=""header"">
            this is header
        </div>
    </Header>
    <Main>
        <div class=""main"">
            this is main
        </div>
    </Main>
    <Footer>
        <div class=""footer"">
            this is footer
        </div>
    </Footer>
</BitLayout>";

    private readonly string example2RazorCode = @"
<style>
    .header {
        display: flex;
        padding: 0.5rem;
        background-color: gray;
        justify-content: center;
    }

    .main {
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .footer {
        display: flex;
        padding: 0.5rem;
        background-color: gray;
        justify-content: center;
    }
</style>

<BitLayout Classes=""@(new() { Header=""header"", Main=""main"", Footer=""footer"" })""
           Styles=""@(new() { Main=""height:500px"" })"">
    <Header>
        this is header
    </Header>
    <Main>
        this is main
    </Main>
    <Footer>
        this is footer
    </Footer>
</BitLayout>";

}

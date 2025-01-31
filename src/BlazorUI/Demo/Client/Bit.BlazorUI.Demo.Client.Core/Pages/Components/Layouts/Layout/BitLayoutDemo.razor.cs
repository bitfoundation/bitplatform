namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Layout;

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
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the footer section.",
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
            Name = "HideNavPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides NavPanel content when true.",
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
            Name = "NavPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the nav panel section.",
        },
        new()
        {
            Name = "NavPanelWidth",
            Type = "int",
            DefaultValue = "0",
            Description = "The width of the nav panel section in px.",
        },
        new()
        {
            Name = "ReverseNavPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the position of the nav panel inside the main container.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitLayout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "StickyFooter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the footer at the bottom of the viewport.",
        },
        new()
        {
            Name = "StickyHeader",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the header at the top of the viewport.",
        },
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
                    Name = "NavPanel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the nav-menu section of the BitLayout."
                },
                new()
                {
                    Name = "MainContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main-content section of the BitLayout."
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



    private bool HideNavPanel;



    private readonly string example1RazorCode = @"
<style>
    .header {
        padding: 0.5rem;
        border: 1px solid gray;
    }

    .main {
        width: 100%;
        height: 100%;
        padding: 0.5rem;
        border: 1px solid gray;
    }

    .footer {
        padding: 0.5rem;
        border: 1px solid gray;
    }
</style>


<BitLayout>
    <Header>
        <div class=""header"">Header</div>
    </Header>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";

    private readonly string example2RazorCode = @"
<style>
    .header {
        padding: 0.5rem;
        border: 1px solid gray;
    }

    .main {
        width: 100%;
        height: 100%;
        padding: 0.5rem;
        border: 1px solid gray;
    }

    .nav-menu {
        width: 100%;
        height: 100%;
        padding: 0.5rem;
        border: 1px solid gray;
    }

    .footer {
        padding: 0.5rem;
        border: 1px solid gray;
    }
</style>


<BitToggle Label=""Hide NavPanel"" @bind-Value=""HideNavPanel"" />

<BitLayout HideNavPanel=""HideNavPanel"">
    <Header>
        <div class=""header"">Header</div>
    </Header>
    <NavPanel>
        <div class=""nav-menu"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example2CsharpCode = @"
private bool HideNavPanel;";

    private readonly string example3RazorCode = @"
<style>
    .header {
        color: black;
        display: flex;
        padding: 1rem;
        border: 1px solid red;
        justify-content: center;
        background-color: lightgreen;
    }

    .main {
        border: 1px solid green;
    }

    .nav-menu2 {
        color: black;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: lightblue;
        border: 1px solid lightgreen;
    }

    .main-content {
        color: black;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 1px solid darkgreen;
        background-color: lightgoldenrodyellow;
    }

    .footer {
        color: black;
        display: flex;
        padding: 1rem;
        border: 1px solid blue;
        justify-content: center;
        background-color: lightpink;
    }
</style>


<BitLayout Styles=""@(new() { Main = ""height: 19rem;"" })"" 
           Classes=""@(new() { Header = ""header2"",
                              Main = ""main2"",
                              NavPanel = ""nav-menu2"",
                              MainContent = ""main-content2"",
                              Footer = ""footer2"" })"">
    <Header>Header</Header>
    <NavPanel>NavPanel</NavPanel>
    <Main>Main</Main>
    <Footer>Footer</Footer>
</BitLayout>";
}

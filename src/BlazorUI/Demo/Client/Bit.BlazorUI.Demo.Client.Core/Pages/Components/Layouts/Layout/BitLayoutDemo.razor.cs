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
            Name = "HideNavMenu",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides NavMenu content when true.",
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
            Name = "NavMenu",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the nav-menu section.",
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
                    Name = "NavMenu",
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



    private bool hideNavMenu;

    private int headerHeight = 60;
    private int footerHeight = 60;


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


<BitToggle Label=""Hide NavMenu"" @bind-Value=""hideNavMenu"" />

<BitLayout HideNavMenu=""hideNavMenu"">
    <Header>
        <div class=""header"">Header</div>
    </Header>
    <NavMenu>
        <div class=""nav-menu"">NavMenu</div>
    </NavMenu>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example2CsharpCode = @"
private bool hideNavMenu;";

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
                              NavMenu = ""nav-menu2"",
                              MainContent = ""main-content2"",
                              Footer = ""footer2"" })"">
    <Header>Header</Header>
    <NavMenu>NavMenu</NavMenu>
    <Main>Main</Main>
    <Footer>Footer</Footer>
</BitLayout>";

    private readonly string example4RazorCode = @"
<BitNumberField Label=""Header height"" @bind-Value=""headerHeight"" />
<BitNumberField Label=""Footer height"" @bind-Value=""footerHeight"" />

<BitLayout HeaderHeight=""headerHeight"" 
           FooterHeight=""footerHeight""
           Styles=""@(new() { Root = ""color: black;"",
                             Header = ""background: lightcoral;"",
                             Main = ""background: lightgreen;"",
                             NavMenu = ""padding: 1rem;"",
                             MainContent = ""padding: 1rem;"",
                             Footer = ""background: lightblue;"" })"">
    <Header>Header</Header>
    <NavMenu>NavMenu</NavMenu>
    <Main>Main</Main>
    <Footer>Footer</Footer>
</BitLayout>";
}

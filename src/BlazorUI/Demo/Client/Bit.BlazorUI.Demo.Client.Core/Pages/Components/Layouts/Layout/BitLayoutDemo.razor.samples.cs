namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Layout;

public partial class BitLayoutDemo
{
    private readonly string example1RazorCode = @"
<style>
    .header,
    .footer {
        padding: 0.5rem;
        box-sizing: border-box;
        border: 1px solid gray;
    }

    .nav-panel,
    .main,
    .aside {
        width: 100%;
        height: 100%;
        padding: 0.5rem;
        box-sizing: border-box;
        border: 1px solid gray;
    }

    .fill {
        height: 100%;
        box-sizing: border-box;
    }

    .tall {
        height: 600px;
    }

    .pad {
        padding: 0.5rem;
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
<BitToggle Label=""Reverse NavPanel"" @bind-Value=""reverseNavPanel"" />

<BitLayout NavPanelWidth=""120"" AsideWidth=""120"" ReverseNavPanel=""reverseNavPanel"">
    <Header>
        <div class=""header"">Header</div>
    </Header>
    <NavPanel>
        <div class=""nav-panel"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Aside>
        <div class=""aside"">Aside</div>
    </Aside>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example2CsharpCode = @"
private bool reverseNavPanel;";

    private readonly string example3RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"">
    <BitToggle Label=""Hide Header"" @bind-Value=""hideHeader"" />
    <BitToggle Label=""Hide NavPanel"" @bind-Value=""hideNavPanel"" />
    <BitToggle Label=""Hide Aside"" @bind-Value=""hideAside"" />
    <BitToggle Label=""Hide Footer"" @bind-Value=""hideFooter"" />
</BitStack>

<BitLayout NavPanelWidth=""120""
           AsideWidth=""120""
           HideHeader=""hideHeader""
           HideNavPanel=""hideNavPanel""
           HideAside=""hideAside""
           HideFooter=""hideFooter"">
    <Header>
        <div class=""header"">Header</div>
    </Header>
    <NavPanel>
        <div class=""nav-panel"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Aside>
        <div class=""aside"">Aside</div>
    </Aside>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example3CsharpCode = @"
private bool hideHeader;
private bool hideNavPanel;
private bool hideAside;
private bool hideFooter;";

    private readonly string example4RazorCode = @"
<BitLayout HeaderHeight=""64"" FooterHeight=""40"" Gap=""1rem"" Padding=""1rem"" NavPanelWidth=""120"" AsideWidth=""120"">
    <Header>
        <div class=""header fill"">64px header</div>
    </Header>
    <NavPanel>
        <div class=""nav-panel"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Aside>
        <div class=""aside"">Aside</div>
    </Aside>
    <Footer>
        <div class=""footer fill"">40px footer</div>
    </Footer>
</BitLayout>";

    private readonly string example5RazorCode = @"
<BitLayout Bordered NavPanelWidth=""120"" AsideWidth=""120"">
    <Header>
        <div class=""pad"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""pad"">Main</div>
    </Main>
    <Aside>
        <div class=""pad"">Aside</div>
    </Aside>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";

    private readonly string example6RazorCode = @"
<BitToggle Label=""FullHeightPanels"" @bind-Value=""fullHeightPanels"" />

<BitLayout Bordered NavPanelWidth=""120"" AsideWidth=""100"" FullHeightPanels=""fullHeightPanels"">
    <Header>
        <div class=""pad"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""pad"">Main</div>
    </Main>
    <Aside>
        <div class=""pad"">Aside</div>
    </Aside>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example6CsharpCode = @"
private bool fullHeightPanels;";

    private readonly string example7RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"">
    <BitToggle Label=""Sticky Header"" @bind-Value=""stickyHeader"" />
    <BitToggle Label=""Sticky NavPanel"" @bind-Value=""stickyNavPanel"" />
    <BitToggle Label=""Sticky Aside"" @bind-Value=""stickyAside"" />
    <BitToggle Label=""Sticky Footer"" @bind-Value=""stickyFooter"" />
</BitStack>

<BitLayout HeaderHeight=""40""
           FooterHeight=""40""
           NavPanelWidth=""120""
           AsideWidth=""120""
           StickyHeader=""stickyHeader""
           StickyNavPanel=""stickyNavPanel""
           StickyAside=""stickyAside""
           StickyFooter=""stickyFooter"">
    <Header>
        <div class=""header fill"">Header</div>
    </Header>
    <NavPanel>
        <div class=""nav-panel"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main tall"">Scroll me</div>
    </Main>
    <Aside>
        <div class=""aside"">Aside</div>
    </Aside>
    <Footer>
        <div class=""footer fill"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example7CsharpCode = @"
private bool stickyHeader;
private bool stickyNavPanel;
private bool stickyAside;
private bool stickyFooter;";

    private readonly string example8RazorCode = @"
<BitToggle Label=""FullHeight"" @bind-Value=""fullHeight"" />

<BitLayout FullHeight=""fullHeight"">
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
    private readonly string example8CsharpCode = @"
private bool fullHeight;";

    private readonly string example9RazorCode = @"
<BitToggle Label=""ScrollableMain"" @bind-Value=""scrollableMain"" />

<BitLayout Bordered NavPanelWidth=""120"" ScrollableMain=""scrollableMain"">
    <Header>
        <div class=""pad"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad tall"">Scroll me</div>
    </NavPanel>
    <Main>
        <div class=""pad tall"">Scroll me</div>
    </Main>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example9CsharpCode = @"
private bool scrollableMain;";

    private readonly string example10RazorCode = @"
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtMd"" @bind-IsMatched=""isLtMd"" />
<BitMediaQuery ScreenQuery=""BitScreenQuery.LtSm"" @bind-IsMatched=""isLtSm"" />

<BitLayout Bordered NavPanelWidth=""120"" AsideWidth=""120"" HideAside=""isLtMd"" HideNavPanel=""isLtSm"">
    <Header>
        <div class=""pad"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""pad"">Main</div>
    </Main>
    <Aside>
        <div class=""pad"">Aside</div>
    </Aside>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";
    private readonly string example10CsharpCode = @"
private bool isLtMd;
private bool isLtSm;";

    private readonly string example11RazorCode = @"
<BitLayout Bordered NavPanelWidth=""120"">
    <Header>
        <div class=""pad"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad"">NavPanel</div>
    </NavPanel>
    <Main>
        <BitLayout Nested Bordered NavPanelWidth=""100"" Padding=""0.5rem"">
            <Header>
                <div class=""pad"">Nested header</div>
            </Header>
            <NavPanel>
                <div class=""pad"">Nested nav</div>
            </NavPanel>
            <Main>
                Nested main
            </Main>
        </BitLayout>
    </Main>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";

    private readonly string example12RazorCode = @"
<BitLayout SkipLink
           SkipLinkText=""Skip to the content""
           NavPanelWidth=""120""
           AsideWidth=""120""
           NavPanelAriaLabel=""Main""
           AsideAriaLabel=""On this page"">
    <Header>
        <div class=""header"">Header <a href=""https://bitplatform.dev"">bitplatform.dev</a></div>
    </Header>
    <NavPanel>
        <div class=""nav-panel"">NavPanel</div>
    </NavPanel>
    <Main>
        <div class=""main"">Main</div>
    </Main>
    <Aside>
        <div class=""aside"">Aside</div>
    </Aside>
    <Footer>
        <div class=""footer"">Footer</div>
    </Footer>
</BitLayout>";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""@layoutParams"">
    <BitLayout>
        <Header>
            <div class=""pad"">Takes the cascade</div>
        </Header>
        <NavPanel>
            <div class=""pad"">NavPanel</div>
        </NavPanel>
        <Main>
            <div class=""pad"">Main</div>
        </Main>
    </BitLayout>

    <BitLayout Bordered=""false"" Gap=""0"">
        <Header>
            <div class=""pad"">Its own Bordered and Gap</div>
        </Header>
        <NavPanel>
            <div class=""pad"">NavPanel</div>
        </NavPanel>
        <Main>
            <div class=""pad"">Main</div>
        </Main>
    </BitLayout>
</BitParams>";
    private readonly string example13CsharpCode = @"
private readonly BitLayoutParams[] layoutParams =
[
    new()
    {
        Bordered = true,
        Gap = ""1rem"",
        Padding = ""0.5rem"",
        NavPanelWidth = 120,
        NavPanelAriaLabel = ""Section"",
    }
];";

    private readonly string example14RazorCode = @"
<style>
    .custom-root {
        overflow: hidden;
        border: 1px solid gray;
    }

    .custom-header {
        color: black;
        display: flex;
        padding: 1rem;
        border: 1px solid red;
        justify-content: center;
        background-color: lightgreen;
    }

    .custom-nav-panel {
        color: black;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: lightblue;
        border: 1px solid lightgreen;
    }

    .custom-main-content {
        color: black;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 1px solid darkgreen;
        background-color: lightgoldenrodyellow;
    }

    .custom-aside {
        color: black;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 1px solid darkorange;
        background-color: lightyellow;
    }

    .custom-footer {
        color: black;
        display: flex;
        padding: 1rem;
        border: 1px solid blue;
        justify-content: center;
        background-color: lightpink;
    }
</style>


<BitLayout NavPanelWidth=""120""
           AsideWidth=""120""
           Class=""custom-root""
           Style=""border-radius: 0.5rem;""
           Styles=""@(new() { Main = ""min-height: 12rem;"" })""
           Classes=""@(new() { Header = ""custom-header"",
                              NavPanel = ""custom-nav-panel"",
                              MainContent = ""custom-main-content"",
                              Aside = ""custom-aside"",
                              Footer = ""custom-footer"" })"">
    <Header>Header</Header>
    <NavPanel>NavPanel</NavPanel>
    <Main>Main</Main>
    <Aside>Aside</Aside>
    <Footer>Footer</Footer>
</BitLayout>

<BitLayout Bordered
           NavPanelWidth=""120""
           Style=""--bit-Layout-header-height: 48px;
                  --bit-Layout-padding: 1rem;
                  --bit-Layout-border-width: 2px;
                  --bit-Layout-border-color: var(--bit-clr-pri);
                  --bit-Layout-header-background: var(--bit-clr-pri);
                  --bit-Layout-nav-panel-background: color-mix(in srgb, var(--bit-clr-pri) 10%, transparent);
                  --bit-Layout-footer-background: var(--bit-clr-bg-sec);"">
    <Header>
        <div class=""pad"" style=""color: var(--bit-clr-pri-text)"">Header</div>
    </Header>
    <NavPanel>
        <div class=""pad"">NavPanel</div>
    </NavPanel>
    <Main>Main</Main>
    <Footer>
        <div class=""pad"">Footer</div>
    </Footer>
</BitLayout>";

    private readonly string example15RazorCode = @"
<BitLayout Bordered Dir=""BitDir.Rtl"" NavPanelWidth=""120"" AsideWidth=""120"">
    <Header>
        <div class=""pad"">سربرگ</div>
    </Header>
    <NavPanel>
        <div class=""pad"">منو</div>
    </NavPanel>
    <Main>
        <div class=""pad"">محتوا</div>
    </Main>
    <Aside>
        <div class=""pad"">کناره</div>
    </Aside>
    <Footer>
        <div class=""pad"">پاورقی</div>
    </Footer>
</BitLayout>";
}

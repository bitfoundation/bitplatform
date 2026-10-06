namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Callout;

public partial class BitCalloutDemo
{
    private readonly string example1RazorCode = @"
<style>
    .callout-content {
        padding: 1rem;
    }
</style>

<BitCallout>
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<BitCallout NoOverlay>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">NoOverlay</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">The page keeps its own clicks.</div>
    </Content>
</BitCallout>

<BitCallout DefaultIsOpen=""true"" NoOverlay>
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">DefaultIsOpen</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Open from the start.</div>
    </Content>
</BitCallout>

<BitCallout IsEnabled=""false"">
    <Anchor>
        <BitButton IsEnabled=""false"">Disabled</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Never shown.</div>
    </Content>
</BitCallout>";

    private readonly string example2RazorCode = @"
<BitButton Id=""anchor_id"" OnClick=""() => callout1.Toggle()"">AnchorId</BitButton>
<BitCallout AnchorId=""anchor_id"" @ref=""callout1"">
    <div class=""callout-content"">
        <BitCalendar />
    </div>
</BitCallout>

<button @ref=""anchorEl"" @onclick=""() => callout2.Toggle()"">AnchorEl</button>
<BitCallout AnchorEl=""() => anchorEl"" @ref=""callout2"">
    <div class=""callout-content"">
        <BitCalendar />
    </div>
</BitCallout>";
    private readonly string example2CsharpCode = @"
private ElementReference anchorEl;
private BitCallout callout1;
private BitCallout callout2;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => isOpen = true"">Show callout</BitButton>

<BitCallout @bind-IsOpen=""isOpen"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Anchor</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <div>Close it from inside.</div>
                <BitStack Horizontal Gap=""0.5rem"">
                    <BitButton OnClick=""() => isOpen = false"">Done</BitButton>
                    <BitButton OnClick=""() => isOpen = false"" Variant=""BitVariant.Outline"">Cancel</BitButton>
                </BitStack>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<div>IsOpen: @isOpen</div>";
    private readonly string example3CsharpCode = @"
private bool isOpen;";

    private readonly string example4RazorCode = @"
<BitCallout OpenOnHover>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Hover me</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Opened on hover.</div>
    </Content>
</BitCallout>

<BitCallout OpenOnHover HoverOpenDelay=""500"" HoverCloseDelay=""500"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Delayed (500ms)</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Opening and closing both waited half a second.</div>
    </Content>
</BitCallout>";

    private readonly string example5RazorCode = @"
<BitCallout AutoClose>
    <Anchor>
        <BitButton>AutoClose (@autoCloseAction)</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.25rem"">
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Renamed"")"">Rename</BitButton>
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Duplicated"")"">Duplicate</BitButton>
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Deleted"")"">Delete</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout NoDismissOnScroll>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">NoDismissOnScroll</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Scroll the page: this one follows its anchor.</div>
    </Content>
</BitCallout>

<BitCallout NoDismissOnEscape NoDismissOnOutsideClick @ref=""callout3"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">No auto dismiss</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <div>Neither Escape nor an outside click closes this one.</div>
                <BitButton OnClick=""() => callout3.Close()"">Close</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>";
    private readonly string example5CsharpCode = @"
private BitCallout callout3;
private string autoCloseAction = ""none"";";

    private readonly string example6RazorCode = @"
<style>
    .context-area {
        padding: 2rem;
        border-radius: 4px;
        border: 1px dashed gray;
    }
</style>

<div class=""context-area"" @oncontextmenu=""e => contextCallout.OpenAt(e)"" @oncontextmenu:preventDefault>
    Right-click anywhere in here
</div>

<BitCallout AutoClose NoOverlay @ref=""contextCallout"">
    <div class=""callout-content"">
        <BitStack Gap=""0.25rem"">
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Cut"")"">Cut</BitButton>
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Copied"")"">Copy</BitButton>
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Pasted"")"">Paste</BitButton>
        </BitStack>
    </div>
</BitCallout>

<div>Last action: @contextAction</div>";
    private readonly string example6CsharpCode = @"
private BitCallout contextCallout;
private string contextAction = ""none"";";

    private readonly string example7RazorCode = @"
<BitChoiceGroup Horizontal Label=""Side"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""placementSide"">
    <BitChoiceGroupOption Text=""Auto"" Value=""@(""Auto"")"" />
    <BitChoiceGroupOption Text=""Top"" Value=""@(""Top"")"" />
    <BitChoiceGroupOption Text=""Bottom"" Value=""@(""Bottom"")"" />
    <BitChoiceGroupOption Text=""Start"" Value=""@(""Start"")"" />
    <BitChoiceGroupOption Text=""End"" Value=""@(""End"")"" />
</BitChoiceGroup>
<BitChoiceGroup Horizontal Label=""Alignment"" TItem=""BitChoiceGroupOption<BitPlacement>"" TValue=""BitPlacement"" @bind-Value=""placementAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitPlacement.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitPlacement.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitPlacement.End"" />
</BitChoiceGroup>
<BitNumberField Label=""Gap"" @bind-Value=""placementGap"" Min=""0"" Max=""64"" Style=""max-width:8rem"" />
<BitNumberField Label=""AlignmentOffset"" @bind-Value=""placementOffset"" Min=""0"" Max=""64"" Style=""max-width:8rem"" />
<BitCheckbox Label=""NoFlip"" @bind-Value=""placementNoFlip"" />

<BitCallout Placement=""PlacementSide"" Alignment=""placementAlignment"" Gap=""placementGap"" AlignmentOffset=""placementOffset"" NoFlip=""placementNoFlip"">
    <Anchor>
        <BitButton>A wide anchor to place against</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Placed by the options above.</div>
    </Content>
</BitCallout>


<BitCallout Direction=""BitDropDirection.All"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Direction: All</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 23; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout CollisionPadding=""24"" Direction=""BitDropDirection.All"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">CollisionPadding of 24px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 25; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout @ref=""callout4"" Placement=""BitPlacement.Top"" Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Reposition</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                @for (int i = 1; i <= repositionRows; i++)
                {
                    <div>Callout content @i</div>
                }
                <BitButton OnClick=""AddRepositionRow"">Add a row</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>";
    private readonly string example7CsharpCode = @"
private string placementSide = ""Auto"";
private BitPlacement placementAlignment = BitPlacement.Start;
private int placementGap = 8;
private int placementOffset;
private bool placementNoFlip;

private BitPlacement? PlacementSide => Enum.TryParse<BitPlacement>(placementSide, out var side) ? side : null;

private BitCallout callout4 = default!;
private int repositionRows = 2;
private bool repositionAfterRender;

private void AddRepositionRow()
{
    repositionRows++;

    // The callout is laid out against what is actually in it, so the reposition waits for the render
    // that puts the new row there rather than measuring the content the callout still holds.
    repositionAfterRender = true;
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await base.OnAfterRenderAsync(firstRender);

    if (repositionAfterRender)
    {
        repositionAfterRender = false;

        await callout4.Reposition();
    }
}";

    private readonly string example8RazorCode = @"
<BitCallout Background=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton>Background</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A secondary background.</div>
    </Content>
</BitCallout>

<BitCallout Border=""BitColorKind.Primary"">
    <Anchor>
        <BitButton>Border</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A primary border.</div>
    </Content>
</BitCallout>

<BitCallout NoShadow Border=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton>NoShadow</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">No elevation, only a border.</div>
    </Content>
</BitCallout>";

    private readonly string example9RazorCode = @"
<BitCallout ShowArrow Gap=""8"">
    <Anchor>
        <BitButton>With an arrow</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Points at its anchor.</div>
    </Content>
</BitCallout>

<BitCallout ShowArrow Gap=""8"" Placement=""BitPlacement.End"" Border=""BitColorKind.Secondary"" Background=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Beside, with a border</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">The beak takes the border too.</div>
    </Content>
</BitCallout>

<BitCallout ShowArrow ArrowSize=""20"" Gap=""12"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">ArrowSize of 20px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A bigger beak.</div>
    </Content>
</BitCallout>

<BitCallout ShowArrow ArrowPadding=""64"" Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">ArrowPadding of 64px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">The beak is held 64px away from the corners of the callout.</div>
    </Content>
</BitCallout>";

    private readonly string example10RazorCode = @"
<BitCallout Width=""20rem"">
    <Anchor>
        <BitButton>Width</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A callout of a fixed width.</div>
    </Content>
</BitCallout>

<BitCallout MaxWidth=""16rem"">
    <Anchor>
        <BitButton>MaxWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            A long line of text that wraps inside the callout instead of stretching it across the page.
        </div>
    </Content>
</BitCallout>

<BitCallout MaxHeight=""12rem"" MinWidth=""14rem"">
    <Anchor>
        <BitButton>MaxHeight &amp; MinWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 25; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout SetCalloutWidth>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">A wide anchor with SetCalloutWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Short content.</div>
    </Content>
</BitCallout>

<BitCallout FixedCalloutWidth>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">A wide anchor with FixedCalloutWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            A long line of text that wraps rather than making the callout wider than its anchor.
        </div>
    </Content>
</BitCallout>";

    private readonly string example11RazorCode = @"
<style>
    .section-bar {
        font-weight: 600;
        padding: 0.75rem 1rem;
    }

    .scroller {
        display: flex;
        max-width: 16rem;
        flex-direction: column;
    }

    .scroller-body {
        overflow: auto;
    }

    .scroller-bar {
        padding: 0.5rem 0;
        font-weight: 600;
    }
</style>

<BitCallout MaxWidth=""16rem"">
    <Anchor>
        <BitButton>Header &amp; Footer</BitButton>
    </Anchor>
    <Header>
        <div class=""section-bar"">A header that stays put</div>
    </Header>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 69; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
    <Footer>
        <div class=""section-bar"">A footer that stays put</div>
    </Footer>
</BitCallout>

<BitCallout ScrollContainerId=""scroller-container"" HeaderId=""scroller-header"" FooterId=""scroller-footer"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Wired up by hand</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content scroller"">
            <div id=""scroller-header"" class=""scroller-bar"">A header that stays put</div>
            <div id=""scroller-container"" class=""scroller-body"">
                @for (int i = 1; i < 69; i++)
                {
                    <div>Callout content @i</div>
                }
            </div>
            <div id=""scroller-footer"" class=""scroller-bar"">A footer that stays put</div>
        </div>
    </Content>
</BitCallout>";

    private readonly string example12RazorCode = @"
<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPlacement=""BitPlacement.End"">
    <Anchor>
        <BitButton>End panel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle1"">Filters</BitText>
                <BitCheckbox Label=""Active"" />
                <BitCheckbox Label=""Archived"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPlacement=""BitPlacement.Start"">
    <Anchor>
        <BitButton>Start panel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle1"">Filters</BitText>
                <BitCheckbox Label=""Active"" />
                <BitCheckbox Label=""Archived"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Top"">
    <Anchor>
        <BitButton>Top sheet</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Swipe up to dismiss it.</div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Bottom"">
    <Anchor>
        <BitButton>Bottom sheet</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Swipe down to dismiss it.</div>
    </Content>
</BitCallout>";

    private readonly string example13RazorCode = @"
<BitCallout AutoFocus>
    <Anchor>
        <BitButton>AutoFocus</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitButton Variant=""BitVariant.Text"">Dismiss</BitButton>
                <BitTextField Label=""Name"" InputHtmlAttributes=""@(new() { { ""data-autofocus"", """" } })"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout TrapFocus>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">TrapFocus</BitButton>
    </Anchor>
    <Header>
        <div class=""section-bar"">Contact</div>
    </Header>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitTextField Label=""Name"" />
                <BitTextField Label=""Email"" />
                <BitButton>Submit</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout Modal AriaLabelledBy=""modal-title"" AriaDescribedBy=""modal-text"" @ref=""modalCallout"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Modal</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <b id=""modal-title"">Heads up</b>
                <div id=""modal-text"">The page behind is dimmed and holds still.</div>
                <BitButton OnClick=""() => modalCallout.Close()"">OK</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout Role=""status"" AriaLabel=""Sync status"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">Role &amp; AriaLabel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Everything is up to date.</div>
    </Content>
</BitCallout>";
    private readonly string example13CsharpCode = @"
private BitCallout modalCallout;";

    private readonly string example14RazorCode = @"
<BitCallout MinWidth=""15rem"">
    <Anchor>
        <BitButton>Filters</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle2"">Filters</BitText>
                <BitCheckbox Label=""Active"" />
                <BitCheckbox Label=""Archived"" />

                <BitCallout ShowArrow Gap=""8"" Placement=""BitPlacement.End"">
                    <Anchor>
                        <BitButton Variant=""BitVariant.Outline"">More options</BitButton>
                    </Anchor>
                    <Content>
                        <div class=""callout-content"">
                            <BitStack Gap=""0.25rem"">
                                <BitText>The panel behind is still open.</BitText>

                                <BitCallout ShowArrow Gap=""8"" AutoClose Placement=""BitPlacement.End"">
                                    <Anchor>
                                        <BitButton Variant=""BitVariant.Text"">One more level</BitButton>
                                    </Anchor>
                                    <Content>
                                        <div class=""callout-content"">And so is this one.</div>
                                    </Content>
                                </BitCallout>
                            </BitStack>
                        </div>
                    </Content>
                </BitCallout>
            </BitStack>
        </div>
    </Content>
</BitCallout>";

    private readonly string example15RazorCode = @"
<BitCallout LazyRender>
    <Anchor>
        <BitButton>LazyRender</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitCalendar @bind-Value=""lazyDate"" />
        </div>
    </Content>
</BitCallout>

<BitCallout>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Rendered up front</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitCalendar @bind-Value=""eagerDate"" />
        </div>
    </Content>
</BitCallout>";
    private readonly string example15CsharpCode = @"
private DateTimeOffset? lazyDate;
private DateTimeOffset? eagerDate;";

    private readonly string example16RazorCode = @"
<BitCallout OnToggle=""v => toggleCount++"" OnOpen=""() => openCount++"" OnDismiss=""() => dismissCount++"">
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<div>Toggled: @toggleCount, Opened: @openCount, Dismissed: @dismissCount</div>";
    private readonly string example16CsharpCode = @"
private int openCount;
private int toggleCount;
private int dismissCount;";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""@calloutParams"">
    <BitCallout>
        <Anchor>
            <BitButton>Cascaded</BitButton>
        </Anchor>
        <Content>
            <div class=""callout-content"">Arrow, gap, border and side come from the cascade.</div>
        </Content>
    </BitCallout>

    <BitCallout>
        <Anchor>
            <BitButton Variant=""BitVariant.Outline"">Cascaded too</BitButton>
        </Anchor>
        <Content>
            <div class=""callout-content"">The same defaults.</div>
        </Content>
    </BitCallout>

    <BitCallout Placement=""BitPlacement.Bottom"" ShowArrow=""false"">
        <Anchor>
            <BitButton Variant=""BitVariant.Text"">Own Placement, no arrow</BitButton>
        </Anchor>
        <Content>
            <div class=""callout-content"">Its own values win.</div>
        </Content>
    </BitCallout>
</BitParams>

<BitCallout>
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">Outside the cascade</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">No defaults here.</div>
    </Content>
</BitCallout>";
    private readonly string example17CsharpCode = @"
private readonly BitCalloutParams[] calloutParams =
[
    new()
    {
        ShowArrow = true,
        Gap = 8,
        Placement = BitPlacement.End,
        Border = BitColorKind.Secondary,
        NoShadow = true,
    }
];";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        border-radius: 4px;
        box-shadow: dodgerblue 0 0 8px;
    }

    .custom-content {
        padding: 1rem;
        color: white;
        border-radius: 4px;
        background-color: darkviolet;
    }

    .custom-arrow {
        background-color: darkviolet;
    }

    .custom-anchor {
        color: white;
        cursor: pointer;
        padding: 8px 16px;
        border-radius: 4px;
        background-color: darkviolet;
    }
</style>


<BitCallout ShowArrow Gap=""12""
            Style=""--bit-Callout-background: #1e293b; --bit-Callout-color: #f8fafc; --bit-Callout-radius: 1rem; --bit-Callout-padding: 1rem; --bit-Callout-arrow-size: 16px;"">
    <Anchor>
        <BitButton>Inverted</BitButton>
    </Anchor>
    <Content>
        A dark surface, rounder corners and a bigger beak.
    </Content>
</BitCallout>

<BitCallout MaxWidth=""16rem""
            Styles=""@(new() { Root = ""--bit-Callout-border-width: 2px; --bit-Callout-border-color: #7c3aed; --bit-Callout-divider-color: #ddd6fe; --bit-Callout-shadow: none; --bit-Callout-padding: 0.75rem;"" })"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Outlined</BitButton>
    </Anchor>
    <Header>
        <b>Outlined</b>
    </Header>
    <Content>
        A border instead of elevation; the padding goes inside the header, the body and the footer.
    </Content>
    <Footer>
        <BitButton Variant=""BitVariant.Text"">Got it</BitButton>
    </Footer>
</BitCallout>


<BitCallout Style=""background-color: #ff634733; border-radius: 4px;"">
    <Anchor>
        <BitButton Color=""BitColor.Error"">Component's Style</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<BitCallout Class=""custom-class"">
    <Anchor>
        <BitButton>Component's Class</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>


<BitCallout Styles=""@(new() { Root = ""--anchor-color: #2e8b5775;"",
                              Opened = ""--anchor-color: #04cb5b75;"",
                              AnchorContainer = ""background-color: var(--anchor-color); border-radius: 4px;"",
                              Content = ""border: 2px solid #04cb5b75;"" })"">
    <Anchor>
        <BitActionButton>Styles</BitActionButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<BitCallout ShowArrow Gap=""8"" Classes=""@(new() { Content = ""custom-content"",
                                                 Arrow = ""custom-arrow"",
                                                 AnchorContainer = ""custom-anchor"" })"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">Classes</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>";

    private readonly string example19RazorCode = @"
<BitCallout Dir=""BitDir.Rtl"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton>نمایش کال‌اوت</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">این محتوای کال‌اوت است.</div>
    </Content>
</BitCallout>

<BitCallout Dir=""BitDir.Rtl"" Direction=""BitDropDirection.All"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">همه جهت‌ها</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 13; i++)
            {
                <div>محتوای کال‌اوت @i</div>
            }
        </div>
    </Content>
</BitCallout>";
}

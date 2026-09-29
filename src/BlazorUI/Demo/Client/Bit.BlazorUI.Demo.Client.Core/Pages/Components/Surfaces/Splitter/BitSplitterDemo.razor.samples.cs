namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Splitter;

public partial class BitSplitterDemo
{
    private readonly string example1RazorCode = @"
<BitSplitter Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">
            First panel
            <br /><br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">
            Second panel
            <br /><br />
            Each word carried meaning, each pause brought understanding. The spaces here are open for growth.
        </div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example2RazorCode = @"
<BitSplitter Vertical Style=""height:250px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example3RazorCode = @"
<BitSplitter FirstPanelSize=""200"" FirstPanelMinSize=""120"" FirstPanelMaxSize=""320"" SecondPanelMinSize=""100""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Starts at 200px, stays between 120px and 320px.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Never narrower than 100px.</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter SecondPanelSize=""150"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Takes the rest.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Starts at 150px.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example4RazorCode = @"
<BitSlider Label=""@($""Percent: {PercentValue:F0}%"")"" @bind-Value=""PercentValue"" Min=""0"" Max=""100"" />

<BitSplitter @bind-Percent=""percent"" FirstPanelMinSize=""60"" SecondPanelMinSize=""60""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter DefaultPercent=""30"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Starts at 30%, free to move, double-click to go back.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example4CsharpCode = @"
private double? percent = 30;
private double PercentValue { get => percent ?? 50; set => percent = value; }";

    private readonly string example5RazorCode = @"
<BitSlider Label=""@($""Drag step: {dragStep:F0}px"")"" @bind-Value=""dragStep"" Max=""100"" Step=""10"" />

<BitSplitter DragStep=""@((int)dragStep)"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Stops every @dragStep pixels</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example5CsharpCode = @"
private double dragStep = 50;";

    private readonly string example6RazorCode = @"
<BitSplitter LazyResize FirstPanelMinSize=""80"" SecondPanelMinSize=""80""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">The line moves first; the panels follow when you let go.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example7RazorCode = @"
<BitSlider Label=""@($""Gutter size: {gutterSize:F0}px"")"" @bind-Value=""gutterSize"" Max=""50"" />

<BitSplitter GutterSize=""@((int)gutterSize)"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">GutterSize</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""1"" GutterHitSize=""16"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A 1px hairline, grabbed anywhere within 16px</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIconName=""@BitIconName.GripperDotsVertical""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">GutterIconName</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""14"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <GutterTemplate>
        <div style=""display:flex;flex-direction:column;gap:2px"">
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
        </div>
    </GutterTemplate>
    <FirstPanel>
        <div style=""padding:0.5rem"">GutterTemplate</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example7CsharpCode = @"
private double gutterSize = 10;";

    private readonly string example8RazorCode = @"
<BitSplitter Collapsible CollapsedSize=""8"" @bind-Collapsed=""isCollapsed"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A panel that folds away</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Collapsed: @isCollapsed</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible CollapsedSize=""8"" SnapSize=""40"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Folds only when dragged within 40px of the edge.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible CollapseSecondPanel CollapsedSize=""8"" SecondPanelSize=""180"" SecondPanelMinSize=""120""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A document that takes the whole splitter once the inspector is away</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">An inspector that folds to the end</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example8CsharpCode = @"
private bool isCollapsed;";

    private readonly string example9RazorCode = @"
<BitSplitter Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Press the chevron to fold this away.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Vertical Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""70""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A stacked splitter folds upwards.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible CollapseSecondPanel ShowCollapseButton GutterSize=""16"" CollapsedSize=""8"" SecondPanelSize=""180""
             CollapseIconName=""@BitIconName.ClosePane"" ExpandIconName=""@BitIconName.OpenPane""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">CollapseIconName and ExpandIconName</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">An inspector with icons of its own</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example10RazorCode = @"
<BitToggle Label=""Allow the panel to be folded away"" @bind-Value=""allowCollapse"" />

<BitSplitter Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             OnCollapsing=""@(args => { args.Cancel = args.IsCollapsing && allowCollapse is false;
                                       collapseLog = $""{args.Reason} asked to {(args.IsCollapsing ? ""collapse"" : ""expand"")}: {(args.Cancel ? ""refused"" : ""allowed"")}""; })""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Folds away only with permission</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<div>@collapseLog</div>";
    private readonly string example10CsharpCode = @"
private bool allowCollapse = true;
private string collapseLog = ""Nothing has been folded yet."";";

    private readonly string example11RazorCode = @"
<BitSplitter FirstPanelMinSize=""60"" SecondPanelMinSize=""60"" Collapsible CollapsedSize=""8""
             OnResizeStart=""@(p => resizeLog = $""Started at {p:F1}%"")""
             OnResize=""@(p => resizeLog = $""Resizing: {p:F1}%"")""
             OnResizeEnd=""@(p => resizeLog = $""Ended at {p:F1}%"")""
             OnResizeCancel=""@(p => resizeLog = $""Cancelled, back at {p:F1}%"")""
             OnCollapsedChange=""@(c => resizeLog = c ? ""Collapsed"" : ""Expanded"")""
             OnGutterDoubleClick=""@(() => resizeLog = ""The gutter was double-clicked"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<div>@resizeLog</div>

<BitSplitter NoResetOnDoubleClick @bind-Percent=""evenPercent"" OnGutterDoubleClick=""@(() => evenPercent = 50)""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Double-click the gutter</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">to split evenly.</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example11CsharpCode = @"
private string resizeLog = ""No resize yet."";
private double? evenPercent;";

    private readonly string example12RazorCode = @"
<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""@(() => splitterRef.SetPercent(25))"">25%</BitButton>
    <BitButton OnClick=""@(() => splitterRef.SetPercent(50))"">50%</BitButton>
    <BitButton OnClick=""@(() => splitterRef.SetPercent(75))"">75%</BitButton>
    <BitButton OnClick=""@(() => splitterRef.ToggleCollapse())"">Toggle collapse</BitButton>
    <BitButton OnClick=""@(() => splitterRef.ResetSize())"">Reset</BitButton>
    <BitButton OnClick=""@(() => splitterRef.FocusAsync())"">Focus the gutter</BitButton>
    <BitButton OnClick=""@(async () => measured = await splitterRef.GetPercent())"">Measure</BitButton>
</BitStack>

<BitSplitter @ref=""splitterRef"" Collapsible CollapsedSize=""8"" FirstPanelSize=""180""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<div>@(measured is null ? ""Nothing has been measured yet."" : $""The first panel takes up {measured:F1}% of the splitter."")</div>";
    private readonly string example12CsharpCode = @"
private double? measured;
private BitSplitter splitterRef = default!;";

    private readonly string example13RazorCode = @"
<BitSplitter PersistKey=""demo-splitter"" Collapsible CollapsedSize=""8"" FirstPanelSize=""150""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Where you left it</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example14RazorCode = @"
<BitSplitter ReadOnly FirstPanelSize=""150"" Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Read-only</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">The gutter stays where it is.</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter IsEnabled=""false"" FirstPanelSize=""150"" Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Disabled</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">The whole splitter is dimmed.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example15RazorCode = @"
<BitSplitter FirstPanelSize=""160"" Style=""height:250px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the sidebar"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Sidebar</div>
    </FirstPanel>
    <SecondPanel>
        <BitSplitter Vertical AriaLabel=""Resize the preview"">
            <FirstPanel>
                <div style=""padding:0.5rem"">List</div>
            </FirstPanel>
            <SecondPanel>
                <div style=""padding:0.5rem"">Preview</div>
            </SecondPanel>
        </BitSplitter>
    </SecondPanel>
</BitSplitter>";

    private readonly string example16RazorCode = @"
<BitSplitter Collapsible KeyboardStep=""50"" FirstPanelMinSize=""120"" SecondPanelMinSize=""80""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the navigation"">
    <FirstPanel>
        <div style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Outline"">In the folding panel</BitButton>
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">One arrow key moves the gutter 50px.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""@splitterParams"">
    <BitSplitter Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
        <FirstPanel>
            <div style=""padding:0.5rem"">From the cascade</div>
        </FirstPanel>
        <SecondPanel>
            <div style=""padding:0.5rem"">Second panel</div>
        </SecondPanel>
    </BitSplitter>
    <br />
    <BitSplitter ShowCollapseButton=""false"" Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
        <FirstPanel>
            <div style=""padding:0.5rem"">No collapse button of its own</div>
        </FirstPanel>
        <SecondPanel>
            <div style=""padding:0.5rem"">Second panel</div>
        </SecondPanel>
    </BitSplitter>
</BitParams>

<BitSplitter Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Outside the cascade</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example17CsharpCode = @"
private readonly BitSplitterParams[] splitterParams =
[
    new()
    {
        GutterSize = 12,
        Collapsible = true,
        ShowCollapseButton = true,
        CollapsedSize = 8,
        FirstPanelSize = 160,
    }
];";

    private readonly string example18RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSplitter GutterSize=""16"" GutterIcon=""@(""fa-solid fa-arrows-left-right"")""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">""fa-solid fa-arrows-left-right""</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Css(""fa-solid fa-grip-vertical"")""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Css(""fa-solid fa-grip-vertical"")</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Fa(""solid grip-lines-vertical"")""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Fa(""solid grip-lines-vertical"")</div>
    </SecondPanel>
</BitSplitter>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSplitter GutterSize=""16"" GutterIcon=""@(""bi bi-grip-vertical"")""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">GutterIcon=@@(""bi bi-grip-vertical"")</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Bi(""arrow-left-right"")""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Bi(""arrow-left-right"")</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example19RazorCode = @"
<style>
    .custom-splitter {
        height: 120px;
        border-radius: 0.5rem;
        border: 2px solid mediumpurple;
        box-shadow: mediumpurple 0 0 0.5rem;
        background: var(--bit-clr-bg-pri);
    }

    .custom-first-panel {
        background: color-mix(in srgb, mediumpurple 15%, var(--bit-clr-bg-pri));
    }

    .custom-second-panel {
        background: var(--bit-clr-bg-pri);
    }

    .custom-gutter {
        background: mediumpurple;
    }

    .custom-gutter-indicator {
        background: white;
    }
</style>

<BitSplitter Style=""height:120px;background:var(--bit-clr-bg-pri);border:2px dashed var(--bit-clr-pri);border-radius:0.5rem""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Style</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Class=""custom-splitter"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Class</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""10""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)""
             Styles=""@(new() { FirstPanel = ""background:color-mix(in srgb, var(--bit-clr-pri) 12%, var(--bit-clr-bg-pri))"",
                               SecondPanel = ""background:var(--bit-clr-bg-pri)"",
                               Gutter = ""background:var(--bit-clr-pri)"",
                               GutterIndicator = ""background:var(--bit-clr-pri-text)"" })""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Styles</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""10""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)""
             Classes=""@(new() { FirstPanel = ""custom-first-panel"",
                                SecondPanel = ""custom-second-panel"",
                                Gutter = ""custom-gutter"",
                                GutterIndicator = ""custom-gutter-indicator"" })""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Classes</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""180""
             Style=""height:120px;border:1px solid var(--bit-clr-brd-sec);
                    --bit-Splitter-gutter-size:4px;
                    --bit-Splitter-gutter-background:#ddd6fe;
                    --bit-Splitter-gutter-hover-background:#a78bfa;
                    --bit-Splitter-gutter-active-background:#7c3aed;
                    --bit-Splitter-gutter-indicator-color:#7c3aed;
                    --bit-Splitter-collapse-button-color:#7c3aed;
                    --bit-Splitter-collapse-button-border-color:#a78bfa;
                    --bit-Splitter-collapse-button-hover-background:#7c3aed;
                    --bit-Splitter-collapse-button-radius:4px""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">One splitter's Style</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Hover and drag the gutter</div>
    </SecondPanel>
</BitSplitter>

<div style=""--bit-Splitter-gutter-size:1px;--bit-Splitter-gutter-hit-size:12px;--bit-Splitter-gutter-hover-background:var(--bit-clr-pri);--bit-Splitter-gutter-indicator-color:transparent"">
    <BitSplitter Style=""height:120px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the sidebar"">
        <FirstPanel>
            <div style=""padding:0.5rem"">Set on a wrapper,</div>
        </FirstPanel>
        <SecondPanel>
            <BitSplitter Vertical AriaLabel=""Resize the preview"">
                <FirstPanel>
                    <div style=""padding:0.5rem"">the variables reach</div>
                </FirstPanel>
                <SecondPanel>
                    <div style=""padding:0.5rem"">every splitter inside it.</div>
                </SecondPanel>
            </BitSplitter>
        </SecondPanel>
    </BitSplitter>
</div>";

    private readonly string example20RazorCode = @"
<BitSplitter Dir=""BitDir.Rtl"" FirstPanelSize=""150"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""تغییر اندازه پنل‌ها"">
    <FirstPanel>
        <div style=""padding:0.5rem"">پنل اول</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">پنل دوم</div>
    </SecondPanel>
</BitSplitter>";
}

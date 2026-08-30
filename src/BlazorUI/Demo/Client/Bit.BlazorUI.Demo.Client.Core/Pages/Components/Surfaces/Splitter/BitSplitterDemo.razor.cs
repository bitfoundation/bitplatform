namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Splitter;

public partial class BitSplitterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitSplitterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitSplitter.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CollapseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the collapse button while the panel that folds is open, using BitIconInfo for external icon library support. Takes precedence over CollapseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CollapseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon shown on the collapse button while the panel that folds is open. The default is a chevron pointing at the panel that is about to be folded away, which follows the orientation of the splitter, which panel folds and the writing direction of the page.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Collapsed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the panel that folds - the first one, or the second where CollapseSecondPanel says so - is currently collapsed. It can be bound, so a collapse the user carries out on the gutter is reported back to the page. A collapsed panel keeps its content in the DOM and is folded down to CollapsedSize, ignoring the minimum size it would otherwise hold, while the panel left standing takes the whole splitter.",
        },
        new()
        {
            Name = "CollapsedSize",
            Type = "int",
            DefaultValue = "0",
            Description = "The size, in pixels, the folded panel is held at while it is collapsed.",
        },
        new()
        {
            Name = "Collapsible",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a panel be collapsed - the first one, or the second where CollapseSecondPanel says so: pressing Enter on the gutter folds it away and opens it again, so does Ctrl with an arrow key and the control ShowCollapseButton draws, dragging the gutter close enough to that panel's own edge of the splitter snaps it shut, and Collapse/Expand/ToggleCollapse do the same from code.",
        },
        new()
        {
            Name = "CollapseSecondPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Folds the second panel away rather than the first one, which is what an inspector, a properties pane or a preview sitting at the far end of the splitter needs. It moves what Collapsible offers to the other side of the gutter and nothing else: Collapsed, CollapsedSize, SnapSize, the keys, the collapse control and the Collapse/Expand/ToggleCollapse methods all go on meaning the one panel that folds, and Percent still describes the first panel.",
        },
        new()
        {
            Name = "DragStep",
            Type = "int",
            DefaultValue = "0",
            Description = "The grid, in pixels, a drag of the gutter moves the split along: the first panel comes to rest on a multiple of this rather than wherever the pointer happens to be. The keyboard is held to the same multiples. The default of 0 is no grid at all.",
        },
        new()
        {
            Name = "ExpandIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the collapse button while the panel that folds is away, using BitIconInfo for external icon library support. Takes precedence over ExpandIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ExpandIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon shown on the collapse button while the panel that folds is away. The default is a chevron pointing at the room the panel is about to come back into.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "FirstPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the first panel.",
        },
        new()
        {
            Name = "FirstPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The initial size of the first panel in pixels. From the first drag on, the split is held as a percentage in Percent, which takes precedence over this and over SecondPanelSize.",
        },
        new()
        {
            Name = "FirstPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of first panel in pixels.",
        },
        new()
        {
            Name = "FirstPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of first panel in pixels.",
        },
        new()
        {
            Name = "GutterHitSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The smallest strip, in pixels, a pointer has to land in to take hold of the gutter. A gutter drawn narrower than this still answers a press that lands within the strip, which reaches past what is drawn evenly on both sides without taking any room from the panels. The default is 24, the target size WCAG asks for, and 44 wherever the pointer is a coarse one.",
        },
        new()
        {
            Name = "GutterIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the BitSplitter gutter using BitIconInfo for external icon library support. Takes precedence over GutterIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GutterIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon to render in the BitSplitter gutter. Ignored when GutterIcon is also set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "GutterSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of BitSplitter gutter in pixels.",
        },
        new()
        {
            Name = "GutterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the gutter, in place of the icon or of the default grip indicator. The gutter is the separator itself, so what goes in here is decoration rather than a control.",
        },
        new()
        {
            Name = "KeyboardStep",
            Type = "int",
            DefaultValue = "10",
            Description = "How far, in pixels, one press of an arrow key on the gutter moves the split. Page Up and Page Down, and an arrow key held with Shift, move it ten of these steps at a time; Home and End take it all the way to the smallest and the largest size the panels allow.",
        },
        new()
        {
            Name = "LazyResize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves a line rather than the panels while the gutter is being dragged, and puts the panels where it was left only once the drag is over. It is what makes a panel holding a table of thousands of rows, an editor or a chart draggable: the cost of the drag becomes a single layout at its end.",
        },
        new()
        {
            Name = "NoResetOnDoubleClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the gutter from resetting the splitter to the sizes its parameters declare when it is double-clicked.",
        },
        new()
        {
            Name = "OnCollapsedChange",
            Type = "EventCallback<bool>",
            Description = "The callback invoked when the panel that folds is collapsed or expanded.",
        },
        new()
        {
            Name = "OnCollapsing",
            Type = "EventCallback<BitSplitterCollapseArgs>",
            Description = "The callback invoked before the panel that folds is collapsed or expanded, with what is about to happen and what asked for it. Set Cancel on the arguments to leave the panel as it is. The callback is awaited, and nothing else folds the panel while it is running.",
            LinkType = LinkType.Link,
            Href = "#collapse-args",
        },
        new()
        {
            Name = "OnGutterDoubleClick",
            Type = "EventCallback",
            Description = "The callback invoked when the gutter is double-clicked, whether or not the double-click also resets the splitter.",
        },
        new()
        {
            Name = "OnResize",
            Type = "EventCallback<double>",
            Description = "The callback invoked continuously while the gutter is being dragged, with the new share of the splitter the first panel takes up, as a percentage. It is coalesced to one call per animation frame, and a splitter with no handler for it makes no interop call at all while it is being dragged.",
        },
        new()
        {
            Name = "OnResizeCancel",
            Type = "EventCallback<double>",
            Description = "The callback invoked when a resize is abandoned rather than finished - by Escape, or by the browser taking the pointer away - with the share of the splitter the first panel is put back to. Exactly one of this and OnResizeEnd follows every OnResizeStart.",
        },
        new()
        {
            Name = "OnResizeEnd",
            Type = "EventCallback<double>",
            Description = "The callback invoked when a resize has finished, with the share of the splitter the first panel ended up taking, as a percentage.",
        },
        new()
        {
            Name = "OnResizeStart",
            Type = "EventCallback<double>",
            Description = "The callback invoked when a resize starts, with the share of the splitter the first panel takes up at that moment, as a percentage.",
        },
        new()
        {
            Name = "PersistKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "The key the splitter remembers its position under, so that a reader who has moved the gutter finds it where they left it the next time the page is opened. Both the position and whether the first panel was folded away are kept, and what is restored is offered to the component the way a drag is. The key has to be unique to the splitter within the origin.",
        },
        new()
        {
            Name = "PersistInSessionStorage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps what PersistKey remembers in the browser's session storage rather than its local storage, so the position lasts as long as the tab and no longer.",
        },
        new()
        {
            Name = "Percent",
            Type = "double?",
            DefaultValue = "null",
            Description = "The share of the splitter the first panel takes up, as a percentage between 0 and 100. It survives the container being resized and can be bound, so every drag, key press and collapse is reported back to the page. While it has a value it takes precedence over FirstPanelSize and SecondPanelSize.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the splitter as it is: the gutter is still shown and still looks like itself, but it cannot be dragged or moved from the keyboard.",
        },
        new()
        {
            Name = "SecondPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the second panel.",
        },
        new()
        {
            Name = "SecondPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The initial size of the second panel in pixels. Ignored while Percent has a value, which is the case from the first drag on.",
        },
        new()
        {
            Name = "SecondPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of second panel in pixels.",
        },
        new()
        {
            Name = "SecondPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of second panel in pixels.",
        },
        new()
        {
            Name = "ShowCollapseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a control on the gutter that folds the collapsible panel away and brings it back, so that a reader working with the pointer can see that the panel folds at all. Only a Collapsible splitter has one. It is the pointer's way to what the gutter already does from the keyboard, so it stays out of the tab order and out of the accessibility tree.",
        },
        new()
        {
            Name = "SnapSize",
            Type = "int",
            DefaultValue = "0",
            Description = "How small, in pixels, a drag has to leave the panel that folds for it to snap shut instead of staying open. Only a Collapsible splitter snaps at all. The default of 0 leaves the splitter to work it out from the minimum size of the panel: half of it, or a twentieth of the splitter where there is no minimum.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSplitterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitSplitter.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the orientation of BitSplitter to vertical, stacking the two panels instead of placing them side by side.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Collapse",
            Type = "Task",
            Description = "Collapses the panel that folds. Does nothing if it is already collapsed. Not turned away by Collapsible, which is about what the reader may do to the gutter.",
        },
        new()
        {
            Name = "Expand",
            Type = "Task",
            Description = "Expands the panel that folds, putting the split back where it was before the fold.",
        },
        new()
        {
            Name = "ToggleCollapse",
            Type = "Task",
            Description = "Collapses the panel that folds if it is expanded and expands it if it is collapsed.",
        },
        new()
        {
            Name = "SetPercent",
            Type = "Task",
            Description = "Moves the split so that the first panel takes up the given share of the splitter, as a percentage between 0 and 100. The value is still held to the minimum and maximum sizes of both panels.",
        },
        new()
        {
            Name = "GetPercent",
            Type = "ValueTask<double?>",
            Description = "Reads the share of the splitter the first panel takes up at this moment, as a percentage, measured off the panels themselves rather than read back from Percent. It is the only way to a split nobody has moved yet, which Percent does not hold until the first drag. A splitter whose JavaScript side is not up, and one laid out in a box with no room in it, answer null.",
        },
        new()
        {
            Name = "ResetSize",
            Type = "Task",
            Description = "Clears Percent and hands the layout back to FirstPanelSize and SecondPanelSize - which is what a double-click on the gutter does.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives the focus to the gutter, which is the control a splitter is driven by. The overload taking a bool prevents the gutter from being scrolled into view.",
        },
    ];



    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "collapse-reason-enum",
            Name = "BitSplitterCollapseReason",
            Description = "What made the collapsible panel of a BitSplitter collapse or expand.",
            Items =
            [
                new()
                {
                    Name = "Gutter",
                    Description = "The gutter was pressed, or moved by the Enter key or Ctrl with an arrow key.",
                    Value = "0",
                },
                new()
                {
                    Name = "Drag",
                    Description = "The gutter was dragged close enough to the panel's own edge of the splitter for it to snap shut.",
                    Value = "1",
                },
                new()
                {
                    Name = "Method",
                    Description = "The Collapse, Expand or ToggleCollapse method of the splitter was called.",
                    Value = "2",
                },
                new()
                {
                    Name = "Restore",
                    Description = "The position the splitter had remembered under its PersistKey was restored.",
                    Value = "3",
                }
            ]
        },
    ];



    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "collapse-args",
            Title = "BitSplitterCollapseArgs",
            Parameters =
            [
                new()
                {
                    Name = "IsCollapsing",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "The state the panel that folds is about to move to: true while it is being folded away, false while it is being brought back."
                },
                new()
                {
                    Name = "Reason",
                    Type = "BitSplitterCollapseReason",
                    DefaultValue = "",
                    Description = "What made the panel collapse or expand: the gutter, a drag that snapped it shut, a call to one of the Collapse, Expand and ToggleCollapse methods, or the remembered position being restored.",
                    LinkType = LinkType.Link,
                    Href = "#collapse-reason-enum",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the collapse or the expansion and leave the panel as it is."
                }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitSplitterClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the root element of the BitSplitter."
                },
                new()
                {
                    Name = "CollapseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the control on the gutter that folds the first panel of the BitSplitter away and brings it back."
                },
                new()
                {
                    Name = "CollapseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the icon of the control on the gutter that folds the first panel of the BitSplitter away and brings it back."
                },
                new()
                {
                    Name = "FirstPanel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the first panel of the BitSplitter."
                },
                new()
                {
                    Name = "Gutter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the gutter (the separator) of the BitSplitter."
                },
                new()
                {
                    Name = "GutterIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the icon rendered inside the gutter of the BitSplitter."
                },
                new()
                {
                    Name = "GutterIndicator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the default grip indicator rendered inside the gutter of the BitSplitter."
                },
                new()
                {
                    Name = "Preview",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the line a lazy drag moves in place of the panels of the BitSplitter."
                },
                new()
                {
                    Name = "SecondPanel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the second panel of the BitSplitter."
                },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
    ];



    private double? percent = 30;
    private double PercentValue { get => percent ?? 50; set => percent = value; }
    private bool isCollapsed;
    private double dragStep = 50;
    private double gutterSize = 10;
    private bool allowCollapse = true;
    private string resizeLog = "No resize yet.";
    private string collapseLog = "Nothing has been folded yet.";
    private double? measured;
    private BitSplitter splitterRef = default!;



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

    private readonly string example3RazorCode = @"
<BitSplitter FirstPanelSize=""150"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A first panel that starts at 150px</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">A second panel that takes the rest</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter SecondPanelSize=""150"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A first panel that takes the rest</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">A second panel that starts at 150px</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example4RazorCode = @"
<BitSplitter FirstPanelSize=""200"" FirstPanelMinSize=""120"" FirstPanelMaxSize=""320"" SecondPanelMinSize=""100""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Never narrower than 120px, never wider than 320px.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Never narrower than 100px, whatever the first panel does.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example5RazorCode = @"
<BitSlider Label=""@($""Percent: {PercentValue:F0}%"")"" @bind-Value=""PercentValue"" Min=""0"" Max=""100"" />

<BitSplitter @bind-Percent=""percent"" FirstPanelMinSize=""60"" SecondPanelMinSize=""60""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example5CsharpCode = @"
private double? percent = 30;
private double PercentValue { get => percent ?? 50; set => percent = value; }";

    private readonly string example6RazorCode = @"
<BitSlider Label=""@($""Drag step: {dragStep:F0}px"")"" @bind-Value=""dragStep"" Max=""100"" Step=""10"" />

<BitSplitter DragStep=""@((int)dragStep)"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A panel that stops every @dragStep pixels</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example6CsharpCode = @"
private double dragStep = 50;";

    private readonly string example7RazorCode = @"
<BitSplitter LazyResize FirstPanelMinSize=""80"" SecondPanelMinSize=""80""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Drag the gutter: the line moves first, the panels follow when you let go.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example8RazorCode = @"
<BitSplitter KeyboardStep=""50"" FirstPanelMinSize=""80"" SecondPanelMinSize=""80""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">One arrow key moves the gutter 50px.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Shift, Page Up and Page Down move it 500px.</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter NoResetOnDoubleClick FirstPanelSize=""150""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Double-clicking the gutter</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">leaves this splitter where the reader put it.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example9RazorCode = @"
<BitSplitter Collapsible CollapsedSize=""8"" @bind-Collapsed=""isCollapsed"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A panel that can be folded away</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Collapsed: @isCollapsed</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible CollapsedSize=""8"" SnapSize=""40"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Drag the gutter to the first 40px to fold this away.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible CollapseSecondPanel ShowCollapseButton CollapsedSize=""8"" SecondPanelSize=""180""
             SecondPanelMinSize=""120"" FirstPanelMinSize=""100""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A document that takes the whole splitter once the inspector is away</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">An inspector that folds to the right</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example9CsharpCode = @"
private bool isCollapsed;";

    private readonly string example10RazorCode = @"
<BitSplitter Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             Style=""height:180px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Press the chevron to fold this away.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Vertical Collapsible ShowCollapseButton CollapsedSize=""8"" FirstPanelSize=""70""
             Style=""height:220px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A stacked splitter folds upwards.</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Collapsible ShowCollapseButton GutterSize=""16"" CollapsedSize=""8"" FirstPanelSize=""180""
             CollapseIconName=""@BitIconName.ClosePane"" ExpandIconName=""@BitIconName.OpenPane""
             Style=""height:180px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Icons of the page's own choosing</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">CollapseIconName and ExpandIconName</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example11RazorCode = @"
<BitToggle Label=""Allow the panel to be folded away"" @bind-Value=""allowCollapse"" />

<BitSplitter Collapsible CollapsedSize=""8"" FirstPanelSize=""180"" FirstPanelMinSize=""120""
             OnCollapsing=""@(args => { args.Cancel = args.IsCollapsing && allowCollapse is false;
                                       collapseLog = $""{args.Reason} asked to {(args.IsCollapsing ? ""collapse"" : ""expand"")}: {(args.Cancel ? ""refused"" : ""allowed"")}""; })""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A panel that folds away only with permission</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<div>@collapseLog</div>";
    private readonly string example11CsharpCode = @"
private bool allowCollapse = true;
private string collapseLog = ""Nothing has been folded yet."";";

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
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
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
<BitSplitter FirstPanelMinSize=""60"" SecondPanelMinSize=""60""
             Collapsible CollapsedSize=""8""
             OnResizeStart=""@(p => resizeLog = $""Started at {p:F1}%"")""
             OnResize=""@(p => resizeLog = $""Resizing: {p:F1}%"")""
             OnResizeEnd=""@(p => resizeLog = $""Ended at {p:F1}%"")""
             OnResizeCancel=""@(p => resizeLog = $""Cancelled, back at {p:F1}%"")""
             OnCollapsedChange=""@(c => resizeLog = c ? ""Collapsed"" : ""Expanded"")""
             OnGutterDoubleClick=""@(() => resizeLog = ""The gutter was double-clicked"")""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<div>@resizeLog</div>";
    private readonly string example13CsharpCode = @"
private string resizeLog = ""No resize yet."";";

    private readonly string example14RazorCode = @"
<BitSplitter PersistKey=""demo-splitter"" Collapsible CollapsedSize=""8"" FirstPanelSize=""150""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A panel that is where you left it</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example15RazorCode = @"
<BitSplitter ReadOnly FirstPanelSize=""150"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Read-only</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">The gutter stays where it is.</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter IsEnabled=""false"" FirstPanelSize=""150"" Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Disabled</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">The whole splitter is dimmed.</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example16RazorCode = @"
<BitSplitter Style=""height:250px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">The first panel of the outer splitter</div>
    </FirstPanel>
    <SecondPanel>
        <BitSplitter Vertical AriaLabel=""Resize the panels"">
            <FirstPanel>
                <div style=""padding:0.5rem"">The first panel of the nested splitter</div>
            </FirstPanel>
            <SecondPanel>
                <div style=""padding:0.5rem"">The second panel of the nested splitter</div>
            </SecondPanel>
        </BitSplitter>
    </SecondPanel>
</BitSplitter>";

    private readonly string example17RazorCode = @"
<BitSlider Label=""@($""Gutter size: {gutterSize:F0}px"")"" @bind-Value=""gutterSize"" Max=""50"" />

<BitSplitter GutterSize=""@((int)gutterSize)"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example17CsharpCode = @"
private double gutterSize = 10;";

    private readonly string example18RazorCode = @"
<BitSplitter GutterSize=""16"" GutterIconName=""@BitIconName.GripperDotsVertical""
             Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example19RazorCode = @"
<BitSplitter GutterSize=""14"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <GutterTemplate>
        <div style=""display:flex;flex-direction:column;gap:2px"">
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
            <div style=""width:4px;height:4px;border-radius:50%;background:var(--bit-clr-fg-sec)""></div>
        </div>
    </GutterTemplate>
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSplitter GutterSize=""16"" GutterIcon=""@(""fa-solid fa-arrows-left-right"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">""fa-solid fa-arrows-left-right""</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Css(""fa-solid fa-grip-vertical"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Css(""fa-solid fa-grip-vertical"")</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Fa(""solid grip-lines-vertical"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Fa(""solid grip-lines-vertical"")</div>
    </SecondPanel>
</BitSplitter>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSplitter GutterSize=""16"" GutterIcon=""@(""bi bi-grip-vertical"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">GutterIcon=@@(""bi bi-grip-vertical"")</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""16"" GutterIcon=""@BitIconInfo.Bi(""arrow-left-right"")""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">First panel</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">BitIconInfo.Bi(""arrow-left-right"")</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example21RazorCode = @"
<style>
    .custom-splitter {
        height: 150px;
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

<BitSplitter Style=""height:150px;background:var(--bit-clr-bg-pri);border:2px dashed var(--bit-clr-pri);border-radius:0.5rem""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A splitter with a Style of its own</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter Class=""custom-splitter"" AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">A splitter with a Class of its own</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">Second panel</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""10""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)""
             Styles=""@(new() { FirstPanel = ""background:color-mix(in srgb, var(--bit-clr-pri) 12%, var(--bit-clr-bg-pri))"",
                               SecondPanel = ""background:var(--bit-clr-bg-pri)"",
                               Gutter = ""background:var(--bit-clr-pri)"",
                               GutterIndicator = ""background:var(--bit-clr-pri-text)"" })""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Panels and gutter painted</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">through the Styles slots</div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterSize=""10""
             Style=""height:150px;border:1px solid var(--bit-clr-brd-sec)""
             Classes=""@(new() { FirstPanel = ""custom-first-panel"",
                                SecondPanel = ""custom-second-panel"",
                                Gutter = ""custom-gutter"",
                                GutterIndicator = ""custom-gutter-indicator"" })""
             AriaLabel=""Resize the panels"">
    <FirstPanel>
        <div style=""padding:0.5rem"">Panels and gutter painted</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">through the Classes slots</div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example22RazorCode = @"
<BitSplitter Dir=""BitDir.Rtl"" FirstPanelSize=""150"" Style=""height:200px;border:1px solid var(--bit-clr-brd-sec)"" AriaLabel=""تغییر اندازه پنل‌ها"">
    <FirstPanel>
        <div style=""padding:0.5rem"">پنل اول</div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding:0.5rem"">پنل دوم</div>
    </SecondPanel>
</BitSplitter>";
}

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
            Description = "Whether the panel that folds is collapsed; bindable. A collapsed panel keeps its content in the DOM, drops to CollapsedSize past its minimum, and is made inert when that size is 0. Expanding restores the previous split.",
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
            Description = "Lets the reader fold a panel away from the gutter: Enter, Ctrl + arrow, the collapse button, or a drag that snaps it shut at its edge.",
        },
        new()
        {
            Name = "CollapseSecondPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Folds the second panel instead of the first - an inspector or a preview at the far end. Everything collapse-related follows it; Percent still describes the first panel.",
        },
        new()
        {
            Name = "DefaultPercent",
            Type = "double?",
            DefaultValue = "null",
            Description = "The share of the splitter, 0 to 100, the first panel starts at and is reset to, while the reader stays free to move it. Wins over the panel sizes; Percent wins over it.",
        },
        new()
        {
            Name = "DragStep",
            Type = "int",
            DefaultValue = "0",
            Description = "A pixel grid the split lands on, for the pointer and the keyboard alike. 0 is no grid.",
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
            Description = "The max size of the first panel in pixels.",
        },
        new()
        {
            Name = "FirstPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of the first panel in pixels.",
        },
        new()
        {
            Name = "GutterHitSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The smallest strip, in pixels, a press takes hold of the gutter in; it reaches past a thinner gutter without taking room from the panels. Unset, 24 (the WCAG target size), and 44 for a coarse pointer.",
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
            Description = "How far, in pixels, an arrow key moves the gutter. Shift + arrow, Page Up and Page Down move ten steps; Home and End go to the limits.",
        },
        new()
        {
            Name = "LazyResize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Drags a line instead of the panels and lays them out once, on release - for content too heavy to lay out on every frame.",
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
            Description = "The storage key the position and the fold are remembered under, and restored from on the next visit. Unique per splitter within the origin.",
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
            Description = "The max size of the second panel in pixels.",
        },
        new()
        {
            Name = "SecondPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of the second panel in pixels.",
        },
        new()
        {
            Name = "ShowCollapseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a fold/unfold button on the gutter of a Collapsible splitter. It is kept out of the tab order, since the gutter's keys do the same.",
        },
        new()
        {
            Name = "SnapSize",
            Type = "int",
            DefaultValue = "0",
            Description = "How close, in pixels, to its edge a drag must leave the folding panel for it to snap shut. 0 uses half its minimum size, or a twentieth of the splitter without one.",
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
            Description = "Measures the first panel's current share of the splitter - including a split nobody has moved yet, which Percent does not hold. Null before the splitter is set up or when it has no room.",
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
                    Description = "The custom CSS class/style for the collapse button on the gutter of the BitSplitter."
                },
                new()
                {
                    Name = "CollapseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom CSS class/style for the icon of the collapse button of the BitSplitter."
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Splitter-gutter-size",
            DefaultValue = "spacing(1.25)",
            Description = "Thickness of the gutter. GutterSize wins over it.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-hit-size",
            DefaultValue = "spacing(3), spacing(5.5) for a coarse pointer",
            Description = "The smallest strip a press takes hold of the gutter in, reaching past a thinner gutter without taking room from the panels. GutterHitSize wins over it.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-background",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "The gutter at rest, and while it cannot be moved.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-hover-background",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "The gutter under the pointer.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-active-background",
            DefaultValue = "The hover background",
            Description = "The gutter while it is being dragged.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-indicator-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "The default grip drawn on the gutter.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-icon-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "The icon GutterIcon or GutterIconName draws on the gutter.",
        },
        new()
        {
            Name = "--bit-Splitter-gutter-icon-size",
            DefaultValue = "--bit-tg-fs-xs",
            Description = "Size of the icon drawn on the gutter.",
        },
        new()
        {
            Name = "--bit-Splitter-preview-background",
            DefaultValue = "--bit-clr-pri",
            Description = "The line a LazyResize drag moves in place of the panels.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "The chevron of the collapse button.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "The collapse button at rest.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "The outline of the collapse button at rest.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-hover-color",
            DefaultValue = "--bit-clr-pri-text",
            Description = "The chevron of the collapse button under the pointer.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-hover-background",
            DefaultValue = "--bit-clr-pri",
            Description = "The collapse button and its outline under the pointer.",
        },
        new()
        {
            Name = "--bit-Splitter-collapse-button-radius",
            DefaultValue = "--bit-shp-radius-full",
            Description = "Corner radius of the collapse button.",
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
    private double? evenPercent;
    private double? measured;
    private BitSplitter splitterRef = default!;

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
    ];
}

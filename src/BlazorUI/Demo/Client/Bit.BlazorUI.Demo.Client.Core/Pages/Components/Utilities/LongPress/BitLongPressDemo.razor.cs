namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.LongPress;

public partial class BitLongPressDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content the press is held on.",
        },
        new()
        {
            Name = "Delay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The time in milliseconds a press has to be held before it is a long press. Defaults to 500.",
        },
        new()
        {
            Name = "KeyboardTrigger",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the long press a tab stop whose context-menu keys - Shift+F10 and the Menu key - raise OnLongPress, at the middle of the element that has the focus and with a PointerType of \"keyboard\". The keys are named in aria-keyshortcuts, and are left alone on a field being typed in.",
        },
        new()
        {
            Name = "MoveTolerance",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "How far in pixels the pointer may move while it is held before the press is called off. Defaults to 10.",
        },
        new()
        {
            Name = "OnLongPress",
            Type = "EventCallback<BitLongPressEventArgs>",
            DefaultValue = "",
            Description = "The callback for when a press has been held for the Delay, or the context-menu keys of KeyboardTrigger are pressed.",
            LinkType = LinkType.Link,
            Href = "#long-press-event-args",
        },
        new()
        {
            Name = "TouchOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the mouse out: only a touch or a pen is held, and a mouse button held down does nothing.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "long-press-event-args",
            Title = "BitLongPressEventArgs",
            Parameters =
            [
                new() { Name = "ClientX", Type = "double", DefaultValue = "", Description = "The horizontal point the press was held at, in pixels relative to the viewport: where the pointer went down, or the middle of the focused element for the keyboard. It is what a menu opened at a point is given (BitContextMenu.OpenAt)." },
                new() { Name = "ClientY", Type = "double", DefaultValue = "", Description = "The vertical point the press was held at, in pixels relative to the viewport." },
                new() { Name = "PointerType", Type = "string", DefaultValue = "", Description = "What held the press: \"touch\", \"pen\", \"mouse\", or \"keyboard\" for the keys of KeyboardTrigger." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-LongPress-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focus ring color of a KeyboardTrigger long press." },
    ];



    private int basicCount;
    private string? basicLast;

    private void HandleBasicLongPress(BitLongPressEventArgs e)
    {
        basicCount++;
        basicLast = $"{e.PointerType} at {e.ClientX:0}, {e.ClientY:0}";
    }

    private BitContextMenu<BitMenuButtonItem> contextMenu = default!;
    private string? menuClicked;

    private static readonly List<BitMenuButtonItem> menuItems =
    [
        new() { Text = "Share", Key = "share", IconName = BitIconName.Share },
        new() { Text = "Rename", Key = "rename", IconName = BitIconName.Rename },
        new() { IsSeparator = true },
        new() { Text = "Delete", Key = "delete", IconName = BitIconName.Delete },
    ];

    private int clickClicks;
    private int clickLongPresses;
    private string? clickLog;

    private void HandleClick() => clickLog = $"Clicked ({++clickClicks})";

    private void HandleClickLongPress() => clickLog = $"Long-pressed ({++clickLongPresses})";

    private int delayCount;
    private string? delayLog;

    private void HandleSlowLongPress() => delayLog = $"Held for 1.5 seconds ({++delayCount})";

    private void HandleTolerantLongPress() => delayLog = $"Held within 40px ({++delayCount})";

    private string? touchLog;

    private string? keyboardLog;

    private void HandleKeyboardLongPress(BitLongPressEventArgs e) => keyboardLog = $"{e.PointerType} at {e.ClientX:0}, {e.ClientY:0}";

    private int styleCount;
}

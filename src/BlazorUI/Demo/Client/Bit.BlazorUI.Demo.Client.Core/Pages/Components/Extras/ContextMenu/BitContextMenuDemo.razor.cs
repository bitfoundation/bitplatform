namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ContextMenu;

public partial class BitContextMenuDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the area the menu is opened over.",
        },
        new()
        {
            Name = "CloseOnItemClick",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the menu closes once one of its items is clicked.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the menu is open. Two-way bindable.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "[]",
            Description = "The items of the menu.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem?>?",
            DefaultValue = "null",
            Description = "The template the items of the menu are rendered with.",
        },
        new()
        {
            Name = "MenuClasses",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the menu. Those of the parts of the menu button's own button do nothing here, since the button is never shown.",
            LinkType = LinkType.Link,
            Href = "/components/menubutton#class-styles",
        },
        new()
        {
            Name = "MenuStyles",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the menu. Those of the parts of the menu button's own button do nothing here, since the button is never shown.",
            LinkType = LinkType.Link,
            Href = "/components/menubutton#class-styles",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitMenuButtonNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names the members of a custom item class to read the text, icon, children and the rest of an item from.",
            LinkType = LinkType.Link,
            Href = "/components/menubutton#name-selectors",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "The callback for when an item of the menu is clicked.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The items of the menu, declared as BitMenuButtonOption components.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "OpenAt",
            Type = "(double x, double y) => Task",
            DefaultValue = "",
            Description = "Opens the menu at the given point of the viewport, in CSS pixels, or moves it there when it is open already. It is still placed to stay on the screen, and the focus is taken into it.",
        },
        new()
        {
            Name = "OpenAt",
            Type = "(MouseEventArgs e) => Task",
            DefaultValue = "",
            Description = "Opens the menu at the point of a mouse event (its clientX and clientY), or moves it there when it is open already.",
        },
    ];



    private string? basicClicked;
    private string? optionClicked;
    private bool showGrid = true;
    private string? customClicked;

    private string? currentFile;
    private (string? Action, string? File)? fileAction;

    private BitContextMenu<BitMenuButtonItem> boundContextMenu = default!;
    private bool isBoundOpen;

    private static readonly List<BitMenuButtonItem> basicItems =
    [
        new() { Text = "Cut", Key = "cut", IconName = BitIconName.Cut },
        new() { Text = "Copy", Key = "copy", IconName = BitIconName.Copy },
        new() { Text = "Paste", Key = "paste", IconName = BitIconName.Paste },
        new() { IsSeparator = true },
        new() { Text = "Delete", Key = "delete", IconName = BitIconName.Delete }
    ];

    private static readonly List<ContextMenuOperation> customItems =
    [
        new() { Name = "Undo", Icon = BitIconName.Undo },
        new() { Name = "Redo", Icon = BitIconName.Redo },
        new() { Name = "Select all", Icon = BitIconName.SelectAll }
    ];

    private static readonly BitMenuButtonNameSelectors<ContextMenuOperation> customNameSelectors = new()
    {
        Text = { Name = nameof(ContextMenuOperation.Name) },
        IconName = { Name = nameof(ContextMenuOperation.Icon) },
    };

    private static readonly List<string> files = ["report.pdf", "budget.xlsx", "photo.jpg"];

    private static readonly List<BitMenuButtonItem> fileItems =
    [
        new() { Text = "Open", Key = "open", IconName = BitIconName.OpenFile },
        new() { Text = "Rename", Key = "rename", IconName = BitIconName.Rename },
        new() { Text = "Delete", Key = "delete", IconName = BitIconName.Delete }
    ];

    private static readonly List<BitMenuButtonItem> rtlItems =
    [
        new() { Text = "برش", Key = "cut", IconName = BitIconName.Cut },
        new() { Text = "رونوشت", Key = "copy", IconName = BitIconName.Copy },
        new() { Text = "چسباندن", Key = "paste", IconName = BitIconName.Paste }
    ];
}

public class ContextMenuOperation
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ContextMenu;

public partial class BitContextMenuDemo
{
    private readonly string example1RazorCode = @"
<BitContextMenu Items=""basicItems"" OnItemClick=""(BitMenuButtonItem item) => basicClicked = item.Text"">
    <div class=""context-area"">Right-click here</div>
</BitContextMenu>

<div>Clicked item: <b>@basicClicked</b></div>";
    private readonly string example1CsharpCode = @"
private string? basicClicked;

private static readonly List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Cut"", Key = ""cut"", IconName = BitIconName.Cut },
    new() { Text = ""Copy"", Key = ""copy"", IconName = BitIconName.Copy },
    new() { Text = ""Paste"", Key = ""paste"", IconName = BitIconName.Paste },
    new() { IsSeparator = true },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete }
];";

    private readonly string example2RazorCode = @"
<BitContextMenu TItem=""BitMenuButtonOption"" OnItemClick=""(BitMenuButtonOption item) => optionClicked = item.Text"">
    <ChildContent>
        <div class=""context-area"">Right-click here</div>
    </ChildContent>
    <Options>
        <BitMenuButtonOption Text=""Cut"" Key=""cut"" IconName=""@BitIconName.Cut"" />
        <BitMenuButtonOption Text=""Copy"" Key=""copy"" IconName=""@BitIconName.Copy"" />
        <BitMenuButtonOption Text=""Paste"" Key=""paste"" IconName=""@BitIconName.Paste"" />
        <BitMenuButtonOption IsSeparator />
        <BitMenuButtonOption Text=""Share"" Key=""share"" IconName=""@BitIconName.Share"">
            <BitMenuButtonOption Text=""Email"" Key=""email"" IconName=""@BitIconName.Mail"" />
            <BitMenuButtonOption Text=""Link"" Key=""link"" IconName=""@BitIconName.Link"" />
        </BitMenuButtonOption>
        <BitMenuButtonOption IsSeparator />
        <BitMenuButtonOption Text=""Show grid"" Key=""grid"" Checkable @bind-IsChecked=""showGrid"" />
    </Options>
</BitContextMenu>

<div>Clicked item: <b>@optionClicked</b>, show grid: <b>@showGrid</b></div>";
    private readonly string example2CsharpCode = @"
private string? optionClicked;
private bool showGrid = true;";

    private readonly string example3RazorCode = @"
<BitContextMenu Items=""customItems""
                NameSelectors=""customNameSelectors""
                OnItemClick=""(ContextMenuOperation item) => customClicked = item.Name"">
    <div class=""context-area"">Right-click here</div>
</BitContextMenu>

<div>Clicked item: <b>@customClicked</b></div>";
    private readonly string example3CsharpCode = @"
public class ContextMenuOperation
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private string? customClicked;

private static readonly List<ContextMenuOperation> customItems =
[
    new() { Name = ""Undo"", Icon = BitIconName.Undo },
    new() { Name = ""Redo"", Icon = BitIconName.Redo },
    new() { Name = ""Select all"", Icon = BitIconName.SelectAll }
];

private static readonly BitMenuButtonNameSelectors<ContextMenuOperation> customNameSelectors = new()
{
    Text = { Name = nameof(ContextMenuOperation.Name) },
    IconName = { Name = nameof(ContextMenuOperation.Icon) },
};";

    private readonly string example4RazorCode = @"
<BitContextMenu Items=""fileItems"" OnItemClick=""(BitMenuButtonItem item) => fileAction = (item.Text, currentFile)"">
    <div class=""file-list"">
        @foreach (var file in files)
        {
            <div class=""file-row @(currentFile == file ? ""current"" : null)"" tabindex=""0""
                 @onfocus=""() => currentFile = file"" @oncontextmenu=""() => currentFile = file"">
                @file
            </div>
        }
    </div>
</BitContextMenu>

<div>Last action: <b>@(fileAction is { } a ? $""{a.Action} {a.File}"" : null)</b></div>";
    private readonly string example4CsharpCode = @"
private string? currentFile;
private (string? Action, string? File)? fileAction;

private static readonly List<string> files = [""report.pdf"", ""budget.xlsx"", ""photo.jpg""];

private static readonly List<BitMenuButtonItem> fileItems =
[
    new() { Text = ""Open"", Key = ""open"", IconName = BitIconName.OpenFile },
    new() { Text = ""Rename"", Key = ""rename"", IconName = BitIconName.Rename },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete }
];";

    private readonly string example5RazorCode = @"
<BitContextMenu @ref=""boundContextMenu"" Items=""basicItems"" @bind-IsOpen=""isBoundOpen"">
    <div class=""context-area"">Right-click here</div>
</BitContextMenu>

<BitButton OnClick=""() => boundContextMenu.OpenAt(24, 24)"">Open at (24, 24)</BitButton>
<BitText>IsOpen: <b>@isBoundOpen</b></BitText>";
    private readonly string example5CsharpCode = @"
private BitContextMenu<BitMenuButtonItem> boundContextMenu = default!;
private bool isBoundOpen;

private static readonly List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Cut"", Key = ""cut"", IconName = BitIconName.Cut },
    new() { Text = ""Copy"", Key = ""copy"", IconName = BitIconName.Copy },
    new() { Text = ""Paste"", Key = ""paste"", IconName = BitIconName.Paste },
    new() { IsSeparator = true },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete }
];";

    private readonly string example6RazorCode = @"
<BitContextMenu Items=""basicItems"" Disabled>
    <div class=""context-area"">Right-click here</div>
</BitContextMenu>";

    private readonly string example7RazorCode = @"
<style>
    .custom-callout {
        border-radius: 0.75rem;
    }
</style>

<BitContextMenu Items=""basicItems""
                AriaLabel=""Clipboard""
                Style=""border-radius: 1rem; background-color: var(--bit-clr-bg-sec);""
                MenuStyles=""@(new() { CalloutContainer = ""min-width: 12rem;"", ItemText = ""color: tomato;"" })""
                MenuClasses=""@(new() { Callout = ""custom-callout"" })"">
    <div class=""context-area"">Right-click here</div>
</BitContextMenu>";
    private readonly string example7CsharpCode = @"
private static readonly List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Cut"", Key = ""cut"", IconName = BitIconName.Cut },
    new() { Text = ""Copy"", Key = ""copy"", IconName = BitIconName.Copy },
    new() { Text = ""Paste"", Key = ""paste"", IconName = BitIconName.Paste },
    new() { IsSeparator = true },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete }
];";

    private readonly string example8RazorCode = @"
<div dir=""rtl"">
    <BitContextMenu Dir=""BitDir.Rtl"" Items=""rtlItems"">
        <div class=""context-area"">اینجا کلیک راست کنید</div>
    </BitContextMenu>
</div>";
}

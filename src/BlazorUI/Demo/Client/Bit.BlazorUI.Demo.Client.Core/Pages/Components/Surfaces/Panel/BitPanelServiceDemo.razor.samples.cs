namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Panel;

public partial class BitPanelServiceDemo
{
    private const string panelContentCode = @"
<BitStack Gap=""1rem"">
    <BitText>This content reaches its own panel through the cascaded BitPanelReference.</BitText>
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""() => panelReference.Close()"">Ok</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => panelReference.Dismiss()"">Cancel</BitButton>
    </BitStack>
</BitStack>

@code {
    [CascadingParameter] private BitPanelReference panelReference { get; set; } = default!;
}";

    private const string pickPanelContentCode = @"
<BitStack Gap=""0.5rem"">
    @foreach (var size in sizes)
    {
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => panelReference.CloseWith(size)"">@size</BitButton>
    }
</BitStack>

@code {
    private static readonly string[] sizes = [""Small"", ""Medium"", ""Large""];

    [CascadingParameter] private BitPanelReference panelReference { get; set; } = default!;
}";

    private readonly string example1RazorCode = @"
@* in the layout *@
<BitPanelContainer />

<BitButton OnClick=""ShowPanel"">Show</BitButton>";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private async Task ShowPanel()
{
    await panelService.Show<PanelContent>(new BitPanelParameters { HeaderText = ""Shown by the service"", ShowCloseButton = true });
}";
    private readonly DemoCodeFile[] example1CodeFiles = [new("PanelContent.razor", panelContentCode)];

    private readonly string example2RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => ShowAt(BitPlacement.Start)"">Start</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => ShowAt(BitPlacement.End)"">End</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => ShowAt(BitPlacement.Top)"">Top</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => ShowAt(BitPlacement.Bottom)"">Bottom</BitButton>";
    private readonly string example2CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private async Task ShowAt(BitPlacement placement)
{
    await panelService.Show<PanelContent>(new BitPanelParameters
    {
        Placement = placement,
        Size = 320,
        HeaderText = $""From the {placement.ToString().ToLowerInvariant()}"",
        ShowCloseButton = true,
    });
}";
    private readonly DemoCodeFile[] example2CodeFiles = [new("PanelContent.razor", panelContentCode)];

    private readonly string example3RazorCode = @"
<BitButton OnClick=""ShowSheet"">Show sheet</BitButton>";
    private readonly string example3CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private async Task ShowSheet()
{
    await panelService.Show<PanelContent>(new BitPanelParameters
    {
        Placement = BitPlacement.Bottom,
        SnapPoints = [0.3, 0.6, 0.9],
        SnapPoint = 1,
        HeaderText = ""A bottom sheet"",
    });
}";
    private readonly DemoCodeFile[] example3CodeFiles = [new("PanelContent.razor", panelContentCode)];

    private readonly string example4RazorCode = @"
<BitButton OnClick=""PickSize"">Pick a size</BitButton>
<span>Picked: <b>@pickedSize</b></span>";
    private readonly string example4CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private string? pickedSize;

private async Task PickSize()
{
    var panel = await panelService.Show<PickPanelContent>(new BitPanelParameters { HeaderText = ""Pick a size"", ShowCloseButton = true });

    var size = await panel.GetResult<string>();

    pickedSize = panel.IsDismissed ? ""(dismissed)"" : size;
}";
    private readonly DemoCodeFile[] example4CodeFiles = [new("PickPanelContent.razor", pickPanelContentCode)];

    private readonly string example5RazorCode = @"
<BitButton OnClick=""ShowMarkup"">Show markup</BitButton>

@code {
    // a template taking the panel's own reference, so the markup can close it
    private async Task ShowMarkup()
    {
        await panelService.Show(panel => @<BitButton OnClick=""() => panel.Close()"">Got it</BitButton>,
                                new BitPanelParameters { HeaderText = ""Markup that closes itself"" });
    }
}";
    private readonly string example5CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;";

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""hasUnsavedChanges"" Label=""Has unsaved changes"" />
<BitButton OnClick=""ShowGuarded"">Show</BitButton>";
    private readonly string example6CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private bool hasUnsavedChanges = true;

private async Task ShowGuarded()
{
    await panelService.Show<PanelContent>(new BitPanelParameters
    {
        HeaderText = ""Guarded"",
        ShowCloseButton = true,
        CanClose = () => Task.FromResult(hasUnsavedChanges is false),
    });
}";
    private readonly DemoCodeFile[] example6CodeFiles = [new("PanelContent.razor", panelContentCode)];

    private readonly string example7RazorCode = @"
<BitButton OnClick=""ShowCountdown"">Show</BitButton>";
    private readonly string example7CsharpCode = @"
[AutoInject] private BitPanelService panelService = default!;

private async Task ShowCountdown()
{
    var panel = await panelService.Show<PanelContent>(new BitPanelParameters { HeaderText = ""Closing in 5"", ShowCloseButton = true });

    for (var left = 4; left >= 0 && panel.IsClosed is false; left--)
    {
        await Task.Delay(1000);

        if (left == 0)
        {
            await panel.Close();
        }
        else
        {
            await panel.Update(p => p.HeaderText = $""Closing in {left}"");
        }
    }
}";
    private readonly DemoCodeFile[] example7CodeFiles = [new("PanelContent.razor", panelContentCode)];
}

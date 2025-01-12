namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.SwipeTrap;

public partial class BitSwipeTrapDemo
{

    private readonly string example1RazorCode = @"
<style>
    .basic-container {
        width: 100%;
        cursor: grab;
        height: 500px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<BitSwipeTrap Throttle=""10""
              OnStart=""HandleOnStartBasic""
              OnMove=""HandleOnMoveBasic""
              OnEnd=""HandleOnEndBasic""
              OnTrigger=""HandleOnTriggerBasic"">
    <div class=""basic-container"">
        <div>StartX: @swipeTrapEventArgs?.StartX</div>
        <div>StartY: @swipeTrapEventArgs?.StartY</div>
        <div>DiffX: @swipeTrapEventArgs?.DiffX</div>
        <div>DiffY: @swipeTrapEventArgs?.DiffY</div>
        <div>---</div>
        <div>Triggered? @isTriggered</div>
        <div>Trigger direction: <b>@swipeTrapTriggerArgs?.Direction</b></div>
        <div>Trigger diffX: @swipeTrapTriggerArgs?.DiffX</div>
        <div>Trigger diffY: @swipeTrapTriggerArgs?.DiffY</div>
    </div>
</BitSwipeTrap>";
    private readonly string example1CsharpCode = @"
private bool isTriggeredBasic;
BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;
private void HandleOnStartBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnMoveBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnEndBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnTriggerBasic(BitSwipeTrapTriggerArgs args)
{
    isTriggeredBasic = true;
    swipeTrapTriggerArgsBasic = args;
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        isTriggeredBasic = false;
        swipeTrapEventArgsBasic = null;
        swipeTrapTriggerArgsBasic = null;
        InvokeAsync(StateHasChanged);
    });
}";

    private readonly string example2RazorCode = @"
<style>
    .panel-container {
        width: 100%;
        height: 300px;
        overflow: hidden;
        user-select: none;
        position: relative;
        border: 1px solid lightgray;
    }

    .panel-container button {
        padding: 0.5rem;
    }

    .panel-container .panel {
        left: 0;
        color: black;
        width: 200px;
        cursor: grab;
        inset-block: 0;
        position: absolute;
        background-color: lightgray;
        transform: translateX(-100%);
    }

    .panel-container .panel.open {
        transform: translateX(0);
    }

    .panel-container .panel-trap {
        gap: 1rem;
        height: 100%;
        display: flex;
        flex-direction: column;
        background-color: gray;
    }
</style>

<div class=""panel-container"">
    <button @onclick=""OpenPanel"">
        Open
    </button>
    <div class=""panel@(isPanelOpen ? "" open"": """")"" style=""@GetPanelStyle()"">
        <button @onclick=""ClosePanel"" style=""position:absolute;top:0;right:0"">
            Close
        </button>
        <BitSwipeTrap Style=""width:100%;height:100%""
                      OnMove=""HandleOnMovePanel""
                      OnEnd=""HandleOnEndPanel""
                      OnTrigger=""HandleOnTriggerPanel"">
            <div class=""panel-trap"">
                <h3>Title</h3>
                <div>Item1</div>
                <div>Item2</div>
                <div>Item3</div>
            </div>
        </BitSwipeTrap>
    </div>
</div>";
    private readonly string example2CsharpCode = @"
private decimal diffXPanel;
private bool isPanelOpen;
private void OpenPanel()
{
    isPanelOpen = true;
}
private void ClosePanel()
{
    isPanelOpen = false;
}
private void HandleOnMovePanel(BitSwipeTrapEventArgs args)
{
    diffXPanel = args.DiffX;
}
private void HandleOnEndPanel(BitSwipeTrapEventArgs args)
{
    diffXPanel = 0;
}
private void HandleOnTriggerPanel(BitSwipeTrapTriggerArgs args)
{
    if (args.Direction == BitSwipeDirection.Left)
    {
        diffXPanel = 0;
        ClosePanel();
    }
}
private string GetPanelStyle()
{
    return diffXPanel < 0 ? $""transform: translateX({diffXPanel}px)"" : """";
}";

    private readonly string example3RazorCode = @"
<style>
    .list-container {
        gap: 4px;
        width: 100%;
        color: black;
        height: 300px;
        display: flex;
        overflow-y: auto;
        user-select: none;
        overflow-x: hidden;
        position: relative;
        flex-direction: column;
        border: 1px solid lightgray;
    }

    .list-container .row {
        min-height: 40px;
        position: relative;
    }

    .list-container .delete {
        width: 60px;
        color: white;
        height: 100%;
        padding: 4px;
        position: absolute;
        background-color: red;
    }

    .list-container .row-trap {
        width: 100%;
        height: 100%;
        cursor: grab;
        padding: 4px;
        position: absolute;
        background-color: gray;
    }
</style>

<div class=""list-container"">
    @foreach (int idx in itemsList)
    {
        var i = idx;
        <div @key=""@i"" class=""row"">
            <div class=""delete"">Delete</div>
            <BitSwipeTrap Style=""width:100%;height:100%""
                          Trigger=""60m""
                          Threshold=""10""
                          OnMove=""args => HandleOnMoveList(args, i)""
                          OnEnd=""args => HandleOnEndList(args, i)""
                          OnTrigger=""args => HandleOnTriggerList(args, i)"">
                <div class=""row-trap"" style=""@GetRowStyle(i)"">
                    <div>Item@(i + 1)</div>
                </div>
            </BitSwipeTrap>
        </div>
    }
</div>
<BitButton OnClick=""ResetList"">Reset</BitButton>
<BitDialog @bind-IsOpen=""isListDialogOpen""
           Title=""Delete item?""
           Message=""Are you sure you want to delete this item?""
           OnOk=""HandleOnOkList""
           OnCancel=""HandleOnCancelList"" />";
    private readonly string example3CsharpCode = @"
private int deletingIndex = -1;
private bool isListDialogOpen;
private TaskCompletionSource listTcs;
private List<int> itemsList = Enumerable.Range(0, 10).ToList();
private decimal[] diffXList = Enumerable.Repeat(0m, 10).ToArray();
private void HandleOnMoveList(BitSwipeTrapEventArgs args, int index)
{
    diffXList[index] = args.DiffX;
}
private void HandleOnEndList(BitSwipeTrapEventArgs args, int index)
{
    if (diffXList[index] < 60)
    {
        diffXList[index] = 0;
    }
}
private async Task HandleOnTriggerList(BitSwipeTrapTriggerArgs args, int index)
{
    if (args.Direction == BitSwipeDirection.Right)
    {
        deletingIndex = index;
        listTcs = new();
        isListDialogOpen = true;
        await listTcs.Task;
        isListDialogOpen = false;
        diffXList[index] = 0;
        deletingIndex = -1;
    }
}
private string GetRowStyle(int index)
{
    var x = Math.Min(diffXList[index], 60);
    return x > 0 ? $""transform: translateX({x}px)"" : """";
}
private void HandleOnOkList()
{
    if (deletingIndex != -1)
    {
        itemsList.Remove(deletingIndex);
    }
    listTcs.SetResult();
}
private void HandleOnCancelList()
{
    listTcs.SetResult();
}
private void ResetList()
{
    itemsList = Enumerable.Range(0, 10).ToList();
}";

    private readonly string example4RazorCode = @"
<style>
    .mobile-frame {
        height: 500px;
        max-width: 375px;
        overflow: hidden;
        position: relative;
        border-radius: 36px;
        border: 16px solid #333;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        background-color: var(--bit-clr-fg-sec);
    }

    .mobile-frame .screen {
        width: 100%;
        height: 100%;
        overflow: auto;
    }

    .mobile-frame .layout {
        height: 100%;
        display: flex;
        flex-direction: column;
    }

    .mobile-frame .header {
        gap: 1rem;
        width: 100%;
        height: 66px;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: var(--bit-clr-bg-sec);
    }

    .mobile-frame .main {
        flex-grow: 1;
        position: relative;
    }

    .mobile-frame .main-text {
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .mobile-frame .panel {
        color: black;
        cursor: grab;
        inset-block: 0;
        user-select: none;
        position: absolute;
        background-color: lightgray;

    .mobile-frame .panel.left {
        left: 0;
        width: 200px;
        transform: translateX(-100%);
    }

    .mobile-frame .panel.right {
        right: 0;
        width: 200px;
        transform: translateX(100%);
    }

    .mobile-frame .panel-trap {
        gap: 1rem;
        height: 100%;
        display: flex;
        padding-top: 0.2rem;
        padding-left: 0.8rem;
        flex-direction: column;
        background-color: gray;
    }
</style>

<div class=""mobile-frame"">
    <div class=""screen"">
        <div class=""layout"">
            <div class=""header"">
                <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                    bit BlazorUI
                </BitText>
            </div>
            <div class=""main"">
                <BitSwipeTrap Style=""width:100%;height:100%""
                              OnMove=""HandleOnMovePanelAdvanced""
                              OnEnd=""HandleOnEndPanelAdvanced""
                              OnTrigger=""HandleOnTriggerPanelAdvanced"">
                    <div class=""main-text"">
                        <BitText Style=""user-select:none""
                                 Typography=""BitTypography.H4""
                                 Color=""BitColor.SecondaryBackground"">
                            Swipe left or right
                        </BitText>
                    </div
                    <div class=""panel left"" style=""@GetLeftPanelAdvancedStyle()"">
                        <div class=""panel-trap"">
                            <h3>Left Menu</h3>
                            <div>Item1</div>
                            <div>Item2</div>
                            <div>Item3</div>
                        </div>
                    </div>
                    <div class=""panel right"" style=""@GetRightPanelAdvancedStyle()"">
                        <div class=""panel-trap"">
                            <h3>Right Menu</h3>
                            <div>Item1</div>
                            <div>Item2</div>
                            <div>Item3</div>
                        </div>
                    </div>
                </BitSwipeTrap>
            </div>
        </div>
    </div>
</div>";
    private readonly string example4CsharpCode = @"
private decimal? diffXPanelAdvanced;
private BitSwipeDirection? direction;
private BitSwipeDirection? panelOpen;
private void OpenPanelAdvanced(BitSwipeDirection swipeDirection)
{
    if (panelOpen == swipeDirection) return;

    direction = null;
    panelOpen = swipeDirection;
    diffXPanelAdvanced = 0;
}
private void ClosePanelAdvanced()
{
    panelOpen = null;
    diffXPanelAdvanced = null;
}
private void HandleOnMovePanelAdvanced(BitSwipeTrapEventArgs args)
{
    diffXPanelAdvanced = args.DiffX;

    if (Math.Abs(args.DiffX) > 2 || Math.Abs(args.DiffY) > 2)
    {
        direction = Math.Abs(args.DiffX) > Math.Abs(args.DiffY)
        ? args.DiffX > 0 ? BitSwipeDirection.Right : BitSwipeDirection.Left
        : args.DiffY > 0 ? BitSwipeDirection.Bottom : BitSwipeDirection.Top;
    }
    else
    {
        direction = null;
    }
}
private void HandleOnEndPanelAdvanced(BitSwipeTrapEventArgs args)
{
    if (panelOpen.HasValue)
    {
        diffXPanelAdvanced = 0;
    }
    else
    {
        diffXPanelAdvanced = null;
    }
}
private void HandleOnTriggerPanelAdvanced(BitSwipeTrapTriggerArgs args)
{
    if (args.Direction == BitSwipeDirection.Left)
    {
        if (panelOpen.HasValue is false || panelOpen == BitSwipeDirection.Right)
        {
            OpenPanelAdvanced(BitSwipeDirection.Right);
        }
        else if (panelOpen == BitSwipeDirection.Left)
        {
            ClosePanelAdvanced();
        }
    }
    else if (args.Direction == BitSwipeDirection.Right)
    {
        if (panelOpen.HasValue is false || panelOpen == BitSwipeDirection.Left)
        {
            OpenPanelAdvanced(BitSwipeDirection.Left);
        }
        else if (panelOpen == BitSwipeDirection.Right)
        {
            ClosePanelAdvanced();
        }
    }
}
private string GetLeftPanelAdvancedStyle()
{
    if (panelOpen == BitSwipeDirection.Left && direction != BitSwipeDirection.Left)
    {
        return ""transform: translateX(0px)"";
    }
    else if((panelOpen.HasValue is false && direction == BitSwipeDirection.Right) || (panelOpen == BitSwipeDirection.Left && direction == BitSwipeDirection.Left))
    {
        return diffXPanelAdvanced switch
        {
            0 or > 200 => ""transform: translateX(0px)"",
            < 0 and < 200 => $""transform: translateX({diffXPanelAdvanced}px)"",
            > 0 => $""transform: translateX(calc(-100% + {diffXPanelAdvanced}px))"",
            _ => string.Empty
        };
    }

    return string.Empty;
}
private string GetRightPanelAdvancedStyle()
{
    if (panelOpen == BitSwipeDirection.Right && direction != BitSwipeDirection.Right)
    {
        return ""transform: translateX(0px)"";
    }
    else if ((panelOpen.HasValue is false && direction == BitSwipeDirection.Left) || (panelOpen == BitSwipeDirection.Right && direction == BitSwipeDirection.Right))
    {
        return diffXPanelAdvanced switch
        {
            0 or < -200 => ""transform: translateX(0px)"",
            > 0 => $""transform: translateX({diffXPanelAdvanced}px)"",
            < 0 => $""transform: translateX(calc(100% - {(-1 * diffXPanelAdvanced)}px))"",
            _ => string.Empty
        };
    }

    return string.Empty;
}";
}


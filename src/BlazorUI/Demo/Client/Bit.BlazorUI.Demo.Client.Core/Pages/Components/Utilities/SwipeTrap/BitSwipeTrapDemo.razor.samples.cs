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

    .bit-stp-swp .basic-container {
        cursor: grabbing;
    }
</style>

<BitSwipeTrap Style=""width:100%""
              OnStart=""HandleOnStartBasic""
              OnMove=""HandleOnMoveBasic""
              OnEnd=""HandleOnEndBasic""
              OnTrigger=""HandleOnTriggerBasic"">
    <div class=""basic-container"">
        <div>StartX: @swipeTrapEventArgsBasic?.StartX</div>
        <div>StartY: @swipeTrapEventArgsBasic?.StartY</div>
        <div>DiffX: @swipeTrapEventArgsBasic?.DiffX</div>
        <div>DiffY: @swipeTrapEventArgsBasic?.DiffY</div>
        <div>VelocityX: @swipeTrapEventArgsBasic?.VelocityX.ToString(""0.00"")</div>
        <div>VelocityY: @swipeTrapEventArgsBasic?.VelocityY.ToString(""0.00"")</div>
        <div>Duration: @swipeTrapEventArgsBasic?.Duration.ToString(""0"") ms</div>
        <div>PointerType: @swipeTrapEventArgsBasic?.PointerType</div>
        <div>IsCanceled: @swipeTrapEventArgsBasic?.IsCanceled</div>
        <div>---</div>
        <div>Triggered? @isTriggeredBasic</div>
        <div>Trigger direction: <b>@swipeTrapTriggerArgsBasic?.Direction</b></div>
        <div>Trigger diffX: @swipeTrapTriggerArgsBasic?.DiffX</div>
        <div>Trigger diffY: @swipeTrapTriggerArgsBasic?.DiffY</div>
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
    _ = Task.Delay(3000).ContinueWith(async _ =>
    {
        isTriggeredBasic = false;
        swipeTrapEventArgsBasic = null;
        swipeTrapTriggerArgsBasic = null;
        await InvokeAsync(StateHasChanged);
    });
}";

    private readonly string example2RazorCode = @"
<style>
    .lock-boxes {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }

    .lock-container {
        width: 250px;
        cursor: grab;
        height: 250px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<div class=""lock-boxes"">
    <BitSwipeTrap Trigger=""0.5m"" OnTrigger=""HandleOnTriggerFractional"">
        <div class=""lock-container"">
            <div><b>Trigger=""0.5m""</b></div>
            <div>(half of the container)</div>
            <div>Direction: <b>@triggerArgsFractional?.Direction</b></div>
            <div>DiffX: @triggerArgsFractional?.DiffX</div>
            <div>DiffY: @triggerArgsFractional?.DiffY</div>
        </div>
    </BitSwipeTrap>
    <BitSwipeTrap Trigger=""80m"" OnTrigger=""HandleOnTriggerAbsolute"">
        <div class=""lock-container"">
            <div><b>Trigger=""80m""</b></div>
            <div>(80 pixels)</div>
            <div>Direction: <b>@triggerArgsAbsolute?.Direction</b></div>
            <div>DiffX: @triggerArgsAbsolute?.DiffX</div>
            <div>DiffY: @triggerArgsAbsolute?.DiffY</div>
        </div>
    </BitSwipeTrap>
</div>";
    private readonly string example2CsharpCode = @"
private BitSwipeTrapTriggerArgs? triggerArgsFractional;
private BitSwipeTrapTriggerArgs? triggerArgsAbsolute;
private void HandleOnTriggerFractional(BitSwipeTrapTriggerArgs args)
{
    triggerArgsFractional = args;
}
private void HandleOnTriggerAbsolute(BitSwipeTrapTriggerArgs args)
{
    triggerArgsAbsolute = args;
}";

    private readonly string example3RazorCode = @"
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

    .bit-stp-swp .basic-container {
        cursor: grabbing;
    }
</style>

<BitSwipeTrap Style=""width:100%""
              Trigger=""0.9m""
              TriggerVelocity=""0.5m""
              OnTrigger=""HandleOnTriggerFlick"">
    <div class=""basic-container"">
        <div>Flicked? @isFlicked</div>
        <div>Flick direction: <b>@swipeTrapTriggerArgsFlick?.Direction</b></div>
        <div>Flick velocityX: @swipeTrapTriggerArgsFlick?.VelocityX.ToString(""0.00"")</div>
        <div>Flick velocityY: @swipeTrapTriggerArgsFlick?.VelocityY.ToString(""0.00"")</div>
        <div>Flick duration: @swipeTrapTriggerArgsFlick?.Duration.ToString(""0"") ms</div>
    </div>
</BitSwipeTrap>";
    private readonly string example3CsharpCode = @"
private bool isFlicked;
private BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsFlick;
private void HandleOnTriggerFlick(BitSwipeTrapTriggerArgs args)
{
    isFlicked = true;
    swipeTrapTriggerArgsFlick = args;
    _ = Task.Delay(3000).ContinueWith(async _ =>
    {
        isFlicked = false;
        swipeTrapTriggerArgsFlick = null;
        await InvokeAsync(StateHasChanged);
    });
}";

    private readonly string example4RazorCode = @"
<style>
    .lock-boxes {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }

    .lock-container {
        width: 250px;
        cursor: grab;
        height: 250px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<div class=""lock-boxes"">
    <BitSwipeTrap OrientationLock=""BitSwipeOrientation.Horizontal""
                  OnMove=""HandleOnMoveHorizontalLock""
                  OnEnd=""HandleOnEndHorizontalLock"">
        <div class=""lock-container"">
            <div><b>Horizontal lock</b></div>
            <div>DiffX: @diffXHorizontalLock</div>
            <div>DiffY: @diffYHorizontalLock</div>
        </div>
    </BitSwipeTrap>
    <BitSwipeTrap OrientationLock=""BitSwipeOrientation.Vertical""
                  OnMove=""HandleOnMoveVerticalLock""
                  OnEnd=""HandleOnEndVerticalLock"">
        <div class=""lock-container"">
            <div><b>Vertical lock</b></div>
            <div>DiffX: @diffXVerticalLock</div>
            <div>DiffY: @diffYVerticalLock</div>
        </div>
    </BitSwipeTrap>
    <BitSwipeTrap OrientationLock=""BitSwipeOrientation.Auto""
                  OnMove=""HandleOnMoveAutoLock""
                  OnEnd=""HandleOnEndAutoLock"">
        <div class=""lock-container"">
            <div><b>Auto lock</b></div>
            <div>DiffX: @diffXAutoLock</div>
            <div>DiffY: @diffYAutoLock</div>
        </div>
    </BitSwipeTrap>
</div>";
    private readonly string example4CsharpCode = @"
private decimal diffXHorizontalLock;
private decimal diffYHorizontalLock;
private decimal diffXVerticalLock;
private decimal diffYVerticalLock;
private decimal diffXAutoLock;
private decimal diffYAutoLock;
private void HandleOnMoveHorizontalLock(BitSwipeTrapEventArgs args)
{
    diffXHorizontalLock = args.DiffX;
    diffYHorizontalLock = args.DiffY;
}
private void HandleOnEndHorizontalLock(BitSwipeTrapEventArgs args)
{
    diffXHorizontalLock = 0;
    diffYHorizontalLock = 0;
}
private void HandleOnMoveVerticalLock(BitSwipeTrapEventArgs args)
{
    diffXVerticalLock = args.DiffX;
    diffYVerticalLock = args.DiffY;
}
private void HandleOnEndVerticalLock(BitSwipeTrapEventArgs args)
{
    diffXVerticalLock = 0;
    diffYVerticalLock = 0;
}
private void HandleOnMoveAutoLock(BitSwipeTrapEventArgs args)
{
    diffXAutoLock = args.DiffX;
    diffYAutoLock = args.DiffY;
}
private void HandleOnEndAutoLock(BitSwipeTrapEventArgs args)
{
    diffXAutoLock = 0;
    diffYAutoLock = 0;
}";

    private readonly string example5RazorCode = @"
<style>
    .lock-boxes {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }

    .lock-container {
        width: 250px;
        cursor: grab;
        height: 250px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<div class=""lock-boxes"">
    <BitSwipeTrap Threshold=""30""
                  OnMove=""HandleOnMoveThreshold""
                  OnEnd=""HandleOnEndThreshold"">
        <div class=""lock-container"">
            <div><b>Threshold=""30""</b></div>
            <div>(the first 30px are free)</div>
            <div>DiffX: @diffXThreshold</div>
            <div>DiffY: @diffYThreshold</div>
        </div>
    </BitSwipeTrap>
    <BitSwipeTrap Throttle=""200""
                  OnMove=""HandleOnMoveThrottle""
                  OnEnd=""HandleOnEndThrottle"">
        <div class=""lock-container"">
            <div><b>Throttle=""200""</b></div>
            <div>(at most one move per 200ms)</div>
            <div>Moves: @moveCountThrottle</div>
            <div>DiffX: @diffXThrottle</div>
            <div>DiffY: @diffYThrottle</div>
        </div>
    </BitSwipeTrap>
</div>";
    private readonly string example5CsharpCode = @"
private decimal diffXThreshold;
private decimal diffYThreshold;
private int moveCountThrottle;
private decimal diffXThrottle;
private decimal diffYThrottle;
private void HandleOnMoveThreshold(BitSwipeTrapEventArgs args)
{
    diffXThreshold = args.DiffX;
    diffYThreshold = args.DiffY;
}
private void HandleOnEndThreshold(BitSwipeTrapEventArgs args)
{
    diffXThreshold = 0;
    diffYThreshold = 0;
}
private void HandleOnMoveThrottle(BitSwipeTrapEventArgs args)
{
    moveCountThrottle++;
    diffXThrottle = args.DiffX;
    diffYThrottle = args.DiffY;
}
private void HandleOnEndThrottle(BitSwipeTrapEventArgs args)
{
    moveCountThrottle = 0;
    diffXThrottle = 0;
    diffYThrottle = 0;
}";

    private readonly string example6RazorCode = @"
<style>
    .lock-boxes {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }

    .lock-container {
        width: 250px;
        cursor: grab;
        height: 250px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<div class=""lock-boxes"">
    <BitSwipeTrap TouchOnly
                  OnMove=""HandleOnMoveTouchOnly""
                  OnEnd=""HandleOnEndTouchOnly"">
        <div class=""lock-container"">
            <div><b>TouchOnly</b></div>
            <div>(mouse drags are ignored)</div>
            <div>DiffX: @diffXTouchOnly</div>
            <div>DiffY: @diffYTouchOnly</div>
        </div>
    </BitSwipeTrap>
    <BitSwipeTrap SkipSelector="".no-swipe""
                  OnMove=""HandleOnMoveSkip""
                  OnEnd=""HandleOnEndSkip"">
        <div class=""lock-container"">
            <div><b>SkipSelector</b></div>
            <div>DiffX: @diffXSkip</div>
            <div>DiffY: @diffYSkip</div>
            <input class=""no-swipe"" placeholder=""No swipe starts here"" />
        </div>
    </BitSwipeTrap>
</div>";
    private readonly string example6CsharpCode = @"
private decimal diffXTouchOnly;
private decimal diffYTouchOnly;
private decimal diffXSkip;
private decimal diffYSkip;
private void HandleOnMoveTouchOnly(BitSwipeTrapEventArgs args)
{
    diffXTouchOnly = args.DiffX;
    diffYTouchOnly = args.DiffY;
}
private void HandleOnEndTouchOnly(BitSwipeTrapEventArgs args)
{
    diffXTouchOnly = 0;
    diffYTouchOnly = 0;
}
private void HandleOnMoveSkip(BitSwipeTrapEventArgs args)
{
    diffXSkip = args.DiffX;
    diffYSkip = args.DiffY;
}
private void HandleOnEndSkip(BitSwipeTrapEventArgs args)
{
    diffXSkip = 0;
    diffYSkip = 0;
}";

    private readonly string example7RazorCode = @"
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
    private readonly string example7CsharpCode = @"
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

    private readonly string example8RazorCode = @"
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
                          OrientationLock=""BitSwipeOrientation.Horizontal""
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
    private readonly string example8CsharpCode = @"
private int deletingIndex = -1;
private bool isListDialogOpen;
private TaskCompletionSource? listTcs;
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
    listTcs?.SetResult();
}
private void HandleOnCancelList()
{
    listTcs?.SetResult();
}
private void ResetList()
{
    itemsList = Enumerable.Range(0, 10).ToList();
}";

    private readonly string example9RazorCode = @"
<style>
    .mobile-frame {
        height: 666px;
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
    }

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
                <BitImage Src=""/images/bit-logo.svg"" Width=""50"" />
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
                    </div>

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
    private readonly string example9CsharpCode = @"
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
        direction = Math.Abs(args.DiffX) >= Math.Abs(args.DiffY)
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

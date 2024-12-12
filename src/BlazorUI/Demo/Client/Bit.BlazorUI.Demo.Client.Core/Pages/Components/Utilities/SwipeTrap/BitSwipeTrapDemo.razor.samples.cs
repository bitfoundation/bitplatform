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
private BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
private BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;

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
    _ = Task.Delay(3000).ContinueWith(_ =>
    {
        isTriggeredBasic = false;
        swipeTrapEventArgsBasic = null;
        swipeTrapTriggerArgsBasic = null;
        StateHasChanged();
    });
}";

    private readonly string example2RazorCode = @"
<style>
    .mobile-frame {
        width: 375px;
        height: 712px;
        border: 16px solid #333;
        border-radius: 36px;
        background-color: #fff;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        position: relative;
        overflow: hidden;
    }

    .mobile-frame:before {
        left: 50%;
        z-index: 2;
        content: '';
        width: 50px;
        height: 50px;
        bottom: 10px;
        display: block;
        background: #333;
        position: absolute;
        border-radius: 50%;
        transform: translateX(-50%);
    }

    .mobile-frame:after {
        top: 10px;
        left: 50%;
        content: '';
        width: 60px;
        height: 5px;
        display: block;
        background: #333;
        position: absolute;
        border-radius: 10px;
        transform: translateX(-50%);
    }

    .screen {
        width: 100%;
        height: 100%;
        overflow: auto;
    }

    .panel-container {
        cursor: grab;
        overflow: hidden;
        position: relative;
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
        padding-top: 0.2rem;
        padding-left: 0.8rem;
        flex-direction: column;
        background-color: gray;
    }
</style>

<div class=""mobile-frame"">
    <div class=""screen"">
        <BitLayout Classes=""@(new() { MainContent = ""panel-container"" })"">
            <Header>
                <BitCard FullWidth>
                    <BitStack Horizontal HorizontalAlign=""BitAlignment.Start"" VerticalAlign=""BitAlignment.Center"">
                        <BitButton IconOnly
                                    IconName=""@BitIconName.CollapseMenu""
                                    Variant=""BitVariant.Text""
                                    Color=""BitColor.Tertiary""
                                    OnClick=""OpenPanel"" />
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
                <BitSwipeTrap Style=""width:100%;height:100%""
                                OnMove=""HandleOnMovePanel""
                                OnEnd=""HandleOnEndPanel""
                                OnTrigger=""HandleOnTriggerPanel"">
                    <BitStack HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.SecondaryBackground"">
                            bit BlazorUI
                        </BitText>
                    </BitStack>
                </BitSwipeTrap>
                <div class=""panel@(isPanelOpen ? "" open"": """")"" style=""@GetPanelStyle()"">
                    <BitButton IconOnly
                                IconName=""@BitIconName.ChromeClose""
                                Variant=""BitVariant.Text""
                                Color=""BitColor.Tertiary""
                                OnClick=""ClosePanel""
                                Style=""position:absolute;top:0;right:0"" />

                    <BitSwipeTrap Style=""width:100%;height:100%""
                                    OnMove=""HandleOnMovePanel""
                                    OnEnd=""HandleOnEndPanel""
                                    OnTrigger=""HandleOnTriggerPanel"">
                        <div class=""panel-trap"">
                            <h3>Menu</h3>
                            <div>Item1</div>
                            <div>Item2</div>
                            <div>Item3</div>
                        </div>
                    </BitSwipeTrap>
                </div>
            </Main>
        </BitLayout>
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
    else if (args.Direction == BitSwipeDirection.Right)
    {
        OpenPanel();
    }
}
private string GetPanelStyle()
{
    return diffXPanel switch
    {
        0 => string.Empty,
        < 0 => $""transform: translateX({diffXPanel}px)"",
        > 0 => $""transform: translateX(calc(-100% + {diffXPanel}px))""
    };
}";

    private readonly string example3RazorCode = @"
<style>
    .mobile-frame {
        width: 375px;
        height: 712px;
        border: 16px solid #333;
        border-radius: 36px;
        background-color: #fff;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        position: relative;
        overflow: hidden;
    }

    .mobile-frame:before {
        left: 50%;
        z-index: 2;
        content: '';
        width: 50px;
        height: 50px;
        bottom: 10px;
        display: block;
        background: #333;
        position: absolute;
        border-radius: 50%;
        transform: translateX(-50%);
    }

    .mobile-frame:after {
        top: 10px;
        left: 50%;
        content: '';
        width: 60px;
        height: 5px;
        display: block;
        background: #333;
        position: absolute;
        border-radius: 10px;
        transform: translateX(-50%);
    }

    .screen {
        width: 100%;
        height: 100%;
        overflow: auto;
    }

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

<div class=""mobile-frame"">
    <div class=""screen"">
        <BitLayout Classes=""@(new() { MainContent = ""list-container"" })"">
            <Header>
                <BitCard FullWidth>
                    <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                            bit BlazorUI
                        </BitText>
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
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

                <BitDialog @bind-IsOpen=""isListDialogOpen""
                            Title=""Delete item?""
                            Message=""Are you sure you want to delete this item?""
                            OnOk=""HandleOnOkList""
                            OnCancel=""HandleOnCancelList"" />
            </Main>
        </BitLayout>
    </div>
</div>";
    private readonly string example3CsharpCode = @"
private int deletingIndex = -1;
private bool isListDialogOpen;
private TaskCompletionSource listTcs;
private List<int> itemsList = [.. Enumerable.Range(0, 13)];
private decimal[] diffXList = [.. Enumerable.Repeat(0m, 13)];

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
    return x > 0 ? $""transform: translateX({x}px)"" : string.Empty;
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
}";
}


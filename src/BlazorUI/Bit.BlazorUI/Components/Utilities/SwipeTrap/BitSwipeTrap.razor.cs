﻿namespace Bit.BlazorUI;

public partial class BitSwipeTrap : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the swipe trap.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The event callback for when the swipe action starts on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnStart { get; set; }

    /// <summary>
    /// The event callback for when the swipe action moves on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnMove { get; set; }

    /// <summary>
    /// The event callback for when the swipe action ends on the container of the swipe trap.
    /// </summary>
    [Parameter] public EventCallback<BitSwipeTrapEventArgs> OnEnd { get; set; }

    /// <summary>
    /// The event callback for when the swipe action triggers based on the Trigger constraint.
    /// </summary>
    [Parameter] public EventCallback OnTrigger { get; set; }

    /// <summary>
    /// The threshold in pixel for swiping distance that starts the swipe process process which stops the default behavior.
    /// </summary>
    [Parameter] public decimal? Threshold { get; set; }

    /// <summary>
    /// The swiping point (difference percentage) based on the width of the element to trigger and call the OnTrigger event (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? Trigger { get; set; }



    [JSInvokable("OnStart")]
    public async Task _OnStart(decimal startX, decimal startY)
    {
        await OnStart.InvokeAsync(new(startX, startY, 0, 0));
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal startX, decimal startY, decimal diffX, decimal diffY)
    {
        await OnMove.InvokeAsync(new(startX, startY, diffX, diffY));
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal startX, decimal startY, decimal diffX, decimal diffY)
    {
        await OnEnd.InvokeAsync(new(startX, startY, diffX, diffY));
    }

    [JSInvokable("OnTrigger")]
    public async Task _OnTrigger()
    {
        await OnTrigger.InvokeAsync();
    }



    protected override string RootElementClass => "bit-stp";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitSwipeTrapSetup(UniqueId, RootElement, Trigger ?? 0.25m, Threshold ?? 0, dotnetObj);
        }

        StateHasChanged();
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        try
        {
            await _js.BitSwipeTrapDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}

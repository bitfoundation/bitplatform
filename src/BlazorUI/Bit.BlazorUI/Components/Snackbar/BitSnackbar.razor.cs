using System.Threading;
using System.Timers;

namespace Bit.BlazorUI;

public partial class BitSnackbar : IDisposable
{
    private BitSnackbarType type = BitSnackbarType.Info;
    private BitSnackbarPosition position = BitSnackbarPosition.BottomRight;

    private bool _disposed;
    private System.Timers.Timer _timer = default!;
    private int _dismissCounter = default!;
    private bool _isProgressBarFullWidth;

    private bool _isOpen;
    private string _title = default!;
    private string _body = default!;

    /// <summary>
    /// Whether or not to dismiss itself automatically.
    /// </summary>
    [Parameter] public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// How long the Snackbar will automatically dismiss (default is 3 seconds).
    /// </summary>
    [Parameter] public TimeSpan AutoDismissTime { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Used to customize how content inside the Body is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<string>? BodyTemplate { get; set; }

    /// <summary>
    /// Dismiss Icon in Snackbar.
    /// </summary>
    [Parameter] public BitIconName? DismissIconName { get; set; }

    /// <summary>
    /// Callback for when the Dismissed.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// The position of Snackbar to show.
    /// </summary>
    [Parameter]
    public BitSnackbarPosition Position
    {
        get => position;
        set
        {
            position = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The type of Snackbar to show.
    /// </summary>
    [Parameter]
    public BitSnackbarType Type
    {
        get => type;
        set
        {
            type = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Used to customize how content inside the Title is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<string>? TitleTemplate { get; set; }

    protected override string RootElementClass => "bit-snb";

    public async Task Show(string title, string body = "")
    {
        if (AutoDismiss)
        {
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        _title = title;
        _body = body;
        _isOpen = true;

        await InvokeAsync(StateHasChanged);
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => Position is BitSnackbarPosition.TopCenter ? "top-center"
                                  : Position is BitSnackbarPosition.TopRight ? "top-right"
                                  : Position is BitSnackbarPosition.TopLeft ? "top-left"
                                  : Position is BitSnackbarPosition.BottomCenter ? "bottom-center"
                                  : Position is BitSnackbarPosition.BottomRight ? "bottom-right"
                                  : Position is BitSnackbarPosition.BottomLeft ? "bottom-left"
                                  : string.Empty);

        ClassBuilder.Register(() => Type is BitSnackbarType.Info ? "info"
                                  : Type is BitSnackbarType.Success ? "success"
                                  : Type is BitSnackbarType.Warning ? "warning"
                                  : Type is BitSnackbarType.Error ? "error"
                                  : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_isOpen)
        {
            await Task.Delay(10);
            _isProgressBarFullWidth = true;
        }
        else
        {
            _isProgressBarFullWidth = false;
        }

        StateHasChanged();
    }

    private async void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _dismissCounter++;

        if (_dismissCounter == AutoDismissTime.Seconds)
        {
            await Dismiss();
        }
    }

    private async Task Dismiss()
    {
        await OnDismiss.InvokeAsync();

        if (AutoDismiss && _timer is not null)
        {
            _timer.Elapsed -= TimerElapsed;
            _timer.Stop();
            _timer.Close();
        }

        _isOpen = false;

        await InvokeAsync(StateHasChanged);
    }

    private BitIconName GetIconName() => Type is BitSnackbarType.Info ? BitIconName.Info
                                       : Type is BitSnackbarType.Success ? BitIconName.Completed
                                       : Type is BitSnackbarType.Warning ? BitIconName.Warning
                                       : Type is BitSnackbarType.Error ? BitIconName.Error
                                       : BitIconName.Info;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        if (AutoDismiss && _timer is not null)
        {
            _timer.Elapsed -= TimerElapsed;
            _timer.Stop();
            _timer.Close();
        }

        _disposed = true;
    }
}

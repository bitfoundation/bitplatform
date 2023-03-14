
using System.Reflection;
using System.Timers;

namespace Bit.BlazorUI;

public partial class BitSnackbar
{
    private BitSnackbarPosition position = BitSnackbarPosition.BottomRight;

    private List<BitSnackbarItem> _items = new();

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
    /// Used to customize how content inside the Title is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<string>? TitleTemplate { get; set; }

    protected override string RootElementClass => "bit-snb";

    public async Task Show(string title, string? body = "", BitSnackbarType? type = BitSnackbarType.Info)
    {
        var item = new BitSnackbarItem
        {
            Title = title,
            Body = body,
            Type = type
        };

        if (AutoDismiss)
        {
            item.Timerr = new System.Timers.Timer();
            item.Timerr.Interval = 1000;
            item.Timerr.Elapsed += (sender, e) => TimerElapsed(sender, e, item);
            item.Timerr.Start();
        }

        _items.Add(item);

        await InvokeAsync(StateHasChanged);
    }

    public Task Info(string title, string? body = "")
    {
        return Show(title, body, BitSnackbarType.Info);
    }

    public Task Success(string title, string? body = "")
    {
        return Show(title, body, BitSnackbarType.Success);
    }

    public Task Warning(string title, string? body = "")
    {
        return Show(title, body, BitSnackbarType.Warning);
    }

    public Task Error(string title, string? body = "")
    {
        return Show(title, body, BitSnackbarType.Error);
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() =>
            Position is BitSnackbarPosition.TopCenter ? "top-center"
            : Position is BitSnackbarPosition.TopRight ? "top-right"
            : Position is BitSnackbarPosition.TopLeft ? "top-left"
            : Position is BitSnackbarPosition.BottomCenter ? "bottom-center"
            : Position is BitSnackbarPosition.BottomRight ? "bottom-right"
            : Position is BitSnackbarPosition.BottomLeft ? "bottom-left"
            : string.Empty);

        base.RegisterComponentClasses();
    }

    private async Task Dismiss(BitSnackbarItem item)
    {
        _items.Remove(item);

        await OnDismiss.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }

    private async void TimerElapsed(object? sender, ElapsedEventArgs e, BitSnackbarItem item)
    {
        item.Counter++;

        if (item.Counter == AutoDismissTime.TotalSeconds)
        {
            item.Timerr!.Elapsed -= (sender, e) => TimerElapsed(sender, e, item);
            item.Timerr!.Stop();
            item.Timerr!.Close();
            await Dismiss(item);
        }
    }

    private static string GetItemClasses(BitSnackbarItem item)
    {
        var type = item.Type is BitSnackbarType.Info ? "info"
            : item.Type is BitSnackbarType.Success ? "success"
            : item.Type is BitSnackbarType.Warning ? "warning"
            : item.Type is BitSnackbarType.Error ? "error"
            : string.Empty;

        return $"{type}";
    }
}

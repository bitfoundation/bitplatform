
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

    public async Task Show(string body)
    {
        await Task.Run(() =>
        {
            _items.Add(new BitSnackbarItem(this)
            {
                Body = body,
            });
        });

        await InvokeAsync(StateHasChanged);
    }

    public async Task Show(string title, string body)
    {
        await Task.Run(() =>
        {
            _items.Add(new BitSnackbarItem(this)
            {
                Title = title,
                Body = body,
            });
        });

        await InvokeAsync(StateHasChanged);
    }

    public async Task Show(BitSnackbarType type, string title, string body)
    {
        await Task.Run(() =>
        {
            _items.Add(new BitSnackbarItem(this)
            {
                Type = type,
                Title = title,
                Body = body,
            });
        });

        await InvokeAsync(StateHasChanged);
    }

    internal async Task Dismiss(BitSnackbarItem item)
    {
        await OnDismiss.InvokeAsync();

        await Task.Run(() =>
        {
            _items.Remove(item);
        });

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
    }

    private static string GetItemClasses(BitSnackbarItem item)
    {
        return item.Type is BitSnackbarType.Info ? "info"
             : item.Type is BitSnackbarType.Success ? "success"
             : item.Type is BitSnackbarType.Warning ? "warning"
             : item.Type is BitSnackbarType.Error ? "error"
             : string.Empty;
    }

    private static BitIconName GetIconName(BitSnackbarItem item)
    {
        return item.Type is BitSnackbarType.Info ? BitIconName.Info
             : item.Type is BitSnackbarType.Success ? BitIconName.Completed
             : item.Type is BitSnackbarType.Warning ? BitIconName.Warning
             : item.Type is BitSnackbarType.Error ? BitIconName.Error
             : BitIconName.Info;
    }
}

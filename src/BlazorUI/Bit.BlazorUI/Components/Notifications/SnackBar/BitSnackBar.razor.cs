﻿namespace Bit.BlazorUI;

/// <summary>
/// SnackBars provide brief notifications. The component is also known as a toast.
/// </summary>
public partial class BitSnackBar : BitComponentBase
{
    private List<BitSnackBarItem> _items = [];



    /// <summary>
    /// Whether or not automatically dismiss the snack bar.
    /// </summary>
    [Parameter] public bool AutoDismiss { get; set; }

    /// <summary>
    /// How long does it take to automatically dismiss the snack bar (default is 3 seconds).
    /// </summary>
    [Parameter] public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// Used to customize how the content inside the body is rendered.
    /// </summary>
    [Parameter] public RenderFragment<string>? BodyTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the snack bar.
    /// </summary>
    [Parameter] public BitSnackBarClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon name of the dismiss button.
    /// </summary>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// Enables the multiline mode of both title and body.
    /// </summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>
    /// Callback for when any snack bar is dismissed.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Makes the snack bar non-dismissible in UI and removes the dismiss button.
    /// </summary>
    [Parameter] public bool Persistent { get; set; }

    /// <summary>
    /// The position of the snack bars to show (default is bottom right).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSnackBarPosition? Position { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the snack bar.
    /// </summary>
    [Parameter] public BitSnackBarClassStyles? Styles { get; set; }

    /// <summary>
    /// Used to customize how content inside the title is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<string>? TitleTemplate { get; set; }



    /// <summary>
    /// Shows a new snackbar with Info color.
    /// </summary>
    public Task<BitSnackBarItem> Info(string title, string? body = "", bool persistent = false) => Show(title, body, BitColor.Info, persistent: persistent);

    /// <summary>
    /// Shows a new snackbar with Success color.
    /// </summary>
    public Task<BitSnackBarItem> Success(string title, string? body = "", bool persistent = false) => Show(title, body, BitColor.Success, persistent: persistent);

    /// <summary>
    /// Shows a new snackbar with Warning color.
    /// </summary>
    public Task<BitSnackBarItem> Warning(string title, string? body = "", bool persistent = false) => Show(title, body, BitColor.Warning, persistent: persistent);

    /// <summary>
    /// Shows a new snackbar with SevereWarning color.
    /// </summary>
    public Task<BitSnackBarItem> SevereWarning(string title, string? body = "", bool persistent = false) => Show(title, body, BitColor.SevereWarning, persistent: persistent);

    /// <summary>
    /// Shows a new snackbar with Error color.
    /// </summary>
    public Task<BitSnackBarItem> Error(string title, string? body = "", bool persistent = false) => Show(title, body, BitColor.Error, persistent: persistent);

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    public Task<BitSnackBarItem> Show(string title, string? body = "", BitColor color = BitColor.Info, string? cssClass = null, string? cssStyle = null, bool persistent = false)
    {
        var item = new BitSnackBarItem
        {
            Title = title,
            Body = body,
            Color = color,
            CssClass = cssClass,
            CssStyle = cssStyle,
            Persistent = persistent
        };

        return Show(item);
    }

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    public async Task<BitSnackBarItem> Show(BitSnackBarItem item)
    {
        if (AutoDismiss && Persistent is false && item.Persistent is false)
        {
            var timer = new System.Timers.Timer((AutoDismissTime ?? TimeSpan.FromSeconds(3)).TotalMilliseconds);
            timer.Elapsed += (_, _) =>
            {
                timer.Close();
                Dismiss(item);
            };
            timer.Start();
        }

        _items.Add(item);

        await InvokeAsync(StateHasChanged);

        return item;
    }

    /// <summary>
    /// Closes a snackbar item.
    /// </summary>
    public async Task Close(BitSnackBarItem item)
    {
        Dismiss(item);
    }



    protected override string RootElementClass => "bit-snb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Position switch
        {
            BitSnackBarPosition.TopStart => "bit-snb-tst",
            BitSnackBarPosition.TopCenter => "bit-snb-tcn",
            BitSnackBarPosition.TopEnd => "bit-snb-ten",
            BitSnackBarPosition.BottomStart => "bit-snb-bst",
            BitSnackBarPosition.BottomCenter => "bit-snb-bcn",
            BitSnackBarPosition.BottomEnd => "bit-snb-ben",
            _ => "bit-snb-ben"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    private void Dismiss(BitSnackBarItem item)
    {
        if (_items.Contains(item) is false) return;

        _items.Remove(item);

        OnDismiss.InvokeAsync();

        InvokeAsync(StateHasChanged);
    }

    private string GetDuration()
    {
        return FormattableString.Invariant($"{(AutoDismissTime ?? TimeSpan.FromSeconds(3)).TotalSeconds}s");
    }

    private static string GetItemClasses(BitSnackBarItem item)
    {
        return item.Color switch
        {
            BitColor.Primary => "bit-snb-pri",
            BitColor.Secondary => "bit-snb-sec",
            BitColor.Tertiary => "bit-snb-ter",
            BitColor.Info => "bit-snb-inf",
            BitColor.Success => "bit-snb-suc",
            BitColor.Warning => "bit-snb-wrn",
            BitColor.SevereWarning => "bit-snb-swr",
            BitColor.Error => "bit-snb-err",
            BitColor.PrimaryBackground => "bit-snb-pbg",
            BitColor.SecondaryBackground => "bit-snb-sbg",
            BitColor.TertiaryBackground => "bit-snb-tbg",
            BitColor.PrimaryForeground => "bit-snb-pfg",
            BitColor.SecondaryForeground => "bit-snb-sfg",
            BitColor.TertiaryForeground => "bit-snb-tfg",
            BitColor.PrimaryBorder => "bit-snb-pbr",
            BitColor.SecondaryBorder => "bit-snb-sbr",
            BitColor.TertiaryBorder => "bit-snb-tbr",
            _ => "bit-snb-inf"
        };
    }
}

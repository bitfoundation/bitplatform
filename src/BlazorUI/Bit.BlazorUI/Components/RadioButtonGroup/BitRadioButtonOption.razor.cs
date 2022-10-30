using System.Drawing;

namespace Bit.BlazorUI;

public partial class BitRadioButtonOption : IDisposable
{
    private bool _isChecked;
    private string? _imageSizeStyle;
    public string _inputId = default!;
    public string _textId = default!;
    private bool IsCheckedHasBeenSet;

    /// <summary>
    /// Used to customize the label for the RadioButtonOption.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether or not the option is checked
    /// </summary>
    [Parameter]
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (value == _isChecked) return;
            _isChecked = value;
            ClassBuilder.Reset();
            _ = IsCheckedChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when the option IsChecked changes
    /// </summary>
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>
    /// Icon to display with this option.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Image src to display with this option.
    /// </summary>
    [Parameter] public string? ImageSrc { get; set; }

    /// <summary>
    /// Alt text if the option is an image. default is an empty string
    /// </summary>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// The width and height of the image in px for choice field.
    /// </summary>
    [Parameter] public Size? ImageSize { get; set; }

    /// <summary>
    /// This value is used to group each RadioButtonGroupOption into the same logical RadioButtonGroup
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// RadioButtonOption content, It can be a text
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The src of image for choice field which is selected.
    /// </summary>
    [Parameter] public string? SelectedImageSrc { get; set; }

    /// <summary>
    /// Value of selected RadioButtonOption
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Callback for when the RadioButtonOption clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the option has been changed
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    [CascadingParameter] protected BitRadioButtonGroup? RadioButtonGroup { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (RadioButtonGroup is not null)
        {
            if (Name.HasNoValue())
            {
                Name = RadioButtonGroup.Name;
            }

            RadioButtonGroup.RegisterOption(this);

            _inputId = $"RadioButtonGroup{RadioButtonGroup.UniqueId}-{Value}";
            _textId = $"RadioButtonGroupLabel{RadioButtonGroup.UniqueId}-{Value}";
        }

        return base.OnInitializedAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        if (ImageSize is not null)
        {
            _imageSizeStyle = $" width:{ImageSize.Value.Width}px; height:{ImageSize.Value.Height}px;";
        }

        return base.OnParametersSetAsync();
    }

    protected override string RootElementClass => "bit-rbo";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => (ImageSrc.HasValue() || IconName.HasValue) && ChildContent is null
                                 ? $"{RootElementClass}-with-img-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsChecked
                                 ? $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() =>
        {
            var isDisabled = IsEnabled is false || (RadioButtonGroup is not null && RadioButtonGroup.IsEnabled is false);
            return $"{RootElementClass}-{(isDisabled ? "disabled" : "enabled")}-{VisualClassRegistrar()}";
        });
    }

    internal void SetState(bool status)
    {
        IsChecked = status;
        StateHasChanged();
    }

    private string GetLabelClassNameStr()
    {
        var className = (ImageSrc.HasValue() || IconName.HasValue) && ChildContent is null ? "bit-rbo-lbl-with-img" : "bit-rbo-lbl";
        return className;
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false || (RadioButtonGroup is not null && RadioButtonGroup.IsEnabled is false)) return;

        if (RadioButtonGroup is not null)
        {
            await RadioButtonGroup.SelectOption(this);
        }

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || (RadioButtonGroup is not null && RadioButtonGroup.IsEnabled is false)) return;

        if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;

        await OnChange.InvokeAsync(IsChecked);
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (RadioButtonGroup is not null)
        {
            RadioButtonGroup.UnregisterOption(this);
        }

        _disposed = true;
    }
}

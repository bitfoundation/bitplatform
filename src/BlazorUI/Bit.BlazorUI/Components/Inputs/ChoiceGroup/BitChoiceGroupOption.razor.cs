﻿namespace Bit.BlazorUI;

public partial class BitChoiceGroupOption<TValue> : IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitChoiceGroup<BitChoiceGroupOption<TValue>, TValue> Parent { get; set; } = default!;

    /// <summary>
    /// Used to customize the label for the RadioButtonOption.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Set attribute of Id for the GroupOption Option input.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The icon to show as Option content.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The image address to show as Option content.
    /// </summary>
    [Parameter] public string? ImageSrc { get; set; }

    /// <summary>
    /// Provides alternative information for the Option image.
    /// </summary>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// Provides Height and Width for the Option image.
    /// </summary>
    [Parameter] public BitSize? ImageSize { get; set; }

    /// <summary>
    /// Provides a new image for the selected Option in the Image-GroupOption.
    /// </summary>
    [Parameter] public string? SelectedImageSrc { get; set; }

    /// <summary>
    /// Text to show as content of GroupOption Option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// This value is returned when GroupOption Option is Clicked.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }

    protected override string RootElementClass => "bit-chgo";

    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Parent.UnregisterOption(this);

        _disposed = true;
    }
}

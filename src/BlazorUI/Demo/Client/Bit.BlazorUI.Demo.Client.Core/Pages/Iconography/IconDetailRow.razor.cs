namespace Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

public partial class IconDetailRow
{
    [Parameter] public string Label { get; set; } = default!;

    [Parameter] public string Value { get; set; } = default!;

    [Parameter] public string CopyKey { get; set; } = default!;

    [Parameter] public string? CopyFeedbackKey { get; set; }

    [Parameter] public EventCallback<(string Text, string Key)> OnCopy { get; set; }



    private bool IsCopied => CopyFeedbackKey == CopyKey;



    private Task HandleCopy() => OnCopy.InvokeAsync((Value, CopyKey));
}

using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadOption : IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitBreadGroup? BreadGroup { get; set; }

    /// <summary>
    /// URL to navigate to when this BitBreadOption is clicked.
    /// If provided, the BitBreadOption will be rendered as a link.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// By default, the Selected option is the last option. But it can also be specified manually.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// Callback for when the BitBreadOption clicked and Href is empty.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Text to display in the BitBreadOption option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    protected override string RootElementClass => "bit-bro";

    protected override async Task OnInitializedAsync()
    {
        if (BreadGroup is not null)
        {
            BreadGroup.RegisterOptions(this);
        }
        
        await base.OnInitializedAsync();
    }

    internal async Task HandleOnOptionClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (Href.HasValue()) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnOverfelowClick()
    {
        if (IsEnabled is false || BreadGroup is null) return;

        await BreadGroup.HandleCallout();
    }

    internal string GetOptionClasses()
    {
        if (BreadGroup is null) return string.Empty;

        StringBuilder optionClasses = new();

        optionClasses.Append("option");

        if (IsSelectedOption())
        {
            optionClasses.Append(" selected-option");
        }

        if (IsSelectedOption() && BreadGroup.SelectedOptionClass.HasValue())
        {
            optionClasses.Append(' ');
            optionClasses.Append(BreadGroup.SelectedOptionClass);
        }

        return optionClasses.ToString();
    }

    internal string GetOptionStyles()
    {
        if (BreadGroup is null) return string.Empty;

        if (IsSelectedOption() && BreadGroup.SelectedOptionStyle.HasValue())
        {
            return BreadGroup.SelectedOptionStyle!;
        }

        return string.Empty;
    }

    internal bool IsSelectedOption() => BreadGroup is not null && this == (BreadGroup._allOptions.LastOrDefault(o => o.IsSelected) ?? BreadGroup._allOptions[^1]);

    private bool IsOverfelowButton() => BreadGroup is not null && BreadGroup._overflowOptions.Any(o => o == this) && BreadGroup._allOptions.IndexOf(this) == BreadGroup._internalOverfelowIndex;

    private bool IsDisplayedOption() => BreadGroup is not null && BreadGroup._displayOptions.Any(o => o == this);

    private bool HasDividerIconOption() => BreadGroup is not null && (BreadGroup._displayOptions.Any(o => o == this) && this != BreadGroup._allOptions[^1]) || IsOverfelowButton();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!disposing) return;

        if (BreadGroup is not null)
        {
            BreadGroup.UnregisterOptions(this);
        }

        _disposed = true;
    }
}

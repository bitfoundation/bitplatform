using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadOption : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// 
    /// </summary>
    [CascadingParameter] protected BitBreadGroup? BreadGroup { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool IsCurrentOption { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Callback for when the BitBreadOption clicked and Href is empty.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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

    internal async Task HandleOnOverfelowClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (BreadGroup is null) return;

        await BreadGroup.HandleOnClick(e);
    }

    internal string GetOptionClasses()
    {
        if (BreadGroup is null) return string.Empty;

        StringBuilder optionClasses = new();

        optionClasses.Append("option");

        if (ItIsCurrentOption())
        {
            optionClasses.Append(" current-option");
        }

        if (ItIsCurrentOption() && BreadGroup.CurrentOptionClass.HasValue())
        {
            optionClasses.Append(' ');
            optionClasses.Append(BreadGroup.CurrentOptionClass);
        }

        return optionClasses.ToString();
    }

    internal string GetOptionStyles()
    {
        if (BreadGroup is null) return string.Empty;

        if (ItIsCurrentOption() && BreadGroup.CurrentOptionStyle.HasValue())
        {
            return BreadGroup.CurrentOptionStyle!;
        }

        return string.Empty;
    }

    internal bool ItIsCurrentOption() => BreadGroup is not null && this == (BreadGroup._allOptions.LastOrDefault(o => o.IsCurrentOption) ?? BreadGroup._allOptions[^1]);

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
            BreadGroup.UnRegisterOptions(this);
        }

        _disposed = true;
    }
}

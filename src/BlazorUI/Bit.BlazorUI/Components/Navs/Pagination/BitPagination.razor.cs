using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Pagination component helps users easily navigate through content, allowing swift browsing across multiple pages or sections, commonly used in lists, tables, and content-rich interfaces.
/// </summary>
public partial class BitPagination : BitComponentBase
{
    private int _count = 1;
    private int _middleCount = 3;
    private int _boundaryCount = 2;



    /// <summary>
    /// The number of items at the start and end of the pagination.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetBoundaryCount))]
    public int BoundaryCount { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the pagination.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetCount))]
    public int Count { get; set; }

    /// <summary>
    /// The default selected page number.
    /// </summary>
    [Parameter] public int DefaultSelectedPage { get; set; }

    /// <summary>
    /// The icon for the first button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="FirstButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? FirstButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the first button.
    /// For external icon libraries, use <see cref="FirstButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? FirstButtonIconName { get; set; }

    /// <summary>
    /// The icon for the last button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="LastButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? LastButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the last button.
    /// For external icon libraries, use <see cref="LastButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? LastButtonIconName { get; set; }

    /// <summary>
    /// The number of items to render in the middle of the pagination.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMiddleCount))]
    public int MiddleCount { get; set; }

    /// <summary>
    /// The icon for the next button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the next button.
    /// For external icon libraries, use <see cref="NextButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? NextButtonIconName { get; set; }

    /// <summary>
    /// The event callback for when selected page changes.
    /// </summary>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The icon for the previous button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PreviousButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PreviousButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the previous button.
    /// For external icon libraries, use <see cref="PreviousButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? PreviousButtonIconName { get; set; }

    /// <summary>
    /// The selected page number.
    /// </summary>
    [Parameter, TwoWayBound]
    public int SelectedPage { get; set; }

    /// <summary>
    /// Determines whether to show the first button.
    /// </summary>
    [Parameter] public bool ShowFirstButton { get; set; }

    /// <summary>
    /// Determines whether to show the last button.
    /// </summary>
    [Parameter] public bool ShowLastButton { get; set; }

    /// <summary>
    /// Determines whether to show the next button.
    /// </summary>
    [Parameter] public bool ShowNextButton { get; set; } = true;

    /// <summary>
    /// Determines whether to show the previous button.
    /// </summary>
    [Parameter] public bool ShowPreviousButton { get; set; } = true;

    /// <summary>
    /// The size of the buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Styles { get; set; }

    /// <summary>
    /// The visual variant of the pagination.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-pgn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-pgn-pri",
            BitColor.Secondary => "bit-pgn-sec",
            BitColor.Tertiary => "bit-pgn-ter",
            BitColor.Info => "bit-pgn-inf",
            BitColor.Success => "bit-pgn-suc",
            BitColor.Warning => "bit-pgn-wrn",
            BitColor.SevereWarning => "bit-pgn-swr",
            BitColor.Error => "bit-pgn-err",
            _ => "bit-pgn-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-pgn-sm",
            BitSize.Medium => "bit-pgn-md",
            BitSize.Large => "bit-pgn-lg",
            _ => "bit-pgn-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        if (SelectedPageHasBeenSet is false && DefaultSelectedPage != 0)
        {
            await AssignSelectedPage(DefaultSelectedPage);
        }

        if (SelectedPage == 0)
        {
            await AssignSelectedPage(1);
        }

        await base.OnInitializedAsync();
    }



    private IEnumerable<int> GeneratePages()
    {
        if (_count <= 4 || _count <= 2 * _boundaryCount + _middleCount + 2)
        {
            return Enumerable.Range(1, _count).ToArray();
        }

        var length = 2 * _boundaryCount + _middleCount + 2;
        var pages = new int[length];

        for (var i = 0; i < _boundaryCount; i++)
        {
            pages[i] = i + 1;
        }

        for (var i = 0; i < _boundaryCount; i++)
        {
            pages[length - i - 1] = _count - i;
        }

        int startValue;
        if (SelectedPage <= _boundaryCount + _middleCount / 2 + 1)
        {
            startValue = _boundaryCount + 2;
        }
        else if (SelectedPage >= _count - _boundaryCount - _middleCount / 2)
        {
            startValue = _count - _boundaryCount - _middleCount;
        }
        else
        {
            startValue = SelectedPage - _middleCount / 2;
        }

        for (var i = 0; i < _middleCount; i++)
        {
            pages[_boundaryCount + 1 + i] = startValue + i;
        }

        pages[_boundaryCount] = (_boundaryCount + _middleCount / 2 + 1 < SelectedPage) ? -1 : _boundaryCount + 1;

        pages[length - _boundaryCount - 1] = (_count - _boundaryCount - _middleCount / 2 > SelectedPage) ? -1 : _count - _boundaryCount;

        for (var i = 0; i < length - 2; i++)
        {
            if (pages[i] + 2 == pages[i + 2])
            {
                pages[i + 1] = pages[i] + 1;
            }
        }

        return pages;
    }

    private async Task ChangePage(int page)
    {
        if (SelectedPageHasBeenSet && SelectedPageChanged.HasDelegate is false) return;

        if (page > _count) page = _count;

        if (page < 1) page = 1;

        await AssignSelectedPage(page);

        await OnChange.InvokeAsync(page);
    }

    private string GetButtonClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(' ').Append(Variant switch
        {
            BitVariant.Fill => "bit-pgn-fil",
            BitVariant.Outline => "bit-pgn-otl",
            BitVariant.Text => "bit-pgn-txt",
            _ => "bit-pgn-fil"
        });

        return className.ToString();
    }

    private void OnSetBoundaryCount()
    {
        _boundaryCount = Math.Max(1, BoundaryCount);
    }

    private void OnSetCount()
    {
        _count = Math.Max(1, Count);
        _ = AssignSelectedPage(Math.Min(SelectedPage, Count));
    }

    private void OnSetMiddleCount()
    {
        _middleCount = Math.Max(1, MiddleCount);
    }
}

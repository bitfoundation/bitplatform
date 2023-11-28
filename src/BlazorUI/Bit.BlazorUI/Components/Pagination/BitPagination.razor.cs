using System.Text;

namespace Bit.BlazorUI;

public partial class BitPagination
{
    private BitPaginationSize? size;
    private BitPaginationColor? color;
    private BitAppearance appearance = BitAppearance.Primary;

    private int _count = 1;
    private int _selected = 1;
    private int _middleCount = 3;
    private int _boundaryCount = 2;
    private bool _selectedFirstSet;


    /// <summary>
    /// The appearance of pagination buttons, Possible values: Primary | Standard | Text
    /// </summary>
    [Parameter]
    public BitAppearance Appearance
    {
        get => appearance;
        set
        {
            if (appearance == value) return;

            appearance = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Icon of before button.
    /// </summary>
    [Parameter] public string BeforeIcon { get; set; } = "ChevronLeft";

    /// <summary>
    /// The number of items at the start and end of the pagination.
    /// </summary>
    [Parameter]
    public int BoundaryCount
    {
        get => _boundaryCount;
        set
        {
            _boundaryCount = Math.Max(1, value);
        }
    }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the component.
    /// </summary>
    [Parameter]
    public BitPaginationColor? Color
    {
        get => color;
        set
        {
            if (color == value) return;

            color = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The number of pages.
    /// </summary>
    [Parameter]
    public int Count
    {
        get => _count;
        set
        {
            _count = Math.Max(1, value);
            SelectedPage = Math.Min(SelectedPage, _count);
        }
    }

    /// <summary>
    /// Icon of first button.
    /// </summary>
    [Parameter] public string FirstIcon { get; set; } = "ChevronLeftEnd6";

    /// <summary>
    /// Icon of last button.
    /// </summary>
    [Parameter] public string LastIcon { get; set; } = "ChevronRightEnd6";

    /// <summary>
    /// The number of items in the middle of the pagination.
    /// </summary>
    [Parameter]
    public int MiddleCount
    {
        get => _middleCount;
        set
        {
            _middleCount = Math.Max(1, value);
        }
    }

    /// <summary>
    /// Icon of next button.
    /// </summary>
    [Parameter] public string NextIcon { get; set; } = "ChevronRight";

    /// <summary>
    /// Invokes the callback when a control button is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPage> OnControlButtonClick { get; set; }

    /// <summary>
    /// Invokes the callback when selected page changes.
    /// </summary>
    [Parameter] public EventCallback<int> OnSelectedChanged { get; set; }

    /// <summary>
    /// The selected page number.
    /// </summary>
    [Parameter]
    public int SelectedPage
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;

            if (_selectedFirstSet is false)
            {
                _selected = value;
                _selectedFirstSet = true;
            }
            else
            {
                _selected = Math.Max(1, Math.Min(value, Count));
            }

            OnSelectedChanged.InvokeAsync(_selected);
        }
    }

    /// <summary>
    /// If true, the navigate to first page button is shown.
    /// </summary>
    [Parameter] public bool ShowFirstButton { get; set; }

    /// <summary>
    /// If true, the navigate to last page button is shown.
    /// </summary>
    [Parameter] public bool ShowLastButton { get; set; }

    /// <summary>
    /// If true, the navigate to next page button is shown.
    /// </summary>
    [Parameter] public bool ShowNextButton { get; set; } = true;

    /// <summary>
    /// If true, the navigate to previous page button is shown.
    /// </summary>
    [Parameter] public bool ShowPreviousButton { get; set; } = true;

    /// <summary>
    /// The size of pagination buttons, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitPaginationSize? Size
    {
        get => size;
        set
        {
            if (size == value) return;

            size = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Styles { get; set; }


    public void NavigateTo(BitPage page)
    {
        SelectedPage = page switch
        {
            BitPage.First => 1,
            BitPage.Last => Math.Max(1, Count),
            BitPage.Next => Math.Min(SelectedPage + 1, Count),
            BitPage.Previous => Math.Max(1, SelectedPage - 1),
            _ => SelectedPage
        };
    }

    public void NavigateTo(int pageIndex)
    {
        SelectedPage = pageIndex + 1;
    }


    protected override string RootElementClass => "bit-pgn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }


    private IEnumerable<int> GeneratePagination()
    {
        if (Count <= 4 || Count <= 2 * BoundaryCount + MiddleCount + 2)
        {
            return Enumerable.Range(1, Count).ToArray();
        }

        var length = 2 * BoundaryCount + MiddleCount + 2;
        var pages = new int[length];

        for (var i = 0; i < BoundaryCount; i++)
        {
            pages[i] = i + 1;
        }

        for (var i = 0; i < BoundaryCount; i++)
        {
            pages[length - i - 1] = Count - i;
        }

        int startValue;
        if (SelectedPage <= BoundaryCount + MiddleCount / 2 + 1)
        {
            startValue = BoundaryCount + 2;
        }
        else if (SelectedPage >= Count - BoundaryCount - MiddleCount / 2)
        {
            startValue = Count - BoundaryCount - MiddleCount;
        }
        else
        {
            startValue = SelectedPage - MiddleCount / 2;
        }

        for (var i = 0; i < MiddleCount; i++)
        {
            pages[BoundaryCount + 1 + i] = startValue + i;
        }

        pages[BoundaryCount] = (BoundaryCount + MiddleCount / 2 + 1 < SelectedPage) ? -1 : BoundaryCount + 1;

        pages[length - BoundaryCount - 1] = (Count - BoundaryCount - MiddleCount / 2 > SelectedPage) ? -1 : Count - BoundaryCount;

        for (var i = 0; i < length - 2; i++)
        {
            if (pages[i] + 2 == pages[i + 2])
            {
                pages[i + 1] = pages[i] + 1;
            }
        }

        return pages;
    }

    private void OnClickControlButton(BitPage page)
    {
        OnControlButtonClick.InvokeAsync(page);
        NavigateTo(page);
    }

    private string GetButtonClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(' ').Append(Appearance switch
        {
            BitAppearance.Primary => "bit-pgn-pri",
            BitAppearance.Standard => "bit-pgn-std",
            BitAppearance.Text => "bit-pgn-txt",
            _ => "bit-pgn-pri"
        });

        className.Append(' ').Append(Color switch
        {
            BitPaginationColor.Info => "bit-pgn-inf",
            BitPaginationColor.Success => "bit-pgn-suc",
            BitPaginationColor.Warning => "bit-pgn-wrn",
            BitPaginationColor.SevereWarning => "bit-pgn-swr",
            BitPaginationColor.Error => "bit-pgn-err",
            _ => string.Empty
        });

        className.Append(' ').Append(Size switch
        {
            BitPaginationSize.Small => "bit-pgn-sm",
            BitPaginationSize.Medium => "bit-pgn-md",
            BitPaginationSize.Large => "bit-pgn-lg",
            _ => string.Empty
        });

        return className.ToString();
    }
}

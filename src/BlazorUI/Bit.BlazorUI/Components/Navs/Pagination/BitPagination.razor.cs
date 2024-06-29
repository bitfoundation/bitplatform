using System.Text;

namespace Bit.BlazorUI;

public partial class BitPagination
{
    private bool SelectedPageHasBeenSet;

    private int count = 1;
    private BitSize? size;
    private BitColor? color;
    private int selectedPage;
    private int middleCount = 3;
    private int boundaryCount = 2;
    private BitAppearance appearance = BitAppearance.Primary;



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
    /// The number of items at the start and end of the pagination.
    /// </summary>
    [Parameter]
    public int BoundaryCount
    {
        get => boundaryCount;
        set
        {
            boundaryCount = Math.Max(1, value);
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
    public BitColor? Color
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
        get => count;
        set
        {
            count = Math.Max(1, value);
            SelectedPage = Math.Min(SelectedPage, count);
        }
    }

    /// <summary>
    /// The default selected page number.
    /// </summary>
    [Parameter] public int DefaultSelectedPage { get; set; }

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
        get => middleCount;
        set
        {
            middleCount = Math.Max(1, value);
        }
    }

    /// <summary>
    /// Icon of next button.
    /// </summary>
    [Parameter] public string NextIcon { get; set; } = "ChevronRight";

    /// <summary>
    /// Invokes the callback when selected page changes.
    /// </summary>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// Icon of previous button.
    /// </summary>
    [Parameter] public string PreviousIcon { get; set; } = "ChevronLeft";

    /// <summary>
    /// The selected page number.
    /// </summary>
    [Parameter]
    public int SelectedPage
    {
        get => selectedPage;
        set
        {
            if (selectedPage == value) return;

            selectedPage = value;
            _ = SelectedPageChanged.InvokeAsync(selectedPage);
        }
    }

    [Parameter] public EventCallback<int> SelectedPageChanged { get; set; }

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
    public BitSize? Size
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


    protected override string RootElementClass => "bit-pgn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-pgn-sm",
            BitSize.Medium => "bit-pgn-md",
            BitSize.Large => "bit-pgn-lg",
            _ => string.Empty
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
            SelectedPage = DefaultSelectedPage;
        }

        if (SelectedPage == 0)
        {
            SelectedPage = 1;
        }

        await base.OnInitializedAsync();
    }


    private IEnumerable<int> GeneratePages()
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

    private async Task ChangePage(int page)
    {
        if (SelectedPageHasBeenSet && SelectedPageChanged.HasDelegate is false) return;

        if (page > Count) page = Count;

        if (page < 1) page = 1;

        SelectedPage = page;

        await OnChange.InvokeAsync(page);
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
            BitColor.Info => "bit-pgn-inf",
            BitColor.Success => "bit-pgn-suc",
            BitColor.Warning => "bit-pgn-wrn",
            BitColor.SevereWarning => "bit-pgn-swr",
            BitColor.Error => "bit-pgn-err",
            _ => string.Empty
        });

        return className.ToString();
    }
}

namespace Bit.BlazorUI;

public partial class BitPdfReader
{
    private int _currentPage = 1;
    private bool _allPageRendered;
    private int _numberOfPages = 1;



    [Inject] private IJSRuntime _js { get; set; }


    /// <summary>
    /// The configuration of the pdf reader (<see cref="BitPdfReaderConfig"/>).
    /// </summary>
    [Parameter] public BitPdfReaderConfig Config { get; set; }

    /// <summary>
    /// Renders the pages horizontally.
    /// </summary>
    [Parameter] public bool Horizontal { get; set; }

    /// <summary>
    /// The page number to render initially.
    /// </summary>
    [Parameter] public int InitialPageNumber { get; set; } = 1;

    /// <summary>
    /// Whether render all pages at start.
    /// </summary>
    [Parameter] public bool RenderAllPages { get; set; }

    /// <summary>
    /// The CSS selector of the scroll element that is the parent of the pdf reader.
    /// </summary>
    [Parameter] public string ScrollElement { get; set; } = "body";



    public Task First()
    {
        return Go(1);
    }

    public Task Prev()
    {
        return Go(_currentPage - 1);
    }

    public Task Next()
    {
        return Go(_currentPage + 1);
    }

    public Task Last()
    {
        return Go(_numberOfPages);
    }

    public async Task Go(int pageNumber)
    {
        if (pageNumber < 1) return;
        if (pageNumber > _numberOfPages) return;

        _currentPage = pageNumber;

        if (RenderAllPages)
        {
            //await _js.
        }
        else
        {
            await _js.renderPdfJsPage(Config.Id, _currentPage);
        }
    }

    public int CurrentPage => _currentPage;
    public int NumberOfPages => _numberOfPages;



    protected override Task OnParametersSetAsync()
    {
        if (_currentPage == 1)
        {
            _currentPage = InitialPageNumber;
        }

        return base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string[] scripts = [
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76.js",
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76-worker.js"
            ];

            await _js.InitPdfJs(scripts);

            _numberOfPages = await _js.SetupPdfJs(Config);

            if (RenderAllPages is false)
            {
                await _js.renderPdfJsPage(Config.Id, InitialPageNumber);
            }

            StateHasChanged();
        }
        else
        {
            if (RenderAllPages && _allPageRendered is false)
            {
                for (int i = 0; i < _numberOfPages; i++)
                {
                    await _js.renderPdfJsPage(Config.Id, i + 1);
                }

                _allPageRendered = true;
            }
        }
    }
}

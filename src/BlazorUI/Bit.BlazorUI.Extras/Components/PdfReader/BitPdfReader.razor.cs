using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitPdfReader
{
    private int _currentPageNumber = 1;
    private bool _allPageRendered;
    private int _numberOfPages = 1;
    private bool _parametersInitialized;



    [Inject] private IJSRuntime _js { get; set; }



    /// <summary>
    /// The CSS class of the canvas element(s).
    /// </summary>
    [Parameter] public string? CanvasClass { get; set; }

    /// <summary>
    /// The CSS style of the canvas element(s).
    /// </summary>
    [Parameter] public string? CanvasStyle { get; set; }

    /// <summary>
    /// The CSS class of the root element.
    /// </summary>
    [Parameter] public string? Class { get; set; }

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
    /// The callback for when the pdf page is done rendering.
    /// </summary>
    [Parameter] public EventCallback OnPdfPageRendered { get; set; }

    /// <summary>
    /// The callback for when the pdf document is done loading and processing.
    /// </summary>
    [Parameter] public EventCallback OnPdfLoaded { get; set; }

    /// <summary>
    /// Whether render all pages at start.
    /// </summary>
    [Parameter] public bool RenderAllPages { get; set; }

    /// <summary>
    /// The CSS style of the root element.
    /// </summary>
    [Parameter] public string? Style { get; set; }


    /// <summary>
    /// Re-renders the provided page number or the current page.
    /// </summary>
    public ValueTask Refresh(int pageNumber = 0)
    {
        pageNumber = pageNumber == 0 ? _currentPageNumber : pageNumber;

        if (pageNumber > _numberOfPages) return ValueTask.CompletedTask;

        return RefreshPage(pageNumber);
    }

    /// <summary>
    /// Re-renders all of the pages.
    /// </summary>
    public Task RefreshAll()
    {
        return RefreshAllPages();
    }

    /// <summary>
    /// Renders the first page.
    /// </summary>
    public Task First()
    {
        return Go(1);
    }

    /// <summary>
    /// Renders the previous page.
    /// </summary>
    public Task Prev()
    {
        return Go(_currentPageNumber - 1);
    }

    /// <summary>
    /// Renders the next page.
    /// </summary>
    public Task Next()
    {
        return Go(_currentPageNumber + 1);
    }

    /// <summary>
    /// Renders the last page.
    /// </summary>
    public Task Last()
    {
        return Go(_numberOfPages);
    }

    /// <summary>
    /// Renders the provided page number.
    /// </summary>
    public async Task Go(int pageNumber)
    {
        if (pageNumber < 1) return;
        if (pageNumber > _numberOfPages) return;

        _currentPageNumber = pageNumber;

        if (RenderAllPages)
        {
            //TODO: implement scroll to the page
        }
        else
        {
            await _js.BitPdfReaderRenderPage(Config.Id, _currentPageNumber);
        }
    }

    /// <summary>
    /// The current page number that is currently rendered.
    /// </summary>
    public int CurrentPageNumber => _currentPageNumber;

    /// <summary>
    /// Number of total pages of the current pdf document.
    /// </summary>
    public int NumberOfPages => _numberOfPages;



    protected override Task OnParametersSetAsync()
    {
        if (_parametersInitialized is false)
        {
            _currentPageNumber = InitialPageNumber;
            _parametersInitialized = true;
        }

        return base.OnParametersSetAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPdfReaderConfig))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string[] scripts = [
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76.js",
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76-worker.js"
            ];

            await _js.BitPdfReaderInitPdfJs(scripts);

            _numberOfPages = await _js.BitPdfReaderSetupPdfDoc(Config);

            await OnPdfLoaded.InvokeAsync();

            if (RenderAllPages is false)
            {
                await Render(InitialPageNumber);
            }

            StateHasChanged();
        }
        else
        {
            if (RenderAllPages && _allPageRendered is false)
            {
                await RenderAll();

                _allPageRendered = true;
            }
        }
    }

    private async ValueTask Render(int pageNumber)
    {
        await _js.BitPdfReaderRenderPage(Config.Id, pageNumber);

        await OnPdfPageRendered.InvokeAsync();
    }

    private async Task RenderAll()
    {
        if (RenderAllPages is false) return;

        List<Task> tasks = [];

        for (int i = 0; i < _numberOfPages; i++)
        {
            tasks.Add(_js.BitPdfReaderRenderPage(Config.Id, i + 1).AsTask());
        }

        await Task.WhenAll(tasks);

        await OnPdfPageRendered.InvokeAsync();
    }

    private async ValueTask RefreshPage(int pageNumber)
    {
        await _js.BitPdfReaderRefreshPage(Config, pageNumber);

        await OnPdfPageRendered.InvokeAsync();
    }

    private async Task RefreshAllPages()
    {
        if (RenderAllPages is false) return;

        List<Task> tasks = [];

        for (int i = 0; i < _numberOfPages; i++)
        {
            tasks.Add(_js.BitPdfReaderRefreshPage(Config, i + 1).AsTask());
        }

        await Task.WhenAll(tasks);

        await OnPdfPageRendered.InvokeAsync();
    }
}

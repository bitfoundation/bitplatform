namespace Bit.BlazorUI;

public partial class BitPdfReader
{
    [Parameter] public BitPdfReaderConfig Config { get; set; }



    [Inject] private IJSRuntime _js { get; set; }



    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string[] scripts = [ 
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76.js",
                "_content/Bit.BlazorUI.Extras/pdf.js/pdfjs-4.7.76-worker.js"
            ];
            
            await _js.InitPdfJs(scripts);

            await _js.SetupPdfJs(Config);
        }
    }
}

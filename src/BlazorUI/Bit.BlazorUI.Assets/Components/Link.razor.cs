namespace Bit.BlazorUI;

public partial class Link
{
    [Parameter(CaptureUnmatchedValues = true)] 
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string Href { get; set; } = "";
    [Parameter] public bool AppendVersion { get; set; } = true;



    [Inject] private IServiceProvider serviceProvider { get; set; } = default!;



    private string? href;



    protected override void OnInitialized()
    {
        base.OnInitialized();

        href = AppendVersion ? BitAssetVersion.TryAppend(serviceProvider, Href) : Href;
    }
}

using Microsoft.AspNetCore.Http;

namespace Bit.BlazorUI;

public partial class Link
{
    [Parameter(CaptureUnmatchedValues = true)] 
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public bool AppendVersion { get; set; } = true;
    [Parameter] public required string Href { get; set; } = "";



    [Inject] private IHttpContextAccessor httpContextAccessor { get; set; } = default!;
    [Inject] private BitFileVersionProvider bitFileVersionProvider { get; set; } = default!;



    private string? href;



    protected override void OnInitialized()
    {
        base.OnInitialized();
        href = (Href is not null && AppendVersion) 
                ? bitFileVersionProvider.AppendFileVersion(httpContextAccessor.HttpContext!.Request.PathBase, Href) 
                : Href;
    }
}

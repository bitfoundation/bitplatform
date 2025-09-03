using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Bit.BlazorUI;

public partial class Link
{
    [Parameter(CaptureUnmatchedValues = true)] 
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string Href { get; set; } = "";
    [Parameter] public bool AppendVersion { get; set; } = true;



    [Inject] private IWebHostEnvironment webHost { get; set; } = default!;
    [Inject] private IHttpContextAccessor httpContextAccessor { get; set; } = default!;



    private string? href;



    protected override void OnInitialized()
    {
        base.OnInitialized();
        href = (Href is not null && AppendVersion) 
                ? BitFileVersionProvider.AppendFileVersion(webHost.WebRootFileProvider, httpContextAccessor?.HttpContext?.Request.PathBase ?? PathString.Empty, Href) 
                : Href;
    }
}

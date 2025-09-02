using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Bit.BlazorUI;

public partial class Script
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string? Src { get; set; }
    [Parameter] public bool AppendVersion { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }



    [Inject] private IWebHostEnvironment webHost { get; set; } = default!;
    [Inject] private IHttpContextAccessor httpContextAccessor { get; set; } = default!;



    private string? src;



    protected override void OnInitialized()
    {
        base.OnInitialized();

        src = (Src is not null && AppendVersion)
                ? BitFileVersionProvider.AppendFileVersion(webHost.WebRootFileProvider, httpContextAccessor?.HttpContext?.Request.PathBase ?? PathString.Empty, Src) 
                : Src;
    }
}

using Microsoft.AspNetCore.Http;

namespace Bit.BlazorUI;

public partial class Script
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string? Src { get; set; }
    [Parameter] public bool AppendVersion { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }



    [Inject] private IHttpContextAccessor httpContextAccessor { get; set; } = default!;
    [Inject] private BitFileVersionProvider bitFileVersionProvider { get; set; } = default!;



    private string? src;



    protected override void OnInitialized()
    {
        base.OnInitialized();

        src = (Src is not null && AppendVersion)
                ? bitFileVersionProvider.AppendFileVersion(httpContextAccessor.HttpContext!.Request.PathBase, Src) 
                : Src;
    }
}

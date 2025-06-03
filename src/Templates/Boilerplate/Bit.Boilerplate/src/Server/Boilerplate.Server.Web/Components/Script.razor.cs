using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Boilerplate.Server.Web.Components;

public partial class Script
{
    [AutoInject] private IFileVersionProvider fileVersionProvider = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    [Parameter] public string? Src { get; set; }
    [Parameter] public bool AppendVersion { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;


    private string? src;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        src = (Src is not null && AppendVersion) ? fileVersionProvider.AddFileVersionToPath(httpContextAccessor.HttpContext!.Request.PathBase, Src) : Src;
    }
}

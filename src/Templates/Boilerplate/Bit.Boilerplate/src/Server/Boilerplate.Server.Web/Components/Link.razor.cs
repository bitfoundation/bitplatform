using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Boilerplate.Server.Web.Components;

public partial class Link
{
    [AutoInject] private IFileVersionProvider fileVersionProvider = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    [Parameter] public bool AppendVersion { get; set; } = true;
    [Parameter] public required string Href { get; set; } = "";
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    private string href = "";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        href = AppendVersion ? fileVersionProvider.AddFileVersionToPath(httpContextAccessor.HttpContext!.Request.PathBase, Href) : Href;
    }
}

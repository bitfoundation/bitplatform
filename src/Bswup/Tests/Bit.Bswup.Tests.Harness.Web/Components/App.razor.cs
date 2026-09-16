using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Tests.Harness.Web.Components;

public partial class App
{
    [Inject] public IConfiguration Configuration { get; set; } = default!;

    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

    private HarnessSessionOptions Options => HarnessSessions.Current(HttpContext).Options;

    private HarnessProgressOptions Progress => Options.Progress;

    private string Mode => HarnessRenderModes.FromConfiguration(Configuration);

    // The document the service worker downloads as the app shell carries the noPrerenderQuery: it must not
    // bake one request's prerendered output into every later navigation (compare the FullSample's App.razor).
    private IComponentRenderMode RenderMode => HarnessRenderModes.Resolve(Mode, prerender: HttpContext.Request.Query.ContainsKey("no-prerender") is false);

    private Dictionary<string, object> ScriptAttributes => HostedApp.Root.ScriptAttributes(Options).ToDictionary(a => a.Key, a => (object)a.Value);

#if NET9_0_OR_GREATER
    private string BlazorScript => Options.FingerprintedBlazorScript ? Assets["_framework/blazor.web.js"] : "_framework/blazor.web.js";
#else
    private string BlazorScript => "_framework/blazor.web.js";
#endif
}

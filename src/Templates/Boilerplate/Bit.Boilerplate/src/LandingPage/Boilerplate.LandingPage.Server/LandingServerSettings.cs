//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;

namespace Boilerplate.LandingPage.Server;

public partial class LandingServerSettings
{
    public WebAppRenderOptions WebAppRender { get; set; } = new();

    public SiteOptions Site { get; set; } = new();

    public EmailOptions Email { get; set; } = new();

    public AIOptions AI { get; set; } = new();
}

// [mirror] WebAppRenderOptions - keep in sync with:
// - src/Server/Boilerplate.Server.Web/ServerWebSettings.cs
public partial class WebAppRenderOptions
{
    public bool PrerenderEnabled { get; set; } = true;

    public BlazorWebAppMode BlazorMode { get; set; } = BlazorWebAppMode.BlazorWebAssembly;

    private BlazorWebAppMode EffectiveBlazorMode
    {
        get
        {
            var mode = BlazorMode;
#if DebugBlazorServer
            mode = BlazorWebAppMode.BlazorServer;
#endif
            return mode;
        }
    }

    public IComponentRenderMode RenderMode => EffectiveBlazorMode switch
    {
        BlazorWebAppMode.BlazorAuto => new InteractiveAutoRenderMode(PrerenderEnabled),
        BlazorWebAppMode.BlazorServer => new InteractiveServerRenderMode(PrerenderEnabled),
        _ => new InteractiveWebAssemblyRenderMode(PrerenderEnabled)
    };

    public bool IsWebAssembly => EffectiveBlazorMode is BlazorWebAppMode.BlazorWebAssembly;
}

public enum BlazorWebAppMode
{
    BlazorAuto,
    BlazorServer,
    BlazorWebAssembly
}

public partial class SiteOptions
{
    public Uri? Url { get; set; }

    public bool AllowIndexing { get; set; }

    public string? GoogleSiteVerification { get; set; }

    public string? BingSiteVerification { get; set; }
}

public partial class EmailOptions
{
    public string DefaultFromEmail { get; set; } = "DoNotReply@bitplatform.dev";

    public string DefaultFromName { get; set; } = "Boilerplate";
}

public partial class AIOptions
{
    public ChatOptions ChatOptions { get; set; } = new();

    public OpenAIOptions OpenAI { get; set; } = new();
}

public partial class OpenAIOptions
{
    public string ChatModel { get; set; } = "gpt-5.6-luna";

    public string? ChatApiKey { get; set; }

    public Uri ChatEndpoint { get; set; } = new("https://api.openai.com/v1");
}

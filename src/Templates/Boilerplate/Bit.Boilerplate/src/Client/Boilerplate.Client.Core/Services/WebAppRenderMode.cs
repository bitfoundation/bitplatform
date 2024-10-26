//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Services;

public partial class WebAppRenderMode
{
    [AutoInject] private ClientAppSettings clientAppSettings { get; set; } = default!;

    public bool PrerenderEnabled => clientAppSettings.WebAppRender.PreRenderEnabled;

    public static IComponentRenderMode NoPrerenderBlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false);

    public IComponentRenderMode? Current
    {
        get
        {
            return clientAppSettings.WebAppRender.BlazorMode switch
            {
                BlazorWebAppMode.BlazorAuto => new InteractiveAutoRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorWebAssembly => new InteractiveWebAssemblyRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorServer => new InteractiveServerRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorSsr => null,
                _ => throw new NotImplementedException(),
            };
        }
    }

    /// <summary>
    /// To enable/disable pwa support, navigate to Directory.Build.props and modify the PwaEnabled flag.
    /// </summary>
    public bool PwaEnabled { get; } =
#if PwaEnabled
        true;
#else
    false;
#endif
}

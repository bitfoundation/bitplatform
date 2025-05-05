//+:cnd:noEmit
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Core.Services;

public partial class ClientPubSubMessages
    //#if (signalR == true)
    : SharedPubSubMessages
    //#endif
{
    public const string SHOW_SNACK = nameof(SHOW_SNACK);
    public const string SHOW_MODAL = nameof(SHOW_MODAL);
    public const string CLOSE_MODAL = nameof(CLOSE_MODAL);

    public const string THEME_CHANGED = nameof(THEME_CHANGED);
    public const string OPEN_NAV_PANEL = nameof(OPEN_NAV_PANEL);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);
    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    public const string IS_ONLINE_CHANGED = nameof(IS_ONLINE_CHANGED);
    public const string PAGE_DATA_CHANGED = nameof(PAGE_DATA_CHANGED);
    public const string ROUTE_DATA_UPDATED = nameof(ROUTE_DATA_UPDATED);

    /// <summary>
    /// Supposed to be called using JavaScript to navigate between pages without reloading the app.
    /// </summary>
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);

    //#if (signalR != true)
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    //#endif

    //#if(module == "Sales")
    public const string SEARCH_PRODUCTS = nameof(SEARCH_PRODUCTS);
    //#endif

    //#if(ads == true)
    public const string AD_HAVE_TROUBLE = nameof(AD_HAVE_TROUBLE);
    //#endif
}

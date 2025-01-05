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
    public const string PAGE_TITLE_CHANGED = nameof(PAGE_TITLE_CHANGED);
    public const string ROUTE_DATA_UPDATED = nameof(ROUTE_DATA_UPDATED);
    public const string UPDATE_IDENTITY_HEADER_BACK_LINK = nameof(UPDATE_IDENTITY_HEADER_BACK_LINK);
    public const string IDENTITY_HEADER_BACK_LINK_CLICKED = nameof(IDENTITY_HEADER_BACK_LINK_CLICKED);

    /// <summary>
    /// Supposed to be called using JavaScript to navigate between pages without reloading the app.
    /// </summary>
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);

    //#if (signalR != true)
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    //#endif
}

//+:cnd:noEmit
namespace Boilerplate.Client.Core.Services;

public static partial class ClientPubSubMessages
{
    public const string SHOW_SNACK = nameof(SHOW_SNACK);
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);

    public const string THEME_CHANGED = nameof(THEME_CHANGED);
    public const string OPEN_NAV_PANEL = nameof(OPEN_NAV_PANEL);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    /// <summary>
    /// <inheritdoc ParameterName />
    /// </summary>
    public const string IS_ONLINE_CHANGED = nameof(IS_ONLINE_CHANGED);
    public const string PAGE_TITLE_CHANGED = nameof(PAGE_TITLE_CHANGED);
    public const string ROUTE_DATA_UPDATED = nameof(ROUTE_DATA_UPDATED);
    public const string UPDATE_IDENTITY_HEADER_BACK_LINK = nameof(UPDATE_IDENTITY_HEADER_BACK_LINK);
    public const string IDENTITY_HEADER_BACK_LINK_CLICKED = nameof(IDENTITY_HEADER_BACK_LINK_CLICKED);

    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);
}

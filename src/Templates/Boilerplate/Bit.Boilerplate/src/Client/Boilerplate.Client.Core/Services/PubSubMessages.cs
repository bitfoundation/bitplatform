namespace Boilerplate.Client.Core.Services;

public static class PubSubMessages
{
    public const string USER_DATA_UPDATED = nameof(USER_DATA_UPDATED);
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);

    /// <summary>
    /// <inheritdoc cref="HeaderName.LocalHttpServerPort"/>
    /// </summary>
    public const string LOCAL_HTTP_SERVER_STARTED = nameof(LOCAL_HTTP_SERVER_STARTED);
}

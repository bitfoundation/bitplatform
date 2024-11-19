namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

/// <summary>
/// The generated HTTP client proxy by Bit.SourceGenerators will automatically include these request options in the constructed HttpRequestMessage.
/// You can access these values within HTTP message handlers, such as  <see cref="AuthDelegatingHandler.HasAuthorizedApiAttribute(HttpRequestMessage)"/>.
/// </summary>
public partial class RequestOptionNames
{
    public const string LogLevel = nameof(LogLevel);
    public const string ActionName = nameof(ActionName);
    public const string RequestType = nameof(RequestType);
    public const string ResponseType = nameof(ResponseType);
    public const string LogScopeData = nameof(LogScopeData);
    public const string IControllerType = nameof(IControllerType);
    public const string ActionParametersInfo = nameof(ActionParametersInfo);
}

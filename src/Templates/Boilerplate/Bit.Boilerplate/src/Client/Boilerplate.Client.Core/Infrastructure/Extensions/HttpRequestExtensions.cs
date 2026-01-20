using System.Reflection;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace System.Net.Http;

public static class HttpRequestExtensions
{
    /// <summary>
    /// <inheritdoc cref="AuthorizedApiAttribute"/>
    /// </summary>
    public static bool HasAuthorizedApiAttribute(this HttpRequestMessage request)
    {
        return request.HasApiAttribute<AuthorizedApiAttribute>();
    }

    /// <summary>
    /// <see cref="NoRetryPolicyAttribute"/>
    /// </summary>
    public static bool HasNoRetryPolicyAttribute(this HttpRequestMessage request)
    {
        return request.HasApiAttribute<NoRetryPolicyAttribute>();
    }

    /// <summary>
    /// <see cref="ExternalApiAttribute"/>
    /// </summary>
    public static bool HasExternalApiAttribute(this HttpRequestMessage request)
    {
        return request.HasApiAttribute<ExternalApiAttribute>();
    }

    private static bool HasApiAttribute<TApiAttribute>(this HttpRequestMessage request)
        where TApiAttribute : Attribute
    {
        if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
            return false;

        var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();
        var method = controllerType!.GetMethod((string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!, parameterTypes)!;

        return controllerType.GetCustomAttribute<TApiAttribute>(inherit: true) is not null ||
               method.GetCustomAttribute<TApiAttribute>() is not null;
    }
}

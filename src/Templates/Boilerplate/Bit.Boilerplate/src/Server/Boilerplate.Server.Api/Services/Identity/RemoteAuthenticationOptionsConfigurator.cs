using Microsoft.Identity.Web;
using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Boilerplate.Server.Api.Services.Identity;

/// <summary>
/// Configures External Identity Providers BackchannelHttpHandler to use Microsoft.Extensions.HttpClient Factory
/// </summary>
public class RemoteAuthenticationOptionsConfigurator(IHttpClientFactory httpClientFactory)
    : IPostConfigureOptions<OpenIdConnectOptions>,
        IPostConfigureOptions<GitHubAuthenticationOptions>,
        IPostConfigureOptions<TwitterOptions>,
        IPostConfigureOptions<FacebookOptions>,
        IPostConfigureOptions<MicrosoftIdentityOptions>,
        IPostConfigureOptions<AppleAuthenticationOptions>,
        IPostConfigureOptions<GoogleOptions>
{
    public void PostConfigure(string? name, RemoteAuthenticationOptions options)
    {
        options.Backchannel = httpClientFactory.CreateClient(name!);
    }

    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, GitHubAuthenticationOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, TwitterOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, FacebookOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, MicrosoftIdentityOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, AppleAuthenticationOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void PostConfigure(string? name, GoogleOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }
}

/// <summary>
/// <inheritdoc cref="RemoteAuthenticationOptionsConfigurator"/>
/// </summary>
public static class RemoteAuthenticationOptionsExtensions
{
    public static void ConfigureHttpClientFactoryForExternalIdentityProviders(this IServiceCollection services)
    {
        services.AddHttpClient("GitHub", httpClient =>
        {

        });

        services.AddHttpClient("Apple", httpClient =>
        {

        });

        services.AddHttpClient("Google", httpClient =>
        {

        });

        services.AddHttpClient("Twitter", httpClient =>
        {

        });

        services.AddHttpClient("Facebook", httpClient =>
        {

        });

        services.AddHttpClient("Keycloak", httpClient =>
        {

        });

        services.AddHttpClient("AzureAD", httpClient =>
        {

        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<GoogleOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<FacebookOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<MicrosoftIdentityOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AppleAuthenticationOptions>, RemoteAuthenticationOptionsConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<GitHubAuthenticationOptions>, RemoteAuthenticationOptionsConfigurator>());

    }
}

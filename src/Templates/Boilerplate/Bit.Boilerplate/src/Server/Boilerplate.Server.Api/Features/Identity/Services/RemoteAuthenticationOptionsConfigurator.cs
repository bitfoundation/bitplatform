using Microsoft.Identity.Web;
using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// Routes every external identity provider's backchannel through Microsoft.Extensions.Http's client factory, so the
/// named-client configuration (See ConfigureHttpClientFactoryForExternalIdentityProviders) and everything
/// ConfigureHttpClientDefaults applies - resilience, service discovery, the SocketsHttpHandler settings - reach it.
/// <para>
/// OpenIdConnect and Apple are configured through <see cref="IConfigureNamedOptions{TOptions}"/> rather than
/// <see cref="IPostConfigureOptions{TOptions}"/> on purpose. Their own framework post-configure builds a
/// ConfigurationManager around whatever <c>Backchannel</c> it finds and keeps that instance for the process lifetime,
/// so it has to already be the factory's client by then; assigning it afterwards leaves the discovery-document and
/// JWKS fetches on a bare HttpClient while only the token/userinfo calls get the configured one. The OAuth-family
/// handlers capture nothing, so they stay on post-configure.
/// </para>
/// </summary>
public class RemoteAuthenticationOptionsConfigurator(IHttpClientFactory httpClientFactory)
    : IConfigureNamedOptions<OpenIdConnectOptions>,
        IConfigureNamedOptions<AppleAuthenticationOptions>,
        IPostConfigureOptions<GitHubAuthenticationOptions>,
        IPostConfigureOptions<TwitterOptions>,
        IPostConfigureOptions<FacebookOptions>,
        IPostConfigureOptions<MicrosoftIdentityOptions>,
        IPostConfigureOptions<GoogleOptions>
{
    public void PostConfigure(string? name, RemoteAuthenticationOptions options)
    {
        options.Backchannel = httpClientFactory.CreateClient(name!);
    }

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void Configure(OpenIdConnectOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    public void Configure(string? name, AppleAuthenticationOptions options)
    {
        PostConfigure(name, (RemoteAuthenticationOptions)options);
    }

    public void Configure(AppleAuthenticationOptions options)
    {
        Configure(Options.DefaultName, options);
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
    extension(IServiceCollection services)
    {
        public void ConfigureHttpClientFactoryForExternalIdentityProviders()
        {
            services.AddHttpClient("GitHub", httpClient =>
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Boilerplate-Client");
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

            // Program.Services.cs also registers a "Keycloak" client (with its BaseAddress, which
            // AppUserClaimsPrincipalFactory's relative POST depends on). AddHttpClient accumulates rather than
            // replaces, so both configure actions run; this one exists so the scheme always has a named client.
            services.AddHttpClient("Keycloak", httpClient =>
            {

            });

            services.AddHttpClient("AzureAD", httpClient =>
            {

            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<GoogleOptions>, RemoteAuthenticationOptionsConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, RemoteAuthenticationOptionsConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<FacebookOptions>, RemoteAuthenticationOptionsConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<MicrosoftIdentityOptions>, RemoteAuthenticationOptionsConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<GitHubAuthenticationOptions>, RemoteAuthenticationOptionsConfigurator>());

            // Configure, not PostConfigure - see the note on RemoteAuthenticationOptionsConfigurator. The framework's
            // own post-configure for these two captures Backchannel into a ConfigurationManager it keeps forever, so
            // ours has to run first or the metadata/JWKS channel never sees the factory's client.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<OpenIdConnectOptions>, RemoteAuthenticationOptionsConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<AppleAuthenticationOptions>, RemoteAuthenticationOptionsConfigurator>());
        }
    }
}

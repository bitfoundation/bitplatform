//+:cnd:noEmit
using Boilerplate.Shared.Infrastructure.Services.Contracts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A scope of <see cref="AppTestServer.WebApp"/>'s services is a client of the api: once its <see cref="AuthManager"/>
/// signs a user in, what is created from it calls the api as that user; until then, anonymously.
/// </summary>
public static class AsyncServiceScopeExtensions
{
    extension(AsyncServiceScope scope)
    {
        //#if (api == "Standalone")
        /// <summary>
        /// An HttpClient that works exactly like the one in the app: a test that calls the api through it tests not only the
        /// api's logic, but also the way the client connects to it, only without the UI. ExceptionDelegatingHandler turns an
        /// error response into the exception the app would get, for example, and the request goes to Server.Web first, which
        /// forwards it to Server.Api through YARP.
        /// </summary>
        //#else
        /// <summary>
        /// An HttpClient that works exactly like the one in the app: a test that calls the api through it tests not only the
        /// api's logic, but also the way the client connects to it, only without the UI. ExceptionDelegatingHandler turns an
        /// error response into the exception the app would get, for example, and the request goes to Server.Web, which
        /// hosts the api.
        /// </summary>
        //#endif
        public HttpClient CreateRichHttpClient()
        {
            return scope.ServiceProvider.GetRequiredService<HttpClient>();
        }

        /// <summary>
        /// Creates the app's client of the api controller <typeparamref name="T"/> - <see cref="IUserController"/> and the
        /// rest - connected to <see cref="CreateRichHttpClient"/>.
        /// </summary>
        public T CreateAppController<T>()
            where T : class, IAppController
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }
    }
}

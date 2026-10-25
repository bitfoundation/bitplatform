using Boilerplate.Tests.Services;
using Boilerplate.Shared.Features.Statistics;

namespace Boilerplate.Tests.Infrastructure;

public static class TestServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces <see cref="IStatisticsController"/> with a fake so that pre-rendering the home page
        /// does not reach out to the external NuGet/GitHub APIs. This keeps the pre-rendering / SEO tests
        /// fast and deterministic while still exercising the real server-side rendering pipeline.
        /// </summary>
        public IServiceCollection FakeExternalStatistics()
        {
            var statisticsController = A.Fake<IStatisticsController>();
            // The home page awaits GetNugetStats during pre-rendering; returning null makes it render the
            // "stats could not be loaded" branch (no NullReferenceException) without hitting the real API.
            // GetGitHubStats is skipped during pre-rendering and its failures are swallowed by the page, so it needs no setup.
            A.CallTo(() => statisticsController.GetNugetStats(A<string>._, A<CancellationToken>._))
                .ReturnsLazily(() => NoNugetStats());

            services.RemoveAll<IStatisticsController>();
            services.AddScoped(_ => statisticsController);

            return services;
        }

        /// <summary>
        /// Lets a test use the app's typed API clients - <see cref="IUserController"/> and the rest - outside a browser, as
        /// integration tests do. Behind those clients <see cref="AuthManager"/> keeps the signed-in user's tokens in
        /// <see cref="IStorageService"/>, and <c>AuthDelegatingHandler</c> attaches them to every request through
        /// <see cref="IAuthTokenProvider"/>. The real implementations need a browser (local storage through JS interop) or the
        /// request's cookies, and a test has neither, so this swaps both for in-memory ones: once <see cref="AuthManager"/>
        /// signs a user in, every client resolved from the same scope calls the api as that user.
        /// <see cref="AppTestServer"/> registers them itself on a server no browser drives.
        /// </summary>
        public IServiceCollection AddBrowserlessClientServices()
        {
            services.AddScoped<IStorageService, TestStorageService>();
            services.AddTransient<IAuthTokenProvider, TestAuthTokenProvider>();

            return services;
        }
    }

    /// <summary>
    /// Yields before returning, so the home page's <c>OnInitAsync</c> does not complete inside the first render pass -
    /// which is what the real controller (an http call to nuget.org) does, and what makes the page stream. A
    /// synchronously completed task renders the whole page in one pass and leaves the response with no
    /// <c>blazor-ssr</c> block at all, so every assertion about streaming would silently be asserting about nothing.
    /// That is configuration dependent: with the Sales module the home page also awaits the product sections and
    /// streams either way, while with Admin / no module the statistics call is the only async work on the page.
    /// </summary>
    private static async Task<NugetStatsDto> NoNugetStats()
    {
        await Task.Yield();
        return null!;
    }
}

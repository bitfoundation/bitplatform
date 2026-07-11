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
                .Returns(Task.FromResult<NugetStatsDto>(null!));

            services.RemoveAll<IStatisticsController>();
            services.AddScoped(_ => statisticsController);

            return services;
        }
    }
}

//+:cnd:noEmit
using Hangfire;
using Hangfire.Storage;
using Boilerplate.Server.Api.Features.Identity;

namespace Boilerplate.Tests.Features.Jobs;

/// <summary>
/// Every recurring job id is <c>nameof(TheRunner)</c> and AddOrUpdate only ever adds, so renaming a runner - or
/// turning off the feature that registered it - used to strand the old id in Hangfire storage, where it fails to load
/// on every tick until Hangfire stops scheduling it. Found on the live AdminPanel deployment through the Dev MCP:
/// <c>UserSessionsCleanupJobRunner</c> had been failing to load since the class became
/// <c>UserSessionsRetentionJobRunner</c>.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public class RecurringJobPruningTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task Startup_Should_RemoveARecurringJobItNoLongerRegisters()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var storage = server.WebApp.Services.GetRequiredService<JobStorage>();
        var recurringJobManager = server.WebApp.Services.GetRequiredService<IRecurringJobManager>();

        // Exactly the shape a rename leaves behind: an id nothing registers any more.
        var orphanId = $"RenamedAwayJobRunner-{Guid.NewGuid():N}";
        recurringJobManager.AddOrUpdate<UserSessionsRetentionJobRunner>(orphanId,
            runner => runner.EnforceRetention(CancellationToken.None), Cron.Daily);

        Assert.Contains(orphanId, ReadRecurringJobIds(storage), "The orphan has to exist before there is anything to prune.");

        Boilerplate.Server.Api.Program.ScheduleAppRecurringJobs(server.WebApp);

        var afterPrune = ReadRecurringJobIds(storage);

        Assert.DoesNotContain(orphanId, afterPrune,
            "A schedule nothing registers any more must be removed, or Hangfire keeps retrying a job it cannot load.");

        Assert.Contains(UserSessionsRetentionJobRunner.RecurringJobId, afterPrune,
            "Pruning must leave the jobs this app does register - otherwise it would just delete everything.");
    }

    private static string[] ReadRecurringJobIds(JobStorage storage)
    {
        using var connection = storage.GetConnection();
        return [.. connection.GetRecurringJobs().Select(job => job.Id)];
    }
}

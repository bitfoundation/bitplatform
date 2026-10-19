using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Tests;

/// <summary>
/// BevTools runs inside the application it inspects, so a leak between sessions is a leak between users, and a leak
/// into production is a leak of everything. These tests pin the boundaries rather than trusting them.
/// </summary>
[TestClass]
public class IsolationAndSecurityTests
{
    [TestMethod]
    public void Sessions_never_see_each_other_activity()
    {
        var (a, scopeA) = TestHelpers.CreateSession();
        var (b, scopeB) = TestHelpers.CreateSession();
        using (scopeA)
        using (scopeB)
        {
            a.Timeline.Record(new BevToolsEvent { Title = "secret of A" });
            a.Errors.Record(new InvalidOperationException("A failed"), "test");
            a.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false);

            Assert.IsEmpty(b.Timeline.Snapshot());
            Assert.IsEmpty(b.Errors.Snapshot());
            Assert.AreEqual(0, b.Components.TrackedCount);
            Assert.AreNotEqual(a.Id, b.Id);
        }
    }

    [TestMethod]
    public async Task Http_activity_is_not_attributed_to_an_unrelated_server_session()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            // A circuit exists, but the request runs on a thread pool thread with no dispatcher: on a server this is
            // background work, and guessing that it belongs to "the circuit that happens to be connected" would put
            // one user's server activity in another user's BevTools.
            session.Platform = "Server";

            using var handler = new BevToolsHttpMessageHandler(new BitBevToolsOptions()) { InnerHandler = new StubHandler() };
            using var client = new HttpClient(handler);
            await Task.Run(() => client.GetAsync("https://example.com/background"));

            Assert.AreEqual(0, session.Http.Count);
        }
    }

    [TestMethod]
    public async Task Http_activity_is_attributed_when_the_session_owns_it()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            using var handler = new BevToolsHttpMessageHandler(session) { InnerHandler = new StubHandler() };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer super-secret");
            client.DefaultRequestHeaders.Add("X-Trace", "keep-me");

            await client.GetAsync("https://api.example.com/orders?apiKey=leak&page=2");

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.DoesNotContain("leak", record.Url, StringComparison.Ordinal);
            Assert.Contains("page=2", record.Url, StringComparison.Ordinal);
            Assert.AreEqual(BevRedactor.RedactedValue, record.RequestHeaders!.Single(h => h.Key == "Authorization").Value);
            Assert.AreEqual("keep-me", record.RequestHeaders!.Single(h => h.Key == "X-Trace").Value);
        }
    }

    [TestMethod]
    public void A_disabled_bevtools_records_nothing_at_all()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = false);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();

        session.Timeline.Record(new BevToolsEvent { Title = "should not be kept" });

        Assert.IsFalse(session.IsEnabled);
        Assert.IsEmpty(session.Timeline.Snapshot());
        Assert.IsFalse(provider.GetRequiredService<BevToolsRegistry>().IsEnabled);
        Assert.Contains("Enabled was set to false", provider.GetRequiredService<BevToolsRegistry>().EnabledReason, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_production_environment_keeps_bevtools_off_and_says_why()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new StubEnvironment("Production"));
        services.AddBitBevToolsServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();

        Assert.IsFalse(registry.IsEnabled);
        Assert.Contains("Production", registry.EnabledReason, StringComparison.Ordinal);
        Assert.DoesNotContain(d => d.ServiceType == typeof(Microsoft.AspNetCore.Components.IComponentActivator), services);
        Assert.DoesNotContain(d => d.ServiceType == typeof(ILoggerProvider), services);
        Assert.DoesNotContain(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Components.ComponentsActivitySource", services);
    }

    [TestMethod]
    public void A_development_environment_turns_bevtools_on_and_says_why()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new StubEnvironment("Development"));
        services.AddBitBevToolsServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();

        Assert.IsTrue(registry.IsEnabled);
        Assert.Contains("Development", registry.EnabledReason, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_di_inspector_never_reads_instance_values()
    {
        var secret = new SecretHolder();
        var services = new ServiceCollection();
        services.AddSingleton(secret);
        services.AddBitBevToolsServices(o => o.Enabled = true);

        using var provider = services.BuildServiceProvider();
        var graph = provider.GetRequiredService<BevToolsRegistry>().ServiceGraph!;

        var info = Assert.ContainsSingle(s => s.ServiceType == typeof(SecretHolder), graph.Services);
        Assert.AreEqual("Instance", info.RegistrationKind);
        Assert.AreEqual(0, secret.Reads);
    }

    private sealed class SecretHolder
    {
        public int Reads { get; private set; }

        public string ConnectionString
        {
            get
            {
                Reads++;
                return "Server=prod;Password=hunter2";
            }
        }
    }

    private sealed class StubEnvironment(string environmentName) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }
}

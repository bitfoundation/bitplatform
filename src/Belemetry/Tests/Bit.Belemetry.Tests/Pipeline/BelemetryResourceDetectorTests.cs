namespace Bit.Belemetry.Tests.Pipeline;

// The environment is process-wide: these tests set and restore it, so they must not overlap with others.
[TestClass]
[DoNotParallelize]
public class BelemetryResourceDetectorTests
{
    private static readonly string[] _envNames =
    [
        "WEBSITE_SITE_NAME", "WEBSITE_INSTANCE_ID", "WEBSITE_RESOURCE_GROUP", "WEBSITE_OWNER_NAME", "WEBSITE_HOME_STAMPNAME", "REGION_NAME",
        "FUNCTIONS_EXTENSION_VERSION", "CONTAINER_APP_NAME", "CONTAINER_APP_REPLICA_NAME", "CONTAINER_APP_REVISION", "OTEL_RESOURCE_ATTRIBUTES",
    ];

    private Dictionary<string, string?> _savedEnv = [];

    [TestInitialize]
    public void SaveEnvironment()
    {
        _savedEnv = _envNames.ToDictionary(n => n, Environment.GetEnvironmentVariable);
        foreach (var name in _envNames) Environment.SetEnvironmentVariable(name, null);
    }

    [TestCleanup]
    public void RestoreEnvironment()
    {
        foreach (var (name, value) in _savedEnv) Environment.SetEnvironmentVariable(name, value);
    }

    private static BelemetrySettings Server(Action<BelemetryOptions>? configure = null) => TestHost.CreateSettings(o =>
    {
        o.ServiceInstanceId = null;
        o.UseEnvironmentVariables = true;
        configure?.Invoke(o);
    }, webHost: true);

    [TestMethod]
    public void Server_ReportsHostAndProcess_DevicesDoNot()
    {
        var a = Server().Resource.Attributes;
        Assert.AreEqual(Environment.MachineName, a["host.name"]);
        Assert.AreEqual((long)Environment.ProcessId, a["process.pid"]);
        Assert.IsNotNull(a["process.executable.name"]);
        Assert.AreEqual(Environment.UserName, a["process.owner"]);

        var hybrid = TestHost.CreateSettings().Resource.Attributes;
        Assert.AreEqual((long)Environment.ProcessId, hybrid["process.pid"]);
        Assert.IsFalse(hybrid.Contains("host.name"), "A device's name is its owner's, not the app's.");
        Assert.IsFalse(hybrid.Contains("process.owner"));
        Assert.IsFalse(hybrid.Contains("process.executable.name"));
    }

    [TestMethod]
    public void InstanceId_ServerKeepsItsNameAcrossRestarts_DevicesGetARandomOne()
    {
        Assert.AreEqual(Environment.MachineName, Server().ServiceInstanceId);
        Assert.AreEqual(Environment.MachineName, TestHost.CreateSettings(o => o.ServiceInstanceId = null, webHost: true).ServiceInstanceId, "Without environment variables too.");

        Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", "abc123");
        Assert.AreEqual("abc123", Server().ServiceInstanceId, "App Service names its instances.");
        Assert.AreEqual(Environment.MachineName, TestHost.CreateSettings(o => o.ServiceInstanceId = null, webHost: true).ServiceInstanceId, "Only with UseEnvironmentVariables.");
        Assert.AreEqual("explicit", Server(o => o.ServiceInstanceId = "explicit").ServiceInstanceId);

        Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", null);
        Environment.SetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME", "myapp--rev1-abc");
        Assert.AreEqual("myapp--rev1-abc", Server().ServiceInstanceId, "Container Apps names its replicas.");

        // An orchestrator's id (the Aspire AppHost hands one per replica) beats the platform's and the machine name.
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.instance.id=replica-7,team=core");
        var orchestrated = Server();
        Assert.AreEqual("replica-7", orchestrated.ServiceInstanceId);
        Assert.AreEqual("replica-7", orchestrated.Resource.Attributes["service.instance.id"]);
        Assert.AreEqual("explicit", Server(o => o.ServiceInstanceId = "explicit").ServiceInstanceId, "Code still wins over the environment.");

        var device = TestHost.CreateSettings(o => o.ServiceInstanceId = null);
        Assert.IsTrue(Guid.TryParse(device.ServiceInstanceId, out _), "A device is one of many and identifies nobody.");
    }

    [TestMethod]
    public void AppService_IsDetectedFromItsEnvironment()
    {
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "shop-web");
        Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", "abc123");
        Environment.SetEnvironmentVariable("WEBSITE_RESOURCE_GROUP", "shop-rg");
        Environment.SetEnvironmentVariable("WEBSITE_OWNER_NAME", "11111111-2222-3333-4444-555555555555+shop-rg-WestEuropewebspace");
        Environment.SetEnvironmentVariable("WEBSITE_HOME_STAMPNAME", "waws-prod-am2-123");
        Environment.SetEnvironmentVariable("REGION_NAME", "West Europe");

        var a = Server().Resource.Attributes;
        Assert.AreEqual("azure", a["cloud.provider"]);
        Assert.AreEqual("azure_app_service", a["cloud.platform"]);
        Assert.AreEqual("West Europe", a["cloud.region"]);
        Assert.AreEqual("waws-prod-am2-123", a["azure.app.service.stamp"]);
        Assert.AreEqual("abc123", a["host.id"]);
        Assert.AreEqual("/subscriptions/11111111-2222-3333-4444-555555555555/resourceGroups/shop-rg/providers/Microsoft.Web/sites/shop-web", a["cloud.resource_id"]);

        Environment.SetEnvironmentVariable("FUNCTIONS_EXTENSION_VERSION", "~4");
        Assert.AreEqual("azure_functions", Server().Resource.Attributes["cloud.platform"]);

        Assert.IsFalse(TestHost.CreateSettings(webHost: true).Resource.Attributes.Contains("cloud.provider"), "Not read without UseEnvironmentVariables.");
        Assert.AreEqual("override", Server(o => o.ResourceAttributes["cloud.region"] = "override").Resource.Attributes["cloud.region"], "Explicit resource attributes win.");
    }

    [TestMethod]
    public void ContainerApps_IsDetectedFromItsEnvironment()
    {
        Environment.SetEnvironmentVariable("CONTAINER_APP_NAME", "shop-api");
        Environment.SetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME", "shop-api--v3-xyz");
        Environment.SetEnvironmentVariable("CONTAINER_APP_REVISION", "shop-api--v3");

        var a = Server().Resource.Attributes;
        Assert.AreEqual("azure", a["cloud.provider"]);
        Assert.AreEqual("azure_container_apps", a["cloud.platform"]);
        Assert.AreEqual("shop-api", a["azure.container_apps.app"]);
        Assert.AreEqual("shop-api--v3", a["azure.container_apps.revision"]);
        Assert.AreEqual("shop-api--v3-xyz", a["host.id"]);
        Assert.IsFalse(a.Contains("cloud.resource_id"));
    }

    [TestMethod]
    public void AppServiceResourceId_NeedsTheOwnerName()
    {
        Assert.IsNull(BelemetryResourceDetector.AppServiceResourceId("site", "rg", null, false));
        Assert.IsNull(BelemetryResourceDetector.AppServiceResourceId("site", null, "sub+rg-region", false));
        Assert.IsNull(BelemetryResourceDetector.AppServiceResourceId("site", "rg", "no-plus-sign", false));
        Assert.AreEqual("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Web/sites/site", BelemetryResourceDetector.AppServiceResourceId("site", "rg", "sub+rg-regionwebspace", true));
    }

    [TestMethod]
    public void ContainerId_IsFoundInMountInfoAndCgroupFiles()
    {
        const string id = "3b4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d";

        string[] mountInfo =
        [
            "22 1 0:21 / /proc rw,nosuid,nodev,noexec,relatime - proc proc rw",
            $"650 639 8:1 /var/lib/docker/containers/{id}/hostname /etc/hostname rw,relatime - ext4 /dev/sda1 rw",
        ];
        Assert.AreEqual(id, BelemetryResourceDetector.FindContainerId(mountInfo, mountInfo: true));

        string[] cgroupV1 = ["12:memory:/docker/" + id, "11:cpu:/docker/" + id];
        Assert.AreEqual(id, BelemetryResourceDetector.FindContainerId(cgroupV1, mountInfo: false));

        string[] systemd = ["0::/system.slice/docker-" + id + ".scope"];
        Assert.AreEqual(id, BelemetryResourceDetector.FindContainerId(systemd, mountInfo: false));

        string[] host = ["0::/user.slice/user-1000.slice/session-2.scope"];
        Assert.IsNull(BelemetryResourceDetector.FindContainerId(host, mountInfo: false));
        Assert.IsNull(BelemetryResourceDetector.FindContainerId(["36 25 0:32 / /sys/fs/cgroup rw - cgroup2 cgroup rw"], mountInfo: true));
    }
}

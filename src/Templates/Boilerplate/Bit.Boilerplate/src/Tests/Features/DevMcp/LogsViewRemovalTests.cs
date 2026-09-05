//+:cnd:noEmit
using System.Reflection;
//#if (signalR == true)
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("UnitTest")]
public class LogsViewRemovalTests
{
    [TestMethod]
    public void SystemFeatures_Should_NotContainLogsView_AndShouldContainDevMcp()
    {
        Assert.IsNull(typeof(AppFeatures.System).GetField("Logs_View", BindingFlags.Public | BindingFlags.Static),
            "Logs_View was a deletion, not a rename. It must not remain as a feature constant.");

        var devMcp = typeof(AppFeatures.System).GetField("DevMcp", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(devMcp, "System.DevMcp must exist as the global-admin-only feature for /dev-mcp.");
        Assert.AreEqual("2.2", devMcp.GetRawConstantValue());
        Assert.AreEqual("2.1", typeof(AppFeatures.System).GetField("Jobs_Manage")!.GetRawConstantValue());
    }

    //#if (signalR == true)
    [TestMethod]
    public void SharedAppMessages_Should_NotContainSessionLogUpload()
    {
        var names = typeof(SharedAppMessages).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => field.Name).ToArray();
        Assert.DoesNotContain("UPLOAD_DIAGNOSTIC_LOGGER_STORE", names);
        Assert.DoesNotContain("GetUserSessionLogs", names);
    }

    [TestMethod]
    public void AppHub_Should_NotExposeGetUserSessionLogs()
    {
        Assert.IsNull(typeof(AppHub).GetMethod("GetUserSessionLogs", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
    }
    //#endif
}

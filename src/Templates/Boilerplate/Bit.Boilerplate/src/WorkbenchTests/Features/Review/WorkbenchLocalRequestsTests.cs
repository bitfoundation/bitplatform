namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// Only someone at this computer may reach the agent: a request through a tunnel connects from loopback too, so the host
/// name and the headers tunnels add are what tell it apart.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchLocalRequestsTests
{
    private static DefaultHttpContext Request(string host, IPAddress? from = null)
    {
        var context = new DefaultHttpContext();

        context.Connection.RemoteIpAddress = from ?? IPAddress.Loopback;
        context.Request.Host = HostString.FromUriComponent(host);

        return context;
    }

    [TestMethod]
    [DataRow("localhost:6030")]
    [DataRow("127.0.0.1:6030")]
    [DataRow("[::1]:6030")]
    [DataRow("localhost")]
    public void ALoopbackRequestToALocalHostName_Should_BeLocal(string host)
    {
        Assert.IsTrue(WorkbenchLocalRequests.IsLocal(Request(host)));
        Assert.IsTrue(WorkbenchLocalRequests.IsLocal(Request(host, IPAddress.IPv6Loopback)));
    }

    [TestMethod]
    [DataRow("X-Forwarded-For")]
    [DataRow("X-Forwarded-Host")]
    [DataRow("X-Forwarded-Proto")]
    [DataRow("Forwarded")]
    [DataRow("Cf-Connecting-Ip")]
    [DataRow("X-Real-IP")]
    [DataRow("X-Original-For")]
    [DataRow("True-Client-IP")]
    [DataRow("Via")]
    public void ARequestWithAHeaderATunnelAdds_Should_NotBeLocal(string header)
    {
        var context = Request("localhost:6030");

        context.Request.Headers[header] = "203.0.113.7";

        Assert.IsFalse(WorkbenchLocalRequests.IsLocal(context));
    }

    [TestMethod]
    [DataRow("workbench.example.com")]
    [DataRow("abc-def.trycloudflare.com")]
    [DataRow("192.168.1.20:6030")]
    public void ARequestForAnotherHostName_Should_NotBeLocal(string host)
    {
        Assert.IsFalse(WorkbenchLocalRequests.IsLocal(Request(host)));
    }

    [TestMethod]
    public void ARequestFromAnotherMachine_Should_NotBeLocal()
    {
        Assert.IsFalse(WorkbenchLocalRequests.IsLocal(Request("localhost:6030", IPAddress.Parse("192.168.1.20"))));
        Assert.IsFalse(WorkbenchLocalRequests.IsLocal(new DefaultHttpContext()), "A request without an address isn't local.");
    }
}

using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Drives the harness against the worker scripts in
/// <c>Bit.Butil.Samples.Web/wwwroot/workers</c> - a worker runs a script you supply, so the harness
/// has to supply one.
/// </summary>
[TestClass]
public class WorkerTests : ButilPageTest
{
    [TestMethod]
    public async Task A_Worker_Answers_And_Sees_The_Name_It_Was_Given()
    {
        // Not binary, and the reply carries self.name - which is what WorkerOptions.Name set.
        await ClickAndExpectAsync("worker-echo", """worker:echo:False/{"op":"echo","payload":"ping","name":"butil-e2e"}""");
    }

    [TestMethod]
    public async Task Bytes_Round_Trip_As_Binary()
    {
        // The worker increments every byte, so this is a real transfer both ways rather than the
        // page reporting the array it sent.
        await ClickAndExpectAsync("worker-binary", "worker:binary:True/[2,3,4,5]");
    }

    [TestMethod]
    public async Task An_Uncaught_Throw_Reaches_OnError_And_Leaves_The_Worker_Running()
    {
        await ClickAndExpectAsync("worker-error", "worker:error:True/True");
    }

    [TestMethod]
    public async Task A_Port_Can_Be_Transferred_To_The_Worker_And_Answered_On()
    {
        await ClickAndExpectAsync("worker-port", """worker:port:True/{"op":"fromWorker","echoOf":{"hello":"worker"}}""");
    }

    [TestMethod]
    public async Task A_Shared_Worker_Reports_Its_Connection_Count()
    {
        await ClickAndExpectAsync("worker-shared", """worker:shared:{"op":"count","connections":1}""");
    }

    [TestMethod]
    public async Task Posting_To_A_Terminated_Worker_Is_Reported_Rather_Than_Thrown()
    {
        await ClickAndExpectAsync("worker-echo", "worker:echo:False/");
        await ClickAndExpectAsync("worker-terminate", "worker:terminate:False");
    }
}

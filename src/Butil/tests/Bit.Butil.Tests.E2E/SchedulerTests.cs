using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class SchedulerTests : ButilPageTest
{
    [TestMethod]
    public async Task RequestAnimationFrame_Fires_With_A_Timestamp()
    {
        await ClickAndExpectAsync("sched-frame", "sched:frame:yes");
    }

    [TestMethod]
    public async Task A_Frame_Loop_Runs_Every_Frame_And_Disposing_Stops_It()
    {
        // Five frames seen, and no further frames after the subscription was disposed - disposing
        // has to cancel the loop, not merely detach the handler.
        await ClickAndExpectAsync("sched-loop", "sched:loop:True/True");
    }

    [TestMethod]
    public async Task An_Idle_Callback_On_An_Idle_Page_Reports_Slack_Rather_Than_A_Timeout()
    {
        await ClickAndExpectAsync("sched-idle", "sched:idle:False/True");
    }

    [TestMethod]
    public async Task An_Idle_Callback_On_A_Busy_Page_Runs_Because_Of_Its_Timeout()
    {
        await ClickAndExpectAsync("sched-idle-timeout", "sched:idle-timeout:True");
    }

    [TestMethod]
    public async Task PostTask_Runs_The_Work_And_Reports_No_Error()
    {
        await ClickAndExpectAsync("sched-task", "sched:task:True/True");
    }

    [TestMethod]
    public async Task A_Task_Whose_Signal_Is_Already_Aborted_Never_Runs()
    {
        // An error reported, and the work not run - true whether the real scheduler or the timeout
        // fallback handled it.
        await ClickAndExpectAsync("sched-task-abort", "sched:task-abort:True/False");
    }

    [TestMethod]
    public async Task Yield_Returns_And_IsInputPending_Answers()
    {
        await ClickAndExpectAsync("sched-yield", "sched:yield:ok/True");
    }
}

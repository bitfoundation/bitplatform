using Bit.Cli.Infrastructure;
using Spectre.Console.Testing;

namespace Bit.Cli.Tests;

[TestClass]
public class AgentIdeasTests
{
    [TestMethod]
    public void TheIdeas_Should_ComeInTheirOrder()
    {
        Assert.AreSequenceEqual(new[] { "Run", "Rebrand", "Port", "Deploy", "Feature", "Fix" }, AgentIdeas.All.Select(i => i.Kind).ToArray());
    }

    [TestMethod]
    [DataRow(64)]
    [DataRow(80)]
    [DataRow(110)]
    public void ThePanel_Should_KeepItsHeightAndFitTheWidth(int width)
    {
        for (var seconds = 0.0; seconds < AgentIdeas.CycleSeconds; seconds += 0.2)
        {
            var console = new TestConsole().Width(width);
            console.Profile.Capabilities.Unicode = true;
            console.Write(AgentIdeas.Render(seconds, width, "⠋"));

            var lines = console.Output.TrimEnd('\n').Split('\n');

            Assert.HasCount(AgentIdeas.Rows + 2, lines, $"at {seconds:0.0}s");
            Assert.IsTrue(lines.All(l => l.TrimEnd().Length <= width), $"at {seconds:0.0}s");
        }
    }

    [TestMethod]
    public void TheLastIdea_Should_BeShownBeforeTheCycleRestarts()
    {
        var console = new TestConsole().Width(110);
        console.Profile.Capabilities.Unicode = true;
        console.Write(AgentIdeas.Render(AgentIdeas.CycleSeconds - 2, 110, "⠋"));

        Assert.Contains("Fixed on your Test site, ready to go to Production", console.Output);
    }

    [TestMethod]
    public async Task AStep_Should_ReturnItsResultUnderTheIdeas()
    {
        var output = new TestConsole().Width(100).Interactive();
        output.Profile.Capabilities.Unicode = true;
        output.Profile.Height = 40;
        var console = new CliConsole(output, new TestConsole()) { Ideas = new AgentIdeas() };

        var result = await console.RunWithStatusAsync("Installing the .NET SDK", async progress =>
        {
            progress("downloading");
            await Task.Delay(300, TestContext.CancellationToken);
            return 42;
        });

        Assert.AreEqual(42, result);
        Assert.IsGreaterThan(TimeSpan.Zero, console.Ideas.Shown);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}

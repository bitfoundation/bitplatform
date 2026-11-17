namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// Which way a task's texts read. An agent writing Persian keeps English names and terms in it, so the task's first word and
/// most of its letters are often Latin; it still reads right to left, while a line with no Persian in it, like a command,
/// reads left to right.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchTextDirectionTests
{
    private static WorkbenchProgressTask Task(string title, string? summary, string[] steps, string? language = null)
    {
        return new("t", title, summary, "Claude Code", WorkbenchProgressStatus.Working, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null,
                   [.. steps.Select((text, index) => new WorkbenchProgressStep($"s{index}", text, WorkbenchProgressOwner.Agent, WorkbenchProgressStepStatus.Todo, null, null))],
                   [], [], [], [])
        {
            Language = language
        };
    }

    // An agent's task, word for word, with under half of its letters Persian.
    private static readonly WorkbenchProgressTask persianTask = Task("ارتقا به template 1.2.3-pre-04 و telemetry صفحه‌ها و اپ‌ها",
                                                                     "bit Fabric روی template 1.2.3-pre-04 است و روی Test کار می‌کند.",
                                                                     ["bit ci روی bit Fabric", "squash-merge به main (Production)", "rule تونل Cloudflare برای telemetry روی Test و Production"]);

    [TestMethod]
    public void APersianTaskFullOfEnglishTerms_Should_ReadRightToLeft()
    {
        Assert.AreEqual(WorkbenchTextDirection.RightToLeft, WorkbenchTextDirection.Of(persianTask));
    }

    [TestMethod]
    [DataRow("bit ci روی bit Fabric", WorkbenchTextDirection.RightToLeft)]
    [DataRow("squash-merge به main (Production)", WorkbenchTextDirection.RightToLeft)]
    [DataRow("bit ci passed (exit 0)", WorkbenchTextDirection.LeftToRight)]
    [DataRow("`dotnet test`", WorkbenchTextDirection.LeftToRight)]
    [DataRow("1.2.3-pre-04", WorkbenchTextDirection.LeftToRight)]
    [DataRow("۲۰۲۶-۱۱-۱۵", WorkbenchTextDirection.RightToLeft)]
    public void ATextOfAPersianTask_Should_ReadRightToLeftOnlyWithPersianInIt(string text, string direction)
    {
        Assert.AreEqual(direction, WorkbenchTextDirection.Of(text, WorkbenchTextDirection.Of(persianTask)));
    }

    [TestMethod]
    public void AnEnglishTask_Should_ReadLeftToRightAndAPersianLineInItRightToLeft()
    {
        var task = Task("Loyalty points", "Customers earn points on every order and spend them at checkout.", ["Build the ledger (v2.1)", "Translate \"Welcome\""]);

        Assert.AreEqual(WorkbenchTextDirection.LeftToRight, WorkbenchTextDirection.Of(task));
        Assert.AreEqual(WorkbenchTextDirection.LeftToRight, WorkbenchTextDirection.Of("Translate \"خوش آمدید\" on the home page and its menu", WorkbenchTextDirection.LeftToRight));
        Assert.AreEqual(WorkbenchTextDirection.RightToLeft, WorkbenchTextDirection.Of("PR رو merge کن", WorkbenchTextDirection.LeftToRight));
    }

    [TestMethod]
    public void TheLanguageTheAgentGives_Should_DecideOverTheWords()
    {
        Assert.AreEqual(WorkbenchTextDirection.RightToLeft, WorkbenchTextDirection.Of(Task("Deploy to Test", null, ["bit ci"], language: "fa")));
        Assert.AreEqual(WorkbenchTextDirection.LeftToRight, WorkbenchTextDirection.Of(Task("ارتقا به template 1.2.3-pre-04", null, [], language: "en")));
        Assert.AreEqual(WorkbenchTextDirection.RightToLeft, WorkbenchTextDirection.Of(Task("Deploy", null, [], language: "ar-SA")));
        Assert.AreEqual(BitDir.Rtl, WorkbenchTextDirection.ToBitDir(WorkbenchTextDirection.RightToLeft));
    }
}

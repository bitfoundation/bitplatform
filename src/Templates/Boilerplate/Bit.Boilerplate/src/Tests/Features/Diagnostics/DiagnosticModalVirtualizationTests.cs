using Bunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Boilerplate.Client.Core.Components.Layout.Diagnostic;
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// A parameter name that no longer exists is the quietest failure a Bit.BlazorUI component has: every one of them
/// inherits <c>BitComponentBase.SetParametersAsync</c>, whose <c>default:</c> arm puts an unrecognised parameter into
/// <c>HtmlAttributes</c>. So the markup compiles, the app runs, the attribute is splatted onto the DOM as a stray
/// <c>EnableVirtualization</c>, and the feature it was meant to switch on is simply off. Nothing errors, nothing
/// warns, and the only symptom is that a list which is supposed to render a window renders all thousand of its rows.
/// <para>
/// The diagnostic modal is where that costs the most: <c>DiagnosticLogger</c> keeps up to 1000 entries, each row is
/// six components, and the modal is what a user is asked to open when something has already gone wrong.
/// </para>
/// <para>
/// This test asserts the observable consequence rather than the spelling, so it stays true if the parameter is
/// renamed again: with far more entries than fit on a screen, the list must render a window of them, not all.
/// </para>
/// </summary>
[TestClass]
public class DiagnosticModalVirtualizationTests
{
    private const int LogCount = 300;

    [TestMethod]
    public async Task DiagnosticModal_Should_VirtualizeTheLogList()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        WriteLogs();

        try
        {
            var cut = ctx.Render<AppDiagnosticModal>();

            // The modal shows itself only when something asks it to - the header spacer, the keyboard shortcut, or
            // the error boundary.
            ctx.Services.GetRequiredService<PubSubService>().Publish(ClientAppMessages.SHOW_DIAGNOSTIC_MODAL);

            cut.WaitForAssertion(() => Assert.Contains(MessagePrefix, cut.Markup), timeout: TimeSpan.FromSeconds(10));

            // Every row renders exactly one "Details" button, so counting those counts the rows without depending on
            // any class name the component library owns.
            var renderedRows = cut.FindAll("[title='Details']").Count;

            Assert.IsGreaterThan(0, renderedRows, "No log row rendered at all, so this test is not looking at the list.");

            Assert.IsLessThan(LogCount, renderedRows,
                $"All {LogCount} log entries were rendered at once, so the list is not virtualized. The most likely " +
                $"cause is a BitBasicList parameter that no longer exists - it is swallowed into HtmlAttributes and " +
                $"emitted as a stray DOM attribute instead of switching anything on.");

            // Belt and braces on the cause, because the consequence above could one day be produced some other way:
            // a parameter name that reaches the DOM is always a mistake.
            Assert.DoesNotContain("EnableVirtualization", cut.Markup,
                "A PascalCase parameter name reached the rendered html, which means the component did not recognise it.");
        }
        finally
        {
            DiagnosticLogger.ClearStore();
        }
    }

    private const string MessagePrefix = "b31-log-entry-";

    /// <summary>
    /// The store is process-wide and shared with everything else in the run, so it is emptied first and last.
    /// The clock is fake but advanced, so the entries sort in a defined order.
    /// </summary>
    private static void WriteLogs()
    {
        DiagnosticLogger.ClearStore();

        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-08-23T10:00:00Z"));

        var logger = new DiagnosticLogger(timeProvider) { Category = nameof(DiagnosticModalVirtualizationTests) };

        for (var i = 0; i < LogCount; i++)
        {
            // Error level so the entries pass the modal's default filter in every environment: outside Development
            // it starts at Warning, and Information entries would be filtered out and prove nothing.
            logger.Log(LogLevel.Error, default, $"{MessagePrefix}{i}", null, (state, _) => state);

            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        }

        Assert.AreEqual(LogCount, DiagnosticLogger.Store.Count);
    }

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;
}

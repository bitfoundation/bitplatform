//+:cnd:noEmit
using System.Reflection;

namespace Boilerplate.Tests.Extensions;

/// <summary>
/// Captures Playwright video recording functionality for test methods.
/// </summary>
public static class PlaywrightVideoRecordingExtensions
{
    //Pass full name of the test method to 'testMethodFullName' param or it will be inferred from the test context
    public static async Task FinalizeVideoRecording(this IBrowserContext browserContext, TestContext testContext, string? testMethodFullName = null)
    {
        await browserContext.CloseAsync();
        if (testContext.CurrentTestOutcome is not UnitTestOutcome.Failed)
        {
            var directory = GetVideoDirectory(testContext, testMethodFullName);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    //Pass full name of the test method to 'testMethodFullName' param or it will be inferred from the test context
    public static BrowserNewContextOptions EnableVideoRecording(this BrowserNewContextOptions options, TestContext testContext, string? testMethodFullName = null)
    {
        options.RecordVideoDir = GetVideoDirectory(testContext, testMethodFullName);
        return options;
    }

    private static string GetVideoDirectory(TestContext testContext, string? testMethodFullName = null)
    {
        testMethodFullName ??= $"{testContext.FullyQualifiedTestClassName}.{GetTestMethodName(testContext)}";

        // Remove invalid characters from the test method name
        char[] notAllowedChars = [')', '"', '<', '>', '|', '*', '?', '\r', '\n', .. Path.GetInvalidFileNameChars()];
        testMethodFullName = new string(testMethodFullName.Where(ch => !notAllowedChars.Contains(ch)).ToArray()).Replace('(', '_').Replace(',', '_');

        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Videos", testMethodFullName);
        return Path.GetFullPath(dir);
    }

    private static string GetTestMethodName(TestContext testContext)
    {
        return testContext.TestName!;
    }
}

//+:cnd:noEmit
namespace Microsoft.Playwright;

/// <summary>
/// Captures Playwright video recording functionality for test methods.
/// </summary>
public static class PlaywrightVideoRecordingExtensions
{
    extension(IBrowserContext browserContext)
    {
        //Pass full name of the test method to 'testMethodFullName' param or it will be inferred from the test context
        public async Task FinalizeVideoRecording(TestContext testContext, string? testMethodFullName = null)
        {
            try
            {
                await browserContext.CloseAsync();
            }
            catch (PlaywrightException)
            {
                // Firefox intermittently fails Browser.removeBrowserContext while tearing its window down. A context
                // that refuses to close is the driver's problem; failing the test it belonged to hides its real result.
            }

            if (testContext.CurrentTestOutcome is not UnitTestOutcome.Failed)
            {
                var directory = GetVideoDirectory(testContext, testMethodFullName);
                try
                {
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                    // Housekeeping: a video the driver still holds must not turn a passing test into a failed one.
                }
            }
        }
    }

    extension(BrowserNewContextOptions options)
    {
        //Pass full name of the test method to 'testMethodFullName' param or it will be inferred from the test context
        public BrowserNewContextOptions EnableVideoRecording(TestContext testContext, string? testMethodFullName = null)
        {
            options.RecordVideoDir = GetVideoDirectory(testContext, testMethodFullName);
            return options;
        }
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

    /// <summary>
    /// The display name, not the method name: the data rows of one method run in parallel, and sharing a folder let
    /// one row's cleanup delete it under another row's recording.
    /// </summary>
    private static string GetTestMethodName(TestContext testContext)
    {
        return testContext.TestDisplayName ?? testContext.TestName!;
    }
}

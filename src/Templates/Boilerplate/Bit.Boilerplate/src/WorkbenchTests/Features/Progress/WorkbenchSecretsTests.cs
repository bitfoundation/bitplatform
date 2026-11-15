using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// Where the Workbench puts a secret the user pastes: a GitHub secret reaches gh on its input, never on a command line, and a
/// file holds exactly what was pasted until its task ends.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchSecretsTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>Records what gh would have been run with, and answers with the problem it is given.</summary>
    private sealed class RecordingSecrets(string? problem = null) : WorkbenchSecrets(NullLogger<WorkbenchSecrets>.Instance)
    {
        public List<(string[] Arguments, string Input)> Calls { get; } = [];

        protected override Task<string?> RunGitHubCli(IReadOnlyList<string> arguments, string input, CancellationToken cancellationToken)
        {
            Calls.Add(([.. arguments], input));
            return Task.FromResult(problem);
        }
    }

    private static WorkbenchProgressAsk Ask(string id, WorkbenchProgressSecret secret)
    {
        return new(id, "Key?", null, WorkbenchProgressAskKind.Secret, [], null, DateTimeOffset.UtcNow, null, false) { Secret = secret };
    }

    [TestMethod]
    public async Task AGitHubSecret_Should_ReachGhOnItsInputOnly()
    {
        var secrets = new RecordingSecrets();

        Assert.IsNull(await secrets.Store("t1", Ask("smtp", new(WorkbenchProgressSecretTo.GitHub, "SMTP_CONNECTION_STRING", "Test")), "Password=hunter2", TestContext.CancellationToken));
        Assert.IsNull(await secrets.Store("t1", Ask("sentry", new(WorkbenchProgressSecretTo.GitHub, "SENTRY_AUTH_TOKEN", null)), "sntrys_42", TestContext.CancellationToken));

        Assert.AreSequenceEqual(new[] { "secret", "set", "SMTP_CONNECTION_STRING", "--env", "Test" }, secrets.Calls[0].Arguments);
        Assert.AreEqual("Password=hunter2", secrets.Calls[0].Input);
        Assert.AreSequenceEqual(new[] { "secret", "set", "SENTRY_AUTH_TOKEN" }, secrets.Calls[1].Arguments, "Without an environment, it is the repository's.");
        Assert.DoesNotContain(a => a.Contains("hunter2", StringComparison.Ordinal) || a.Contains("sntrys", StringComparison.Ordinal), secrets.Calls.SelectMany(c => c.Arguments));
    }

    [TestMethod]
    public async Task WhatGhSaysWhenItFails_Should_ComeBack()
    {
        var secrets = new RecordingSecrets("failed to fetch public key: HTTP 404: Not Found");

        Assert.AreEqual("failed to fetch public key: HTTP 404: Not Found", await secrets.Store("t2", Ask("smtp", new(WorkbenchProgressSecretTo.GitHub, "SMTP_CONNECTION_STRING", "Staging")), "x", TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task AFileSecret_Should_HoldExactlyWhatWasPastedUntilItsTaskEnds()
    {
        var secrets = new RecordingSecrets();
        var taskId = Guid.NewGuid().ToString("N")[..12];
        var path = WorkbenchSecrets.FilePath(taskId, "token");

        Assert.IsNull(await secrets.Store(taskId, Ask("token", new(WorkbenchProgressSecretTo.File, null, null)), "cf_tök€n", TestContext.CancellationToken));

        Assert.AreSequenceEqual(Encoding.UTF8.GetBytes("cf_tök€n"), await File.ReadAllBytesAsync(path, TestContext.CancellationToken), "No mark before it and no line break after it.");
        Assert.AreEqual(Path.GetFullPath(WorkbenchFolders.Secrets), Path.GetDirectoryName(Path.GetFullPath(path)));
        Assert.IsEmpty(secrets.Calls);

        WorkbenchSecrets.DeleteFiles(taskId);

        Assert.IsFalse(File.Exists(path));
    }
}

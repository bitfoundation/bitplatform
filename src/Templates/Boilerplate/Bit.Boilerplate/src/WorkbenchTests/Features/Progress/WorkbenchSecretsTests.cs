using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// Where the Workbench puts a secret the user pastes: a GitHub secret reaches gh, and a user secret dotnet user-secrets, on
/// its input, never on a command line, and a file holds exactly what was pasted until its task ends.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchSecretsTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>Records what gh or dotnet would have been run with, and answers with the problem it is given.</summary>
    private sealed class RecordingSecrets(string? problem = null) : WorkbenchSecrets(NullLogger<WorkbenchSecrets>.Instance)
    {
        public List<(string Tool, string[] Arguments, string Input)> Calls { get; } = [];

        protected override Task<string?> RunTool(string tool, IReadOnlyList<string> arguments, string input, CancellationToken cancellationToken)
        {
            Calls.Add((tool, [.. arguments], input));
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

        Assert.AreEqual("gh", secrets.Calls[0].Tool);
        Assert.AreSequenceEqual(new[] { "secret", "set", "SMTP_CONNECTION_STRING", "--env", "Test" }, secrets.Calls[0].Arguments);
        Assert.AreEqual("Password=hunter2", secrets.Calls[0].Input);
        Assert.AreSequenceEqual(new[] { "secret", "set", "SENTRY_AUTH_TOKEN" }, secrets.Calls[1].Arguments, "Without an environment, it is the repository's.");
        Assert.DoesNotContain(a => a.Contains("hunter2", StringComparison.Ordinal) || a.Contains("sntrys", StringComparison.Ordinal), secrets.Calls.SelectMany(c => c.Arguments));
    }

    /// <summary>
    /// Points the Workbench, for the length of a test, at a repository of its own under the temp folder, with a server project
    /// in it. Not in parallel: the project folder is one for the whole process.
    /// </summary>
    private static async Task InARepository(Func<string, Task> test)
    {
        var root = Path.Combine(Path.GetTempPath(), $"wb-secrets-{Guid.NewGuid():N}");
        var projectBefore = WorkbenchFolders.Project;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, ".git"));
            await File.WriteAllTextAsync(Path.Combine(root, ".git", "HEAD"), "ref: refs/heads/main\n");
            Directory.CreateDirectory(Path.Combine(root, "src", "Server", "MyApp.Server.Web"));
            WorkbenchFolders.Project = Directory.CreateDirectory(Path.Combine(root, "src", "Workbench")).FullName;

            await test(root);
        }
        finally
        {
            WorkbenchFolders.Project = projectBefore;
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod, DoNotParallelize]
    public Task AUserSecret_Should_ReachDotnetUserSecretsOnItsInputOnly() => InARepository(async root =>
    {
        var secrets = new RecordingSecrets();

        Assert.IsNull(await secrets.Store("t3", Ask("password", new(WorkbenchProgressSecretTo.UserSecrets, "TestAccount:Password", null, "src/Server/MyApp.Server.Web")), "p\"ss wörd", TestContext.CancellationToken));

        var (tool, arguments, input) = secrets.Calls.Single();

        Assert.AreEqual("dotnet", tool);
        Assert.AreSequenceEqual(new[] { "user-secrets", "set", "--project", Path.GetFullPath(Path.Combine(root, "src", "Server", "MyApp.Server.Web")) }, arguments);
        Assert.AreEqual("p\"ss wörd", JsonNode.Parse(input)!["TestAccount:Password"]!.GetValue<string>(), "Read as JSON from its input.");
        Assert.DoesNotContain(a => a.Contains("ss w", StringComparison.Ordinal), arguments);
    });

    [TestMethod, DoNotParallelize]
    public Task AUserSecretOfAProjectOutsideTheRepository_Should_GoNowhere() => InARepository(async _ =>
    {
        var secrets = new RecordingSecrets();

        Assert.Contains("isn't in this repository", (await secrets.Store("t4", Ask("password", new(WorkbenchProgressSecretTo.UserSecrets, "TestAccount:Password", null, "../elsewhere")), "x", TestContext.CancellationToken))!);
        Assert.Contains("isn't in this repository", (await secrets.Store("t4", Ask("password", new(WorkbenchProgressSecretTo.UserSecrets, "TestAccount:Password", null, "src/Server/Missing")), "x", TestContext.CancellationToken))!);
        Assert.IsEmpty(secrets.Calls);
    });

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

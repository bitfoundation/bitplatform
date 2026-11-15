using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Puts a secret the user pastes into a secret question where the question says, so it never passes through the agent, the
/// chat, the review document or a log: into a GitHub secret through gh, GitHub's command line tool, into a user secret of one
/// of the repository's projects through dotnet user-secrets, each reading it from its input rather than its command line, or
/// into a file outside the repository that the agent's commands read. Those files go once their task ends.
/// </summary>
public class WorkbenchSecrets(ILogger<WorkbenchSecrets> logger)
{
    private static readonly TimeSpan timeout = TimeSpan.FromMinutes(1);

    /// <summary>The file a secret question's answer goes into, named after its task and question.</summary>
    public static string FilePath(string taskId, string askId) => Path.Combine(WorkbenchFolders.Secrets, $"{taskId}-{askId}");

    /// <summary>Stores the secret where the question says; returns what went wrong, or null once it is there.</summary>
    public async Task<string?> Store(string taskId, WorkbenchProgressAsk ask, string secret, CancellationToken cancellationToken)
    {
        // An agent may have changed a question's kind after saying where its secret goes, without saying all of it.
        if (ask.Secret is not { } target
            || (target.To is WorkbenchProgressSecretTo.GitHub && target.Name is null)
            || (target.To is WorkbenchProgressSecretTo.UserSecrets && (target.Name is null || target.Project is null)))
            return "The question doesn't say where the secret goes.";

        if (target.To is WorkbenchProgressSecretTo.File)
        {
            try
            {
                WriteFile(FilePath(taskId, ask.Id), secret);
                return null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(exception, "The secret of the question {AskId} couldn't be written to its file.", ask.Id);
                return exception.Message;
            }
        }

        if (target.To is WorkbenchProgressSecretTo.UserSecrets)
            return await StoreUserSecret(target, secret, cancellationToken);

        string[] arguments = target.Environment is { } environment ? ["secret", "set", target.Name!, "--env", environment] : ["secret", "set", target.Name!];

        var problem = await RunTool("gh", arguments, secret, cancellationToken);

        if (problem is not null)
        {
            logger.LogWarning("gh couldn't store the secret {Name} of {Environment}: {Problem}", target.Name, target.Environment ?? "the repository", problem);
        }

        return problem;
    }

    /// <summary>
    /// Sets the secret in the user secrets of a project of the repository, on this computer only: dotnet user-secrets reads it
    /// as JSON from its input, so it stays off the command line too. The project has to have a UserSecretsId.
    /// </summary>
    private async Task<string?> StoreUserSecret(WorkbenchProgressSecret target, string secret, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(WorkbenchFolders.WorkingTree ?? WorkbenchFolders.Source ?? Environment.CurrentDirectory);
        var project = Path.GetFullPath(Path.Combine(root, target.Project!));
        // Two folders differ by case alone only where the file system tells them apart.
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (project.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, comparison) is false
            || (File.Exists(project) || Directory.Exists(project)) is false)
            return $"The project {target.Project} isn't in this repository.";

        var problem = await RunTool("dotnet", ["user-secrets", "set", "--project", project], JsonSerializer.Serialize(new Dictionary<string, string> { [target.Name!] = secret }), cancellationToken);

        if (problem is not null)
        {
            logger.LogWarning("dotnet user-secrets couldn't store {Name} for {Project}: {Problem}", target.Name, target.Project, problem);
        }

        return problem;
    }

    /// <summary>Deletes the files a task's secrets went into; an id that isn't a task's, like a wildcard, deletes nothing.</summary>
    public static void DeleteFiles(string taskId)
    {
        if (taskId.Length == 0 || taskId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') is false || Directory.Exists(WorkbenchFolders.Secrets) is false)
            return;

        foreach (var file in Directory.EnumerateFiles(WorkbenchFolders.Secrets, $"{taskId}-*"))
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// Runs gh or dotnet in the working tree with the secret as its input, which keeps it off every command line; returns the
    /// tool's error, or null.
    /// </summary>
    protected virtual async Task<string?> RunTool(string tool, IReadOnlyList<string> arguments, string input, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(tool)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            WorkingDirectory = WorkbenchFolders.WorkingTree ?? WorkbenchFolders.Source ?? Environment.CurrentDirectory
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["GH_PROMPT_DISABLED"] = "1";
        start.Environment["NO_COLOR"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";

        using var process = new Process { StartInfo = start };

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            return $"{tool}: {exception.Message}";
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            var error = process.StandardError.ReadToEndAsync(timeoutSource.Token);
            var output = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);

            try
            {
                await process.StandardInput.WriteAsync(input.AsMemory(), timeoutSource.Token);
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The tool quit before reading it, and its error says why.
            }

            await process.WaitForExitAsync(timeoutSource.Token);
            await output;

            return process.ExitCode == 0 ? null : (await error).Trim() is { Length: > 0 } message ? message : $"{tool} exited with {process.ExitCode}.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return $"{tool} didn't finish within {timeout.TotalSeconds:0} seconds.";
        }
        finally
        {
            if (process.HasExited is false)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    /// <summary>Writes the secret as it is, with no line break after it, readable by this user only.</summary>
    private static void WriteFile(string path, string secret)
    {
        var folder = Path.GetDirectoryName(path)!;
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Directory.CreateDirectory(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        else
        {
            Directory.CreateDirectory(folder);
        }

        File.Delete(path);

        using var stream = new FileStream(path, options);

        stream.Write(Encoding.UTF8.GetBytes(secret));
    }
}

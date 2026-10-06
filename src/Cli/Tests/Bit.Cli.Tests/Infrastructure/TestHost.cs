using System.Runtime.InteropServices;
using Bit.Cli.Infrastructure;
using Spectre.Console.Testing;

namespace Bit.Cli.Tests.Infrastructure;

public sealed class TestHost : IDisposable
{
    public TestHost(HostOs? os = null, IDictionary<string, string>? variables = null, IPrompter? prompter = null)
    {
        Root = Directory.CreateTempSubdirectory("bit-cli-tests-").FullName;
        Home = Directory.CreateDirectory(Path.Combine(Root, "home")).FullName;
        WorkingDirectory = Directory.CreateDirectory(Path.Combine(Root, "work")).FullName;

        var allVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PATH"] = "",
            ["ProgramFiles"] = Path.Combine(Root, "ProgramFiles"),
            ["ProgramFiles(x86)"] = Path.Combine(Root, "ProgramFilesX86"),
            ["LOCALAPPDATA"] = Path.Combine(Root, "LocalAppData"),
            ["APPDATA"] = Path.Combine(Root, "AppData"),
            ["XDG_CONFIG_HOME"] = Path.Combine(Root, "config")
        };

        foreach (var (key, value) in variables ?? new Dictionary<string, string>())
        {
            allVariables[key] = value;
        }

        Environment = new CliEnvironment
        {
            Os = os ?? (OperatingSystem.IsWindows() ? HostOs.Windows : OperatingSystem.IsMacOS() ? HostOs.MacOS : HostOs.Linux),
            Architecture = Architecture.X64,
            HomeDirectory = Home,
            CurrentDirectory = WorkingDirectory,
            Variables = allVariables,
            IsInputRedirected = true,
            IsOutputRedirected = true
        };

        Out = new TestConsole().Width(140);
        Error = new TestConsole().Width(140);
        Console = new CliConsole(Out, Error);
        Services = new CliServices(Environment, Console, prompter, Runner);
    }

    public string Root { get; }

    public string Home { get; }

    public string WorkingDirectory { get; }

    public CliEnvironment Environment { get; }

    public TestConsole Out { get; }

    public TestConsole Error { get; }

    public CliConsole Console { get; }

    public FakeProcessRunner Runner { get; } = new();

    public CliServices Services { get; }

    public string Output => Out.Output + Error.Output;

    public void AllToolsInstalled()
    {
        Runner.On("git", "--version", 0, "git version 2.47.1");
        Runner.On("node", "--version", 0, "v24.9.0");
        Runner.On("docker", "version", 0, "28.5.1");
        Runner.On("aspire", "--version", 0, "13.6.0");
        Runner.Executables["code"] = typeof(TestHost).Assembly.Location;
    }

    public async Task<int> RunAsync(params string[] args)
    {
        return await CliApp.BuildRootCommand(() => Services)
            .Parse(args)
            .InvokeAsync(new System.CommandLine.InvocationConfiguration { EnableDefaultExceptionHandler = false, Output = new StringWriter(), Error = new StringWriter() });
    }

    public void Dispose()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string RepositoryFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);

            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(string.Join('/', parts));
    }
}

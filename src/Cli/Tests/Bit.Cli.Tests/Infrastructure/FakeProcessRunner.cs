using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tests.Infrastructure;

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<(Func<ProcessSpec, bool> Match, Func<ProcessSpec, ProcessResult> Result)> rules = [];

    public Dictionary<string, string> Executables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<ProcessSpec> Calls { get; } = [];

    public ProcessResult Default { get; set; } = new() { ExitCode = 0 };

    public FakeProcessRunner On(string fileName, string argumentsStartWith, Func<ProcessSpec, ProcessResult> respond)
    {
        rules.Add((spec => string.Equals(Path.GetFileNameWithoutExtension(spec.FileName), fileName, StringComparison.OrdinalIgnoreCase)
            && string.Join(' ', spec.Arguments).StartsWith(argumentsStartWith, StringComparison.Ordinal), respond));
        return this;
    }

    public FakeProcessRunner On(string fileName, string argumentsStartWith, ProcessResult result)
    {
        return On(fileName, argumentsStartWith, _ => result);
    }

    public FakeProcessRunner On(string fileName, string argumentsStartWith, int exitCode, string output = "")
    {
        return On(fileName, argumentsStartWith, new ProcessResult { ExitCode = exitCode, Output = output });
    }

    public FakeProcessRunner NotFound(string fileName)
    {
        return On(fileName, "", new ProcessResult { ExitCode = -1, NotFound = true });
    }

    public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        Calls.Add(spec);

        foreach (var (match, result) in rules.AsEnumerable().Reverse())
        {
            if (match(spec))
                return Task.FromResult(result(spec));
        }

        return Task.FromResult(Default);
    }

    public string? FindExecutable(string name)
    {
        return Executables.TryGetValue(name, out var path) ? path : null;
    }
}

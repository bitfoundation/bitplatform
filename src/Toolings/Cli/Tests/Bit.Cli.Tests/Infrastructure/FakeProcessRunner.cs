using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tests.Infrastructure;

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<(Func<ProcessSpec, bool> Match, Func<ProcessSpec, CancellationToken, Task<ProcessResult>> Result)> rules = [];

    public Dictionary<string, string> Executables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<ProcessSpec> Calls { get; } = [];

    public List<ProcessSpec> Detached { get; } = [];

    public ProcessResult Default { get; set; } = new() { ExitCode = 0 };

    public FakeProcessRunner On(string fileName, string argumentsStartWith, Func<ProcessSpec, ProcessResult> respond)
    {
        return OnAsync(fileName, argumentsStartWith, (spec, _) => Task.FromResult(respond(spec)));
    }

    public FakeProcessRunner OnAsync(string fileName, string argumentsStartWith, Func<ProcessSpec, CancellationToken, Task<ProcessResult>> respond)
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

    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        Calls.Add(spec);

        foreach (var (match, result) in rules.AsEnumerable().Reverse())
        {
            if (match(spec))
                return await result(spec, cancellationToken) with { Spec = spec };
        }

        return Default with { Spec = spec };
    }

    public bool StartDetached(ProcessSpec spec)
    {
        Detached.Add(spec);
        return true;
    }

    public string? FindExecutable(string name)
    {
        return Executables.TryGetValue(name, out var path) ? path : null;
    }
}

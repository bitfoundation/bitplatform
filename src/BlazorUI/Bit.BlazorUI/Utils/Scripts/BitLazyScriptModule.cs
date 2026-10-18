namespace Bit.BlazorUI;

/// <summary>One module of a package's lazy scripts: one TypeScript file of it, served as <c>scripts/modules/&lt;name&gt;.js</c>.</summary>
internal sealed class BitLazyScriptModule(BitLazyScriptPackage package, string name, string path, string[] deps, string[] externals, string[] teardown, bool boot)
{
    private BitLazyScriptModule[]? _closure;
    private BitLazyScriptModule[]? _externalModules;

    public BitLazyScriptPackage Package { get; } = package;

    public string Name { get; } = name;

    /// <summary>The module's path under the package's wwwroot.</summary>
    public string Path { get; } = path;

    /// <summary>What it needs of another package's namespace, by path - which the loader imports ahead of it.</summary>
    public string[] Externals { get; } = externals;

    /// <summary>The paths of its functions that only undo what it did, which a page it never ran in has no use calling.</summary>
    public HashSet<string> Teardown { get; } = new(teardown, StringComparer.Ordinal);

    /// <summary>Whether the package's boot module imports it, as the app starts.</summary>
    public bool Boot { get; } = boot;

    /// <summary>
    /// The module and every module of its package it imports, through the ones it imports in turn: what has run in a page
    /// once it has been imported.
    /// </summary>
    public BitLazyScriptModule[] Closure
    {
        get
        {
            if (_closure is not null) return _closure;

            var seen = new HashSet<BitLazyScriptModule>();
            var pending = new Stack<BitLazyScriptModule>();
            pending.Push(this);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (seen.Add(current) is false) continue;

                foreach (var dep in current._deps)
                {
                    if (current.Package.FindModule(dep) is { } module) pending.Push(module);
                }
            }

            return _closure = [.. seen];
        }
    }

    private readonly string[] _deps = deps;

    /// <summary>The modules of other packages its externals resolve to, once resolved by the loader.</summary>
    internal BitLazyScriptModule[]? ExternalModules
    {
        get => _externalModules;
        set => _externalModules = value;
    }

    public override string ToString() => $"{Package.Id}/{Name}";
}

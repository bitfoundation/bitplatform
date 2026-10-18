namespace Bit.Balette;

/// <summary>Default <see cref="IBaletteService"/>. One instance per scope (circuit / browser tab).</summary>
public sealed class BaletteService : IBaletteService
{
    private readonly object _gate = new();
    private readonly List<Registration> _registrations = [];
    private readonly Dictionary<string, BaletteCommand> _byId = new(StringComparer.Ordinal);
    private readonly List<BaletteCommandMode> _modes = [];
    private readonly List<IBaletteSource> _sources = [];
    private readonly BaletteOptions _options;

    public BaletteService(BaletteOptions options)
    {
        _options = options;
        if (options.Commands.Count > 0)
        {
            Register(options.Commands);
        }

        _modes.AddRange(options.Modes);
        _sources.AddRange(options.Sources);
    }

    public bool IsOpen { get; private set; }

    public bool IsMac { get; private set; }

    public event Action? PlatformDetected;

    internal void SetPlatform(bool isMac)
    {
        IsMac = isMac;
        PlatformDetected?.Invoke();
    }

    public IReadOnlyCollection<BaletteCommand> Commands
    {
        get
        {
            lock (_gate)
            {
                return _byId.Values.ToList();
            }
        }
    }

    public IReadOnlyCollection<BaletteCommandMode> Modes
    {
        get
        {
            lock (_gate)
            {
                return _modes.ToList();
            }
        }
    }

    public IReadOnlyCollection<IBaletteSource> Sources
    {
        get
        {
            lock (_gate)
            {
                return _sources.ToList();
            }
        }
    }

    public event Action? CommandsChanged;

    public event Action? Opened;

    public event Action? Closed;

    public event EventHandler<BaletteCommandExecutedEventArgs>? CommandExecuted;

    public event EventHandler<BaletteCommandErrorEventArgs>? CommandFailed;

    /// <summary>The component asks the service to open with these arguments (internal plumbing).</summary>
    internal event Func<string?, string?, Task>? OpenRequested;

    internal event Func<Task>? CloseRequested;

    /// <summary>The palette resolves the id itself: it also knows the commands declared in its markup and passed to it directly.</summary>
    internal event Func<string, Task>? ExecuteRequested;

    public IDisposable Register(params BaletteCommand[] commands) => Register((IEnumerable<BaletteCommand>)commands);

    public IDisposable Register(IEnumerable<BaletteCommand> commands)
    {
        var list = Flatten(commands).ToList();
        var reg = new Registration(this, list);
        lock (_gate)
        {
            _registrations.Add(reg);
            Rebuild();
        }

        CommandsChanged?.Invoke();
        return reg;
    }

    public void Update(BaletteCommand command)
    {
        lock (_gate)
        {
            var replaced = false;
            foreach (var reg in _registrations)
            {
                var idx = reg.Commands.FindIndex(c => c.Id == command.Id);
                if (idx >= 0)
                {
                    // The old command's static children leave with it, and the new one's are flattened in its place.
                    var stale = new HashSet<string>(StringComparer.Ordinal);
                    CollectChildIds(reg.Commands, reg.Commands[idx], command.Id, stale);
                    var at = idx - reg.Commands.Take(idx).Count(c => stale.Contains(c.Id));
                    reg.Commands.RemoveAt(idx);
                    reg.Commands.RemoveAll(c => stale.Contains(c.Id));
                    reg.Commands.InsertRange(at, Flatten([command]));
                    replaced = true;
                }
            }

            if (!replaced)
            {
                _registrations.Add(new Registration(this, Flatten([command]).ToList()));
            }

            Rebuild();
        }

        CommandsChanged?.Invoke();
    }

    public bool Unregister(string commandId)
    {
        bool removed;
        lock (_gate)
        {
            removed = false;
            foreach (var reg in _registrations)
            {
                removed |= reg.Commands.RemoveAll(c => c.Id == commandId) > 0;
            }

            if (removed)
            {
                Rebuild();
            }
        }

        if (removed)
        {
            CommandsChanged?.Invoke();
        }

        return removed;
    }

    public BaletteCommand? Find(string commandId)
    {
        lock (_gate)
        {
            return _byId.GetValueOrDefault(commandId);
        }
    }

    public IDisposable RegisterMode(BaletteCommandMode mode)
    {
        lock (_gate)
        {
            _modes.RemoveAll(m => m.Prefix == mode.Prefix);
            _modes.Add(mode);
        }

        CommandsChanged?.Invoke();
        return new ActionDisposable(() =>
        {
            lock (_gate)
            {
                _modes.Remove(mode);
            }

            CommandsChanged?.Invoke();
        });
    }

    public IDisposable RegisterSource(IBaletteSource source)
    {
        lock (_gate)
        {
            _sources.Add(source);
        }

        CommandsChanged?.Invoke();
        return new ActionDisposable(() =>
        {
            lock (_gate)
            {
                _sources.Remove(source);
            }

            CommandsChanged?.Invoke();
        });
    }

    public Task OpenAsync(string? query = null, string? pageCommandId = null)
        => OpenRequested?.Invoke(query, pageCommandId) ?? Task.CompletedTask;

    public Task CloseAsync() => CloseRequested?.Invoke() ?? Task.CompletedTask;

    public Task ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();

    public Task ExecuteAsync(string commandId)
    {
        if (ExecuteRequested is { } execute)
        {
            return execute(commandId);
        }

        _ = Find(commandId) ?? throw new KeyNotFoundException($"Command '{commandId}' is not registered.");
        return Task.CompletedTask;
    }

    internal void SetOpen(bool open)
    {
        if (IsOpen == open)
        {
            return;
        }

        IsOpen = open;
        if (open)
        {
            Opened?.Invoke();
        }
        else
        {
            Closed?.Invoke();
        }
    }

    internal void RaiseExecuted(BaletteCommand command, BaletteCommandResult result, TimeSpan duration)
        => CommandExecuted?.Invoke(this, new BaletteCommandExecutedEventArgs(command, result, duration));

    internal bool RaiseFailed(BaletteCommand command, Exception ex)
    {
        var args = new BaletteCommandErrorEventArgs(command, ex);
        CommandFailed?.Invoke(this, args);
        return args.Handled;
    }

    internal BaletteOptions Options => _options;

    /// <summary>Flattens static children into the registry with <see cref="BaletteCommand.Parent"/> set so nested pages work uniformly.</summary>
    private static IEnumerable<BaletteCommand> Flatten(IEnumerable<BaletteCommand> commands, string? parent = null)
    {
        foreach (var c in commands)
        {
            var cmd = parent is not null && c.Parent is null
                ? c.With(b => b.Parent(parent).Id(ChildId(c, parent)))
                : c;
            yield return cmd;
            if (cmd.Children is { Count: > 0 })
            {
                foreach (var child in Flatten(cmd.Children, cmd.Id))
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>The id a static child gets once flattened under the command with id <paramref name="parentId"/>.</summary>
    private static string ChildId(BaletteCommand child, string parentId)
        => child.Parent is not null || child.HasExplicitId ? child.Id : BaletteCommandIdGenerator.FromTitle(child.Title, child.Group, parentId);

    /// <summary>Collects the ids a command's static children were flattened into, down to the last level.</summary>
    private static void CollectChildIds(List<BaletteCommand> commands, BaletteCommand parent, string parentId, HashSet<string> ids)
    {
        foreach (var child in parent.Children ?? [])
        {
            var id = ChildId(child, parentId);
            if (ids.Add(id))
            {
                // A child updated since brings children of its own; one no longer stored, the ones it had.
                CollectChildIds(commands, commands.Find(c => c.Id == id) ?? child, id, ids);
            }
        }
    }

    private void Rebuild()
    {
        _byId.Clear();
        foreach (var c in _registrations.SelectMany(r => r.Commands))
        {
            _byId[c.Id] = c; // later registrations win, allowing pages to override global commands
        }
    }

    private void Remove(Registration reg)
    {
        lock (_gate)
        {
            if (!_registrations.Remove(reg))
            {
                return;
            }

            Rebuild();
        }

        CommandsChanged?.Invoke();
    }

    private sealed class Registration(BaletteService owner, List<BaletteCommand> commands) : IDisposable
    {
        public List<BaletteCommand> Commands { get; } = commands;

        public void Dispose() => owner.Remove(this);
    }

    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}

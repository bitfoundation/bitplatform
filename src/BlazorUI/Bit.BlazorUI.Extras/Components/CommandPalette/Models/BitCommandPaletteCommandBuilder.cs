using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

/// <summary>Fluent builder for <see cref="BitCommandPaletteCommand"/>.</summary>
public sealed class BitCommandPaletteCommandBuilder
{
    private string? _id;
    private string _title;
    private string? _subtitle;
    private string? _description;
    private readonly List<string> _keywords;
    private string? _group;
    private RenderFragment? _icon;
    private string? _iconCss;
    private BitCommandPaletteKeyboardShortcut? _shortcut;
    private bool _globalShortcut = true;
    private int _priority;
    private string? _parent;
    private List<BitCommandPaletteCommand>? _children;
    private Func<BitCommandPaletteCommandContext, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>>? _childrenProvider;
    private List<IBitCommandPaletteSource>? _childSources;
    private string? _childrenPlaceholder;
    private bool _multiSelect;
    private List<BitCommandPaletteCommand>? _actions;
    private Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>>? _handler;
    private string? _href;
    private bool _forceLoad;
    private bool _newTab;
    private BitCommandPaletteCommandKind _kind = BitCommandPaletteCommandKind.Action;
    private bool _disabled;
    private string? _disabledReason;
    private Func<bool>? _isVisible;
    private Func<BitCommandPaletteGuardContext, CancellationToken, ValueTask<BitCommandPaletteGuardResult>>? _guard;
    private bool _destructive;
    private string? _confirm;
    private string? _badge;
    private string? _value;
    private bool? _checked;
    private bool _alwaysShow;
    private bool _excludeFromHistory;
    private object? _data;
    private readonly List<string> _tags;

    /// <summary>Starts a command with the given title; <see cref="BitCommandPaletteCmd"/> is the shorter way to write it.</summary>
    public BitCommandPaletteCommandBuilder(string title)
    {
        _title = title;
        _keywords = [];
        _tags = [];
    }

    internal BitCommandPaletteCommandBuilder(BitCommandPaletteCommand c)
    {
        _id = c.Id; // copies keep their identity even if the title changes
        _title = c.Title;
        _subtitle = c.Subtitle;
        _description = c.Description;
        _keywords = [.. c.Keywords];
        _group = c.Group;
        _icon = c.Icon;
        _iconCss = c.IconCss;
        _shortcut = c.Shortcut;
        _globalShortcut = c.GlobalShortcut;
        _priority = c.Priority;
        _parent = c.Parent;
        _children = c.Children is null ? null : [.. c.Children];
        _childrenProvider = c.ChildrenProvider;
        _childSources = c.ChildSources is null ? null : [.. c.ChildSources];
        _childrenPlaceholder = c.ChildrenPlaceholder;
        _multiSelect = c.MultiSelectChildren;
        _actions = c.Actions is null ? null : [.. c.Actions];
        _handler = c.Handler;
        _href = c.Href;
        _forceLoad = c.ForceLoad;
        _newTab = c.OpenInNewTab;
        _kind = c.Kind;
        _disabled = c.Disabled;
        _disabledReason = c.DisabledReason;
        _isVisible = c.IsVisible;
        _guard = c.Guard;
        _destructive = c.Destructive;
        _confirm = c.ConfirmMessage;
        _badge = c.Badge;
        _value = c.Value;
        _checked = c.Checked;
        _alwaysShow = c.AlwaysShow;
        _excludeFromHistory = c.ExcludeFromHistory;
        _data = c.Data;
        _tags = [.. c.Tags];
    }

    /// <summary>Sets an explicit, stable id. Do so whenever the title can change, so history and selection survive it.</summary>
    public BitCommandPaletteCommandBuilder Id(string id) { _id = id; return this; }
    /// <summary>Sets the label.</summary>
    public BitCommandPaletteCommandBuilder Title(string title) { _title = title; return this; }
    /// <summary>Sets the secondary label next to the title.</summary>
    public BitCommandPaletteCommandBuilder Subtitle(string? subtitle) { _subtitle = subtitle; return this; }
    /// <summary>Sets the longer text shown in the detail pane.</summary>
    public BitCommandPaletteCommandBuilder Description(string? description) { _description = description; return this; }
    /// <summary>Adds aliases, synonyms and abbreviations that also find the command.</summary>
    public BitCommandPaletteCommandBuilder Keywords(params string[] keywords) { _keywords.AddRange(keywords); return this; }
    /// <summary>Sets the heading the command is listed under.</summary>
    public BitCommandPaletteCommandBuilder Group(string? group) { _group = group; return this; }
    /// <summary>Sets an icon rendered before the title.</summary>
    public BitCommandPaletteCommandBuilder Icon(RenderFragment icon) { _icon = icon; return this; }
    /// <summary>Sets the CSS class of an icon element, e.g. <c>bi bi-house</c>.</summary>
    public BitCommandPaletteCommandBuilder Icon(string iconCss) { _iconCss = iconCss; return this; }
    /// <summary>Sets a shortcut such as <c>Mod+K</c> or <c>G I</c>; global ones also fire while the palette is closed.</summary>
    public BitCommandPaletteCommandBuilder Shortcut(string shortcut, bool global = true) { _shortcut = BitCommandPaletteKeyboardShortcut.Parse(shortcut); _globalShortcut = global; return this; }
    /// <summary>Sets a shortcut; global ones also fire while the palette is closed.</summary>
    public BitCommandPaletteCommandBuilder Shortcut(BitCommandPaletteKeyboardShortcut shortcut, bool global = true) { _shortcut = shortcut; _globalShortcut = global; return this; }
    /// <summary>Sets a ranking nudge; see <see cref="BitCommandPaletteCommandPriority"/>.</summary>
    public BitCommandPaletteCommandBuilder Priority(int priority) { _priority = priority; return this; }
    /// <summary>Lists the command on the page of the command with this id instead of the root.</summary>
    public BitCommandPaletteCommandBuilder Parent(string? parentId) { _parent = parentId; return this; }
    /// <summary>Adds nested commands, turning this command into a page.</summary>
    public BitCommandPaletteCommandBuilder Children(params BitCommandPaletteCommand[] children) { (_children ??= []).AddRange(children); if (_kind == BitCommandPaletteCommandKind.Action) { _kind = BitCommandPaletteCommandKind.Page; } return this; }
    /// <summary>Adds nested commands, turning this command into a page.</summary>
    public BitCommandPaletteCommandBuilder Children(IEnumerable<BitCommandPaletteCommand> children) { (_children ??= []).AddRange(children); if (_kind == BitCommandPaletteCommandKind.Action) { _kind = BitCommandPaletteCommandKind.Page; } return this; }
    /// <summary>Loads the nested commands when the page opens, turning this command into a page.</summary>
    public BitCommandPaletteCommandBuilder Children(Func<BitCommandPaletteCommandContext, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> provider, string? placeholder = null) { _childrenProvider = provider; _childrenPlaceholder = placeholder; if (_kind == BitCommandPaletteCommandKind.Action) { _kind = BitCommandPaletteCommandKind.Page; } return this; }
    /// <summary>Searches <paramref name="source"/> as the user types on this command's page, turning it into a page.</summary>
    public BitCommandPaletteCommandBuilder Source(IBitCommandPaletteSource source) { ArgumentNullException.ThrowIfNull(source); (_childSources ??= []).Add(source); if (_kind == BitCommandPaletteCommandKind.Action) { _kind = BitCommandPaletteCommandKind.Page; } return this; }
    /// <summary>
    /// Searches with <paramref name="search"/> as the user types on this command's page - from the moment it opens, as
    /// <paramref name="minQueryLength"/> is 0 by default - and lists the results under <paramref name="name"/>.
    /// </summary>
    public BitCommandPaletteCommandBuilder Source(string name, Func<string, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> search, int minQueryLength = 0, int maxResults = 20, bool applyMatcher = false)
        => Source(new BitCommandPaletteDelegateSource(name, search, minQueryLength, maxResults, applyMatcher));
    /// <summary>
    /// Searches in pages on this command's page: <paramref name="searchPage"/> takes the query and the cursor of the page
    /// to load (null for the first), and a page with a <see cref="BitCommandPaletteSourcePage.NextCursor"/> gets a "Show more…" row.
    /// </summary>
    public BitCommandPaletteCommandBuilder Source(string name, Func<string, string?, CancellationToken, Task<BitCommandPaletteSourcePage>> searchPage, int minQueryLength = 0, int maxResults = 20)
        => Source(new BitCommandPaletteDelegateSource(name, (q, cursor, _, ct) => searchPage(q, cursor, ct), minQueryLength, maxResults));
    /// <summary>Sets the search box placeholder on this command's page.</summary>
    public BitCommandPaletteCommandBuilder ChildrenPlaceholder(string? placeholder) { _childrenPlaceholder = placeholder; return this; }
    /// <summary>Makes the rows of this command's page checkboxes; <c>Ctrl+Enter</c> runs the handler with the checked ones.</summary>
    public BitCommandPaletteCommandBuilder MultiSelect(bool multiSelect = true) { _multiSelect = multiSelect; return this; }
    /// <summary>Adds secondary actions shown in the action panel on <c>Tab</c>.</summary>
    public BitCommandPaletteCommandBuilder Actions(params BitCommandPaletteCommand[] actions) { (_actions ??= []).AddRange(actions); return this; }
    /// <summary>Sets what runs on selection, and what the palette does next.</summary>
    public BitCommandPaletteCommandBuilder Handler(Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> handler) { _handler = handler; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BitCommandPaletteCommandBuilder Handler(Func<BitCommandPaletteCommandContext, Task> handler) { _handler = async ctx => { await handler(ctx).ConfigureAwait(false); return BitCommandPaletteCommandResult.Close; }; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BitCommandPaletteCommandBuilder Handler(Action<BitCommandPaletteCommandContext> handler) { _handler = ctx => { handler(ctx); return Task.FromResult(BitCommandPaletteCommandResult.Close); }; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BitCommandPaletteCommandBuilder Handler(Action handler) { _handler = _ => { handler(); return Task.FromResult(BitCommandPaletteCommandResult.Close); }; return this; }
    /// <summary>Navigates to <paramref name="href"/> on selection when there is no handler, and makes the command a navigation.</summary>
    public BitCommandPaletteCommandBuilder Href(string href, bool forceLoad = false, bool newTab = false) { _href = href; _forceLoad = forceLoad; _newTab = newTab; _kind = BitCommandPaletteCommandKind.Navigation; return this; }
    /// <summary>Sets the semantic kind, which picks the default icon.</summary>
    public BitCommandPaletteCommandBuilder Kind(BitCommandPaletteCommandKind kind) { _kind = kind; return this; }
    /// <summary>Lists the command but does not run it, showing <paramref name="reason"/>.</summary>
    public BitCommandPaletteCommandBuilder Disabled(bool disabled = true, string? reason = null) { _disabled = disabled; _disabledReason = reason; return this; }
    /// <summary>Lists the command only while <paramref name="predicate"/> returns true; evaluated every time the list is built.</summary>
    public BitCommandPaletteCommandBuilder VisibleWhen(Func<bool> predicate) { _isVisible = predicate; return this; }
    /// <summary>
    /// Checks, every time the command is about to run, whether it may - a permission, a plan, a feature flag - and refuses
    /// with <see cref="BitCommandPaletteGuardResult.Deny"/> otherwise. On a page, it guards every command under it too. See
    /// <see cref="IBitCommandPaletteCommandGuard"/> for when guards run. A command has one guard: a later call replaces an earlier
    /// one, so combine checks in one guard.
    /// </summary>
    public BitCommandPaletteCommandBuilder Guard(Func<BitCommandPaletteGuardContext, CancellationToken, ValueTask<BitCommandPaletteGuardResult>> guard) { ArgumentNullException.ThrowIfNull(guard); _guard = guard; return this; }
    /// <summary>
    /// Checks, every time the command is about to run, that <paramref name="allowed"/> returns true, and refuses it with
    /// <paramref name="deniedMessage"/> (or <see cref="BitCommandPaletteStrings.CommandRefused"/>) otherwise. Replaces a guard set earlier.
    /// </summary>
    public BitCommandPaletteCommandBuilder Guard(Func<BitCommandPaletteGuardContext, bool> allowed, string? deniedMessage = null)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        _guard = (ctx, _) => ValueTask.FromResult(allowed(ctx) ? BitCommandPaletteGuardResult.Allow : BitCommandPaletteGuardResult.Deny(deniedMessage));
        return this;
    }
    /// <summary>Styles the command as dangerous; with a message, <c>Enter</c> has to be pressed twice. Without one, a confirmation set by <see cref="Confirm"/> stays.</summary>
    public BitCommandPaletteCommandBuilder Destructive(string? confirmMessage = null) { _destructive = true; _confirm = confirmMessage ?? _confirm; return this; }
    /// <summary>Asks for a second <c>Enter</c> before running, showing <paramref name="message"/>.</summary>
    public BitCommandPaletteCommandBuilder Confirm(string message) { _confirm = message; return this; }
    /// <summary>Sets a small pill at the end of the row, e.g. "New".</summary>
    public BitCommandPaletteCommandBuilder Badge(string? badge) { _badge = badge; return this; }
    /// <summary>Sets the current value of a setting, e.g. "Dark".</summary>
    public BitCommandPaletteCommandBuilder Value(string? value) { _value = value; if (_kind == BitCommandPaletteCommandKind.Action) { _kind = BitCommandPaletteCommandKind.Setting; } return this; }
    /// <summary>Makes the command a toggle with this state.</summary>
    public BitCommandPaletteCommandBuilder Checked(bool? isChecked) { _checked = isChecked; _kind = BitCommandPaletteCommandKind.Toggle; return this; }
    /// <summary>Lists the command whatever the query.</summary>
    public BitCommandPaletteCommandBuilder AlwaysShow(bool alwaysShow = true) { _alwaysShow = alwaysShow; return this; }
    /// <summary>Keeps the command out of recents and frecency.</summary>
    public BitCommandPaletteCommandBuilder ExcludeFromHistory(bool exclude = true) { _excludeFromHistory = exclude; return this; }
    /// <summary>Attaches anything templates or handlers need.</summary>
    public BitCommandPaletteCommandBuilder Data(object? data) { _data = data; return this; }
    /// <summary>Adds free-form labels that modes can scope to.</summary>
    public BitCommandPaletteCommandBuilder Tags(params string[] tags) { _tags.AddRange(tags); return this; }

    /// <summary>Creates the immutable command. Rarely needed: a builder converts to a command implicitly.</summary>
    public BitCommandPaletteCommand Build() => new()
    {
        Id = _id!, // null keeps the id derived (see BitCommandPaletteCommand.Id)
        Title = _title,
        Subtitle = _subtitle,
        Description = _description,
        Keywords = _keywords,
        Group = _group,
        Icon = _icon,
        IconCss = _iconCss,
        Shortcut = _shortcut,
        GlobalShortcut = _globalShortcut,
        Priority = _priority,
        Parent = _parent,
        Children = _children,
        ChildrenProvider = _childrenProvider,
        ChildSources = _childSources,
        ChildrenPlaceholder = _childrenPlaceholder,
        MultiSelectChildren = _multiSelect,
        Actions = _actions,
        Handler = _handler,
        Href = _href,
        ForceLoad = _forceLoad,
        OpenInNewTab = _newTab,
        Kind = _kind,
        Disabled = _disabled,
        DisabledReason = _disabledReason,
        IsVisible = _isVisible,
        Guard = _guard,
        Destructive = _destructive,
        ConfirmMessage = _confirm,
        Badge = _badge,
        Value = _value,
        Checked = _checked,
        AlwaysShow = _alwaysShow,
        ExcludeFromHistory = _excludeFromHistory,
        Data = _data,
        Tags = _tags,
    };

    /// <summary>Builds the command, so a builder can be passed wherever a command is expected.</summary>
    public static implicit operator BitCommandPaletteCommand(BitCommandPaletteCommandBuilder b) => b.Build();
}

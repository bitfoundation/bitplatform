using Microsoft.AspNetCore.Components;

namespace Bit.Balette;

/// <summary>Fluent builder for <see cref="BaletteCommand"/>.</summary>
public sealed class BaletteCommandBuilder
{
    private string? _id;
    private string _title;
    private string? _subtitle;
    private string? _description;
    private readonly List<string> _keywords;
    private string? _group;
    private RenderFragment? _icon;
    private string? _iconCss;
    private BaletteKeyboardShortcut? _shortcut;
    private bool _globalShortcut = true;
    private int _priority;
    private string? _parent;
    private List<BaletteCommand>? _children;
    private Func<BaletteCommandContext, CancellationToken, Task<IReadOnlyList<BaletteCommand>>>? _childrenProvider;
    private string? _childrenPlaceholder;
    private bool _multiSelect;
    private List<BaletteCommand>? _actions;
    private Func<BaletteCommandContext, Task<BaletteCommandResult>>? _handler;
    private string? _href;
    private bool _forceLoad;
    private bool _newTab;
    private BaletteCommandKind _kind = BaletteCommandKind.Action;
    private bool _disabled;
    private string? _disabledReason;
    private Func<bool>? _isVisible;
    private bool _destructive;
    private string? _confirm;
    private string? _badge;
    private string? _value;
    private bool? _checked;
    private bool _alwaysShow;
    private bool _excludeFromHistory;
    private object? _data;
    private readonly List<string> _tags;

    /// <summary>Starts a command with the given title; <see cref="BaletteCmd"/> is the shorter way to write it.</summary>
    public BaletteCommandBuilder(string title)
    {
        _title = title;
        _keywords = [];
        _tags = [];
    }

    internal BaletteCommandBuilder(BaletteCommand c)
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
    public BaletteCommandBuilder Id(string id) { _id = id; return this; }
    /// <summary>Sets the label.</summary>
    public BaletteCommandBuilder Title(string title) { _title = title; return this; }
    /// <summary>Sets the secondary label next to the title.</summary>
    public BaletteCommandBuilder Subtitle(string? subtitle) { _subtitle = subtitle; return this; }
    /// <summary>Sets the longer text shown in the detail pane.</summary>
    public BaletteCommandBuilder Description(string? description) { _description = description; return this; }
    /// <summary>Adds aliases, synonyms and abbreviations that also find the command.</summary>
    public BaletteCommandBuilder Keywords(params string[] keywords) { _keywords.AddRange(keywords); return this; }
    /// <summary>Sets the heading the command is listed under.</summary>
    public BaletteCommandBuilder Group(string? group) { _group = group; return this; }
    /// <summary>Sets an icon rendered before the title.</summary>
    public BaletteCommandBuilder Icon(RenderFragment icon) { _icon = icon; return this; }
    /// <summary>Sets the CSS class of an icon element, e.g. <c>bi bi-house</c>.</summary>
    public BaletteCommandBuilder Icon(string iconCss) { _iconCss = iconCss; return this; }
    /// <summary>Sets a shortcut such as <c>Mod+K</c> or <c>G I</c>; global ones also fire while the palette is closed.</summary>
    public BaletteCommandBuilder Shortcut(string shortcut, bool global = true) { _shortcut = BaletteKeyboardShortcut.Parse(shortcut); _globalShortcut = global; return this; }
    /// <summary>Sets a shortcut; global ones also fire while the palette is closed.</summary>
    public BaletteCommandBuilder Shortcut(BaletteKeyboardShortcut shortcut, bool global = true) { _shortcut = shortcut; _globalShortcut = global; return this; }
    /// <summary>Sets a ranking nudge; see <see cref="BaletteCommandPriority"/>.</summary>
    public BaletteCommandBuilder Priority(int priority) { _priority = priority; return this; }
    /// <summary>Lists the command on the page of the command with this id instead of the root.</summary>
    public BaletteCommandBuilder Parent(string? parentId) { _parent = parentId; return this; }
    /// <summary>Adds nested commands, turning this command into a page.</summary>
    public BaletteCommandBuilder Children(params BaletteCommand[] children) { (_children ??= []).AddRange(children); if (_kind == BaletteCommandKind.Action) { _kind = BaletteCommandKind.Page; } return this; }
    /// <summary>Adds nested commands, turning this command into a page.</summary>
    public BaletteCommandBuilder Children(IEnumerable<BaletteCommand> children) { (_children ??= []).AddRange(children); if (_kind == BaletteCommandKind.Action) { _kind = BaletteCommandKind.Page; } return this; }
    /// <summary>Loads the nested commands when the page opens, turning this command into a page.</summary>
    public BaletteCommandBuilder Children(Func<BaletteCommandContext, CancellationToken, Task<IReadOnlyList<BaletteCommand>>> provider, string? placeholder = null) { _childrenProvider = provider; _childrenPlaceholder = placeholder; if (_kind == BaletteCommandKind.Action) { _kind = BaletteCommandKind.Page; } return this; }
    /// <summary>Sets the search box placeholder on this command's page.</summary>
    public BaletteCommandBuilder ChildrenPlaceholder(string? placeholder) { _childrenPlaceholder = placeholder; return this; }
    /// <summary>Makes the rows of this command's page checkboxes; <c>Ctrl+Enter</c> runs the handler with the checked ones.</summary>
    public BaletteCommandBuilder MultiSelect(bool multiSelect = true) { _multiSelect = multiSelect; return this; }
    /// <summary>Adds secondary actions shown in the action panel on <c>Tab</c>.</summary>
    public BaletteCommandBuilder Actions(params BaletteCommand[] actions) { (_actions ??= []).AddRange(actions); return this; }
    /// <summary>Sets what runs on selection, and what the palette does next.</summary>
    public BaletteCommandBuilder Handler(Func<BaletteCommandContext, Task<BaletteCommandResult>> handler) { _handler = handler; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BaletteCommandBuilder Handler(Func<BaletteCommandContext, Task> handler) { _handler = async ctx => { await handler(ctx).ConfigureAwait(false); return BaletteCommandResult.Close; }; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BaletteCommandBuilder Handler(Action<BaletteCommandContext> handler) { _handler = ctx => { handler(ctx); return Task.FromResult(BaletteCommandResult.Close); }; return this; }
    /// <summary>Sets what runs on selection; the palette closes afterwards.</summary>
    public BaletteCommandBuilder Handler(Action handler) { _handler = _ => { handler(); return Task.FromResult(BaletteCommandResult.Close); }; return this; }
    /// <summary>Navigates to <paramref name="href"/> on selection when there is no handler, and makes the command a navigation.</summary>
    public BaletteCommandBuilder Href(string href, bool forceLoad = false, bool newTab = false) { _href = href; _forceLoad = forceLoad; _newTab = newTab; _kind = BaletteCommandKind.Navigation; return this; }
    /// <summary>Sets the semantic kind, which picks the default icon.</summary>
    public BaletteCommandBuilder Kind(BaletteCommandKind kind) { _kind = kind; return this; }
    /// <summary>Lists the command but does not run it, showing <paramref name="reason"/>.</summary>
    public BaletteCommandBuilder Disabled(bool disabled = true, string? reason = null) { _disabled = disabled; _disabledReason = reason; return this; }
    /// <summary>Lists the command only while <paramref name="predicate"/> returns true; evaluated every time the list is built.</summary>
    public BaletteCommandBuilder VisibleWhen(Func<bool> predicate) { _isVisible = predicate; return this; }
    /// <summary>Styles the command as dangerous; with a message, <c>Enter</c> has to be pressed twice.</summary>
    public BaletteCommandBuilder Destructive(string? confirmMessage = null) { _destructive = true; _confirm = confirmMessage; return this; }
    /// <summary>Asks for a second <c>Enter</c> before running, showing <paramref name="message"/>.</summary>
    public BaletteCommandBuilder Confirm(string message) { _confirm = message; return this; }
    /// <summary>Sets a small pill at the end of the row, e.g. "New".</summary>
    public BaletteCommandBuilder Badge(string? badge) { _badge = badge; return this; }
    /// <summary>Sets the current value of a setting, e.g. "Dark".</summary>
    public BaletteCommandBuilder Value(string? value) { _value = value; if (_kind == BaletteCommandKind.Action) { _kind = BaletteCommandKind.Setting; } return this; }
    /// <summary>Makes the command a toggle with this state.</summary>
    public BaletteCommandBuilder Checked(bool? isChecked) { _checked = isChecked; _kind = BaletteCommandKind.Toggle; return this; }
    /// <summary>Lists the command whatever the query.</summary>
    public BaletteCommandBuilder AlwaysShow(bool alwaysShow = true) { _alwaysShow = alwaysShow; return this; }
    /// <summary>Keeps the command out of recents and frecency.</summary>
    public BaletteCommandBuilder ExcludeFromHistory(bool exclude = true) { _excludeFromHistory = exclude; return this; }
    /// <summary>Attaches anything templates or handlers need.</summary>
    public BaletteCommandBuilder Data(object? data) { _data = data; return this; }
    /// <summary>Adds free-form labels that modes can scope to.</summary>
    public BaletteCommandBuilder Tags(params string[] tags) { _tags.AddRange(tags); return this; }

    /// <summary>Creates the immutable command. Rarely needed: a builder converts to a command implicitly.</summary>
    public BaletteCommand Build() => new()
    {
        Id = _id!, // null keeps the id derived (see BaletteCommand.Id)
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
    public static implicit operator BaletteCommand(BaletteCommandBuilder b) => b.Build();
}

/// <summary>Entry points for the fluent API: <c>BaletteCmd.New("Title").Href("/").Build()</c>.</summary>
public static class BaletteCmd
{
    /// <summary>Starts a command with the given title.</summary>
    public static BaletteCommandBuilder New(string title) => new(title);
    /// <summary>Starts a command that navigates to <paramref name="href"/>.</summary>
    public static BaletteCommandBuilder Navigate(string title, string href) => new BaletteCommandBuilder(title).Href(href);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BaletteCommandBuilder Action(string title, Action action) => new BaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BaletteCommandBuilder Action(string title, Func<Task> action) => new BaletteCommandBuilder(title).Handler(_ => action());
    /// <summary>Starts a command whose handler decides what the palette does next.</summary>
    public static BaletteCommandBuilder Action(string title, Func<BaletteCommandContext, Task<BaletteCommandResult>> action) => new BaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that opens a page listing <paramref name="children"/>.</summary>
    public static BaletteCommandBuilder Page(string title, params BaletteCommand[] children) => new BaletteCommandBuilder(title).Kind(BaletteCommandKind.Page).Children(children);
}

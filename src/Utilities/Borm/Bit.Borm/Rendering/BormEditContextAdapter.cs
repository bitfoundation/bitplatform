using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

/// <summary>
/// A Blazor <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> that tells the same story as a
/// <see cref="BormState"/> — so the components the rest of the Blazor ecosystem builds on it
/// (<c>InputBase&lt;T&gt;</c> and everything derived from it, <c>ValidationMessage&lt;T&gt;</c>,
/// <c>ValidationSummary</c>, and the inputs of component libraries) can be used inside a Borm form and
/// show its validation as their own.
/// </summary>
/// <remarks>
/// <para>
/// The state stays the source of truth. Every error it records is mirrored into the context's
/// <see cref="ValidationMessageStore"/> under the <see cref="Microsoft.AspNetCore.Components.Forms.FieldIdentifier"/>
/// of its path — the object that owns the field and the member's name, exactly what <c>InputBase</c>
/// computes from its <c>ValueExpression</c> — once the state would show it (the field touched, or the
/// form submitted), so an input reads <c>invalid</c> no sooner than Borm's own controls do. Warnings and
/// notes stay Borm's: an edit context has no severity, and every message in it is an error.
/// </para>
/// <para>
/// Asking the context to validate (<see cref="EditContext.Validate"/>, which is what a hand-written
/// submit button over an <c>EditForm</c> does) runs the state's validation; the synchronous rules give
/// their answer before it returns. Values are never written through the context: a component inside a
/// Borm field writes through its <see cref="BormFieldContext"/>, which keeps conditions, computed
/// values and dirty tracking right.
/// </para>
/// <para>Dispose it when the state it follows goes away; <see cref="BormView"/> does so for the one it creates.</para>
/// </remarks>
public sealed class BormEditContextAdapter : IDisposable
{
    private readonly BormState _state;
    private readonly ValidationMessageStore _store;
    private readonly Func<Action, Task>? _dispatch;
    private List<(FieldIdentifier Field, string Message)> _shown = [];
    private bool _disposed;

    // What a path's owner is taken to be when nothing is there to own it — a row not created yet.
    private static readonly object StandIn = new();

    /// <param name="state">The state to follow.</param>
    /// <param name="dispatch">
    /// Runs the copy on the renderer's thread — a component's <c>InvokeAsync</c>. The state can change
    /// from anywhere (a timer, a SignalR callback), and the copy makes the inputs that use the context
    /// render. Without it, the copy runs wherever the state announced the change.
    /// </param>
    public BormEditContextAdapter(BormState state, Func<Action, Task>? dispatch = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _dispatch = dispatch;
        // A dictionary-backed form's root is its dictionary, a reference type like any model.
        EditContext = new EditContext(state.Data.Root ?? StandIn);
        _store = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += OnValidationRequested;
        _state.StateChanged += OnStateChanged;
        // What the form's field contexts report. The first over a state keeps the place until it is disposed.
        _state.EditContextAdapter ??= this;
        Sync();
    }

    private void OnStateChanged()
    {
        if (_dispatch is null) Sync();
        else _ = _dispatch(Sync);
    }

    /// <summary>The edit context to cascade.</summary>
    public EditContext EditContext { get; }

    /// <summary>The state the context follows.</summary>
    public BormState EditContextFor => _state;

    /// <summary>
    /// The field identifier of <paramref name="path"/>: the object the field belongs to — the model, a nested
    /// object, a repeater row — and the member's name within it.
    /// </summary>
    public FieldIdentifier FieldIdentifier(string path) => Identify(_state, path);

    /// <summary>The field identifier of <paramref name="path"/> in <paramref name="state"/>, with no adapter needed.</summary>
    internal static FieldIdentifier Identify(BormState state, string path) => new(OwnerOf(state, path), MemberOf(path));

    /// <summary>
    /// An expression reading the value at <paramref name="path"/>, for a component that needs one to find
    /// its field — <c>InputBase&lt;T&gt;.ValueExpression</c>, <c>ValidationMessage&lt;T&gt;.For</c>. It names
    /// the property of the object that owns the field (or the key of a dictionary-backed one), so the
    /// identifier the component works out is the one this adapter keeps the field's messages under.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The value at the path is not of type <typeparamref name="TValue"/> — an <c>InputText</c> over an
    /// <c>int</c> property — which the component would read wrongly. Use the property's own type.
    /// </exception>
    public Expression<Func<TValue>> ValueExpression<TValue>(string path) => ValueExpression<TValue>(_state, path);

    internal static Expression<Func<TValue>> ValueExpression<TValue>(BormState state, string path)
    {
        var owner = OwnerOf(state, path);
        var member = MemberOf(path);

        if (owner is not IDictionary && owner.GetType().GetProperty(member, BindingFlags.Public | BindingFlags.Instance) is { } property)
        {
            if (property.PropertyType != typeof(TValue))
                throw new InvalidOperationException(
                    $"'{path}' is a {property.PropertyType.Name}, not a {typeof(TValue).Name}; use the property's own type.");
            return Expression.Lambda<Func<TValue>>(Expression.Property(Expression.Constant(owner), property));
        }

        // A key of a dictionary, read through its indexer: FieldIdentifier.Create takes the dictionary as the
        // model and the key as the field name, which is what this adapter keys the field under. A call to
        // the getter, as the compiler writes dictionary["key"] — FieldIdentifier refuses an IndexExpression.
        var indexer = owner.GetType().GetProperty("Item", [typeof(string)]);
        if (indexer?.GetMethod is not { } getter || !typeof(TValue).IsAssignableFrom(indexer.PropertyType))
            throw new InvalidOperationException($"'{path}' cannot be read as a {typeof(TValue).Name}.");
        return Expression.Lambda<Func<TValue>>(Expression.Call(Expression.Constant(owner), getter, Expression.Constant(member)));
    }

    private static object OwnerOf(BormState state, string path)
    {
        var parent = BormPath.Parent(path);
        return (parent.Length == 0 ? state.Data.Root : state.GetValue(parent)) ?? StandIn;
    }

    private static string MemberOf(string path)
    {
        var segments = BormPath.Parse(path);
        return segments.Count == 0 ? path : segments[^1].ToString();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // Every message stands as soon as the user asks for a verdict, as a submit makes them.
        _state.MarkAllTouched();
        // The state announces the verdict when it lands — synchronously, for rules that are — and that
        // announcement is what copies it here, on the renderer's own thread. A run that faults has its
        // exception observed rather than left to surface wherever the finalizer happens to run.
        var pending = _state.ValidateAsync();
        if (!pending.IsCompleted)
            _ = pending.AsTask().ContinueWith(t => _ = t.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }

    /// <summary>Copies what the state currently shows into the context, and says so when it changed.</summary>
    private void Sync()
    {
        if (_disposed) return;

        var shown = new List<(FieldIdentifier Field, string Message)>();
        foreach (var message in _state.AllMessages)
        {
            if (message.Severity != BormValidationSeverity.Error || !(_state.IsSubmitted || _state.IsTouched(message.FieldPath))) continue;
            shown.Add((message.FieldPath.Length == 0
                ? new FieldIdentifier(EditContext.Model, string.Empty)
                : FieldIdentifier(message.FieldPath), message.Message));
        }

        // Rebuilt and announced only when what is shown changed: the state raises its event for every
        // keystroke, and the inputs listening to the context would re-render for each. Compared by
        // identifier, not path: a list replaced wholesale keeps its paths and messages, but its rows are
        // other objects, and the inputs bound to them look their messages up by those.
        if (shown.SequenceEqual(_shown)) return;
        _shown = shown;

        _store.Clear();
        foreach (var (field, message) in shown) _store.Add(field, message);
        EditContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state.StateChanged -= OnStateChanged;
        if (ReferenceEquals(_state.EditContextAdapter, this)) _state.EditContextAdapter = null;
        EditContext.OnValidationRequested -= OnValidationRequested;
        _store.Clear();
    }
}

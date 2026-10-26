using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Base class for all headless components. Provides the shared infrastructure:
/// polymorphic element rendering via <see cref="As"/>, automatic ID generation,
/// element reference forwarding via <see cref="Ref"/>, attribute merging that
/// combines internal accessibility/data attributes with user-supplied ones, and
/// controlled/uncontrolled detection via <see cref="IsProvided"/>.
///
/// Design principles (aligned with Radix UI, React Aria, Headless UI, and Ark UI):
///  - Zero visual opinion: no CSS, no styles, only semantic HTML and data-* hooks
///  - Accessibility built-in: ARIA attributes managed internally
///  - Polymorphic rendering: any HTML element via the As parameter
///  - Styling hooks via data attributes: [data-state], [data-disabled], etc.
///  - Attribute merging: class and style concatenate; everything else user-wins
///  - Controlled and uncontrolled: a parameter is "controlled" when it is supplied
///
/// Subclass authoring pattern:
/// <code>
/// protected override string DefaultTag => "div";
///
/// protected override Dictionary&lt;string, object&gt; BuildComponentAttributes()
/// {
///     var attrs = base.BuildComponentAttributes();
///     SetDataState(attrs, IsOpen);
///     attrs["aria-expanded"] = IsOpen ? "true" : "false";
///     return attrs;
/// }
/// </code>
/// Then in BuildRenderTree call <see cref="GetFinalAttributes"/> once:
/// <code>
/// builder.AddMultipleAttributes(10, GetFinalAttributes());
/// </code>
/// </summary>
public abstract class BitBhComponentBase : ComponentBase
{
    private string? _componentId;
    private BitBhInterop? _interop;
    private readonly HashSet<string> _providedParameters = new(StringComparer.Ordinal);

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// The JS runtime wrapper. Uses the registered <see cref="BitBhInterop"/> service
    /// when <c>AddBitBlazorHeadlessUIServices()</c> was called, otherwise a shared per-runtime
    /// instance — so components work without any service registration.
    /// </summary>
    protected BitBhInterop Interop => _interop ??= BitBhInterop.Resolve(Services, JSRuntime);

    /// <summary>Resolves an optional service.</summary>
    protected T? GetService<T>() where T : class => Services.GetService<T>();

    /// <summary>
    /// Override the rendered HTML element. Each component declares its own
    /// <see cref="DefaultTag"/> (e.g. "button") which is used when As is null.
    /// </summary>
    [Parameter]
    public string? As { get; set; }

    /// <summary>
    /// Explicit HTML id. When omitted a stable auto-generated id is used.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Callback to capture the underlying <see cref="ElementReference"/>
    /// for DOM access / focus management.
    /// </summary>
    [Parameter]
    public Action<ElementReference>? Ref { get; set; }

    /// <summary>
    /// Captures all HTML attributes not matched by explicit parameters.
    /// These are merged onto the root element, giving consumers full control
    /// over class, style, data-*, aria-*, and any other HTML attribute.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// The default HTML element tag rendered when <see cref="As"/> is not set.
    /// </summary>
    protected abstract string DefaultTag { get; }

    /// <summary>
    /// The resolved HTML tag: user-supplied <see cref="As"/> or <see cref="DefaultTag"/>.
    /// </summary>
    protected string Tag => string.IsNullOrEmpty(As) ? DefaultTag : As;

    /// <summary>
    /// The resolved HTML id: user-supplied <see cref="Id"/> or auto-generated.
    /// </summary>
    protected string ComponentId => Id ?? (_componentId ??= BitBhId.New());

    /// <summary>The most recently captured root element reference (when the component captures one).</summary>
    protected ElementReference Element { get; private set; }

    /// <summary>
    /// An element-reference capture callback that records <see cref="Element"/>
    /// and forwards to the consumer's <see cref="Ref"/>.
    /// </summary>
    protected void CaptureElement(ElementReference element)
    {
        Element = element;
        Ref?.Invoke(element);
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _providedParameters.Clear();
        foreach (var parameter in parameters)
        {
            if (!parameter.Cascading)
                _providedParameters.Add(parameter.Name);
        }
        return base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Returns <c>true</c> when the consumer explicitly supplied the parameter
    /// named <paramref name="parameterName"/> in the latest render. This is how
    /// components decide between controlled and uncontrolled mode — it works for
    /// value types and for "controlled <c>null</c>", unlike null checks.
    /// </summary>
    protected bool IsProvided(string parameterName) => _providedParameters.Contains(parameterName);

    // ── Attribute building ───────────────────────────────────────────────────

    /// <summary>
    /// Override to provide this component's internal attributes: ARIA roles,
    /// data-state, data-* flags, and any other non-visual attributes the
    /// component manages. The base implementation returns an empty dictionary.
    /// Always call <c>base.BuildComponentAttributes()</c> to start with
    /// the base dictionary and add to it.
    /// </summary>
    /// <remarks>
    /// Do NOT call <see cref="BitBhAttributeUtilities.Merge"/> here — that is done
    /// automatically by <see cref="GetFinalAttributes"/>. Return the raw
    /// internal-only dictionary from this method.
    /// </remarks>
    protected virtual Dictionary<string, object> BuildComponentAttributes()
        => new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the final merged attribute dictionary ready to pass to
    /// <c>builder.AddMultipleAttributes</c>. Merges the result of
    /// <see cref="BuildComponentAttributes"/> with user-supplied
    /// <see cref="AdditionalAttributes"/>: <c>class</c> and <c>style</c>
    /// are concatenated; all other user-supplied values take precedence.
    /// </summary>
    protected Dictionary<string, object> GetFinalAttributes()
        => BitBhAttributeUtilities.Merge(BuildComponentAttributes(), AdditionalAttributes);

    /// <summary>
    /// Merges <paramref name="internalAttributes"/> with
    /// <see cref="AdditionalAttributes"/>. Prefer <see cref="GetFinalAttributes"/>
    /// combined with <see cref="BuildComponentAttributes"/> for new components.
    /// </summary>
    protected Dictionary<string, object> MergeAttributes(Dictionary<string, object> internalAttributes)
        => BitBhAttributeUtilities.Merge(internalAttributes, AdditionalAttributes);

    // ── DOM event handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates the component's own handler of the DOM event <paramref name="eventName"/>
    /// (<c>"onclick"</c>, <c>"onkeydown"</c> …). An element holds one handler per event, and the
    /// component's - written after the splatted attributes - replaces one the consumer put on the
    /// component (<c>&lt;BitBhTab @onclick="Track"&gt;</c>), so this runs the consumer's first.
    /// </summary>
    protected EventCallback<TArgs> On<TArgs>(string eventName, Func<TArgs, Task> handler)
        => EventCallback.Factory.Create<TArgs>(this, async args =>
        {
            await InvokeConsumerHandlerAsync(eventName, args);
            await handler(args);
        });

    /// <inheritdoc cref="On{TArgs}(string, Func{TArgs, Task})"/>
    protected EventCallback<TArgs> On<TArgs>(string eventName, Action<TArgs> handler)
        => On<TArgs>(eventName, args => { handler(args); return Task.CompletedTask; });

    /// <inheritdoc cref="On{TArgs}(string, Func{TArgs, Task})"/>
    protected EventCallback<TArgs> On<TArgs>(string eventName, Func<Task> handler)
        => On<TArgs>(eventName, _ => handler());

    /// <inheritdoc cref="On{TArgs}(string, Func{TArgs, Task})"/>
    protected EventCallback<TArgs> On<TArgs>(string eventName, Action handler)
        => On<TArgs>(eventName, _ => { handler(); return Task.CompletedTask; });

    /// <summary>Runs the handler of <paramref name="eventName"/> the consumer put on the component, if any.</summary>
    protected Task InvokeConsumerHandlerAsync<TArgs>(string eventName, TArgs args)
    {
        if (AdditionalAttributes is null || AdditionalAttributes.TryGetValue(eventName, out var handler) is false)
            return Task.CompletedTask;

        switch (handler)
        {
            case EventCallback<TArgs> callback: return callback.InvokeAsync(args);
            case EventCallback callback: return callback.InvokeAsync(args);
            case Func<TArgs, Task> func: return func(args);
            case Func<Task> func: return func();
            case Action<TArgs> action: action(args); return Task.CompletedTask;
            case Action action: action(); return Task.CompletedTask;
            default: return Task.CompletedTask;
        }
    }

    /// <summary>Returns the consumer-supplied value of an unmatched attribute, if any.</summary>
    protected string? GetAdditionalAttribute(string name)
        => AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;

    // ── Data-attribute helpers ────────────────────────────────────────────────

    /// <summary>
    /// Sets <c>data-state</c> following the Radix UI convention.
    /// When <paramref name="condition"/> is true the value is
    /// <paramref name="trueValue"/> (default <c>"open"</c>),
    /// otherwise <paramref name="falseValue"/> (default <c>"closed"</c>).
    /// </summary>
    /// <example>
    /// SetDataState(attrs, IsOpen);                          // "open" / "closed"
    /// SetDataState(attrs, IsChecked, "checked", "unchecked");
    /// SetDataState(attrs, IsSelected, "selected", "unselected");
    /// </example>
    protected static void SetDataState(
        Dictionary<string, object> attrs,
        bool condition,
        string trueValue = "open",
        string falseValue = "closed")
        => attrs["data-state"] = condition ? trueValue : falseValue;

    /// <summary>
    /// Adds a boolean presence attribute <c>data-{name}</c> (empty string
    /// value) when <paramref name="condition"/> is true, omitting it otherwise.
    /// Use for flags like <c>data-disabled</c>, <c>data-loading</c>,
    /// <c>data-required</c>.
    /// </summary>
    protected static void SetDataFlag(
        Dictionary<string, object> attrs,
        string name,
        bool condition)
        => BitBhAttributeUtilities.SetDataAttribute(attrs, name, condition);

    /// <summary>
    /// Sets <c>data-{name}="{value}"</c> unconditionally.
    /// Use for enumerated data attributes like
    /// <c>data-orientation="horizontal"</c>, <c>data-side="top"</c>.
    /// </summary>
    protected static void SetDataValue(
        Dictionary<string, object> attrs,
        string name,
        string value)
        => BitBhAttributeUtilities.SetDataAttribute(attrs, name, value);

    /// <summary>Sets an ARIA boolean attribute to <c>"true"</c>/<c>"false"</c>.</summary>
    protected static void SetAriaBool(Dictionary<string, object> attrs, string name, bool value)
        => attrs[name] = value ? "true" : "false";
}

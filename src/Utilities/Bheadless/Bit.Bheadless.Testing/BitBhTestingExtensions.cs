using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bheadless.Testing;

/// <summary>bUnit integration: context setup, accessibility assertions and keyboard helpers.</summary>
public static class BitBhTestingExtensions
{
    /// <summary>
    /// Prepares a bUnit context for Bheadless components: loose JS interop (the
    /// browser runtime is a progressive enhancement, so components work without it),
    /// the library services and a recording announcer, which is returned.
    /// </summary>
    public static BitBhTestAnnouncer AddBitBheadlessServices(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var announcer = new BitBhTestAnnouncer();
        context.Services.AddSingleton<IBitBhAnnouncer>(announcer);
        context.Services.AddSingleton(announcer);
        context.Services.AddBitBheadlessServices();
        return announcer;
    }

    /// <summary>Audits the rendered markup of <paramref name="cut"/>.</summary>
    public static BitBhA11yReport Audit<TComponent>(this IRenderedComponent<TComponent> cut, BitBhA11yOptions? options = null)
        where TComponent : IComponent
        => BitBhA11y.Audit(cut.Nodes, options);

    /// <summary>Throws a <see cref="BitBhA11yException"/> listing every violation when the markup isn't accessible.</summary>
    public static IRenderedComponent<TComponent> ShouldBeAccessible<TComponent>(this IRenderedComponent<TComponent> cut, BitBhA11yOptions? options = null)
        where TComponent : IComponent
    {
        var report = cut.Audit(options);
        if (!report.Passed) throw new BitBhA11yException(report);
        return cut;
    }

    /// <summary>The Tab sequence of the rendered markup.</summary>
    public static IReadOnlyList<BitBhTabStop> TabSequence<TComponent>(this IRenderedComponent<TComponent> cut)
        where TComponent : IComponent
        => BitBhFocusOrder.TabSequence(cut.Nodes);

    /// <summary>The accessible name of an element.</summary>
    public static string AccessibleName(this IElement element) => BitBhAccessibleName.Compute(element);

    /// <summary>The accessible description of an element.</summary>
    public static string AccessibleDescription(this IElement element) => BitBhAccessibleName.ComputeDescription(element);

    /// <summary>The element's role (explicit or implicit).</summary>
    public static string? Role(this IElement element) => BitBhAria.GetRole(element);

    /// <summary>The element referenced by <c>aria-activedescendant</c>, or null.</summary>
    public static IElement? ActiveDescendant<TComponent>(this IRenderedComponent<TComponent> cut, IElement element)
        where TComponent : IComponent
    {
        var id = element.GetAttribute("aria-activedescendant");
        // Compared, not selected: an id may hold quotes or other characters a selector would need escaped.
        return string.IsNullOrEmpty(id) ? null : cut.FindAll("[id]").FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// The id of the element Bheadless last asked the browser to focus, or null.
    /// bUnit has no real focus, so keyboard tests assert on the focus request instead.
    /// </summary>
    public static string? LastFocusRequest(this BunitContext context)
        => context.JSInterop.Invocations
            .Where(i => i.Identifier == "focus.focus")
            .Select(i => i.Arguments.Count > 0 ? i.Arguments[0] as string : null)
            .LastOrDefault();

    /// <summary>
    /// Dispatches a keydown for <paramref name="key"/> (a <c>KeyboardEvent.key</c> value like "ArrowDown", " ", "a").
    /// <c>KeyboardEvent.code</c> is the physical key of a US layout: "KeyA", "Digit1", "Space", "Slash", "ArrowDown" …
    /// (empty when there is none).
    /// </summary>
    /// <remarks>
    /// Like a browser, one press reaches both kinds of handler: <c>onkeydown</c>, and the library's owned
    /// <c>onbitbhkeydown</c> (see <see cref="BitBhEventHandlers"/>), whose owner is worked out from
    /// <paramref name="element"/> the way the runtime works it out from the event's target.
    /// </remarks>
    /// <param name="element">The element the key is pressed on.</param>
    /// <param name="key">The <c>KeyboardEvent.key</c> value.</param>
    /// <param name="shift">Whether Shift is held.</param>
    /// <param name="ctrl">Whether Ctrl is held.</param>
    /// <param name="alt">Whether Alt is held.</param>
    /// <param name="meta">Whether Meta (⌘) is held.</param>
    /// <param name="repeat">Whether this is an auto-repeat of a key held down.</param>
    public static void Press(this IElement element, string key, bool shift = false, bool ctrl = false, bool alt = false, bool meta = false, bool repeat = false)
    {
        var code = KeyCode(key);
        Dispatch(element, "onkeydown",
            new KeyboardEventArgs { Key = key, Code = code, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Repeat = repeat, Type = "keydown" },
            BitBhEventHandlers.KeyDown,
            ownerId => new BitBhKeyboardEventArgs { Key = key, Code = code, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Repeat = repeat, Type = "keydown", OwnerId = ownerId });
    }

    /// <summary>
    /// Clicks <paramref name="element"/> the way a pointer does in a browser: one click reaches both
    /// <c>onclick</c> and the library's owned <c>onbitbhclick</c> (see <see cref="BitBhEventHandlers"/>) -
    /// which is what a grid cell, a tree item and the like act on, so click those with this rather than bUnit's
    /// <c>Click()</c>.
    /// </summary>
    public static void PointerClick(this IElement element, bool shift = false, bool ctrl = false, bool alt = false, bool meta = false)
        => Dispatch(element, "onclick",
            new MouseEventArgs { Detail = 1, Button = 0, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Type = "click" },
            BitBhEventHandlers.Click,
            ownerId => new BitBhMouseEventArgs { Detail = 1, Button = 0, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Type = "click", OwnerId = ownerId });

    // The runtime's rule (Scripts/Bit.Bheadless.lib.module.ts): the nearest owner around the target,
    // unless interactive content nested in it comes first. A label counts, since it activates its control.
    private const string Interactive =
        "a[href], area[href], button, input, select, textarea, label, summary, iframe, audio[controls], video[controls], " +
        "[contenteditable]:not([contenteditable=\"false\"]), [tabindex], [role=\"button\"], [role=\"link\"], [role=\"checkbox\"], " +
        "[role=\"switch\"], [role=\"radio\"], [role=\"slider\"], [role=\"spinbutton\"], [role=\"textbox\"], [role=\"combobox\"], " +
        "[role=\"menuitem\"], [role=\"option\"], [role=\"tab\"]";

    private static string EventOwnerId(IElement target)
    {
        for (var element = target; element is not null; element = element.ParentElement)
        {
            if (element.HasAttribute(BitBhEventHandlers.OwnerAttribute)) return element.Id ?? string.Empty;
            if (element.Matches(Interactive)) return string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Dispatches one browser event to both kinds of handler of it that the element or an ancestor has (bUnit
    /// bubbles each from there). Everything is read off the element first: the first handler may re-render it
    /// away.
    /// </summary>
    private static void Dispatch<TOwnedArgs>(IElement element, string eventName, EventArgs args, string ownedEventName, Func<string, TOwnedArgs> ownedArgs)
        where TOwnedArgs : EventArgs
    {
        var plain = HasHandler(element, eventName);
        var owned = HasHandler(element, ownedEventName);
        if (plain is false && owned is false) throw new MissingEventHandlerException(element, eventName);
        var ownedEventArgs = owned ? ownedArgs(EventOwnerId(element)) : null;

        if (plain) Await(element.TriggerEventAsync(eventName, args));
        if (ownedEventArgs is null) return;
        try
        {
            Await(element.TriggerEventAsync(ownedEventName, ownedEventArgs));
        }
        catch (ElementRemovedFromDomException) when (plain)
        {
            // The plain handler re-rendered the element away; in a browser the owned handlers it had went with it.
        }
    }

    /// <summary>Whether the event, bubbling from <paramref name="element"/>, reaches a handler before a stopPropagation stops it.</summary>
    private static bool HasHandler(IElement element, string eventName)
    {
        for (var node = element; node is not null; node = node.ParentElement)
        {
            if (node.HasAttribute($"blazor:{eventName}")) return true;
            if (node.HasAttribute($"blazor:{eventName}:stoppropagation")) return false;
        }
        return false;
    }

    // A handler that failed synchronously fails the test right here, as bUnit's own helpers do.
    private static void Await(Task task)
    {
        if (task.IsFaulted) task.GetAwaiter().GetResult();
    }

    /// <summary>Dispatches keydowns for a sequence of keys.</summary>
    public static void PressSequence(this IElement element, params string[] keys)
    {
        foreach (var key in keys) element.Press(key);
    }

    /// <summary>Types text: one keydown per character (typeahead), with Shift held for uppercase letters and shifted symbols.</summary>
    public static void Type(this IElement element, string text)
    {
        foreach (var c in text) element.Press(c.ToString(), shift: char.IsUpper(c) || ShiftedSymbols.Contains(c));
    }

    private const string ShiftedSymbols = "~!@#$%^&*()_+{}|:\"<>?";

    /// <summary>The <c>KeyboardEvent.code</c> of a key on a US layout.</summary>
    internal static string KeyCode(string key)
    {
        if (key.Length == 1)
        {
            var c = key[0];
            if (char.IsAsciiLetter(c)) return "Key" + char.ToUpperInvariant(c);
            if (char.IsAsciiDigit(c)) return "Digit" + c;
            return c switch
            {
                ' ' => "Space",
                ')' => "Digit0", '!' => "Digit1", '@' => "Digit2", '#' => "Digit3", '$' => "Digit4",
                '%' => "Digit5", '^' => "Digit6", '&' => "Digit7", '*' => "Digit8", '(' => "Digit9",
                '-' or '_' => "Minus", '=' or '+' => "Equal", '[' or '{' => "BracketLeft", ']' or '}' => "BracketRight",
                '\\' or '|' => "Backslash", ';' or ':' => "Semicolon", '\'' or '"' => "Quote", ',' or '<' => "Comma",
                '.' or '>' => "Period", '/' or '?' => "Slash", '`' or '~' => "Backquote",
                _ => string.Empty,
            };
        }
        return key switch
        {
            "Enter" or "Escape" or "Tab" or "Backspace" or "Delete" or "Insert" or "Home" or "End" or "PageUp" or "PageDown"
                or "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight" or "CapsLock" or "ContextMenu" => key,
            "Shift" or "Control" or "Alt" or "Meta" => key + "Left",
            _ when key.Length is 2 or 3 && key[0] == 'F' && int.TryParse(key.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is >= 1 and <= 24 => key,
            _ => string.Empty,
        };
    }
}

using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorHeadlessUI.Testing;

/// <summary>bUnit integration: context setup, accessibility assertions and keyboard helpers.</summary>
public static class BitBhTestingExtensions
{
    /// <summary>
    /// Prepares a bUnit context for BlazorHeadlessUI components: loose JS interop (the
    /// browser runtime is a progressive enhancement, so components work without it),
    /// the library services and a recording announcer, which is returned.
    /// </summary>
    public static BitBhTestAnnouncer AddBitBlazorHeadlessUIServices(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var announcer = new BitBhTestAnnouncer();
        context.Services.AddSingleton<IBitBhAnnouncer>(announcer);
        context.Services.AddSingleton(announcer);
        context.Services.AddBitBlazorHeadlessUIServices();
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
    /// The id of the element BlazorHeadlessUI last asked the browser to focus, or null.
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
    public static void Press(this IElement element, string key, bool shift = false, bool ctrl = false, bool alt = false, bool meta = false)
        => element.KeyDown(new KeyboardEventArgs { Key = key, Code = KeyCode(key), ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Type = "keydown" });

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

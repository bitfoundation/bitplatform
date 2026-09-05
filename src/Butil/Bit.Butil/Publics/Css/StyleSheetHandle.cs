using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A stylesheet created by <see cref="Css.CreateStyleSheet"/>, already applied to the document.
/// </summary>
/// <remarks>
/// Rules here reach the whole page through selectors, which is the thing setting a style on one
/// element cannot do: a theme, a print stylesheet, a <c>::highlight()</c> rule, a
/// <c>@media</c> block.
/// </remarks>
public sealed class StyleSheetHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _removed;

    internal StyleSheetHandle(IJSRuntime js, Guid id)
    {
        _js = js;
        Id = id;
    }

    /// <summary>The internal stylesheet id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Adds a rule.
    /// </summary>
    /// <param name="rule">A complete rule, selector and braces included: <c>".card { color: red }"</c>.</param>
    /// <param name="index">Where to put it, or -1 (the default) for the end. Later rules win ties, so the end is usually what you want.</param>
    /// <returns>The index it went in at, or -1 when the parser rejected it.</returns>
    /// <remarks>
    /// A rule the parser cannot read is refused rather than ignored - which is more useful than a
    /// stylesheet quietly missing a line, and is why this answers with -1 instead of pretending.
    /// </remarks>
    public ValueTask<int> InsertRule(string rule, int index = -1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        return _js.Invoke<int>("BitButil.css.insertRule", Id, rule, index);
    }

    /// <summary>
    /// Removes the rule at an index. False when there is none there.
    /// </summary>
    /// <remarks>
    /// Indices shift as rules are removed, so removing several is a matter of going backwards - or
    /// of <see cref="Replace"/>ing the whole sheet.
    /// </remarks>
    public ValueTask<bool> DeleteRule(int index) => _js.Invoke<bool>("BitButil.css.deleteRule", Id, index);

    /// <summary>
    /// Every rule in the sheet, as text.
    /// </summary>
    /// <returns>The rules, or an empty array for a sheet that cannot be read.</returns>
    public ValueTask<string[]> GetRules() => _js.Invoke<string[]>("BitButil.css.rules", Id);

    /// <summary>
    /// Replaces the whole sheet with the given CSS. The simplest way to keep a theme in sync -
    /// build the text and hand it over, rather than tracking rule indices.
    /// </summary>
    public ValueTask<bool> Replace(string css)
    {
        ArgumentNullException.ThrowIfNull(css);
        return _js.Invoke<bool>("BitButil.css.replaceSheet", Id, css);
    }

    /// <summary>
    /// Removes the stylesheet from the document. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_removed) return;
        _removed = true;

        try { await _js.InvokeVoid("BitButil.css.removeSheet", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}

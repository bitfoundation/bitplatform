using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

/// <summary>
/// The parts of <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element">Element</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement">HTMLElement</see>
/// that change what an element contains or how it is presented: inserting text and markup around
/// and inside it, its class list, its <c>data-*</c> attributes, its inline style, its popover state
/// and the queries that count what is under it.
/// </summary>
/// <remarks>
/// Blazor owns the DOM it rendered, and a diff can undo anything written here on the next render.
/// These are for elements Blazor does not re-render - a container it renders once, or an element
/// outside the component's own markup.
/// <br/>
/// The DOM insertion methods take nodes as well as strings; only the strings cross this boundary.
/// A <see cref="ElementReference"/> is minted by Blazor's renderer and cannot be handed back to it
/// from JavaScript, so there is no way to name another element as the thing being inserted. Each
/// string becomes a text node - use <see cref="InsertAdjacentHtml"/> to insert markup.
/// </remarks>
public static class ElementReferenceDomExtensions
{
    /// <summary>
    /// Inserts text nodes immediately after the element, as siblings.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/after">https://developer.mozilla.org/en-US/docs/Web/API/Element/after</see>
    /// </summary>
    public static ValueTask After(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.after", element, nodes);

    /// <summary>
    /// Appends text nodes inside the element, after its last child.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/append">https://developer.mozilla.org/en-US/docs/Web/API/Element/append</see>
    /// </summary>
    public static ValueTask Append(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.append", element, nodes);

    /// <summary>
    /// Inserts text nodes immediately before the element, as siblings.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/before">https://developer.mozilla.org/en-US/docs/Web/API/Element/before</see>
    /// </summary>
    public static ValueTask Before(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.before", element, nodes);

    /// <summary>
    /// Prepends text nodes inside the element, before its first child.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/prepend">https://developer.mozilla.org/en-US/docs/Web/API/Element/prepend</see>
    /// </summary>
    public static ValueTask Prepend(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.prepend", element, nodes);

    /// <summary>
    /// Replaces every child of the element with the given text nodes. Passing nothing empties it.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/replaceChildren">https://developer.mozilla.org/en-US/docs/Web/API/Element/replaceChildren</see>
    /// </summary>
    public static ValueTask ReplaceChildren(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.replaceChildren", element, nodes);

    /// <summary>
    /// Replaces the element itself with the given text nodes. The reference is dangling afterwards.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/replaceWith">https://developer.mozilla.org/en-US/docs/Web/API/Element/replaceWith</see>
    /// </summary>
    public static ValueTask ReplaceWith(this ElementReference element, params string[] nodes)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.replaceWith", element, nodes);

    /// <summary>
    /// Parses <paramref name="html"/> as markup and inserts the result at the given position,
    /// without reparsing the element's existing children.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/insertAdjacentHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/insertAdjacentHTML</see>
    /// </summary>
    /// <remarks>
    /// <b>Security note:</b> the markup is parsed as-is and bypasses Blazor's encoding. Never pass
    /// untrusted input - use <see cref="InsertAdjacentText"/>, which cannot introduce elements, or
    /// <see cref="SetHtml"/>, which sanitizes.
    /// </remarks>
    public static ValueTask InsertAdjacentHtml(this ElementReference element, InsertPosition position, string html)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.insertAdjacentHTML", element, PositionName(position), html);

    /// <summary>
    /// Inserts <paramref name="text"/> as a text node at the given position. Markup in the string
    /// stays text.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/insertAdjacentText">https://developer.mozilla.org/en-US/docs/Web/API/Element/insertAdjacentText</see>
    /// </summary>
    public static ValueTask InsertAdjacentText(this ElementReference element, InsertPosition position, string text)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.insertAdjacentText", element, PositionName(position), text);

    /// <summary>
    /// Serializes the element's contents to HTML, optionally including the shadow roots that were
    /// attached as serializable - which <c>InnerHtml</c> always leaves out.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/getHTML</see>
    /// </summary>
    /// <remarks>
    /// Where the browser has no <c>getHTML</c>, this falls back to <c>innerHTML</c> - the same
    /// answer for a tree with no shadow roots in it.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns an empty string rather than throwing.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GetHtmlJsOptions))]
    public static ValueTask<string> GetHtml(this ElementReference element, GetHtmlOptions? options = null)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getHTML", element, options?.ToJsObject());

    /// <summary>
    /// Replaces the element's contents with <paramref name="html"/>, running it through the
    /// browser's HTML sanitizer first: scripts, event-handler attributes and <c>javascript:</c> URLs
    /// are stripped.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/setHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/setHTML</see>
    /// </summary>
    /// <remarks>
    /// This is the one to reach for when the markup came from a user. It throws where the browser
    /// has no <c>setHTML</c>: quietly falling back to an unsanitized write would turn the safe call
    /// into the unsafe one. Feature-detect with a <c>try</c> and fall back to your own sanitizer.
    /// </remarks>
    public static ValueTask SetHtml(this ElementReference element, string html)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setHTML", element, html, null);

    /// <summary>
    /// Replaces the element's contents with <paramref name="html"/> without sanitizing, parsing
    /// declarative shadow roots in it along the way.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/setHTMLUnsafe">https://developer.mozilla.org/en-US/docs/Web/API/Element/setHTMLUnsafe</see>
    /// </summary>
    /// <remarks>
    /// <b>Security note:</b> "unsafe" is the spec's own word for it. Only for markup you produced.
    /// Use <see cref="SetHtml"/> for anything else.
    /// </remarks>
    public static ValueTask SetHtmlUnsafe(this ElementReference element, string html)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setHTMLUnsafe", element, html);

    /// <summary>
    /// Every border box the element occupies, in viewport coordinates - more than one for an inline
    /// element that wraps across lines.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getClientRects">https://developer.mozilla.org/en-US/docs/Web/API/Element/getClientRects</see>
    /// </summary>
    /// <remarks>
    /// Empty for an element that generates no boxes at all, which is how <c>display: none</c> reads
    /// here - and also what prerender/SSR (no JS runtime) hands back, so the two cannot be told
    /// apart. Defer the read to <c>OnAfterRenderAsync</c> if you branch on it.
    /// </remarks>
    public static async ValueTask<Rect[]> GetClientRects(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<Rect[]>("BitButil.element.getClientRects", element);

    /// <summary>
    /// Adds classes to the element, ignoring the ones it already has.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    public static ValueTask AddClass(this ElementReference element, params string[] tokens)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.classListAdd", element, tokens);

    /// <summary>
    /// Removes classes from the element, ignoring the ones it does not have.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    public static ValueTask RemoveClass(this ElementReference element, params string[] tokens)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.classListRemove", element, tokens);

    /// <summary>
    /// Adds the class when absent and removes it when present, or pins it to
    /// <paramref name="force"/>. Returns whether the class is on the element afterwards.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    public static async ValueTask<bool> ToggleClass(this ElementReference element, string token, bool? force = null)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.classListToggle", element, token, force);

    /// <summary>
    /// Swaps <paramref name="oldToken"/> for <paramref name="newToken"/> in place, keeping its
    /// position in the list. Returns false when the old class was not there, in which case nothing
    /// is added.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    public static async ValueTask<bool> ReplaceClass(this ElementReference element, string oldToken, string newToken)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.classListReplace", element, oldToken, newToken);

    /// <summary>
    /// Returns whether the element carries the class.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>false</c> rather than throwing, so the
    /// result can't be distinguished from a genuine one. Defer the read to <c>OnAfterRenderAsync</c>
    /// if you branch on it.
    /// </remarks>
    public static async ValueTask<bool> ContainsClass(this ElementReference element, string token)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.classListContains", element, token);

    /// <summary>
    /// The element's classes, in document order - <c>className</c> split into its tokens.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/classList">https://developer.mozilla.org/en-US/docs/Web/API/Element/classList</see>
    /// </summary>
    public static async ValueTask<string[]> GetClassList(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<string[]>("BitButil.element.getClassList", element);

    /// <summary>
    /// Reads one <c>data-*</c> attribute by its dataset key - <c>userId</c> for
    /// <c>data-user-id</c>. Null when the attribute is absent.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset</see>
    /// </summary>
    public static ValueTask<string> GetData(this ElementReference element, string key)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getData", element, key);

    /// <summary>
    /// Writes one <c>data-*</c> attribute by its dataset key, creating it when absent.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset</see>
    /// </summary>
    public static ValueTask SetData(this ElementReference element, string key, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setData", element, key, value);

    /// <summary>
    /// Removes one <c>data-*</c> attribute by its dataset key.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset</see>
    /// </summary>
    public static ValueTask RemoveData(this ElementReference element, string key)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.removeData", element, key);

    /// <summary>
    /// The dataset keys the element carries - <c>userId</c> for <c>data-user-id</c>, not the
    /// attribute names.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset</see>
    /// </summary>
    public static async ValueTask<string[]> GetDataNames(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<string[]>("BitButil.element.getDataNames", element);

    /// <summary>
    /// The element's whole inline style, as it would be written in a <c>style</c> attribute.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/style">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/style</see>
    /// </summary>
    /// <remarks>
    /// Inline style only. What a stylesheet contributes is not here - that is
    /// <c>getComputedStyle</c>, which belongs to the window rather than to the element.
    /// </remarks>
    public static ValueTask<string> GetStyleText(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getStyleText", element);

    /// <summary>
    /// Replaces the element's whole inline style. Anything already there is dropped.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/style">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/style</see>
    /// </summary>
    public static ValueTask SetStyleText(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setStyleText", element, value);

    /// <summary>
    /// Reads one inline style declaration by its CSS property name - <c>"background-color"</c>, and
    /// custom properties (<c>"--accent"</c>) too. Empty when the property is not set inline.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/getPropertyValue">https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/getPropertyValue</see>
    /// </summary>
    public static ValueTask<string> GetStyleProperty(this ElementReference element, string name)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getStyleProperty", element, name);

    /// <summary>
    /// Sets one inline style declaration by its CSS property name, leaving the rest alone.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/setProperty">https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/setProperty</see>
    /// </summary>
    /// <remarks>Pass <paramref name="important"/> to mark the declaration <c>!important</c>.</remarks>
    public static ValueTask SetStyleProperty(this ElementReference element, string name, string value, bool important = false)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setStyleProperty", element, name, value, important ? "important" : null);

    /// <summary>
    /// Removes one inline style declaration and returns what it held.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/removeProperty">https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleDeclaration/removeProperty</see>
    /// </summary>
    public static ValueTask<string> RemoveStyleProperty(this ElementReference element, string name)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.removeStyleProperty", element, name);

    /// <summary>
    /// Shows the element as a popover, in the top layer above the rest of the page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/showPopover">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/showPopover</see>
    /// </summary>
    /// <remarks>
    /// The element needs a <c>popover</c> attribute - see
    /// <see cref="ElementReferenceStateExtensions.SetPopover"/>. A no-op where the browser has no
    /// popover support, so a page can call it without feature-detecting first.
    /// </remarks>
    public static ValueTask ShowPopover(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.showPopover", element);

    /// <summary>
    /// Hides the element if it is showing as a popover.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidePopover">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidePopover</see>
    /// </summary>
    public static ValueTask HidePopover(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.hidePopover", element);

    /// <summary>
    /// Shows the popover when hidden and hides it when shown, or pins it to
    /// <paramref name="force"/>. Returns whether it is showing afterwards.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/togglePopover">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/togglePopover</see>
    /// </summary>
    /// <remarks>
    /// False where the browser has no popover support, which is indistinguishable from a popover
    /// that ended up hidden - as it also is during prerender/SSR (no JS runtime).
    /// </remarks>
    public static async ValueTask<bool> TogglePopover(this ElementReference element, bool? force = null)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.togglePopover", element, force);

    /// <summary>
    /// Returns whether any descendant of the element matches <paramref name="selectors"/>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/querySelector">https://developer.mozilla.org/en-US/docs/Web/API/Element/querySelector</see>
    /// </summary>
    /// <remarks>
    /// The DOM method hands back the matched element. An <see cref="ElementReference"/> can only be
    /// minted by Blazor's renderer, so there is nothing to hand back across the boundary - capture
    /// the element you need with <c>@ref</c> instead, and use this for the existence question.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>false</c> rather than throwing.
    /// </remarks>
    public static async ValueTask<bool> QuerySelectorMatches(this ElementReference element, string selectors)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.querySelectorMatches", element, selectors);

    /// <summary>
    /// How many descendants of the element match <paramref name="selectors"/>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/querySelectorAll">https://developer.mozilla.org/en-US/docs/Web/API/Element/querySelectorAll</see>
    /// </summary>
    /// <remarks>
    /// The count rather than the elements, for the same reason as
    /// <see cref="QuerySelectorMatches"/>. During prerender/SSR (no JS runtime) this returns
    /// <c>0</c> rather than throwing.
    /// </remarks>
    public static async ValueTask<int> QuerySelectorAllCount(this ElementReference element, string selectors)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<int>("BitButil.element.querySelectorAllCount", element, selectors);

    private static string PositionName(InsertPosition position) => position switch
    {
        InsertPosition.BeforeBegin => "beforebegin",
        InsertPosition.AfterBegin => "afterbegin",
        InsertPosition.AfterEnd => "afterend",
        _ => "beforeend",
    };
}

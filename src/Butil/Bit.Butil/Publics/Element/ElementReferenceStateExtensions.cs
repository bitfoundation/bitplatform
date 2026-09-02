using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

/// <summary>
/// The remaining state an
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element">Element</see> or an
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement">HTMLElement</see>
/// carries: its identity in the document tree, its tooltip and language, the hints it gives a
/// virtual keyboard and a spell checker, its popover kind and its shadow-DOM wiring.
/// </summary>
/// <remarks>
/// Every read is one interop round trip, and every one of these has a Blazor equivalent that costs
/// nothing: <c>title</c>, <c>lang</c> and <c>draggable</c> are attributes you can simply render.
/// Reach for these when the element is not yours to re-render, or when you need to read what
/// something else put there.
/// <br/>
/// During prerender/SSR (no JS runtime) every read returns a safe default - an empty string, an
/// empty array, <c>false</c>, <c>0</c> - rather than throwing, so a value read there cannot be told
/// apart from a genuine one. Defer reads you branch on to <c>OnAfterRenderAsync</c>.
/// </remarks>
public static class ElementReferenceStateExtensions
{
    /// <summary>
    /// The label a keyboard shortcut for this element would be shown with - "Alt+S" or "⌃⌥S",
    /// depending on the platform. Empty on engines that do not compute one.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKeyLabel">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKeyLabel</see>
    /// </summary>
    public static ValueTask<string> GetAccessKeyLabel(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.accessKeyLabel", element);

    /// <summary>
    /// The name of the slot this element is assigned to inside its shadow host, or null when it is
    /// not slotted. This is where the element <i>landed</i>; <see cref="GetSlot"/> is where it asked
    /// to go.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/assignedSlot">https://developer.mozilla.org/en-US/docs/Web/API/Element/assignedSlot</see>
    /// </summary>
    /// <remarks>
    /// The DOM property is the slot element itself; an <see cref="ElementReference"/> can only be
    /// minted by Blazor's renderer, so what crosses the boundary is the slot's name.
    /// </remarks>
    public static ValueTask<string> GetAssignedSlotName(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.assignedSlotName", element);

    /// <summary>
    /// How a virtual keyboard should capitalize text typed into the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocapitalize">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocapitalize</see>
    /// </summary>
    public static async ValueTask<Autocapitalize> GetAutocapitalize(this ElementReference element)
    {
        var value = await ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAutocapitalize", element);
        return value switch
        {
            "none" => Autocapitalize.None,
            "off" => Autocapitalize.Off,
            "on" => Autocapitalize.On,
            "sentences" => Autocapitalize.Sentences,
            "words" => Autocapitalize.Words,
            "characters" => Autocapitalize.Characters,
            _ => Autocapitalize.NotSet,
        };
    }
    /// <summary>
    /// How a virtual keyboard should capitalize text typed into the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocapitalize">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocapitalize</see>
    /// </summary>
    public static async ValueTask SetAutocapitalize(this ElementReference element, Autocapitalize value)
    {
        var v = value switch
        {
            Autocapitalize.None => "none",
            Autocapitalize.Off => "off",
            Autocapitalize.On => "on",
            Autocapitalize.Sentences => "sentences",
            Autocapitalize.Words => "words",
            Autocapitalize.Characters => "characters",
            _ => "",
        };
        await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAutocapitalize", element, v);
    }

    /// <summary>
    /// Whether the browser may autocorrect what the user types into the element. Safari and
    /// Chromium; false elsewhere, where the feature does not exist.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocorrect">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocorrect</see>
    /// </summary>
    public static async ValueTask<bool> GetAutocorrect(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.getAutocorrect", element);
    /// <summary>
    /// Whether the browser may autocorrect what the user types into the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocorrect">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autocorrect</see>
    /// </summary>
    public static async ValueTask SetAutocorrect(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAutocorrect", element, value);

    /// <summary>
    /// Whether the element asks for focus when the page loads.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autofocus">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autofocus</see>
    /// </summary>
    /// <remarks>
    /// Setting this after load does nothing on its own - the browser has already decided where
    /// focus goes. Use <c>Focus</c> for that.
    /// </remarks>
    public static async ValueTask<bool> GetAutofocus(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.getAutofocus", element);
    /// <summary>
    /// Whether the element asks for focus when the page loads.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autofocus">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/autofocus</see>
    /// </summary>
    public static async ValueTask SetAutofocus(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAutofocus", element, value);

    /// <summary>
    /// How many element children the element has - text nodes and comments not counted.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/childElementCount">https://developer.mozilla.org/en-US/docs/Web/API/Element/childElementCount</see>
    /// </summary>
    public static async ValueTask<int> GetChildElementCount(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<int>("BitButil.element.childElementCount", element);

    /// <summary>
    /// The effective CSS <c>zoom</c> applied to the element by itself and its ancestors - the factor
    /// between the numbers <c>GetBoundingClientRect</c> reports and the ones the layout was written
    /// in. 1 when nothing is zoomed, and on engines with no CSS zoom.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/currentCSSZoom">https://developer.mozilla.org/en-US/docs/Web/API/Element/currentCSSZoom</see>
    /// </summary>
    public static async ValueTask<double> GetCurrentCssZoom(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<double>("BitButil.element.currentCSSZoom", element);

    /// <summary>
    /// Whether the element can be dragged. Note that this is a tri-state attribute in HTML - the
    /// property collapses it to the effective boolean the browser acts on.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/draggable">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/draggable</see>
    /// </summary>
    public static async ValueTask<bool> GetDraggable(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.getDraggable", element);
    /// <summary>
    /// Whether the element can be dragged.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/draggable">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/draggable</see>
    /// </summary>
    public static async ValueTask SetDraggable(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setDraggable", element, value);

    /// <summary>
    /// The name this element is reported under in Element Timing performance entries. Empty when
    /// the element is not being timed.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/elementTiming">https://developer.mozilla.org/en-US/docs/Web/API/Element/elementTiming</see>
    /// </summary>
    public static ValueTask<string> GetElementTiming(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getElementTiming", element);
    /// <summary>
    /// Marks the element for Element Timing under the given name, so a
    /// <c>PerformanceObserver</c> watching "element" entries reports when it was painted.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/elementTiming">https://developer.mozilla.org/en-US/docs/Web/API/Element/elementTiming</see>
    /// </summary>
    /// <remarks>
    /// Only an element that has not been painted yet can be timed - setting this on something
    /// already on screen reports nothing.
    /// </remarks>
    public static ValueTask SetElementTiming(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setElementTiming", element, value);

    /// <summary>
    /// Whether the element hosts a shadow root that scripts can reach - an open one.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/shadowRoot">https://developer.mozilla.org/en-US/docs/Web/API/Element/shadowRoot</see>
    /// </summary>
    /// <remarks>
    /// False for a closed shadow root as well as for no shadow root at all: a closed one is not
    /// exposed to script, which is the point of closing it.
    /// </remarks>
    public static async ValueTask<bool> HasShadowRoot(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.hasShadowRoot", element);

    /// <summary>
    /// The element's language, as a BCP 47 tag. Empty when it inherits one.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/lang">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/lang</see>
    /// </summary>
    public static ValueTask<string> GetLang(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getLang", element);
    /// <summary>
    /// The element's language, as a BCP 47 tag.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/lang">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/lang</see>
    /// </summary>
    public static ValueTask SetLang(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setLang", element, value);

    /// <summary>
    /// The element's local name, without a namespace prefix and in the case the document uses -
    /// lowercase in HTML, where <c>GetTagName</c> reports uppercase.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/localName">https://developer.mozilla.org/en-US/docs/Web/API/Element/localName</see>
    /// </summary>
    public static ValueTask<string> GetLocalName(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.localName", element);

    /// <summary>
    /// The namespace the element belongs to - the XHTML namespace for HTML elements, and the SVG
    /// one for anything inside an <c>&lt;svg&gt;</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/namespaceURI">https://developer.mozilla.org/en-US/docs/Web/API/Element/namespaceURI</see>
    /// </summary>
    public static ValueTask<string> GetNamespaceUri(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.namespaceURI", element);

    /// <summary>
    /// The element's CSP nonce. Empty when it has none.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/nonce">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/nonce</see>
    /// </summary>
    /// <remarks>
    /// Browsers hide the nonce <i>attribute</i> from scripts so an injected selector cannot read it;
    /// the property is what remains, and only same-origin script can reach it.
    /// </remarks>
    public static ValueTask<string> GetNonce(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getNonce", element);
    /// <summary>
    /// The element's CSP nonce.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/nonce">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/nonce</see>
    /// </summary>
    public static ValueTask SetNonce(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setNonce", element, value);

    /// <summary>
    /// The tag name of the element the offset metrics are measured against - the nearest positioned
    /// ancestor. Empty when there is none, which is also what a <c>display:none</c> element reports.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetParent">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetParent</see>
    /// </summary>
    /// <remarks>
    /// The DOM property is the ancestor element itself; an <see cref="ElementReference"/> can only
    /// be minted by Blazor's renderer, so what crosses the boundary is its tag name.
    /// </remarks>
    public static ValueTask<string> GetOffsetParentTagName(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.offsetParentTagName", element);

    /// <summary>
    /// The element's rendered text, the way <c>InnerText</c> reports it.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/outerText">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/outerText</see>
    /// </summary>
    public static ValueTask<string> GetOuterText(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getOuterText", element);
    /// <summary>
    /// Replaces the element itself - not its contents - with the given text.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/outerText">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/outerText</see>
    /// </summary>
    /// <remarks>
    /// The element is gone afterwards and the reference is dangling. This is the asymmetry the DOM
    /// itself has: reading gives the text inside, writing removes the element.
    /// </remarks>
    public static ValueTask SetOuterText(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setOuterText", element, value);

    /// <summary>
    /// The shadow parts the element exposes to the outside - what a <c>::part()</c> selector can
    /// reach it by.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/part">https://developer.mozilla.org/en-US/docs/Web/API/Element/part</see>
    /// </summary>
    public static async ValueTask<string[]> GetPart(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<string[]>("BitButil.element.getPart", element);
    /// <summary>
    /// Sets the shadow parts the element exposes, as a space-separated list.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/part">https://developer.mozilla.org/en-US/docs/Web/API/Element/part</see>
    /// </summary>
    public static ValueTask SetPart(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setPart", element, value);

    /// <summary>
    /// What kind of popover the element is, if any.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/popover">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/popover</see>
    /// </summary>
    public static async ValueTask<ElementPopover> GetPopover(this ElementReference element)
    {
        var value = await ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getPopover", element);
        return value switch
        {
            "auto" => ElementPopover.Auto,
            "manual" => ElementPopover.Manual,
            "hint" => ElementPopover.Hint,
            _ => ElementPopover.NotSet,
        };
    }
    /// <summary>
    /// Makes the element a popover of the given kind, or - with
    /// <see cref="ElementPopover.NotSet"/> - stops it being one.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/popover">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/popover</see>
    /// </summary>
    public static async ValueTask SetPopover(this ElementReference element, ElementPopover value)
    {
        var v = value switch
        {
            ElementPopover.Auto => "auto",
            ElementPopover.Manual => "manual",
            ElementPopover.Hint => "hint",
            _ => null,
        };
        await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setPopover", element, v);
    }

    /// <summary>
    /// The element's namespace prefix - "svg" in <c>&lt;svg:rect&gt;</c>. Empty when it has none,
    /// which is the usual case in an HTML document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/prefix">https://developer.mozilla.org/en-US/docs/Web/API/Element/prefix</see>
    /// </summary>
    public static ValueTask<string> GetPrefix(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.prefix", element);

    /// <summary>
    /// The largest value <c>SetScrollLeft</c> will take - the element's scrollable width minus what
    /// fits.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollLeftMax">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollLeftMax</see>
    /// </summary>
    /// <remarks>
    /// A Firefox property. Everywhere else this computes the same figure from <c>scrollWidth</c> and
    /// <c>clientWidth</c>, which is how the property is defined.
    /// </remarks>
    public static async ValueTask<float> GetScrollLeftMax(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<float>("BitButil.element.scrollLeftMax", element);

    /// <summary>
    /// The largest value <c>SetScrollTop</c> will take - the element's scrollable height minus what
    /// fits.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollTopMax">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollTopMax</see>
    /// </summary>
    /// <remarks>
    /// A Firefox property. Everywhere else this computes the same figure from <c>scrollHeight</c>
    /// and <c>clientHeight</c>, which is how the property is defined.
    /// </remarks>
    public static async ValueTask<float> GetScrollTopMax(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<float>("BitButil.element.scrollTopMax", element);

    /// <summary>
    /// The name of the shadow-DOM slot the element asks to be placed in.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/slot">https://developer.mozilla.org/en-US/docs/Web/API/Element/slot</see>
    /// </summary>
    public static ValueTask<string> GetSlot(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getSlot", element);
    /// <summary>
    /// The name of the shadow-DOM slot the element asks to be placed in.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/slot">https://developer.mozilla.org/en-US/docs/Web/API/Element/slot</see>
    /// </summary>
    public static ValueTask SetSlot(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setSlot", element, value);

    /// <summary>
    /// Whether the browser should spell-check what the user types into the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/spellcheck">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/spellcheck</see>
    /// </summary>
    public static async ValueTask<bool> GetSpellcheck(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.getSpellcheck", element);
    /// <summary>
    /// Whether the browser should spell-check what the user types into the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/spellcheck">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/spellcheck</see>
    /// </summary>
    public static async ValueTask SetSpellcheck(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setSpellcheck", element, value);

    /// <summary>
    /// The element's advisory text - what a browser shows as a tooltip on hover.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/title">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/title</see>
    /// </summary>
    public static ValueTask<string> GetTitle(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getTitle", element);
    /// <summary>
    /// The element's advisory text - what a browser shows as a tooltip on hover.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/title">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/title</see>
    /// </summary>
    public static ValueTask SetTitle(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setTitle", element, value);

    /// <summary>
    /// Whether the element's text should be translated when the page is. True by default.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/translate">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/translate</see>
    /// </summary>
    public static async ValueTask<bool> GetTranslate(this ElementReference element)
        => await ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.element.getTranslate", element);
    /// <summary>
    /// Whether the element's text should be translated when the page is. Set it false for code,
    /// identifiers and proper nouns a machine translator would mangle.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/translate">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/translate</see>
    /// </summary>
    public static async ValueTask SetTranslate(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setTranslate", element, value);

    /// <summary>
    /// Who controls the on-screen keyboard for this editable element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/virtualKeyboardPolicy">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/virtualKeyboardPolicy</see>
    /// </summary>
    /// <remarks>Chromium only; reads as <see cref="VirtualKeyboardPolicy.NotSet"/> elsewhere.</remarks>
    public static async ValueTask<VirtualKeyboardPolicy> GetVirtualKeyboardPolicy(this ElementReference element)
    {
        var value = await ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getVirtualKeyboardPolicy", element);
        return value switch
        {
            "auto" => VirtualKeyboardPolicy.Auto,
            "manual" => VirtualKeyboardPolicy.Manual,
            _ => VirtualKeyboardPolicy.NotSet,
        };
    }
    /// <summary>
    /// Who controls the on-screen keyboard for this editable element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/virtualKeyboardPolicy">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/virtualKeyboardPolicy</see>
    /// </summary>
    public static async ValueTask SetVirtualKeyboardPolicy(this ElementReference element, VirtualKeyboardPolicy value)
    {
        var v = value switch
        {
            VirtualKeyboardPolicy.Auto => "auto",
            VirtualKeyboardPolicy.Manual => "manual",
            _ => "",
        };
        await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setVirtualKeyboardPolicy", element, v);
    }

    /// <summary>
    /// Whether the browser may offer inline writing suggestions inside the element. True unless the
    /// element - or an ancestor - turned them off.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/writingSuggestions">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/writingSuggestions</see>
    /// </summary>
    /// <remarks>
    /// The DOM property is the string "true" or "false"; this reports the boolean it stands for.
    /// Engines without the feature report true, which is what they behave as.
    /// </remarks>
    public static async ValueTask<bool> GetWritingSuggestions(this ElementReference element)
    {
        var value = await ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getWritingSuggestions", element);
        return value != "false";
    }
    /// <summary>
    /// Whether the browser may offer inline writing suggestions inside the element. Set it false for
    /// a field where an autocompleted phrase would be wrong - a password hint, a code editor.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/writingSuggestions">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/writingSuggestions</see>
    /// </summary>
    public static async ValueTask SetWritingSuggestions(this ElementReference element, bool value)
        => await ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setWritingSuggestions", element, value ? "true" : "false");
}

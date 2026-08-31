using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

/// <summary>
/// The ARIA reflection properties of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element">Element</see>: the
/// <c>role</c> and every <c>aria-*</c> attribute, read and written as properties rather than as
/// attribute strings.
/// </summary>
/// <remarks>
/// Prefer rendering these as attributes in your markup - Blazor passes <c>aria-label</c> and friends
/// straight through, it costs no interop, and a re-render cannot undo it. These are for the
/// attributes that have to change in response to something outside the render tree, and for reading
/// what another script or a component library put on an element.
/// <br/>
/// Every value is a string, including the numeric ones (<c>AriaLevel</c>, <c>AriaValueNow</c>) and
/// the boolean ones (<c>AriaExpanded</c>, <c>AriaHidden</c>) - that is how ARIA itself is defined,
/// and <c>"false"</c> and <c>""</c> mean different things to a screen reader. An attribute that is
/// not set reads as null.
/// <br/>
/// During prerender/SSR (no JS runtime) every read returns an empty string rather than throwing, so
/// it cannot be told apart from a genuinely empty one. Defer reads you branch on to
/// <c>OnAfterRenderAsync</c>.
/// </remarks>
public static class ElementReferenceAriaExtensions
{
    /// <summary>
    /// Whether assistive technology presents the whole changed region or only the part that changed. Reflects <c>aria-atomic</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAtomic">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAtomic</see>
    /// </summary>
    public static ValueTask<string> GetAriaAtomic(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaAtomic");
    /// <summary>
    /// Whether assistive technology presents the whole changed region or only the part that changed. Reflects <c>aria-atomic</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAtomic">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAtomic</see>
    /// </summary>
    public static ValueTask SetAriaAtomic(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaAtomic", value);

    /// <summary>
    /// What kind of completion an input offers: <c>inline</c>, <c>list</c>, <c>both</c> or <c>none</c>. Reflects <c>aria-autocomplete</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAutoComplete">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAutoComplete</see>
    /// </summary>
    public static ValueTask<string> GetAriaAutoComplete(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaAutoComplete");
    /// <summary>
    /// What kind of completion an input offers: <c>inline</c>, <c>list</c>, <c>both</c> or <c>none</c>. Reflects <c>aria-autocomplete</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAutoComplete">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaAutoComplete</see>
    /// </summary>
    public static ValueTask SetAriaAutoComplete(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaAutoComplete", value);

    /// <summary>
    /// The label a braille display shows in place of the accessible name. Reflects <c>aria-braillelabel</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleLabel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleLabel</see>
    /// </summary>
    public static ValueTask<string> GetAriaBrailleLabel(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaBrailleLabel");
    /// <summary>
    /// The label a braille display shows in place of the accessible name. Reflects <c>aria-braillelabel</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleLabel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleLabel</see>
    /// </summary>
    public static ValueTask SetAriaBrailleLabel(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaBrailleLabel", value);

    /// <summary>
    /// The role description a braille display shows in place of the spoken one. Reflects <c>aria-brailleroledescription</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleRoleDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleRoleDescription</see>
    /// </summary>
    public static ValueTask<string> GetAriaBrailleRoleDescription(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaBrailleRoleDescription");
    /// <summary>
    /// The role description a braille display shows in place of the spoken one. Reflects <c>aria-brailleroledescription</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleRoleDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBrailleRoleDescription</see>
    /// </summary>
    public static ValueTask SetAriaBrailleRoleDescription(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaBrailleRoleDescription", value);

    /// <summary>
    /// Whether the element is still being updated, so assistive technology waits before announcing it. Reflects <c>aria-busy</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBusy">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBusy</see>
    /// </summary>
    public static ValueTask<string> GetAriaBusy(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaBusy");
    /// <summary>
    /// Whether the element is still being updated, so assistive technology waits before announcing it. Reflects <c>aria-busy</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBusy">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaBusy</see>
    /// </summary>
    public static ValueTask SetAriaBusy(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaBusy", value);

    /// <summary>
    /// The checked state of a checkbox, radio or switch that is not a native input: <c>true</c>, <c>false</c> or <c>mixed</c>. Reflects <c>aria-checked</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaChecked">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaChecked</see>
    /// </summary>
    public static ValueTask<string> GetAriaChecked(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaChecked");
    /// <summary>
    /// The checked state of a checkbox, radio or switch that is not a native input: <c>true</c>, <c>false</c> or <c>mixed</c>. Reflects <c>aria-checked</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaChecked">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaChecked</see>
    /// </summary>
    public static ValueTask SetAriaChecked(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaChecked", value);

    /// <summary>
    /// How many columns the whole table has, when the DOM holds only some of them. Reflects <c>aria-colcount</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColCount">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColCount</see>
    /// </summary>
    public static ValueTask<string> GetAriaColCount(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaColCount");
    /// <summary>
    /// How many columns the whole table has, when the DOM holds only some of them. Reflects <c>aria-colcount</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColCount">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColCount</see>
    /// </summary>
    public static ValueTask SetAriaColCount(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaColCount", value);

    /// <summary>
    /// Which column of the whole table this cell sits in, counting from one. Reflects <c>aria-colindex</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndex">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndex</see>
    /// </summary>
    public static ValueTask<string> GetAriaColIndex(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaColIndex");
    /// <summary>
    /// Which column of the whole table this cell sits in, counting from one. Reflects <c>aria-colindex</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndex">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndex</see>
    /// </summary>
    public static ValueTask SetAriaColIndex(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaColIndex", value);

    /// <summary>
    /// A human-readable column label, announced instead of the column number. Reflects <c>aria-colindextext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndexText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndexText</see>
    /// </summary>
    public static ValueTask<string> GetAriaColIndexText(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaColIndexText");
    /// <summary>
    /// A human-readable column label, announced instead of the column number. Reflects <c>aria-colindextext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndexText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColIndexText</see>
    /// </summary>
    public static ValueTask SetAriaColIndexText(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaColIndexText", value);

    /// <summary>
    /// How many columns the cell spans, for a grid not built from table elements. Reflects <c>aria-colspan</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColSpan">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColSpan</see>
    /// </summary>
    public static ValueTask<string> GetAriaColSpan(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaColSpan");
    /// <summary>
    /// How many columns the cell spans, for a grid not built from table elements. Reflects <c>aria-colspan</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColSpan">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaColSpan</see>
    /// </summary>
    public static ValueTask SetAriaColSpan(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaColSpan", value);

    /// <summary>
    /// Which item of a set is the current one: <c>page</c>, <c>step</c>, <c>location</c>, <c>date</c>, <c>time</c> or <c>true</c>. Reflects <c>aria-current</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaCurrent">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaCurrent</see>
    /// </summary>
    public static ValueTask<string> GetAriaCurrent(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaCurrent");
    /// <summary>
    /// Which item of a set is the current one: <c>page</c>, <c>step</c>, <c>location</c>, <c>date</c>, <c>time</c> or <c>true</c>. Reflects <c>aria-current</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaCurrent">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaCurrent</see>
    /// </summary>
    public static ValueTask SetAriaCurrent(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaCurrent", value);

    /// <summary>
    /// A longer description of the element, announced after its name. Reflects <c>aria-description</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDescription</see>
    /// </summary>
    public static ValueTask<string> GetAriaDescription(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaDescription");
    /// <summary>
    /// A longer description of the element, announced after its name. Reflects <c>aria-description</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDescription</see>
    /// </summary>
    public static ValueTask SetAriaDescription(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaDescription", value);

    /// <summary>
    /// Whether the element is perceivable but not operable. Unlike the <c>disabled</c> attribute it stays focusable. Reflects <c>aria-disabled</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDisabled">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDisabled</see>
    /// </summary>
    public static ValueTask<string> GetAriaDisabled(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaDisabled");
    /// <summary>
    /// Whether the element is perceivable but not operable. Unlike the <c>disabled</c> attribute it stays focusable. Reflects <c>aria-disabled</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDisabled">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaDisabled</see>
    /// </summary>
    public static ValueTask SetAriaDisabled(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaDisabled", value);

    /// <summary>
    /// Whether the thing this element controls is expanded or collapsed. Reflects <c>aria-expanded</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaExpanded">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaExpanded</see>
    /// </summary>
    public static ValueTask<string> GetAriaExpanded(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaExpanded");
    /// <summary>
    /// Whether the thing this element controls is expanded or collapsed. Reflects <c>aria-expanded</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaExpanded">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaExpanded</see>
    /// </summary>
    public static ValueTask SetAriaExpanded(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaExpanded", value);

    /// <summary>
    /// What kind of popup the element opens: <c>menu</c>, <c>listbox</c>, <c>tree</c>, <c>grid</c> or <c>dialog</c>. Reflects <c>aria-haspopup</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHasPopup">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHasPopup</see>
    /// </summary>
    public static ValueTask<string> GetAriaHasPopup(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaHasPopup");
    /// <summary>
    /// What kind of popup the element opens: <c>menu</c>, <c>listbox</c>, <c>tree</c>, <c>grid</c> or <c>dialog</c>. Reflects <c>aria-haspopup</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHasPopup">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHasPopup</see>
    /// </summary>
    public static ValueTask SetAriaHasPopup(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaHasPopup", value);

    /// <summary>
    /// Whether the element and its subtree are hidden from the accessibility tree while staying visible on screen. Reflects <c>aria-hidden</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHidden">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHidden</see>
    /// </summary>
    public static ValueTask<string> GetAriaHidden(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaHidden");
    /// <summary>
    /// Whether the element and its subtree are hidden from the accessibility tree while staying visible on screen. Reflects <c>aria-hidden</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHidden">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaHidden</see>
    /// </summary>
    public static ValueTask SetAriaHidden(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaHidden", value);

    /// <summary>
    /// Whether the entered value is rejected, and why: <c>true</c>, <c>grammar</c> or <c>spelling</c>. Reflects <c>aria-invalid</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaInvalid">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaInvalid</see>
    /// </summary>
    public static ValueTask<string> GetAriaInvalid(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaInvalid");
    /// <summary>
    /// Whether the entered value is rejected, and why: <c>true</c>, <c>grammar</c> or <c>spelling</c>. Reflects <c>aria-invalid</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaInvalid">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaInvalid</see>
    /// </summary>
    public static ValueTask SetAriaInvalid(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaInvalid", value);

    /// <summary>
    /// The keyboard shortcuts that activate the element, as a space-separated list. Reflects <c>aria-keyshortcuts</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaKeyShortcuts">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaKeyShortcuts</see>
    /// </summary>
    public static ValueTask<string> GetAriaKeyShortcuts(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaKeyShortcuts");
    /// <summary>
    /// The keyboard shortcuts that activate the element, as a space-separated list. Reflects <c>aria-keyshortcuts</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaKeyShortcuts">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaKeyShortcuts</see>
    /// </summary>
    public static ValueTask SetAriaKeyShortcuts(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaKeyShortcuts", value);

    /// <summary>
    /// The element's accessible name, for when no visible text supplies one. Reflects <c>aria-label</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLabel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLabel</see>
    /// </summary>
    public static ValueTask<string> GetAriaLabel(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaLabel");
    /// <summary>
    /// The element's accessible name, for when no visible text supplies one. Reflects <c>aria-label</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLabel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLabel</see>
    /// </summary>
    public static ValueTask SetAriaLabel(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaLabel", value);

    /// <summary>
    /// The element's level in a hierarchy - a heading's rank, a tree item's depth. Reflects <c>aria-level</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLevel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLevel</see>
    /// </summary>
    public static ValueTask<string> GetAriaLevel(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaLevel");
    /// <summary>
    /// The element's level in a hierarchy - a heading's rank, a tree item's depth. Reflects <c>aria-level</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLevel">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLevel</see>
    /// </summary>
    public static ValueTask SetAriaLevel(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaLevel", value);

    /// <summary>
    /// How urgently updates to this region are announced: <c>off</c>, <c>polite</c> or <c>assertive</c>. Reflects <c>aria-live</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLive">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLive</see>
    /// </summary>
    public static ValueTask<string> GetAriaLive(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaLive");
    /// <summary>
    /// How urgently updates to this region are announced: <c>off</c>, <c>polite</c> or <c>assertive</c>. Reflects <c>aria-live</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLive">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaLive</see>
    /// </summary>
    public static ValueTask SetAriaLive(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaLive", value);

    /// <summary>
    /// Whether a dialog is modal, so assistive technology confines itself to its contents. Reflects <c>aria-modal</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaModal">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaModal</see>
    /// </summary>
    public static ValueTask<string> GetAriaModal(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaModal");
    /// <summary>
    /// Whether a dialog is modal, so assistive technology confines itself to its contents. Reflects <c>aria-modal</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaModal">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaModal</see>
    /// </summary>
    public static ValueTask SetAriaModal(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaModal", value);

    /// <summary>
    /// Whether a textbox takes more than one line, so Enter inserts a newline rather than submitting. Reflects <c>aria-multiline</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiline">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiline</see>
    /// </summary>
    public static ValueTask<string> GetAriaMultiline(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaMultiline");
    /// <summary>
    /// Whether a textbox takes more than one line, so Enter inserts a newline rather than submitting. Reflects <c>aria-multiline</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiline">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiline</see>
    /// </summary>
    public static ValueTask SetAriaMultiline(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaMultiline", value);

    /// <summary>
    /// Whether more than one item of the list, grid or tree can be selected at once. Reflects <c>aria-multiselectable</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiSelectable">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiSelectable</see>
    /// </summary>
    public static ValueTask<string> GetAriaMultiSelectable(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaMultiSelectable");
    /// <summary>
    /// Whether more than one item of the list, grid or tree can be selected at once. Reflects <c>aria-multiselectable</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiSelectable">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaMultiSelectable</see>
    /// </summary>
    public static ValueTask SetAriaMultiSelectable(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaMultiSelectable", value);

    /// <summary>
    /// Whether the element is laid out <c>horizontal</c>ly or <c>vertical</c>ly. Reflects <c>aria-orientation</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaOrientation">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaOrientation</see>
    /// </summary>
    public static ValueTask<string> GetAriaOrientation(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaOrientation");
    /// <summary>
    /// Whether the element is laid out <c>horizontal</c>ly or <c>vertical</c>ly. Reflects <c>aria-orientation</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaOrientation">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaOrientation</see>
    /// </summary>
    public static ValueTask SetAriaOrientation(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaOrientation", value);

    /// <summary>
    /// The hint shown in an empty input, for controls with no native placeholder. Reflects <c>aria-placeholder</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPlaceholder">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPlaceholder</see>
    /// </summary>
    public static ValueTask<string> GetAriaPlaceholder(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaPlaceholder");
    /// <summary>
    /// The hint shown in an empty input, for controls with no native placeholder. Reflects <c>aria-placeholder</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPlaceholder">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPlaceholder</see>
    /// </summary>
    public static ValueTask SetAriaPlaceholder(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaPlaceholder", value);

    /// <summary>
    /// Which position this item holds in its set, counting from one - for a list the DOM holds only part of. Reflects <c>aria-posinset</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPosInSet">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPosInSet</see>
    /// </summary>
    public static ValueTask<string> GetAriaPosInSet(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaPosInSet");
    /// <summary>
    /// Which position this item holds in its set, counting from one - for a list the DOM holds only part of. Reflects <c>aria-posinset</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPosInSet">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPosInSet</see>
    /// </summary>
    public static ValueTask SetAriaPosInSet(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaPosInSet", value);

    /// <summary>
    /// The pressed state of a toggle button: <c>true</c>, <c>false</c> or <c>mixed</c>. Reflects <c>aria-pressed</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPressed">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPressed</see>
    /// </summary>
    public static ValueTask<string> GetAriaPressed(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaPressed");
    /// <summary>
    /// The pressed state of a toggle button: <c>true</c>, <c>false</c> or <c>mixed</c>. Reflects <c>aria-pressed</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPressed">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaPressed</see>
    /// </summary>
    public static ValueTask SetAriaPressed(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaPressed", value);

    /// <summary>
    /// Whether the value can be read but not changed. Reflects <c>aria-readonly</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaReadOnly">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaReadOnly</see>
    /// </summary>
    public static ValueTask<string> GetAriaReadOnly(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaReadOnly");
    /// <summary>
    /// Whether the value can be read but not changed. Reflects <c>aria-readonly</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaReadOnly">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaReadOnly</see>
    /// </summary>
    public static ValueTask SetAriaReadOnly(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaReadOnly", value);

    /// <summary>
    /// Which changes in a live region are worth announcing: <c>additions</c>, <c>removals</c>, <c>text</c> or <c>all</c>. Reflects <c>aria-relevant</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRelevant">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRelevant</see>
    /// </summary>
    public static ValueTask<string> GetAriaRelevant(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRelevant");
    /// <summary>
    /// Which changes in a live region are worth announcing: <c>additions</c>, <c>removals</c>, <c>text</c> or <c>all</c>. Reflects <c>aria-relevant</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRelevant">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRelevant</see>
    /// </summary>
    public static ValueTask SetAriaRelevant(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRelevant", value);

    /// <summary>
    /// Whether a value must be supplied before the form can be submitted. Reflects <c>aria-required</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRequired">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRequired</see>
    /// </summary>
    public static ValueTask<string> GetAriaRequired(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRequired");
    /// <summary>
    /// Whether a value must be supplied before the form can be submitted. Reflects <c>aria-required</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRequired">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRequired</see>
    /// </summary>
    public static ValueTask SetAriaRequired(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRequired", value);

    /// <summary>
    /// A human-readable name for the element's role, announced instead of the standard one. Reflects <c>aria-roledescription</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRoleDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRoleDescription</see>
    /// </summary>
    public static ValueTask<string> GetAriaRoleDescription(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRoleDescription");
    /// <summary>
    /// A human-readable name for the element's role, announced instead of the standard one. Reflects <c>aria-roledescription</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRoleDescription">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRoleDescription</see>
    /// </summary>
    public static ValueTask SetAriaRoleDescription(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRoleDescription", value);

    /// <summary>
    /// How many rows the whole table has, when the DOM holds only some of them. Reflects <c>aria-rowcount</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowCount">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowCount</see>
    /// </summary>
    public static ValueTask<string> GetAriaRowCount(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRowCount");
    /// <summary>
    /// How many rows the whole table has, when the DOM holds only some of them. Reflects <c>aria-rowcount</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowCount">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowCount</see>
    /// </summary>
    public static ValueTask SetAriaRowCount(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRowCount", value);

    /// <summary>
    /// Which row of the whole table this row or cell sits in, counting from one. Reflects <c>aria-rowindex</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndex">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndex</see>
    /// </summary>
    public static ValueTask<string> GetAriaRowIndex(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRowIndex");
    /// <summary>
    /// Which row of the whole table this row or cell sits in, counting from one. Reflects <c>aria-rowindex</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndex">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndex</see>
    /// </summary>
    public static ValueTask SetAriaRowIndex(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRowIndex", value);

    /// <summary>
    /// A human-readable row label, announced instead of the row number. Reflects <c>aria-rowindextext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndexText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndexText</see>
    /// </summary>
    public static ValueTask<string> GetAriaRowIndexText(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRowIndexText");
    /// <summary>
    /// A human-readable row label, announced instead of the row number. Reflects <c>aria-rowindextext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndexText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowIndexText</see>
    /// </summary>
    public static ValueTask SetAriaRowIndexText(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRowIndexText", value);

    /// <summary>
    /// How many rows the cell spans, for a grid not built from table elements. Reflects <c>aria-rowspan</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowSpan">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowSpan</see>
    /// </summary>
    public static ValueTask<string> GetAriaRowSpan(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaRowSpan");
    /// <summary>
    /// How many rows the cell spans, for a grid not built from table elements. Reflects <c>aria-rowspan</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowSpan">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaRowSpan</see>
    /// </summary>
    public static ValueTask SetAriaRowSpan(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaRowSpan", value);

    /// <summary>
    /// Whether the item is selected - for options, tabs, rows and grid cells. Reflects <c>aria-selected</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSelected">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSelected</see>
    /// </summary>
    public static ValueTask<string> GetAriaSelected(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaSelected");
    /// <summary>
    /// Whether the item is selected - for options, tabs, rows and grid cells. Reflects <c>aria-selected</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSelected">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSelected</see>
    /// </summary>
    public static ValueTask SetAriaSelected(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaSelected", value);

    /// <summary>
    /// How many items the whole set holds, when the DOM holds only some of them. Reflects <c>aria-setsize</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSetSize">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSetSize</see>
    /// </summary>
    public static ValueTask<string> GetAriaSetSize(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaSetSize");
    /// <summary>
    /// How many items the whole set holds, when the DOM holds only some of them. Reflects <c>aria-setsize</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSetSize">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSetSize</see>
    /// </summary>
    public static ValueTask SetAriaSetSize(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaSetSize", value);

    /// <summary>
    /// How a column or row is sorted: <c>ascending</c>, <c>descending</c>, <c>other</c> or <c>none</c>. Reflects <c>aria-sort</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSort">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSort</see>
    /// </summary>
    public static ValueTask<string> GetAriaSort(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaSort");
    /// <summary>
    /// How a column or row is sorted: <c>ascending</c>, <c>descending</c>, <c>other</c> or <c>none</c>. Reflects <c>aria-sort</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSort">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaSort</see>
    /// </summary>
    public static ValueTask SetAriaSort(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaSort", value);

    /// <summary>
    /// The largest value a range widget accepts. Reflects <c>aria-valuemax</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMax">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMax</see>
    /// </summary>
    public static ValueTask<string> GetAriaValueMax(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaValueMax");
    /// <summary>
    /// The largest value a range widget accepts. Reflects <c>aria-valuemax</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMax">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMax</see>
    /// </summary>
    public static ValueTask SetAriaValueMax(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaValueMax", value);

    /// <summary>
    /// The smallest value a range widget accepts. Reflects <c>aria-valuemin</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMin">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMin</see>
    /// </summary>
    public static ValueTask<string> GetAriaValueMin(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaValueMin");
    /// <summary>
    /// The smallest value a range widget accepts. Reflects <c>aria-valuemin</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMin">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueMin</see>
    /// </summary>
    public static ValueTask SetAriaValueMin(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaValueMin", value);

    /// <summary>
    /// The current value of a range widget. Reflects <c>aria-valuenow</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueNow">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueNow</see>
    /// </summary>
    public static ValueTask<string> GetAriaValueNow(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaValueNow");
    /// <summary>
    /// The current value of a range widget. Reflects <c>aria-valuenow</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueNow">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueNow</see>
    /// </summary>
    public static ValueTask SetAriaValueNow(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaValueNow", value);

    /// <summary>
    /// A human-readable rendering of the current value, announced instead of the number. Reflects <c>aria-valuetext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueText</see>
    /// </summary>
    public static ValueTask<string> GetAriaValueText(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "ariaValueText");
    /// <summary>
    /// A human-readable rendering of the current value, announced instead of the number. Reflects <c>aria-valuetext</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueText">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaValueText</see>
    /// </summary>
    public static ValueTask SetAriaValueText(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "ariaValueText", value);

    /// <summary>
    /// The element's ARIA role - what it is to assistive technology, for when the tag alone does not say. Reflects <c>role</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/role">https://developer.mozilla.org/en-US/docs/Web/API/Element/role</see>
    /// </summary>
    public static ValueTask<string> GetRole(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.element.getAria", element, "role");
    /// <summary>
    /// The element's ARIA role - what it is to assistive technology, for when the tag alone does not say. Reflects <c>role</c>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/role">https://developer.mozilla.org/en-US/docs/Web/API/Element/role</see>
    /// </summary>
    public static ValueTask SetRole(this ElementReference element, string value)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.setAria", element, "role", value);

    /// <summary>
    /// Announces <paramref name="message"/> to assistive technology without changing the page - the
    /// direct alternative to mutating a live region so that a screen reader reads it out.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaNotify">https://developer.mozilla.org/en-US/docs/Web/API/Element/ariaNotify</see>
    /// </summary>
    /// <remarks>
    /// Experimental and Chromium-only. A no-op everywhere else rather than a throw: an announcement
    /// that does not happen is not a failure of the page that asked for it, so a caller does not
    /// have to feature-detect. Keep the visible UI telling the same story, since most users will
    /// never receive this.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AriaNotifyJsOptions))]
    public static ValueTask AriaNotify(this ElementReference element, string message, AriaNotifyOptions? options = null)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.element.ariaNotify", element, message, options?.ToJsObject());
}

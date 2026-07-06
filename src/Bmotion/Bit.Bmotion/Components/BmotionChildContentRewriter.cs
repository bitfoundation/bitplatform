using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

// The RenderTree types are public-but-framework-internal (BL0006): Blazor documents them as
// subject to change between releases. They are used deliberately here - rewriting the child
// content's frames is the only way to inject attributes into consumer-authored markup at render
// time (Blazor has no React-style cloneElement), which is what keeps the Bmotion API free of an
// explicit @attributes splat while remaining FOUC-free and prerender-safe. Revisit this file when
// bumping the supported TFMs.
#pragma warning disable BL0006

namespace Bit.Bmotion;

/// <summary>
/// The attributes a <see cref="Bmotion"/> injects into the animated element.
/// <see cref="MotionStyle"/> is the motion-only initial style; the rewriter places it before any
/// consumer-authored declarations so the consumer's win conflicts.
/// </summary>
internal readonly record struct BmotionInjection(string Id, string? MotionStyle, bool AddPathLength);

/// <summary>
/// Decides what to inject into the first root element of the child content, given the element's
/// tag name and any consumer-authored <c>id</c> / <c>style</c> attribute values found on it.
/// </summary>
internal delegate BmotionInjection BmotionInjectionPlanner(string tagName, string? authorId, string? authorStyle);

/// <summary>
/// Replays a <see cref="RenderFragment"/>'s render-tree frames into another builder, injecting the
/// engine attributes (id, initial style, pathLength) into the first root HTML element found -
/// the Blazor equivalent of React's <c>cloneElement</c>. Everything else (keys, event handlers,
/// reference captures, nested components) is replayed verbatim.
/// </summary>
internal static class BmotionChildContentRewriter
{
    /// <summary>
    /// Builds <paramref name="content"/> into a scratch builder and replays it into
    /// <paramref name="output"/> with the injection applied. Returns <c>false</c> when no root
    /// HTML element exists to inject into (content that is empty, text-only, or whose roots are
    /// all components).
    /// </summary>
    public static bool Render(RenderTreeBuilder output, RenderFragment content, BmotionInjectionPlanner plan)
    {
        using var scratch = new RenderTreeBuilder();
        content(scratch);
        var frames = scratch.GetFrames();
        var injected = false;
        Replay(output, frames.Array, 0, frames.Count, parentIsComponent: false, canInject: true, ref injected, plan);
        return injected;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Component frames only exist because the consumer's razor-compiled child " +
                        "content already called OpenComponent<T> with the same type, which roots " +
                        "its members for the trimmer; this replay adds no new type requirements.")]
    private static void Replay(RenderTreeBuilder o, RenderTreeFrame[] frames, int start, int end,
        bool parentIsComponent, bool canInject, ref bool injected, BmotionInjectionPlanner plan)
    {
        var i = start;
        while (i < end)
        {
            ref var f = ref frames[i];
            switch (f.FrameType)
            {
                case RenderTreeFrameType.Element:
                {
                    var subtreeEnd = i + f.ElementSubtreeLength;
                    o.OpenElement(f.Sequence, f.ElementName);
                    if (f.ElementKey is not null) o.SetKey(f.ElementKey);
                    var contentStart = i + 1;
                    if (canInject && !injected)
                    {
                        injected = true;
                        contentStart = ReplayTargetAttributes(o, frames, contentStart, subtreeEnd, in f, plan);
                    }
                    // canInject: false - only the first element at (region-transparent) root level
                    // is the animation target; its descendants replay untouched.
                    Replay(o, frames, contentStart, subtreeEnd, parentIsComponent: false, canInject: false, ref injected, plan);
                    o.CloseElement();
                    i = subtreeEnd;
                    break;
                }
                case RenderTreeFrameType.Component:
                {
                    // Components render their own subtree elsewhere; never descend into one to
                    // inject - its attribute frames are parameters, replayed verbatim below.
                    var subtreeEnd = i + f.ComponentSubtreeLength;
                    o.OpenComponent(f.Sequence, f.ComponentType);
                    if (f.ComponentKey is not null) o.SetKey(f.ComponentKey);
                    Replay(o, frames, i + 1, subtreeEnd, parentIsComponent: true, canInject: false, ref injected, plan);
                    o.CloseComponent();
                    i = subtreeEnd;
                    break;
                }
                case RenderTreeFrameType.Region:
                {
                    // Regions are transparent groupings (@if / @foreach / template invocations);
                    // an element inside a root-level region is still a root element.
                    var subtreeEnd = i + f.RegionSubtreeLength;
                    o.OpenRegion(f.Sequence);
                    Replay(o, frames, i + 1, subtreeEnd, parentIsComponent: false, canInject, ref injected, plan);
                    o.CloseRegion();
                    i = subtreeEnd;
                    break;
                }
                case RenderTreeFrameType.Attribute:
                    // The object overloads faithfully round-trip every value shape a frame can
                    // hold (string, boxed bool, delegates, boxed EventCallback/EventCallback<T>) -
                    // the same contract @attributes splatting relies on.
                    if (parentIsComponent) o.AddComponentParameter(f.Sequence, f.AttributeName, f.AttributeValue);
                    else o.AddAttribute(f.Sequence, f.AttributeName, f.AttributeValue);
                    i++;
                    break;
                case RenderTreeFrameType.Text:
                    o.AddContent(f.Sequence, f.TextContent);
                    i++;
                    break;
                case RenderTreeFrameType.Markup:
                    // Razor collapses fully-static markup (no dynamic attributes/content) into a
                    // single raw-HTML frame - the common case for `<div class="box" />` child
                    // content - so the target element may only exist as text inside it. Inject by
                    // rewriting the first opening tag in the string.
                    if (canInject && !injected &&
                        TryInjectIntoMarkup(f.MarkupContent, plan) is string rewrittenMarkup)
                    {
                        injected = true;
                        o.AddMarkupContent(f.Sequence, rewrittenMarkup);
                    }
                    else
                    {
                        o.AddMarkupContent(f.Sequence, f.MarkupContent);
                    }
                    i++;
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    o.AddElementReferenceCapture(f.Sequence, f.ElementReferenceCaptureAction);
                    i++;
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                    o.AddComponentReferenceCapture(f.Sequence, f.ComponentReferenceCaptureAction);
                    i++;
                    break;
                case RenderTreeFrameType.NamedEvent:
                    o.AddNamedEvent(f.NamedEventType, f.NamedEventAssignedName);
                    i++;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Bmotion cannot replay a render-tree frame of type '{f.FrameType}'.");
            }
        }
    }

    /// <summary>
    /// Replays the target element's attribute run, replacing <c>id</c>/<c>style</c> with the
    /// injected values (the consumer's style declarations are merged after the initial motion
    /// style by the planner, so they win conflicts). Returns the index of the first
    /// non-attribute frame, where the caller resumes (reference captures, then content).
    /// </summary>
    private static int ReplayTargetAttributes(RenderTreeBuilder o, RenderTreeFrame[] frames, int start,
        int subtreeEnd, in RenderTreeFrame element, BmotionInjectionPlanner plan)
    {
        var attrEnd = start;
        string? authorId = null, authorStyle = null;
        var authorPathLength = false;
        while (attrEnd < subtreeEnd && frames[attrEnd].FrameType == RenderTreeFrameType.Attribute)
        {
            var name = frames[attrEnd].AttributeName;
            if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
                authorId = frames[attrEnd].AttributeValue as string;
            else if (string.Equals(name, "style", StringComparison.OrdinalIgnoreCase))
                authorStyle = frames[attrEnd].AttributeValue as string;
            else if (string.Equals(name, "pathLength", StringComparison.OrdinalIgnoreCase))
                authorPathLength = true;
            attrEnd++;
        }

        var injection = plan(element.ElementName, authorId, authorStyle);

        for (var k = start; k < attrEnd; k++)
        {
            ref var attr = ref frames[k];
            if (string.Equals(attr.AttributeName, "id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attr.AttributeName, "style", StringComparison.OrdinalIgnoreCase))
                continue; // replaced by the injected values below
            o.AddAttribute(attr.Sequence, attr.AttributeName, attr.AttributeValue);
        }

        // Attribute diffing is name-based, so reusing the element's sequence number is safe.
        o.AddAttribute(element.Sequence, "id", injection.Id);
        var mergedStyle = MergeStyles(injection.MotionStyle, authorStyle);
        if (!string.IsNullOrEmpty(mergedStyle))
            o.AddAttribute(element.Sequence, "style", mergedStyle);
        if (injection.AddPathLength && !authorPathLength)
            o.AddAttribute(element.Sequence, "pathLength", "1");

        return attrEnd;
    }

    /// <summary>Motion style first, consumer declarations after, so the consumer's win conflicts.</summary>
    private static string? MergeStyles(string? motionStyle, string? authorStyle)
        => string.IsNullOrEmpty(motionStyle) ? authorStyle
         : string.IsNullOrEmpty(authorStyle) ? motionStyle
         : motionStyle + authorStyle;

    // ── Static-markup injection ─────────────────────────────────────────────────

    /// <summary>
    /// Rewrites the first opening tag inside a raw-HTML markup string with the injected
    /// attributes. Returns <c>null</c> when the markup contains no element (text/whitespace/
    /// comments only), in which case the caller keeps searching subsequent frames.
    /// The markup is compiler-generated (well-formed, quoted attributes), so a lightweight
    /// scanner is sufficient - this is not a general HTML parser.
    /// </summary>
    private static string? TryInjectIntoMarkup(string markup, BmotionInjectionPlanner plan)
    {
        // Find the first '<' that starts an element, skipping comments/doctype ("<!").
        var i = 0;
        while (true)
        {
            i = markup.IndexOf('<', i);
            if (i < 0 || i + 1 >= markup.Length) return null;
            if (markup[i + 1] == '!')
            {
                var close = markup.IndexOf('>', i);
                if (close < 0) return null;
                i = close + 1;
                continue;
            }
            if (char.IsAsciiLetter(markup[i + 1])) break;
            i++;
        }

        var nameStart = i + 1;
        var nameEnd = nameStart;
        while (nameEnd < markup.Length &&
               (char.IsAsciiLetterOrDigit(markup[nameEnd]) || markup[nameEnd] is '-' or ':'))
            nameEnd++;
        var tagName = markup[nameStart..nameEnd];

        // Find the end of the opening tag, honoring quoted attribute values.
        var p = nameEnd;
        var quote = '\0';
        var tagEnd = -1;
        while (p < markup.Length)
        {
            var c = markup[p];
            if (quote != '\0') { if (c == quote) quote = '\0'; }
            else if (c is '"' or '\'') quote = c;
            else if (c == '>') { tagEnd = p; break; }
            p++;
        }
        if (tagEnd < 0) return null;

        var selfClosing = markup[tagEnd - 1] == '/';
        var attrRegionEnd = selfClosing ? tagEnd - 1 : tagEnd;

        // Scan the attribute region for id / style / pathLength; record their spans for removal.
        string? authorId = null, authorStyle = null;
        var authorPathLength = false;
        var removals = new List<(int Start, int End)>(2);
        var a = nameEnd;
        while (a < attrRegionEnd)
        {
            if (char.IsWhiteSpace(markup[a])) { a++; continue; }
            var attrStart = a;
            var eq = a;
            while (eq < attrRegionEnd && markup[eq] != '=' && !char.IsWhiteSpace(markup[eq])) eq++;
            var attrName = markup[a..eq];
            var valueStart = -1;
            var valueEnd = -1;
            a = eq;
            while (a < attrRegionEnd && char.IsWhiteSpace(markup[a])) a++;
            if (a < attrRegionEnd && markup[a] == '=')
            {
                a++;
                while (a < attrRegionEnd && char.IsWhiteSpace(markup[a])) a++;
                if (a < attrRegionEnd && markup[a] is '"' or '\'')
                {
                    var q = markup[a];
                    valueStart = ++a;
                    while (a < attrRegionEnd && markup[a] != q) a++;
                    valueEnd = a;
                    if (a < attrRegionEnd) a++; // past closing quote
                }
                else
                {
                    valueStart = a;
                    while (a < attrRegionEnd && !char.IsWhiteSpace(markup[a])) a++;
                    valueEnd = a;
                }
            }

            if (string.Equals(attrName, "id", StringComparison.OrdinalIgnoreCase))
            {
                authorId = valueStart >= 0 ? markup[valueStart..valueEnd] : null;
                removals.Add((attrStart, a));
            }
            else if (string.Equals(attrName, "style", StringComparison.OrdinalIgnoreCase))
            {
                authorStyle = valueStart >= 0 ? markup[valueStart..valueEnd] : null;
                removals.Add((attrStart, a));
            }
            else if (string.Equals(attrName, "pathLength", StringComparison.OrdinalIgnoreCase))
            {
                authorPathLength = true;
            }
        }

        var injection = plan(tagName, authorId, authorStyle);

        var sb = new System.Text.StringBuilder(markup.Length + 96);
        // Opening tag minus the removed id/style attributes.
        var cursor = i;
        foreach (var (rs, re) in removals)
        {
            sb.Append(markup, cursor, rs - cursor);
            cursor = re;
        }
        sb.Append(markup, cursor, attrRegionEnd - cursor);

        // The id can be the consumer-supplied Id parameter - encode it so quotes or other special
        // characters cannot break out of the attribute.
        sb.Append(" id=\"").Append(HtmlAttributeEncode(injection.Id)).Append('"');
        // The author's style text is already HTML-encoded (it came from this markup string);
        // encode only the motion part so the merged value stays a valid attribute.
        var motion = injection.MotionStyle is { Length: > 0 } ms ? HtmlAttributeEncode(ms) : null;
        var mergedStyle = MergeStyles(motion, authorStyle);
        if (!string.IsNullOrEmpty(mergedStyle))
            sb.Append(" style=\"").Append(mergedStyle).Append('"');
        if (injection.AddPathLength && !authorPathLength)
            sb.Append(" pathLength=\"1\"");

        if (selfClosing) sb.Append(" /");
        sb.Append(markup, tagEnd, markup.Length - tagEnd);

        return sb.Insert(0, markup[..i]).ToString();
    }

    private static string HtmlAttributeEncode(string value)
        => value.Contains('&') || value.Contains('"') || value.Contains('<')
            ? value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;")
            : value;
}

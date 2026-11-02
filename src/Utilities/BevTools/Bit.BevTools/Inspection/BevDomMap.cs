using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BevTools;

/// <summary>
/// One step through Blazor's logical DOM: the position among the logical children of the node above, and what is there
/// - an element's tag name, <c>#text</c>, or <c>#c</c> for the comment Blazor inserts as the container of a component
/// or of a markup block. The kind is what the browser verifies each step against.
/// </summary>
public readonly record struct BevDomStep(int Index, string Kind);

/// <summary>
/// Where a component's output is in the page, as a path from the container of the interactive root component it
/// belongs to. <see cref="Anchored"/> roots are found from the BevTools panel, which renders inside them; any other root
/// by <see cref="RootSignature"/> (the kinds of its first logical children) and, among the <see cref="RootCount"/>
/// identical roots of this runtime, by <see cref="RootOrdinal"/>; the page hub finds it by position only when the page
/// holds exactly that many such roots, and otherwise only when the path fits a single one of them.
/// </summary>
public sealed record BevDomLocation(long InstanceId, string Name, bool Anchored, string RootSignature, int RootOrdinal, int RootCount, bool RootAmbiguous, IReadOnlyList<BevDomStep> Path);

/// <summary>What owns a node of the page: the component that rendered it, the element it is, and how sure that is.</summary>
/// <param name="Record">The component reported: the one that rendered the node, or its nearest visible ancestor.</param>
/// <param name="InMarkup">The node was parsed from a markup block (a <c>MarkupString</c>, static HTML in a component) rather than rendered frame by frame.</param>
/// <param name="Ambiguous">Several roots render identical structure down to the node; the one in the same position in the page was chosen.</param>
/// <param name="Depth">How many steps of the requested path lead to the owner's container; -1 when the owner is the root component itself, whose container is the root's.</param>
/// <param name="RenderedBy">The framework or library component that actually rendered the node, when the owner reported is its nearest visible ancestor.</param>
/// <param name="Element">The element the path ends on ("&lt;button.btn&gt;"), when it ends on one.</param>
public sealed record BevDomOwner(BevComponentRecord Record, int Depth, string? RenderedBy, string? Element, bool InMarkup, bool Ambiguous);

/// <summary>
/// What locating many components in one pass can share: each parent's child positions, the anchor root and the roots'
/// signatures. Valid only until a component renders, so it lives for one synchronous pass on the dispatcher (one tick
/// of render flashing), never longer.
/// </summary>
internal sealed class BevDomMapBatch
{
    internal Dictionary<ComponentState, Dictionary<int, BevDomStep[]>> Children { get; } = new(ReferenceEqualityComparer.Instance);

    internal Dictionary<ComponentState, string> Signatures { get; } = new(ReferenceEqualityComparer.Instance);

    internal bool AnchorKnown { get; set; }

    internal ComponentState? Anchor { get; set; }

    internal List<ComponentState>? Roots { get; set; }
}

/// <summary>
/// Maps components to the page and back, through the logical element tree Blazor's browser renderer keeps.
/// <para>
/// Every frame of a component's latest render becomes exactly one logical child in the browser (an element, a text
/// node, or the comment that contains a child component or a markup block); regions are flattened into their parent;
/// attributes, reference captures and named events produce none. So the position of a component is a path of logical
/// indices read from its ancestors' render trees - nothing is inferred - and the browser checks every step against the
/// node it finds, reporting a mismatch (a render not applied yet, the DOM changed by script) instead of guessing.
/// </para>
/// Callers run on the renderer's dispatcher: the render trees are read in place.
/// </summary>
internal static class BevDomMap
{
    private const int SignatureKinds = 12;
    private const int MaxDepth = 10_000;

    public static bool IsAvailable => BlazorReflection.CanReadRenderTree && BlazorReflection.CanResolveHierarchy;

    /// <summary>Where <paramref name="record"/> is in the page, or why that cannot be said.</summary>
    public static BevDomLocation? Locate(BevToolsSession session, BevComponentRecord record, out string? error, BevDomMapBatch? batch = null)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            return LocateCore(session, record, batch, out error);
        }
        finally
        {
            session.Overhead.AddPageMapping(System.Diagnostics.Stopwatch.GetTimestamp() - start);
        }
    }

    private static BevDomLocation? LocateCore(BevToolsSession session, BevComponentRecord record, BevDomMapBatch? batch, out string? error)
    {
        error = null;
        if (!IsAvailable)
        {
            error = "The renderer's render trees cannot be read on this framework version.";
            return null;
        }

        if (record.IsDisposed || record.Component is not { } component || (record.Renderer ?? session.Components.Renderer) is not { } renderer)
        {
            error = record.IsDisposed ? "The component was disposed." : "The component is not attached to a renderer.";
            return null;
        }

        if (BlazorReflection.ResolveState(renderer, component) is not { } state)
        {
            error = "The renderer no longer knows this component.";
            return null;
        }

        var chain = Ancestry(state);
        var steps = new List<BevDomStep>();
        for (var i = chain.Count - 1; i > 0; i--)
        {
            if (!AppendChildSteps(chain[i], chain[i - 1].ComponentId, steps, batch))
            {
                error = TypeNames.Short(chain[i - 1].Component.GetType()) + " is not in the latest render of " + TypeNames.Short(chain[i].Component.GetType()) + " (it is being re-rendered).";
                return null;
            }
        }

        var root = chain[^1];
        if (batch is not null && !batch.AnchorKnown)
        {
            batch.Anchor = AnchorRoot(session, renderer);
            batch.AnchorKnown = true;
        }

        var anchor = batch is null ? AnchorRoot(session, renderer) : batch.Anchor;
        var anchored = anchor is not null && ReferenceEquals(anchor, root);
        var signature = SignatureOf(root, batch);
        var (ordinal, count) = anchored ? (0, 1) : RootOrdinal(renderer, anchor, root, signature, batch);
        return new BevDomLocation(record.InstanceId, record.DisplayName, anchored, signature, ordinal, count, count > 1, steps);
    }

    /// <summary>
    /// The component that rendered the node at the end of <paramref name="path"/>, which starts at an interactive root's
    /// container: the BevTools panel's own root when <paramref name="anchored"/>, otherwise one of this runtime's roots
    /// whose signature is <paramref name="signature"/> - the only one the path fits, or, among identical ones, the
    /// <paramref name="ordinal"/>-th when the page holds exactly this runtime's; anything else is no answer, not a guess.
    /// </summary>
    /// <param name="session">The session whose renderer's render trees are read, on its dispatcher.</param>
    /// <param name="path">The steps from the root's container to the node, as the page hub recorded them.</param>
    /// <param name="anchored">The root is the one the session's BevTools panel renders in.</param>
    /// <param name="signature">The root container's signature in the page.</param>
    /// <param name="ordinal">The root's position among the page's unanchored roots with that signature.</param>
    /// <param name="includeHidden">Report a framework component that rendered the node, instead of its nearest application ancestor.</param>
    /// <param name="error">Why there is no answer.</param>
    /// <param name="sameCount">How many interactive roots of the page, every runtime's and no panel's, share the signature: the roots <paramref name="ordinal"/> counts.</param>
    public static BevDomOwner? Resolve(BevToolsSession session, IReadOnlyList<BevDomStep> path, bool anchored, string? signature, int ordinal, int sameCount, bool includeHidden, out string? error)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            return ResolveCore(session, path, anchored, signature, ordinal, sameCount, includeHidden, out error);
        }
        finally
        {
            session.Overhead.AddPageMapping(System.Diagnostics.Stopwatch.GetTimestamp() - start);
        }
    }

    private static BevDomOwner? ResolveCore(BevToolsSession session, IReadOnlyList<BevDomStep> path, bool anchored, string? signature, int ordinal, int sameCount, bool includeHidden, out string? error)
    {
        error = null;
        if (!IsAvailable || session.Components.Renderer is not { } renderer)
        {
            error = IsAvailable ? "No component of this runtime has rendered yet." : "The renderer's render trees cannot be read on this framework version.";
            return null;
        }

        var ambiguous = false;
        List<ComponentState> roots;
        if (anchored)
        {
            if (AnchorRoot(session, renderer) is not { } anchor)
            {
                error = "The BevTools panel of this runtime is not rendered.";
                return null;
            }

            roots = [anchor];
        }
        else
        {
            // In component id order, the order roots attach in; the anchor is left out here as the hub leaves out every
            // runtime's anchor, so a position counts the same roots on both sides.
            var anchor = AnchorRoot(session, renderer);
            roots = Roots(renderer).Where(r => !ReferenceEquals(r, anchor) && Signature(r) == signature).OrderBy(r => r.ComponentId).ToList();
        }

        var walks = roots.Select(root => Walk(renderer, root, path)).ToList();
        var verified = walks.Where(w => w is not null).Select(w => w!.Value).ToList();
        if (verified.Count == 0)
        {
            error = roots.Count == 0
                ? "No root component of this runtime renders that part of the page."
                : "The page does not match the latest render (a render is still on its way to the browser, or script changed the DOM).";
            return null;
        }

        (ComponentState Owner, int Depth, string? Element, bool InMarkup) match;
        if (verified.Count == 1)
        {
            // The path fits one root only: its structure identifies it.
            match = verified[0];
        }
        else if (sameCount == roots.Count && ordinal < roots.Count && walks[ordinal] is { } positional)
        {
            // Identical roots, and the page holds exactly this runtime's: the position in the page is the position here.
            // Still a match by order, which the answer says.
            match = positional;
            ambiguous = true;
        }
        else
        {
            error = "Several identical roots render this part of the page, and BevTools cannot tell which one it is.";
            return null;
        }

        var (owner, depth, element, inMarkup) = match;
        if (!session.Components.TryGet(owner.Component, out var record))
        {
            error = TypeNames.Short(owner.Component.GetType()) + " rendered it, and BevTools does not track that component.";
            return null;
        }

        if (record.IsBevTools)
        {
            error = "That is BevTools' own UI.";
            return null;
        }

        string? renderedBy = null;
        if (record.IsHidden && !includeHidden)
        {
            renderedBy = record.DisplayName;
            var visible = record.Parent;
            while (visible is not null && visible.IsHidden)
            {
                visible = visible.Parent;
            }

            if (visible is null || visible.IsBevTools)
            {
                error = renderedBy + " rendered it, and no application component encloses it.";
                return null;
            }

            depth = ContainerDepth(owner, visible, depth);
            record = visible;
        }

        return new BevDomOwner(record, depth, renderedBy, element, inMarkup, ambiguous);
    }

    /// <summary>The component and its ancestors, the component first and its root last.</summary>
    private static List<ComponentState> Ancestry(ComponentState state)
    {
        var chain = new List<ComponentState>();
        for (var current = state; current is not null && chain.Count < MaxDepth; current = current.ParentComponentState)
        {
            chain.Add(current);
        }

        return chain;
    }

    /// <summary>The root component the BevTools panel of this session renders in: the root the page hub finds from the panel's element.</summary>
    private static ComponentState? AnchorRoot(BevToolsSession session, Renderer renderer)
    {
        if (session.PanelComponent is not { } panel || BlazorReflection.ResolveState(renderer, panel) is not { } state)
        {
            return null;
        }

        return Ancestry(state)[^1];
    }

    private static IEnumerable<ComponentState> Roots(Renderer renderer) =>
        BlazorReflection.ComponentStates(renderer)?.Where(s => s.ParentComponentState is null) ?? [];

    /// <summary>
    /// Which of this runtime's roots with this signature <paramref name="root"/> is, in component id order - the order
    /// roots attach in - and how many there are; the anchor root is not one of them.
    /// </summary>
    private static (int Ordinal, int Count) RootOrdinal(Renderer renderer, ComponentState? anchor, ComponentState root, string signature, BevDomMapBatch? batch)
    {
        var roots = batch is null ? Roots(renderer).ToList() : batch.Roots ??= Roots(renderer).ToList();
        var same = roots.Where(r => !ReferenceEquals(r, anchor) && SignatureOf(r, batch) == signature).OrderBy(r => r.ComponentId).ToList();
        return (Math.Max(0, same.FindIndex(r => ReferenceEquals(r, root))), Math.Max(1, same.Count));
    }

    private static string SignatureOf(ComponentState root, BevDomMapBatch? batch)
    {
        if (batch is null)
        {
            return Signature(root);
        }

        if (!batch.Signatures.TryGetValue(root, out var signature))
        {
            batch.Signatures[root] = signature = Signature(root);
        }

        return signature;
    }

    /// <summary>"3|div,#text,#c": how many logical children a root's container has, and the kinds of the first few.</summary>
    internal static string Signature(ComponentState root)
    {
        if (BlazorReflection.CurrentFramesInPlace(root) is not { } frames)
        {
            return "?";
        }

        var kinds = new List<string>();
        var count = 0;
        var array = frames.Array;
        for (var i = 0; i < frames.Count; i++)
        {
            ref readonly var frame = ref array[i];
            if (Kind(in frame) is not { } kind)
            {
                continue;
            }

            if (count++ < SignatureKinds)
            {
                kinds.Add(kind);
            }

            i += Skip(in frame);
        }

        return count + "|" + string.Join(",", kinds);
    }

    /// <summary>The kind of node a frame becomes in the browser, or null for frames that produce none (regions are transparent).</summary>
    private static string? Kind(in RenderTreeFrame frame) => frame.FrameType switch
    {
        RenderTreeFrameType.Element => frame.ElementName.ToLowerInvariant(),
        RenderTreeFrameType.Text => "#text",
        RenderTreeFrameType.Component or RenderTreeFrameType.Markup => "#c",
        _ => null,
    };

    /// <summary>How many frames after this one belong to it and are not siblings: an element's or component's subtree.</summary>
    private static int Skip(in RenderTreeFrame frame) => frame.FrameType switch
    {
        RenderTreeFrameType.Element => frame.ElementSubtreeLength - 1,
        RenderTreeFrameType.Component => frame.ComponentSubtreeLength - 1,
        _ => 0,
    };

    /// <summary>
    /// Appends the steps from a parent's container to its child's component frame. Without a batch the parent's frames
    /// are scanned up to the child; with one, every child's position is collected in one walk and reused, which is what
    /// keeps locating the rows of a long list linear.
    /// </summary>
    private static bool AppendChildSteps(ComponentState parent, int componentId, List<BevDomStep> steps, BevDomMapBatch? batch)
    {
        if (BlazorReflection.CurrentFramesInPlace(parent) is not { } frames)
        {
            return false;
        }

        if (batch is null)
        {
            return FindComponent(frames.Array, 0, frames.Count, componentId, steps);
        }

        if (!batch.Children.TryGetValue(parent, out var positions))
        {
            batch.Children[parent] = positions = [];
            CollectComponents(frames.Array, 0, frames.Count, [], positions);
        }

        if (!positions.TryGetValue(componentId, out var path))
        {
            return false;
        }

        steps.AddRange(path);
        return true;
    }

    private static void CollectComponents(RenderTreeFrame[] frames, int start, int end, List<BevDomStep> prefix, Dictionary<int, BevDomStep[]> positions)
    {
        var index = 0;
        for (var i = start; i < end; i++)
        {
            ref readonly var frame = ref frames[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    prefix.Add(new BevDomStep(index, frame.ElementName.ToLowerInvariant()));
                    CollectComponents(frames, i + 1, i + frame.ElementSubtreeLength, prefix, positions);
                    prefix.RemoveAt(prefix.Count - 1);
                    index++;
                    i += frame.ElementSubtreeLength - 1;
                    break;
                case RenderTreeFrameType.Component:
                    positions[frame.ComponentId] = [.. prefix, new BevDomStep(index, "#c")];
                    index++;
                    i += frame.ComponentSubtreeLength - 1;
                    break;
                case RenderTreeFrameType.Text:
                case RenderTreeFrameType.Markup:
                    index++;
                    break;
            }
        }
    }

    /// <summary>Appends the steps from a parent's container to the frame of its child <paramref name="componentId"/>.</summary>
    private static bool FindComponent(RenderTreeFrame[] frames, int start, int end, int componentId, List<BevDomStep> steps)
    {
        var index = 0;
        for (var i = start; i < end; i++)
        {
            ref readonly var frame = ref frames[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    steps.Add(new BevDomStep(index, frame.ElementName.ToLowerInvariant()));
                    if (FindComponent(frames, i + 1, i + frame.ElementSubtreeLength, componentId, steps))
                    {
                        return true;
                    }

                    steps.RemoveAt(steps.Count - 1);
                    index++;
                    i += frame.ElementSubtreeLength - 1;
                    break;
                case RenderTreeFrameType.Component:
                    if (frame.ComponentId == componentId)
                    {
                        steps.Add(new BevDomStep(index, "#c"));
                        return true;
                    }

                    index++;
                    i += frame.ComponentSubtreeLength - 1;
                    break;
                case RenderTreeFrameType.Text:
                case RenderTreeFrameType.Markup:
                    index++;
                    break;
            }
        }

        return false;
    }

    /// <summary>The frame that is the <paramref name="n"/>-th logical child of the range, or -1.</summary>
    private static int NthLogicalChild(RenderTreeFrame[] frames, int start, int end, int n)
    {
        var index = 0;
        for (var i = start; i < end; i++)
        {
            ref readonly var frame = ref frames[i];
            if (Kind(in frame) is null)
            {
                continue;
            }

            if (index++ == n)
            {
                return i;
            }

            i += Skip(in frame);
        }

        return -1;
    }

    /// <summary>Follows a path from a root's container through the render trees, verifying every step's kind.</summary>
    private static (ComponentState Owner, int Depth, string? Element, bool InMarkup)? Walk(Renderer renderer, ComponentState root, IReadOnlyList<BevDomStep> path)
    {
        var owner = root;
        var depth = -1;
        string? element = null;
        if (BlazorReflection.CurrentFramesInPlace(owner) is not { } range)
        {
            return null;
        }

        var frames = range.Array;
        var start = 0;
        var end = range.Count;
        for (var k = 0; k < path.Count; k++)
        {
            var step = path[k];
            var at = NthLogicalChild(frames, start, end, step.Index);
            if (at < 0)
            {
                return null;
            }

            ref readonly var frame = ref frames[at];
            if (!string.Equals(Kind(in frame), step.Kind, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    element = BevComponentWiringReader.DescribeElement(frames, at);
                    start = at + 1;
                    end = at + frame.ElementSubtreeLength;
                    break;
                case RenderTreeFrameType.Component:
                    if (frame.Component is not { } child || BlazorReflection.ResolveState(renderer, child) is not { } childState
                        || BlazorReflection.CurrentFramesInPlace(childState) is not { } childRange)
                    {
                        return null;
                    }

                    owner = childState;
                    depth = k + 1;
                    element = null;
                    frames = childRange.Array;
                    start = 0;
                    end = childRange.Count;
                    break;
                case RenderTreeFrameType.Markup:
                    // The rest of the path runs through nodes parsed from a markup string: the owner rendered them.
                    return (owner, depth, element, true);
                default:
                    return (owner, depth, element, false);
            }
        }

        return (owner, depth, element, false);
    }

    /// <summary>
    /// How many steps of the path lead to the container of <paramref name="visible"/>, an ancestor of
    /// <paramref name="owner"/>: its own position from the root, since every ancestor's container lies on the path.
    /// </summary>
    private static int ContainerDepth(ComponentState owner, BevComponentRecord visible, int ownerDepth)
    {
        var target = visible.Component;
        var chain = Ancestry(owner);
        var index = chain.FindIndex(s => ReferenceEquals(s.Component, target));
        if (index < 0)
        {
            return ownerDepth;
        }

        var root = chain[^1];
        if (ReferenceEquals(chain[index], root))
        {
            return -1;
        }

        var steps = new List<BevDomStep>();
        for (var i = chain.Count - 1; i > index; i--)
        {
            if (BlazorReflection.CurrentFramesInPlace(chain[i]) is not { } frames || !FindComponent(frames.Array, 0, frames.Count, chain[i - 1].ComponentId, steps))
            {
                return ownerDepth;
            }
        }

        return steps.Count;
    }
}

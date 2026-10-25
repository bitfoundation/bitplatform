namespace Bit.BevTools;

/// <summary>
/// One row of a batch's cost tree: a component, or same-type siblings that rendered nothing below them (a list's
/// rows), added up so 120 rows read as one line. Times are BuildRenderTree milliseconds in that batch.
/// </summary>
internal sealed record BevBatchNode(long InstanceId, string Name, int Instances, int Depth, int Renders, double SelfMs, double TotalMs);

/// <summary>
/// What one render batch rendered, as a tree: each component under its nearest ancestor that rendered in the same
/// batch, with its own time and its subtree's. Read from the render samples components still hold, so an old batch
/// shows what is left of it.
/// </summary>
internal sealed class BevBatchCostTree
{
    public static readonly BevBatchCostTree Empty = new([], 0, 0);

    private BevBatchCostTree(IReadOnlyList<BevBatchNode> rows, double totalMs, int omitted)
    {
        Rows = rows;
        TotalMs = totalMs;
        Omitted = omitted;
    }

    /// <summary>Depth-first, the costliest first at each level; at most the requested number.</summary>
    public IReadOnlyList<BevBatchNode> Rows { get; }

    public double TotalMs { get; }

    /// <summary>Components that rendered in the batch but did not fit in <see cref="Rows"/>.</summary>
    public int Omitted { get; }

    public static BevBatchCostTree Build(IEnumerable<BevComponentRecord> records, long batchId, int maxRows)
    {
        var rendered = new Dictionary<long, (BevComponentRecord Record, int Renders, double Ms)>();
        foreach (var record in records)
        {
            if (record.IsBevTools || record.IsHidden)
            {
                continue;
            }

            var renders = 0;
            var ms = 0d;
            foreach (var sample in record.Renders)
            {
                if (sample.BatchId == batchId)
                {
                    renders++;
                    ms += sample.DurationMs;
                }
            }

            if (renders > 0)
            {
                rendered[record.InstanceId] = (record, renders, ms);
            }
        }

        var children = new Dictionary<long, List<long>>();
        var roots = new List<long>();
        foreach (var (id, entry) in rendered)
        {
            var parent = entry.Record.Parent;
            while (parent is not null && !rendered.ContainsKey(parent.InstanceId))
            {
                parent = parent.Parent;
            }

            if (parent is null)
            {
                roots.Add(id);
            }
            else
            {
                (children.TryGetValue(parent.InstanceId, out var list) ? list : children[parent.InstanceId] = []).Add(id);
            }
        }

        // Subtree totals children first, without recursion: a chain of thousands of nested components is one batch's
        // tree too (as in ComponentRegistry.Aggregate).
        var totals = new Dictionary<long, double>(rendered.Count);
        var stack = new Stack<(long Id, bool ChildrenDone)>(roots.Select(r => (r, false)));
        while (stack.TryPop(out var item))
        {
            if (item.ChildrenDone || !children.TryGetValue(item.Id, out var below))
            {
                totals[item.Id] = rendered[item.Id].Ms + (children.TryGetValue(item.Id, out var done) ? done.Sum(c => totals[c]) : 0);
                continue;
            }

            stack.Push((item.Id, true));
            foreach (var child in below)
            {
                stack.Push((child, false));
            }
        }

        // Rows depth first. Recursion here is bounded by maxRows: every level adds a row before it descends.
        var rows = new List<BevBatchNode>();
        void AddLevel(List<long> ids, int depth)
        {
            var leafGroups = ids.Where(id => !children.ContainsKey(id)).GroupBy(id => rendered[id].Record.TypeName).Where(g => g.Count() > 1).ToList();
            var grouped = leafGroups.SelectMany(g => g).ToHashSet();
            var level = ids.Where(id => !grouped.Contains(id)).Select(id => (Ids: (IReadOnlyList<long>)[id], Total: totals[id]))
                .Concat(leafGroups.Select(g => (Ids: (IReadOnlyList<long>)g.ToList(), Total: g.Sum(id => totals[id]))))
                .OrderByDescending(r => r.Total);
            foreach (var (group, total) in level)
            {
                if (rows.Count >= maxRows)
                {
                    return;
                }

                var first = rendered[group[0]];
                rows.Add(new BevBatchNode(group[0], first.Record.DisplayName, group.Count, depth, group.Sum(id => rendered[id].Renders), group.Sum(id => rendered[id].Ms), total));
                if (group.Count == 1 && children.TryGetValue(group[0], out var below))
                {
                    AddLevel(below, depth + 1);
                }
            }
        }

        AddLevel(roots, 0);
        return new BevBatchCostTree(rows, roots.Sum(r => totals[r]), rendered.Count - rows.Sum(n => n.Instances));
    }
}

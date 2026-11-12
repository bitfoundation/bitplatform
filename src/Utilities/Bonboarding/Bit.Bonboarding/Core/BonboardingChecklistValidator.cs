namespace Bit.Bonboarding;

/// <summary>
/// Finds the mistakes in a checklist definition that would otherwise show as a checklist quietly misbehaving: two items
/// sharing an id (so a tick, stored by id, ticks both), a prerequisite that does not exist (so the item stays locked
/// for good), items waiting for each other in a circle.
/// </summary>
internal static class BonboardingChecklistValidator
{
    public static IReadOnlyList<BonboardingValidationIssue> Validate(BonboardingChecklistDefinition checklist)
    {
        var issues = new List<BonboardingValidationIssue>();
        var name = string.IsNullOrWhiteSpace(checklist.Id) ? "(no id)" : checklist.Id;

        if (string.IsNullOrWhiteSpace(checklist.Id))
        {
            issues.Add(Error("missing-checklist-id", "The checklist has no Id. The id is the key its progress is stored under."));
        }

        if (checklist.Items.Count == 0)
        {
            issues.Add(Warning("no-items", $"Checklist '{name}' has no items."));
            return issues;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in checklist.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                issues.Add(Error("blank-item-id", $"An item of checklist '{name}' has a blank Id; its progress is stored under the id."));
            }
            else if (!ids.Add(item.Id))
            {
                issues.Add(Error("duplicate-item-id", $"Checklist '{name}' has more than one item with the id '{item.Id}'; ticking one off ticks off both.", item.Id));
            }
        }

        foreach (var item in checklist.Items)
        {
            foreach (var dependency in item.DependsOn ?? [])
            {
                if (dependency == item.Id)
                {
                    issues.Add(Error("item-depends-on-itself", $"Item '{item.Id}' of checklist '{name}' depends on itself, so it can never be started.", item.Id));
                }
                else if (!ids.Contains(dependency))
                {
                    issues.Add(Error("unknown-dependency", $"Item '{item.Id}' of checklist '{name}' depends on '{dependency}', which is not an item of the checklist, so it stays locked.", item.Id));
                }
            }
        }

        foreach (var cycle in FindCycles(checklist))
        {
            issues.Add(Error("dependency-cycle", $"Items of checklist '{name}' wait for each other in a circle ({string.Join(" -> ", cycle)}), so none of them can be started.", cycle[0]));
        }

        if (checklist.Items.TrueForAll(item => item.Optional))
        {
            issues.Add(Warning("only-optional-items", $"Every item of checklist '{name}' is optional, so its progress counts all of them, as if none were."));
        }

        return issues;
    }

    /// <summary>Each circle of prerequisites once, starting from its first listed item, e.g. <c>a -> b -> a</c>.</summary>
    private static List<string[]> FindCycles(BonboardingChecklistDefinition checklist)
    {
        var byId = new Dictionary<string, BonboardingChecklistItem>(StringComparer.Ordinal);
        foreach (var item in checklist.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Id)) byId.TryAdd(item.Id, item);
        }

        var cycles = new List<string[]>();
        var done = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();
        var onPath = new HashSet<string>(StringComparer.Ordinal);

        void Visit(string id)
        {
            if (done.Contains(id)) return;

            if (!onPath.Add(id))
            {
                var start = path.IndexOf(id);
                cycles.Add([.. path.Skip(start), id]);
                return;
            }

            path.Add(id);

            foreach (var dependency in byId[id].DependsOn ?? [])
            {
                // Self-dependencies and unknown ids are reported on their own.
                if (dependency != id && byId.ContainsKey(dependency)) Visit(dependency);
            }

            path.RemoveAt(path.Count - 1);
            onPath.Remove(id);
            done.Add(id);
        }

        foreach (var id in byId.Keys) Visit(id);

        return cycles;
    }

    private static BonboardingValidationIssue Error(string code, string message, string? itemId = null)
        => new(BonboardingValidationSeverity.Error, code, message, itemId);

    private static BonboardingValidationIssue Warning(string code, string message, string? itemId = null)
        => new(BonboardingValidationSeverity.Warning, code, message, itemId);
}

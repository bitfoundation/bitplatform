namespace Bit.Borm;

/// <summary>
/// A complete form schema: the ordered set of fields, optional wizard steps and metadata.
/// A <see cref="BormDefinition"/> is the single source of truth a renderer consumes.
/// </summary>
public sealed class BormDefinition
{
    /// <summary>Optional form title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional form description.</summary>
    public string? Description { get; set; }

    /// <summary>The CLR model type this schema was generated from, when applicable.</summary>
    public Type? ModelType { get; set; }

    /// <summary>
    /// Number of columns the form lays its fields out in. 1 (the default) is a single stacked column;
    /// higher values switch to a responsive grid that individual fields can span via
    /// <see cref="BormFieldDefinition.ColumnSpan"/>.
    /// </summary>
    public int Columns { get; set; } = 1;

    /// <summary>Top-level fields.</summary>
    public IList<BormFieldDefinition> Fields { get; set; } = new List<BormFieldDefinition>();

    /// <summary>Wizard steps. When non-empty, the form is multi-step.</summary>
    public IList<BormStep> Steps { get; set; } = new List<BormStep>();

    /// <summary>
    /// Rules validating the form as a whole rather than one field. Their messages are attached to the
    /// path each rule reports, so a cross-field rule can surface on the field the user should fix.
    /// </summary>
    public IList<IBormValidationRule> Validators { get; set; } = new List<IBormValidationRule>();

    /// <summary>True when the form is configured as a multi-step wizard.</summary>
    public bool IsWizard => Steps.Count > 0;

    /// <summary>Finds a top-level field by name.</summary>
    public BormFieldDefinition? FindField(string name)
        => Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Resolves a field from a full data path such as <c>Address.City</c> or <c>Items[0].Product</c>,
    /// walking into object children and array item templates. Index segments select the item template,
    /// because every element of an array shares one definition.
    /// </summary>
    public BormFieldDefinition? FindByPath(string path)
    {
        // The typed builder can declare a field whose *name* is already a dotted path
        // (`x => x.Address.City`), so an exact match wins before the path is decomposed.
        var direct = Fields.FirstOrDefault(f => string.Equals(f.Name, path, StringComparison.OrdinalIgnoreCase));
        if (direct is not null) return direct;

        // The same goes for a container: `Object(x => x.Customer.Address, …)` is one top-level field
        // named "Customer.Address", and "Customer.Address.City" lies beneath it. Splitting the whole path
        // at every dot looked for a top-level "Customer" that does not exist, so nothing under such a
        // container could be found — its change handlers never ran, and a wizard step naming one of its
        // fields skipped validating it. So the walk starts from the top-level field whose name is the
        // longest prefix of the path (ending at a segment boundary), which for an ordinary "Address.City"
        // is simply "Address".
        foreach (var root in Fields
                     .Where(f => f.Name.Length > 0
                                 && f.Name.Length < path.Length
                                 && path[f.Name.Length] is '.' or '['
                                 && path.StartsWith(f.Name, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(f => f.Name.Length))
        {
            if (Walk(root, path[root.Name.Length..]) is { } found) return found;
        }

        return null;

        static BormFieldDefinition? Walk(BormFieldDefinition start, string rest)
        {
            var current = start;
            foreach (var seg in BormPath.Parse(rest))
            {
                if (seg.IsIndex)
                {
                    // items[0] resolves to the template describing every element.
                    // An array of objects keeps its children on the template itself, so stay there.
                    if (current.ItemTemplate is not { } template) return null;
                    current = template;
                    continue;
                }

                var candidates = current.Type == BormFieldType.Array && current.ItemTemplate is not null
                    ? current.ItemTemplate.Children
                    : current.Children;

                var next = candidates.FirstOrDefault(f => string.Equals(f.Name, seg.Name, StringComparison.OrdinalIgnoreCase));
                if (next is null) return null;
                current = next;
            }
            return current;
        }
    }

    /// <summary>Enumerates every field in the schema, recursing into objects and array item templates.</summary>
    public IEnumerable<BormFieldDefinition> AllFields()
    {
        foreach (var f in Fields)
        {
            yield return f;
            foreach (var d in f.Descendants()) yield return d;
        }
    }

    /// <summary>
    /// Top-level fields in the order they should be rendered. Ordering is stable: fields with the same
    /// <see cref="BormFieldDefinition.Order"/> keep their declared sequence.
    /// </summary>
    public IEnumerable<BormFieldDefinition> OrderedFields() => Fields.OrderBy(f => f.Order);

    /// <summary>
    /// Returns an independent copy of the schema, so a shared definition can be tailored for one form
    /// without the change leaking into every other one rendering it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A schema is usually built once and kept — a static readonly field, a singleton from DI — and
    /// every collection on it is mutable. Adding this user's options to a field, or hiding a step for
    /// this tenant, edits the copy everyone else is looking at. This is the safe way to do it.
    /// </para>
    /// <para>
    /// Every container is rebuilt: fields, children, item templates, options, rule lists, dependency
    /// lists and attribute bags. What is <em>shared</em> is everything with no state to corrupt — rule
    /// instances, conditions, and the delegates behind <see cref="BormFieldDefinition.Computed"/>,
    /// <see cref="BormFieldDefinition.OptionsProvider"/>,
    /// <see cref="BormFieldDefinition.OnChanged"/> and
    /// <see cref="BormFieldDefinition.Normalize"/>. Values stashed in an attribute bag are
    /// shared too, since the library cannot know how to copy an arbitrary object; a copy is only as
    /// independent as what the caller put in there.
    /// </para>
    /// </remarks>
    public BormDefinition Clone()
    {
        var copy = new BormDefinition
        {
            Title = Title,
            Description = Description,
            ModelType = ModelType,
            Columns = Columns,
            Fields = Fields.Select(CloneField).ToList(),
            Steps = Steps.Select(CloneStep).ToList(),
            Validators = new List<IBormValidationRule>(Validators)
        };
        return copy;

        static BormFieldDefinition CloneField(BormFieldDefinition field)
            => new(field.Name, field.Type)
            {
                ValueType = field.ValueType,
                Label = field.Label,
                Placeholder = field.Placeholder,
                HelpText = field.HelpText,
                ShowLabel = field.ShowLabel,
                Prefix = field.Prefix,
                Suffix = field.Suffix,
                ShowCharacterCount = field.ShowCharacterCount,
                Required = field.Required,
                ReadOnly = field.ReadOnly,
                DefaultValue = field.DefaultValue,
                Order = field.Order,
                Group = field.Group,
                Options = new List<BormSelectOption>(field.Options),
                OptionsProvider = field.OptionsProvider,
                OptionsDependencies = new List<string>(field.OptionsDependencies),
                Suggestions = new List<string>(field.Suggestions),
                Computed = field.Computed,
                ComputedDependencies = new List<string>(field.ComputedDependencies),
                RevalidateOn = new List<string>(field.RevalidateOn),
                OnChanged = field.OnChanged,
                Normalize = field.Normalize,
                MinLength = field.MinLength,
                MaxLength = field.MaxLength,
                Min = field.Min,
                Max = field.Max,
                NumericStep = field.NumericStep,
                Pattern = field.Pattern,
                Multiple = field.Multiple,
                Accept = field.Accept,
                MaxFileSize = field.MaxFileSize,
                Autocomplete = field.Autocomplete,
                InputMode = field.InputMode,
                Autofocus = field.Autofocus,
                UpdateOn = field.UpdateOn,
                DebounceMilliseconds = field.DebounceMilliseconds,
                InputAttributes = new Dictionary<string, object?>(field.InputAttributes),
                ColumnSpan = field.ColumnSpan,
                VisibleWhen = field.VisibleWhen,
                DisabledWhen = field.DisabledWhen,
                ReadOnlyWhen = field.ReadOnlyWhen,
                RequiredWhen = field.RequiredWhen,
                ClearOnHide = field.ClearOnHide,
                Validators = new List<IBormValidationRule>(field.Validators),
                Children = field.Children.Select(CloneField).ToList(),
                ItemTemplate = field.ItemTemplate is { } template ? CloneField(template) : null,
                MinItems = field.MinItems,
                MaxItems = field.MaxItems,
                CustomRenderer = field.CustomRenderer,
                Attributes = new Dictionary<string, object?>(field.Attributes)
            };

        static BormStep CloneStep(BormStep step)
            => new(step.Id, step.Title)
            {
                Description = step.Description,
                Fields = new List<string>(step.Fields),
                VisibleWhen = step.VisibleWhen
            };
    }

    /// <summary>
    /// Checks the schema for the mistakes that are easy to make and hard to see: two siblings sharing a
    /// name (they would bind to the same path and overwrite each other), an array with no item
    /// template, a step naming a field that does not exist, a condition or computed dependency pointing
    /// nowhere. Returns an empty list for a healthy schema.
    /// </summary>
    /// <remarks>
    /// This is a development aid, not a gate: it is never run automatically, because a schema assembled
    /// at runtime from user input has every right to be incomplete while it is being edited. Call it in
    /// a test, or behind a debug flag on a schema editor.
    /// </remarks>
    public IReadOnlyList<BormSchemaDiagnostic> Validate()
    {
        var diagnostics = new List<BormSchemaDiagnostic>();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CheckSiblings(Fields, string.Empty, diagnostics);
        foreach (var field in Fields) Walk(field, field.Name, diagnostics, known);

        foreach (var step in Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
                diagnostics.Add(new BormSchemaDiagnostic(string.Empty, "A wizard step has no id."));

            foreach (var name in step.Fields)
            {
                if (FindByPath(name) is null)
                    diagnostics.Add(new BormSchemaDiagnostic(name,
                        $"Step '{step.Id}' lists a field that is not in the schema; it will render nothing."));
            }

            // A step's condition is checked exactly as a field's is. A dependency naming nothing reads
            // as null forever, so the step is either always shown or never shown — and "never shown" on
            // a wizard means a page of questions the user is never asked and a set of answers the
            // schema quietly drops.
            CheckPaths(BormConditionGroup.NamedPaths(step.VisibleWhen), string.Empty, $"Step '{step.Id}' VisibleWhen", diagnostics);
        }

        // Two fields both asking for focus is a race the schema cannot win: whichever renders last
        // takes it, so the answer changes with the layout rather than with the author's intent.
        var autofocused = AllFields().Where(f => f.Autofocus).Select(f => f.Name).ToList();
        if (autofocused.Count > 1)
            diagnostics.Add(new BormSchemaDiagnostic(autofocused[0],
                $"{autofocused.Count} fields are marked Autofocus ({string.Join(", ", autofocused)}); only one can win.",
                BormSchemaDiagnosticSeverity.Warning));

        var duplicateSteps = Steps.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1);
        foreach (var group in duplicateSteps)
            diagnostics.Add(new BormSchemaDiagnostic(string.Empty,
                $"More than one wizard step has the id '{group.Key}'; GoToStep(id) will always find the first."));

        // Every field a step never mentions is still validated on submit but never shown, which reads
        // to the user as a form that refuses to submit for no visible reason.
        if (IsWizard)
        {
            var stepped = Steps.SelectMany(s => s.Fields).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Fields.Where(f => f.Type != BormFieldType.Static && !stepped.Contains(f.Name)))
                diagnostics.Add(new BormSchemaDiagnostic(field.Name,
                    "This field belongs to no wizard step, so it is never shown — but it is still validated on submit.",
                    BormSchemaDiagnosticSeverity.Warning));
        }

        return diagnostics;

        void Walk(BormFieldDefinition field, string path,
            List<BormSchemaDiagnostic> into, HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(field.Name))
                into.Add(new BormSchemaDiagnostic(path, "A field has no name, so it binds to nothing."));

            if (field.Type == BormFieldType.Array && field.ItemTemplate is null)
                into.Add(new BormSchemaDiagnostic(path,
                    "An array field has no ItemTemplate; rendering it will throw. Use the builder's Array(...) or ArrayOf(...) overload."));

            if (field.Type == BormFieldType.Custom && string.IsNullOrEmpty(field.CustomRenderer))
                into.Add(new BormSchemaDiagnostic(path,
                    "A custom field has no renderer key, so no component can be resolved for it."));

            // A combobox is a text box whose entry is matched back to an option by its *label*, because
            // the label is what the browser puts in the box. Two options sharing one leaves the match
            // ambiguous: the first wins, silently, and the user's choice becomes a coin toss.
            if (field.Type == BormFieldType.Combobox)
            {
                foreach (var group in field.Options
                             .GroupBy(o => o.Label, StringComparer.OrdinalIgnoreCase)
                             .Where(g => g.Count() > 1))
                {
                    into.Add(new BormSchemaDiagnostic(path,
                        $"{group.Count()} of this combobox's options are labelled '{group.Key}'; the entry cannot be " +
                        "matched back to one of them, so the first always wins.",
                        BormSchemaDiagnosticSeverity.Warning));
                }
            }

            if (field.IsChoice && field.Options.Count == 0 && field.OptionsProvider is null)
                into.Add(new BormSchemaDiagnostic(path,
                    "A choice field has neither options nor an options provider; it will render an empty list.",
                    BormSchemaDiagnosticSeverity.Warning));

            // The provider always wins, so the static list is never rendered — but it is still there in
            // the schema, and reading it is the natural way to be wrong about what the field offers.
            if (field.OptionsProvider is not null && field.Options.Count > 0)
                into.Add(new BormSchemaDiagnostic(path,
                    "A field has both static options and an options provider; the provider wins and the static list is never shown.",
                    BormSchemaDiagnosticSeverity.Warning));

            // Bounds that cross over describe a field no value can satisfy, so the form can never be
            // submitted and nothing on the page says why. Almost always a transposed pair of arguments.
            CheckBounds(field.MinLength, field.MaxLength, "MinLength", "MaxLength", path, into);
            CheckBounds(field.MinItems, field.MaxItems, "MinItems", "MaxItems", path, into);
            CheckBounds(field.Min, field.Max, "Min", "Max", path, into);

            // Asking for a value to be written as the user types only means something on a control the
            // user types into. Everywhere else the browser has no `input` event worth the name — a
            // dropdown, a radio group and a file picker only ever commit — and a combobox deliberately
            // refuses, because half of a label stands for no value at all. The setting was accepted and
            // then ignored, which is the kind of thing that costs an afternoon.
            if (field.UpdateOn == BormUpdateTrigger.Input && !TypesThatWriteAsYouType(field.Type))
                into.Add(new BormSchemaDiagnostic(path,
                    $"UpdateOnInput has no effect on a {field.Type} field; it commits when the choice is made.",
                    BormSchemaDiagnosticSeverity.Warning));

            if (field.Computed is not null && !field.ReadOnly)
                into.Add(new BormSchemaDiagnostic(path,
                    "A computed field is editable, so anything typed into it is overwritten the next time a dependency changes.",
                    BormSchemaDiagnosticSeverity.Warning));

            // A confirm-password rule pointing at nothing is silently satisfied: the other value reads
            // as null, this one is compared against it, and the field passes for the wrong reason.
            foreach (var compare in field.Validators.OfType<BormCompareRule>())
                CheckPaths([compare.OtherPath], path, "MatchesField", into);

            CheckPaths(BormConditionGroup.NamedPaths(field.VisibleWhen), path, "VisibleWhen", into);
            CheckPaths(BormConditionGroup.NamedPaths(field.DisabledWhen), path, "DisabledWhen", into);
            CheckPaths(BormConditionGroup.NamedPaths(field.ReadOnlyWhen), path, "ReadOnlyWhen", into);
            CheckPaths(BormConditionGroup.NamedPaths(field.RequiredWhen), path, "RequiredWhen", into);
            CheckPaths(field.ComputedDependencies, path, "a computed dependency", into);
            CheckPaths(field.OptionsDependencies, path, "an options dependency", into);
            CheckPaths(field.RevalidateOn, path, "RevalidateOn", into);

            // A field that reads another one and never revalidates keeps a verdict that has since
            // become wrong: the user is told the two do not match, fixes the *other* field, and the
            // message stays. MatchesField wires itself up, so this only catches a hand-built rule.
            foreach (var compare in field.Validators.OfType<BormCompareRule>())
            {
                if (DeclaresRevalidation(field, compare.OtherPath)) continue;
                into.Add(new BormSchemaDiagnostic(path,
                    $"A rule compares this field with '{compare.OtherPath}' but does not revalidate when it changes, " +
                    "so correcting the other field leaves this one's message behind. Add RevalidateOn(\"" +
                    compare.OtherPath + "\").",
                    BormSchemaDiagnosticSeverity.Warning));
            }

            seen.Add(path);

            if (field.Type == BormFieldType.Object)
            {
                CheckSiblings(field.Children, path, into);
                foreach (var child in field.Children)
                    Walk(child, BormPath.Combine(path, child.Name), into, seen);
            }
            else if (field.ItemTemplate is { } template)
            {
                var itemPath = path + "[]";
                if (template.Type == BormFieldType.Object)
                {
                    CheckSiblings(template.Children, itemPath, into);
                    foreach (var child in template.Children)
                        Walk(child, BormPath.Combine(itemPath, child.Name), into, seen);
                }
                else
                {
                    Walk(template, itemPath, into, seen);
                }
            }
        }

        // Whether the field already says it revalidates when `other` changes. Matched by prefix, so
        // naming a container covers the field inside it, exactly as the runtime match does.
        static bool DeclaresRevalidation(BormFieldDefinition field, string other)
        {
            foreach (var declared in field.RevalidateOn)
                if (declared.Length > 0 && BormPath.IsAtOrUnder(other, declared)) return true;
            return false;
        }

        // Whether a control writes its value while the user is still working on it. A slider is here
        // because dragging *is* the interaction: it always writes live, whatever the schema says.
        static bool TypesThatWriteAsYouType(BormFieldType type) => type is
            BormFieldType.Text or BormFieldType.TextArea or BormFieldType.Email
            or BormFieldType.Password or BormFieldType.Url or BormFieldType.Tel
            or BormFieldType.Search or BormFieldType.Integer or BormFieldType.Number
            or BormFieldType.Date or BormFieldType.DateTime or BormFieldType.Time
            or BormFieldType.Color or BormFieldType.Range or BormFieldType.Custom;

        static void CheckBounds<T>(T? min, T? max, string minName, string maxName, string path,
            List<BormSchemaDiagnostic> into) where T : struct, IComparable<T>
        {
            if (min is { } lo && max is { } hi && lo.CompareTo(hi) > 0)
                into.Add(new BormSchemaDiagnostic(path,
                    $"{minName} ({lo}) is greater than {maxName} ({hi}); no value can satisfy both."));
        }

        void CheckPaths(IEnumerable<string>? paths, string owner, string what, List<BormSchemaDiagnostic> into)
        {
            if (paths is null) return;
            var scope = BormPath.Parent(owner.Replace("[]", "[0]", StringComparison.Ordinal));

            foreach (var dependency in paths)
            {
                if (string.IsNullOrEmpty(dependency)) continue;
                // Resolved absolutely or relative to the owning object, exactly as it is at run time.
                if (FindByPath(dependency) is not null) continue;
                if (scope.Length > 0 && FindByPath(BormPath.Combine(scope, dependency)) is not null) continue;

                into.Add(new BormSchemaDiagnostic(owner,
                    $"{what} refers to '{dependency}', which is not a field in this schema.",
                    BormSchemaDiagnosticSeverity.Warning));
            }
        }

        static void CheckSiblings(IEnumerable<BormFieldDefinition> fields, string parent,
            List<BormSchemaDiagnostic> into)
        {
            foreach (var group in fields.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                into.Add(new BormSchemaDiagnostic(BormPath.Combine(parent, group.Key),
                    $"{group.Count()} sibling fields share the name '{group.Key}'; they bind to the same path and will overwrite each other."));
        }
    }
}

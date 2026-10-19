namespace Bit.Blex.Demo.Client;

/// <summary>One compile-time diagnostic the Blex source generator can report, as the docs explain it.</summary>
/// <param name="Code">The diagnostic id, e.g. <c>BLEX001</c>.</param>
/// <param name="Title">The short title the generator reports.</param>
/// <param name="IsError">True for an error, false for a warning.</param>
/// <param name="Cause">Why the generator rejects the shape (HTML).</param>
/// <param name="Bad">A store the generator rejects with this diagnostic.</param>
/// <param name="Good">The same store, fixed.</param>
public sealed record BlexDiagnosticDoc(
    string Code,
    string Title,
    bool IsError,
    string Cause,
    string Bad = "",
    string Good = "");

/// <summary>
/// Every diagnostic of the Blex source generator, with a rejected and a fixed sample. The diagnostics
/// page renders it and the demo's MCP server hands the same entries out, so the two cannot disagree.
/// </summary>
public static class DiagnosticsCatalog
{
    /// <summary>Every diagnostic, in the order the docs present them.</summary>
    public static IReadOnlyList<BlexDiagnosticDoc> All { get; } =
    [
        new("BLEX001", "Store class must be partial", true,
            "The generator adds members through a second partial declaration, so the class has to be <code>partial</code>.",
            """
            [BlexStore]
            public class CounterStore   // BLEX001
            {
                [BlexState] private int _count;
            }
            """,
            """
            [BlexStore]
            public partial class CounterStore
            {
                [BlexState] private int _count;
            }
            """),

        new("BLEX002", "Cannot derive action name", true,
            "An action or effect method must either start with <code>On</code> followed by an uppercase letter, or supply a usable <code>Name</code>. Note that a <code>Name</code> containing spaces is treated as a display label only, so the method still needs the <code>On</code> prefix.",
            """
            [BlexAction]
            private void Increment() { }              // BLEX002 -- no "On" prefix

            [BlexAction(Name = "Apply preset")]
            private void ApplyPreset() { }            // BLEX002 -- label only, still no "On"
            """,
            """
            [BlexAction]
            private void OnIncrement() { }            // -> Increment()

            [BlexAction(Name = "Bump")]
            private void Increment() { }              // -> Bump()   (valid identifier)

            [BlexAction(Name = "Apply preset")]
            private void OnApplyPreset() { }          // -> ApplyPreset(), labelled "Apply preset"
            """),

        new("BLEX003", "Cannot derive computed name", true,
            "A computed method's name must start with <code>Compute</code> or <code>Get</code> and have something after it.",
            """
            [BlexComputed]
            private int DoubleCount() => Count * 2;   // BLEX003
            """,
            """
            [BlexComputed]
            private int ComputeDoubleCount() => Count * 2;   // -> DoubleCount

            [BlexComputed]
            private int GetDoubleCount() => Count * 2;       // -> DoubleCount
            """),

        new("BLEX004", "Computed method must be parameterless", true,
            "A computed value backs a property, so there is nowhere to pass arguments. Use an ordinary method for a parameterised projection.",
            """
            [BlexComputed]
            private int ComputeTotalFor(int rate) => Count * rate;   // BLEX004
            """,
            """
            [BlexComputed]
            private int ComputeTotal() => Count * Rate;   // rate lives in [BlexState]

            // ... or just a plain method, with no [BlexComputed] and no memoization:
            private int TotalFor(int rate) => Count * rate;
            """),

        new("BLEX005", "Generated member name collides", true,
            "Two generated members would share a name, or a generated member would collide with something you already declared or with a <code>BlexStoreBase</code> member. The check covers the memoization backing fields (<code>__XxxValid</code>, <code>__XxxValue</code>) and the effect fields (<code>__XxxPending</code>, <code>__XxxError</code>, <code>__XxxCts</code>, …) too.",
            """
            [BlexStore]
            public partial class Store
            {
                [BlexState] private int _count;

                [BlexComputed] private int ComputeCount() => 0;   // BLEX005 -- both want "Count"

                [BlexAction] private void OnBatch() { }           // BLEX005 -- BlexStoreBase.Batch
            }
            """,
            """
            [BlexStore]
            public partial class Store
            {
                [BlexState] private int _count;

                [BlexComputed] private int ComputeDoubled() => Count * 2;

                [BlexAction] private void OnApplyAll() { }
            }
            """),

        new("BLEX006", "Unsupported store class shape", true,
            "The generator writes a flat, top-level partial declaration, so nested and generic stores would produce broken output.",
            """
            public class Outer
            {
                [BlexStore]
                public partial class Inner { }        // BLEX006 -- nested
            }

            [BlexStore]
            public partial class Cache<T> { }         // BLEX006 -- generic
            """,
            """
            [BlexStore]
            public partial class Inner { }            // top level

            [BlexStore]
            public partial class StringCache { }      // closed over a concrete type
            """),

        new("BLEX007", "Effect method must return Task or ValueTask", true,
            "An effect owns the loading and error lifecycle, so it cannot hand back a result. Non-generic <code>Task</code> or <code>ValueTask</code> only.",
            """
            [BlexEffect]
            private async Task<User> OnLoadUser(int id) => await _api.GetAsync(id);   // BLEX007

            [BlexEffect]
            private void OnLoadUser(int id) { }                                       // BLEX007
            """,
            """
            [BlexEffect]
            private async Task OnLoadUser(int id)
            {
                User = await _api.GetAsync(id);   // results go into [BlexState]
            }
            """),

        new("BLEX008", "Blex member must be a writable instance member", true,
            "<code>[BlexState]</code>, <code>[BlexComputed]</code>, <code>[BlexAction]</code> and <code>[BlexEffect]</code> members are per-instance and must be assignable. <code>static</code>, <code>const</code> and <code>readonly</code> are rejected.",
            """
            [BlexState] private static int _count;      // BLEX008
            [BlexState] private readonly int _max = 10; // BLEX008

            [BlexAction] private static void OnReset() { }   // BLEX008
            """,
            """
            [BlexState] private int _count;

            // Constants that are not state need no attribute:
            private const int Max = 10;

            [BlexAction] private void OnReset() => Count = 0;
            """),

        new("BLEX009", "Latest effect cannot cancel without a CancellationToken", false,
            "<code>Latest</code> marks the superseded run stale so it cannot overwrite newer state - but without a token the superseded body keeps running to completion, because nothing can observe the cancellation.",
            """
            [BlexEffect(Concurrency = BlexEffectConcurrency.Latest)]
            private async Task OnSearch(string query)          // BLEX009 (warning)
                => Results = await _api.SearchAsync(query);
            """,
            """
            [BlexEffect(Concurrency = BlexEffectConcurrency.Latest)]
            private async Task OnSearch(string query, CancellationToken ct)
                => Results = await _api.SearchAsync(query, ct);
            """),

        new("BLEX010", "Action must not be 'async void'", true,
            "An <code>async void</code> method cannot be awaited, so the dispatch pipeline cannot know when it finished or record it correctly.",
            """
            [BlexAction]
            private async void OnSave() => await _api.SaveAsync();   // BLEX010
            """,
            """
            [BlexAction]
            private async Task OnSave() => await _api.SaveAsync();

            // ... or, if it has a loading/error lifecycle:
            [BlexEffect]
            private async Task OnSave() => await _api.SaveAsync();
            """),

        new("BLEX011", "Action return value is discarded", false,
            "The generated wrapper returns <code>void</code> (or <code>Task</code>), so any value the implementation returns is silently dropped.",
            """
            [BlexAction]
            private int OnIncrement() => ++Count;   // BLEX011 (warning) -- the caller never sees it
            """,
            """
            [BlexAction]
            private void OnIncrement() => Count++;

            // Callers read the result from state:
            store.Increment();
            var current = store.Count;
            """),

        new("BLEX012", "State field name equals generated property name", true,
            "The property is derived by trimming a leading underscore and capitalizing. A field already in that form would collide with its own property.",
            """
            [BlexState] private int Count;    // BLEX012 -- the property would also be "Count"
            """,
            """
            [BlexState] private int _count;   // -> Count
            [BlexState] private int count;    // -> Count  (also fine)
            """),

        new("BLEX013", "Unsupported parameter modifier", true,
            "The wrapper captures arguments by value so they can travel as the action payload, which is impossible for a by-ref parameter.",
            """
            [BlexAction]
            private void OnTryParse(string text, out int value)   // BLEX013
            {
                int.TryParse(text, out value);
            }
            """,
            """
            [BlexAction]
            private void OnParse(string text)
                => Value = int.TryParse(text, out var value) ? value : 0;
            """),

        new("BLEX014", "Generic action/effect methods are not supported", true,
            "The generated wrapper cannot carry type parameters.",
            """
            [BlexAction]
            private void OnSet<T>(T value) { }        // BLEX014
            """,
            """
            [BlexAction]
            private void OnSetName(string value) => Name = value;

            [BlexAction]
            private void OnSetCount(int value) => Count = value;
            """),

        new("BLEX015", "Store must be a plain class", true,
            "Records get value-equality members and a synthesized clone, which fight the mutable reactive-container model a store needs.",
            """
            [BlexStore]
            public partial record CounterStore        // BLEX015
            {
                [BlexState] private int _count;
            }
            """,
            """
            [BlexStore]
            public partial class CounterStore
            {
                [BlexState] private int _count;
            }

            // Records are ideal for the values *inside* state:
            public sealed record Todo(int Id, string Title, bool Done);
            """),

        new("BLEX016", "Store cannot have another base class", true,
            "The generated partial adds <code>: Bit.Blex.BlexStoreBase</code>. A different base class would make the type inherit from two classes.",
            """
            [BlexStore]
            public partial class CounterStore : MyBaseViewModel   // BLEX016
            {
                [BlexState] private int _count;
            }
            """,
            """
            // Compose instead of inheriting -- or implement an interface, which is fine.
            [BlexStore]
            public partial class CounterStore : ICounter
            {
                private readonly MyHelper _helper = new();
                [BlexState] private int _count;
            }

            // Explicitly deriving from BlexStoreBase is also accepted:
            [BlexStore]
            public partial class CounterStore : BlexStoreBase { }
            """),

        new("BLEX017", "Computed method must return a value", true,
            "A computed backs a memoized property, so there has to be something to memoize; a <code>void</code> method would emit <code>public void Xxx { get; }</code>, which does not compile.",
            """
            [BlexComputed]
            private void ComputeSummary() { }         // BLEX017
            """,
            """
            [BlexComputed]
            private string ComputeSummary() => $"{Count} items";

            // Something that only performs work (no value) is an action, not a computed:
            [BlexAction]
            private void OnRefreshSummary() { }
            """),

        new("BLEX018", "Blex member attribute outside a store", false,
            "A <code>[BlexState]</code>, <code>[BlexComputed]</code>, <code>[BlexAction]</code> or <code>[BlexEffect]</code> member only means something inside a <code>[BlexStore]</code> class. Outside one it compiles cleanly and generates nothing at all - which reads exactly like a broken generator, so this warning says so instead.",
            """
            // Forgot the [BlexStore] -- Count never appears.
            public partial class CounterStore
            {
                [BlexState] private int _count;    // BLEX018
            }

            // Same for a type nested inside a store: the nested type is not itself a store.
            [BlexStore]
            public partial class OuterStore
            {
                [BlexState] private int _count;
                public class Inner { [BlexState] private int _x; }   // BLEX018
            }
            """,
            """
            [BlexStore]
            public partial class CounterStore
            {
                [BlexState] private int _count;
            }
            """),

        new("BLEX019", "Effect debounce cannot be negative", true,
            "<code>DebounceMilliseconds</code> becomes a <code>Task.Delay</code> in the generated wrapper, which rejects a negative delay at runtime - from inside generated code, where the stack trace explains nothing. Use <code>0</code> to disable debouncing.",
            """
            [BlexEffect(DebounceMilliseconds = -1)]      // BLEX019
            private async Task OnSearch(string query) { }
            """,
            """
            [BlexEffect(DebounceMilliseconds = 300)]
            private async Task OnSearch(string query) { }

            // 0 (the default) means "no debouncing":
            [BlexEffect]
            private async Task OnSearch(string query) { }
            """),
    ];

    /// <summary>The entry for one diagnostic id (case-insensitive), or <c>null</c>.</summary>
    public static BlexDiagnosticDoc? Find(string? code)
        => All.FirstOrDefault(d => string.Equals(d.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));
}

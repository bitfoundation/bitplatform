using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Butil.Build;

/// <summary>
/// Which JavaScript module each <c>Bit.Butil</c> type needs, worked out from the library's own assembly.
/// </summary>
/// <remarks>
/// The publish-time trimming has one signal when ILLink runs - the interop identifiers left in the trimmed
/// <c>Bit.Butil.dll</c>'s string heap - and needs a different one when it does not, because an untrimmed
/// <c>Bit.Butil.dll</c> carries every identifier the library has. That other signal is the consumer's own
/// assemblies: an app that injects <c>Clipboard</c> names the type, and this map says which module answering
/// that type takes.
/// <br/>
/// It cannot be a list of "the type whose name matches the module": <c>LocalStorage</c> carries no
/// identifiers at all - they live on the <c>ButilStorage</c> base class it inherits - and <c>Window</c>
/// reaches the <c>events</c> module only through an internal interop class it calls. So the map is a
/// closure rather than a lookup: a type needs the modules its own method bodies name, plus those of every
/// type inside Bit.Butil it can reach through a base type, a call, or a field.
/// <br/>
/// Over-inclusion is the safe direction and the direction this errs in. A type that merely mentions another
/// takes on its modules whether or not the path is ever walked at runtime, which costs a consumer some
/// JavaScript it does not need; the opposite - a module left out of a bundle the app does call into - is
/// <c>BitButil.x is undefined</c> in a browser, long after publishing.
/// </remarks>
public sealed class ButilTypeModules
{
    /// <summary>The namespace every public Bit.Butil type sits in, and the prefix its nested ones start with.</summary>
    public const string Namespace = "Bit.Butil";

    private const int TypeDefTable = 0x02;
    private const int FieldTable = 0x04;
    private const int MethodDefTable = 0x06;
    private const int MemberRefTable = 0x0A;
    private const int MethodSpecTable = 0x2B;
    private const int UserStringTable = 0x70;

    private readonly Dictionary<string, SortedSet<string>> _byFullName = new(StringComparer.Ordinal);

    private readonly Dictionary<string, SortedSet<string>> _byName = new(StringComparer.Ordinal);

    private ButilTypeModules()
    {
    }

    /// <summary>Every Bit.Butil type name (unqualified) that needs at least one module.</summary>
    public IEnumerable<string> TypeNames => _byName.Keys;

    /// <summary>Every full type name that needs at least one module, sorted.</summary>
    public IEnumerable<string> FullTypeNames => _byFullName.Keys.OrderBy(name => name, StringComparer.Ordinal);

    /// <summary>True when the map came out empty, which means the assembly read was not Bit.Butil.</summary>
    public bool IsEmpty => _byFullName.Count == 0;

    /// <summary>The modules a type needs, by its full name. Empty for a type that calls no JavaScript.</summary>
    public IReadOnlyCollection<string> ForFullName(string fullName)
        => _byFullName.TryGetValue(fullName, out var modules) ? modules : (IReadOnlyCollection<string>)Array.Empty<string>();

    /// <summary>
    /// The modules a type needs, by its unqualified name. Two Bit.Butil types can share one - a nested type
    /// and a top-level one - and the answer is then the union, which is the safe way round.
    /// </summary>
    public IReadOnlyCollection<string> ForName(string name)
        => _byName.TryGetValue(name, out var modules) ? modules : (IReadOnlyCollection<string>)Array.Empty<string>();

    /// <summary>
    /// Reads <c>Bit.Butil.dll</c> and works out the map. The assembly must be the untrimmed one the package
    /// ships: a trimmed copy has already had the identifiers of everything the app does not call removed,
    /// which is a different question with a different answer.
    /// </summary>
    /// <exception cref="System.BadImageFormatException">The file is not a managed assembly, or is malformed.</exception>
    /// <exception cref="System.IO.IOException">The file could not be read.</exception>
    public static ButilTypeModules Build(string butilAssemblyPath) => Build(PeImage.Load(butilAssemblyPath));

    public static ButilTypeModules Build(PeImage image)
    {
        var tables = MetadataTables.Read(image);
        var typeCount = tables.TypeDefCount;

        var map = new ButilTypeModules();
        if (typeCount == 0) return map;

        // Which type owns each method and each field, so a call or a field access can be turned into an edge
        // between two types.
        var methodOwner = new int[tables.MethodDefCount + 1];
        var fieldOwner = new int[tables.RowCount(FieldTable) + 1];
        for (var type = 1; type <= typeCount; type++)
        {
            var (firstMethod, endMethod) = tables.MethodRange(type);
            for (var method = firstMethod; method < endMethod; method++) methodOwner[method] = type;

            var (firstField, endField) = tables.FieldRange(type);
            for (var field = firstField; field < endField; field++) fieldOwner[field] = type;
        }

        var modules = new SortedSet<string>[typeCount + 1];
        var users = new List<int>[typeCount + 1];
        for (var type = 1; type <= typeCount; type++)
        {
            modules[type] = new SortedSet<string>(StringComparer.Ordinal);
            users[type] = [];
        }

        // A type reaches whatever the types nested inside it reach. This is not a nicety about nested public
        // types: the body of an async method, an iterator or a lambda is compiled into a type nested inside
        // the one it was written in, so the interop calls of - for one - Window.SubscribeEvent live in a
        // nested state machine and in nothing the method itself names.
        for (var row = 1; row <= tables.NestedClassCount; row++)
        {
            var (nested, enclosing) = tables.NestedClass(row);
            if (nested >= 1 && nested <= typeCount && enclosing >= 1 && enclosing <= typeCount && nested != enclosing) users[nested].Add(enclosing);
        }

        // Modules flow from a type to the types that can reach it, so the edges are recorded the other way
        // round from how they are found: "t uses u" is stored as "u is used by t".
        var used = new HashSet<int>();

        void AddMemberRef(int row)
        {
            if (row < 1 || row > tables.MemberRefCount) return;

            var parent = tables.MemberRefParent(row);
            if (parent.Table == TypeDefTable && parent.IsNil is false) used.Add(parent.Row);
        }

        for (var type = 1; type <= typeCount; type++)
        {
            used.Clear();

            var definition = tables.TypeDef(type);
            if (definition.Extends.Table == TypeDefTable && definition.Extends.IsNil is false) used.Add(definition.Extends.Row);

            var (firstMethod, endMethod) = tables.MethodRange(type);
            for (var method = firstMethod; method < endMethod; method++)
            {
                var owner = type;
                MethodBody.ReadTokens(image, tables.MethodRva(method), (table, row) =>
                {
                    switch (table)
                    {
                        case UserStringTable:
                            if (ButilScriptBundler.TryGetModule(UserStringHeap.ReadAt(image, row), out var module)) modules[owner].Add(module);
                            break;

                        case TypeDefTable:
                            if (row >= 1 && row <= typeCount) used.Add(row);
                            break;

                        case MethodDefTable:
                            if (row >= 1 && row < methodOwner.Length) used.Add(methodOwner[row]);
                            break;

                        case FieldTable:
                            if (row >= 1 && row < fieldOwner.Length) used.Add(fieldOwner[row]);
                            break;

                        case MemberRefTable:
                            AddMemberRef(row);
                            break;

                        // A call to a generic method names a MethodSpec, not the method - so without this
                        // step every generic call inside the library is a reference nothing can follow.
                        case MethodSpecTable:
                            if (row >= 1 && row <= tables.MethodSpecCount)
                            {
                                var method = tables.MethodSpecMethod(row);
                                if (method.Table == MethodDefTable && method.Row >= 1 && method.Row < methodOwner.Length) used.Add(methodOwner[method.Row]);
                                else if (method.Table == MemberRefTable) AddMemberRef(method.Row);
                            }
                            break;
                    }
                });
            }

            foreach (var target in used)
            {
                if (target >= 1 && target <= typeCount && target != type) users[target].Add(type);
            }
        }

        // Closure by worklist rather than by recursion: the reference graph inside Bit.Butil holds cycles
        // (a service and the interop class it calls name each other), and a worklist reaches the same fixed
        // point without having to break them.
        var pending = new Queue<int>(Enumerable.Range(1, typeCount));
        var queued = new bool[typeCount + 1];
        for (var type = 1; type <= typeCount; type++) queued[type] = true;

        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            queued[type] = false;

            foreach (var user in users[type])
            {
                var before = modules[user].Count;
                modules[user].UnionWith(modules[type]);
                if (modules[user].Count == before || queued[user]) continue;

                queued[user] = true;
                pending.Enqueue(user);
            }
        }

        for (var type = 1; type <= typeCount; type++)
        {
            if (modules[type].Count == 0) continue;

            var name = tables.TypeDef(type).Name;

            // Types outside the library's own namespace are not types a consumer can name. A nested type
            // carries no namespace of its own (the metadata puts it on the enclosing type), so an empty one
            // is kept rather than skipped - and the compiler-generated classes that share that shape are
            // dropped by their names, which no consumer can write and every assembly has some of.
            if (name.Namespace.Length != 0
                && string.Equals(name.Namespace, Namespace, StringComparison.Ordinal) is false
                && name.Namespace.StartsWith(Namespace + ".", StringComparison.Ordinal) is false) continue;

            if (name.Name.IndexOf('<') >= 0 || name.Name.IndexOf('>') >= 0) continue;

            Add(map._byFullName, name.FullName, modules[type]);
            Add(map._byName, name.Name, modules[type]);
        }

        return map;
    }

    private static void Add(Dictionary<string, SortedSet<string>> into, string key, SortedSet<string> values)
    {
        if (into.TryGetValue(key, out var existing)) existing.UnionWith(values);
        else into[key] = new SortedSet<string>(values, StringComparer.Ordinal);
    }
}

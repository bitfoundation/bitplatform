using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

namespace Bit.Minifier.Tests;

public enum Level
{
    Default,
    Aggressive,
}

[TestClass]
public class AssemblyMinifierTests
{
    private const string Library = "Bit.Minifier.Tests.Library";
    private const string Friend = "Bit.Minifier.Tests.Friend";

    private string root = default!;
    private string original = default!;
    private string minified = default!;
    private readonly List<AssemblyLoadContext> contexts = [];

    [TestInitialize]
    public void Initialize()
    {
        root = Path.Combine(Path.GetTempPath(), "bit-minifier-tests", Guid.NewGuid().ToString("N"));
        original = Directory.CreateDirectory(Path.Combine(root, "original")).FullName;
        minified = Directory.CreateDirectory(Path.Combine(root, "minified")).FullName;
        foreach (var name in new[] { Library, Friend })
        {
            foreach (var extension in new[] { ".dll", ".pdb" })
            {
                File.Copy(Path.Combine(AppContext.BaseDirectory, name + extension), Path.Combine(original, name + extension));
                File.Copy(Path.Combine(AppContext.BaseDirectory, name + extension), Path.Combine(minified, name + extension));
            }
        }
        // an ILLink output folder holds the framework too; the minifier resolves from nowhere else
        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var name in new[] { "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Linq", "System.Runtime.InteropServices", "Microsoft.CSharp" })
        {
            File.Copy(Path.Combine(runtime, name + ".dll"), Path.Combine(minified, name + ".dll"));
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var context in contexts) context.Unload();
        contexts.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public void StripsCompileTimeAttributesButKeepsCompilerGeneratedOnTypes()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var members = module.GetTypes().SelectMany(t => t.Methods.Cast<ICustomAttributeProvider>().Concat(t.Fields).Concat(t.Properties)).ToList();
        Assert.IsFalse(members.Any(m => HasAttribute(m, "CompilerGeneratedAttribute")));
        Assert.IsFalse(module.GetTypes().Cast<ICustomAttributeProvider>().Concat(members).Any(m => HasAttribute(m, "NullableAttribute") || HasAttribute(m, "NullableContextAttribute")));
        Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).SelectMany(m => m.Parameters).Any(p => HasAttribute(p, "NotNullWhenAttribute")));

        // types keep it, whatever they are called by now: debuggers tell closures and state machines apart by it
        Assert.IsTrue(module.GetTypes().Any(t => t.IsNested && HasAttribute(t, "CompilerGeneratedAttribute")));
    }

    [TestMethod]
    public void KeepNullableKeepsWhatNullabilityInfoContextReads()
    {
        // reflection reads the properties by their public names, which aggressive takes away as well
        var expected = Nullability(original);

        Minify(keepNullable: true);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            Assert.IsTrue(HasNullableMetadata(module));
        }
        CollectionAssert.AreEqual(expected, Nullability(minified));

        List<string> Nullability(string directory)
        {
            var context = new NullabilityInfoContext();
            var (library, _) = Load(directory);
            return library.GetTypes().Where(t => t.IsPublic).OrderBy(t => t.FullName).SelectMany(t => t.GetProperties())
                .Select(p => $"{p.DeclaringType!.Name}.{p.Name}: {context.Create(p).ReadState}/{context.Create(p).WriteState}").ToList();
        }
    }

    [TestMethod]
    public void ShortensGeneratedNamesAndKeepsThePublicOnesTheyBelongTo()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);

        // the public names stay, and the generated members around them lose theirs
        string[] handWritten = ["AddAsync", "Doubles", "CountAsync", "Multiplier", "SumWhere", "Label", "Total"];
        foreach (var name in handWritten)
        {
            CollectionAssert.Contains(names, name);
            Assert.IsFalse(names.Any(n => n.StartsWith($"<{name}>", StringComparison.Ordinal)), $"<{name}> was not shortened");
        }

        // where a generated name survives, the kind marker survives with it, so debuggers still recognize it
        Assert.IsTrue(names.Any(n => n.EndsWith(">k__BackingField", StringComparison.Ordinal)));

        // hoisted locals and captured variables keep the names the debugger shows
        Assert.IsTrue(names.Contains("offset") || names.Any(n => n.StartsWith("<offset>", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.StartsWith("<before>5__", StringComparison.Ordinal)));
        Assert.IsTrue(names.Contains("<>4__this"));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public async Task MinifiedCodeBehavesLikeTheOriginal(Level level)
    {
        var expected = await Drive(original);
        // reflection by public names, which only the default level keeps
        var exercised = level == Level.Aggressive ? null : await Exercise(original);

        Minify(level);

        CollectionAssert.AreEqual(expected, await Drive(minified));
        if (exercised is not null) CollectionAssert.AreEqual(exercised, await Exercise(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void StackTracesKeepFilesAndLineNumbers(Level level)
    {
        var expected = FailureStackTrace(original);

        Minify(level);
        var actual = FailureStackTrace(minified);

        StringAssert.Contains(expected[0], "Calculator.cs:line");
        Assert.HasCount(expected.Length, actual);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(LineOf(expected[i]), LineOf(actual[i]));
        }
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public async Task OwnAssembliesKeepTheirNamesAndTheirStackTraces(Level level)
    {
        var expected = FailureStackTrace(original);
        var exercised = level == Level.Aggressive ? null : await Exercise(original);

        var results = Minify(level, own: [Library], full: [Friend]);

        using (var library = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(library);
            foreach (var name in new[] { "Calculator", "AddAsync", "Record", "ShiftAmount", "_history", "_limit" }) CollectionAssert.Contains(names, name);
            // the generated ones too: an async frame is named after the method it belongs to
            Assert.IsTrue(names.Any(n => n.StartsWith("<AddAsync>d__", StringComparison.Ordinal)));
            // what no stack trace shows still goes
            Assert.IsFalse(HasNullableMetadata(library));
        }
        Assert.AreEqual(0, results.Single(r => r.Name == Library).RenamedMembers);
        Assert.IsTrue(results.Single(r => r.Name == Library).RemovedAttributes > 0);
        Assert.IsTrue(results.Single(r => r.Name == Friend).RenamedMembers > 0);

        // the frames of the app's own code read as its source wrote them, and it all still runs
        CollectionAssert.AreEqual(expected, FailureStackTrace(minified));
        if (exercised is not null) CollectionAssert.AreEqual(exercised, await Exercise(minified));
    }

    [TestMethod]
    public void AFullyMinifiedLibraryIsMinifiedEvenWhenItIsBuiltFromSource()
    {
        // it is a package wherever it isn't built alongside the app, and safe for every rule either way
        var results = Minify(Level.Default, own: [Library, Friend], full: [Library]);

        Assert.IsTrue(results.Single(r => r.Name == Library).RenamedMembers > 0);
        Assert.AreEqual(0, results.Single(r => r.Name == Friend).RenamedMembers);
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void TheMapReadsAMinifiedStackTraceBack(Level level)
    {
        var mapFile = Path.Combine(root, "bit-minifier.map");
        var expected = FailureStackTrace(original);

        Minify(level, mapFile: mapFile);
        var decoded = MapDecoder.Load(mapFile).Decode(string.Join(Environment.NewLine, FailureStackTrace(minified)));

        // only the names come back: a parameter name aggressive cleared is in no map
        static string Frame(string line) => line[..line.IndexOf('(', StringComparison.Ordinal)] + line[line.LastIndexOf(" in ", StringComparison.Ordinal)..];
        CollectionAssert.AreEqual(expected.Select(Frame).ToList(), decoded.Split(Environment.NewLine).Select(Frame).ToList());
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void PdbStillDescribesTheMinifiedAssembly(Level level)
    {
        var before = ReadDebugInfo(original, Library);

        Minify(level);
        var after = ReadDebugInfo(minified, Library);

        Assert.IsTrue(after.IdMatches);
        // the dll names its pdb by file name, never by the folder it was written to first
        Assert.AreEqual(Library + ".pdb", after.PdbPath);
        Assert.AreEqual(before.SequencePoints, after.SequencePoints);
        Assert.AreEqual(before.MethodsWithSequencePoints, after.MethodsWithSequencePoints);
        Assert.AreEqual(0, after.SequencePointsOutsideBody);
        CollectionAssert.AreEquivalent(before.LocalNames, after.LocalNames);
        Assert.AreEqual(before.StateMachineMethods, after.StateMachineMethods);
    }

    [TestMethod]
    public void RenamesInternalsOnlyWhenEveryFriendIsMinified()
    {
        Minify(Level.Default, assemblies: [Library, Friend]);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            CollectionAssert.DoesNotContain(names, "Record");
            CollectionAssert.DoesNotContain(names, "_history");
            CollectionAssert.DoesNotContain(names, "InternalSquare");
            CollectionAssert.DoesNotContain(names, "InternalCounter");
            CollectionAssert.DoesNotContain(names, "Rounding");
            // protected and public members are API
            CollectionAssert.Contains(names, "Shift");
            CollectionAssert.Contains(names, "AddAsync");
        }
    }

    [TestMethod]
    public async Task KeepsInternalsWhenAFriendIsNotMinified()
    {
        Minify(Level.Default, assemblies: [Library]);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            CollectionAssert.DoesNotContain(names, "Record");
            CollectionAssert.Contains(names, "InternalSquare");
            CollectionAssert.Contains(names, "InternalCounter");
            // and the friend's nested internal type
            CollectionAssert.Contains(names, "Rounding");
        }

        CollectionAssert.AreEqual(await Exercise(original), await Exercise(minified));
    }

    [TestMethod]
    public void LeavesAnAssemblyAloneWhenItsNewNamesWouldBreakAReference()
    {
        // without the InternalsVisibleTo the minifier believes nobody else can see the internals the friend uses
        var libraryPath = Path.Combine(minified, Library + ".dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true, ReadSymbols = true }))
        {
            var ivt = assembly.CustomAttributes.Single(a => a.AttributeType.Name == "InternalsVisibleToAttribute");
            assembly.CustomAttributes.Remove(ivt);
            assembly.Write(libraryPath, new WriterParameters { WriteSymbols = true });
        }
        var before = Snapshot(minified);

        // the library is the assembly whose names broke it, so it is the one left alone - and it was the only one
        // asked for here, which leaves nothing to write
        var results = Minify(Level.Default, assemblies: [Library]);

        Assert.IsEmpty(results);
        var skipped = Assert.ContainsSingle(minifier.Skipped);
        StringAssert.Contains(skipped, Library);
        StringAssert.Contains(skipped, Friend);
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    public void MinifiesTheRestAroundAnAssemblyWhoseNewNamesWouldBreakAReference()
    {
        const string Extra = "Bit.Minifier.Tests.Extra";
        // an assembly of its own to minify, which the library and the friend know nothing about
        using (var extra = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Extra, new Version(1, 0)), Extra, ModuleKind.Dll))
        {
            var widget = new Mono.Cecil.TypeDefinition(Extra, "Widget", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Class, extra.MainModule.TypeSystem.Object);
            widget.Fields.Add(new Mono.Cecil.FieldDefinition("privateCounter", Mono.Cecil.FieldAttributes.Private, extra.MainModule.TypeSystem.Int32));
            extra.MainModule.Types.Add(widget);
            extra.Write(Path.Combine(minified, Extra + ".dll"));
        }
        // the unminified friend reaches the library's internals, and nothing says so any more: renaming them breaks it
        var libraryPath = Path.Combine(minified, Library + ".dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true, ReadSymbols = true }))
        {
            assembly.CustomAttributes.Remove(assembly.CustomAttributes.Single(a => a.AttributeType.Name == "InternalsVisibleToAttribute"));
            assembly.Write(libraryPath, new WriterParameters { WriteSymbols = true });
        }
        var library = Snapshot(minified).Single(f => f.StartsWith(Library + ".dll:", StringComparison.Ordinal));

        var results = Minify(Level.Default, assemblies: [Library, Extra]);

        // one assembly costing the whole publish its savings is what this must never do
        CollectionAssert.AreEqual(new[] { Extra }, results.Select(r => r.Name).ToArray());
        var skipped = Assert.ContainsSingle(minifier.Skipped);
        StringAssert.Contains(skipped, Library);
        StringAssert.Contains(skipped, Friend);
        CollectionAssert.Contains(Snapshot(minified), library);
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void PutsTheOriginalsBackWhenReplacingThemFails(Level level)
    {
        // only Windows refuses to move a file that is open
        if (OperatingSystem.IsWindows() is false) Assert.Inconclusive("Needs a file that can't be moved.");
        var map = Path.Combine(root, "bit-minifier.map");
        var before = Snapshot(minified);

        // the library is replaced first, then the friend's pdb can't be: an open handle still lets it be read, and
        // a file with one open on it is a file Windows won't move
        using (File.Open(Path.Combine(minified, Friend + ".pdb"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.Throws<IOException>(() => Minify(level, mapFile: map));
        }

        CollectionAssert.AreEqual(before, Snapshot(minified));
        Assert.IsFalse(File.Exists(map));
    }

    [TestMethod]
    public void SkipsAssembliesThatAreNotInTheFolder()
    {
        var before = Snapshot(minified);

        var results = Minify(assemblies: ["Not.There"]);

        Assert.IsEmpty(results);
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    public void ReportsSizesAndWritesTheMap()
    {
        var map = Path.Combine(root, "map", "bit-minifier.map");

        var results = Minify(assemblies: [Library, Friend], mapFile: map);

        var library = results.Single(r => r.Name == Library);
        Assert.IsLessThan(library.OriginalSize, library.MinifiedSize);
        Assert.IsGreaterThan(0, library.RemovedAttributes);
        Assert.IsGreaterThan(0, library.RenamedMembers);
        Assert.AreEqual(library.MinifiedSize, new FileInfo(Path.Combine(minified, Library + ".dll")).Length);

        var lines = File.ReadAllLines(map);
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Library}\tT\t", StringComparison.Ordinal) && l.Contains("<AddAsync>d__", StringComparison.Ordinal)));
        Assert.IsTrue(lines.All(l => l.Split('\t').Length == 4));
    }

    [TestMethod]
    public async Task MinifiesAsOftenAsItIsRun()
    {
        var expected = await Exercise(original);

        Minify();
        Minify();

        // a folder that is minified again is minified, not broken: the second pass reads what the first left
        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    public void RenamesWhatOnlyNullableMetadataIsLeftOn()
    {
        Minify(keepNullable: true);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        Assert.IsTrue(HasNullableMetadata(module));
        // nullable metadata doesn't make a name observable: these go just as they do without it
        foreach (var name in new[] { "Record", "Select", "_history", "Satchel`1", "Pallet" }) CollectionAssert.DoesNotContain(AllNames(module), name);
        var select = module.GetType("Bit.Minifier.Tests.Library.Box`1").Methods.Single(m => m.IsPrivate && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name.StartsWith("Func", StringComparison.Ordinal));
        Assert.AreEqual("", select.Parameters[0].Name);
    }

    [TestMethod]
    [DataRow(Level.Default, false)]
    [DataRow(Level.Default, true)]
    [DataRow(Level.Aggressive, true)]
    public async Task KeepsWhatTheDynamicBinderReads(Level level, bool dynamicPublished)
    {
        var expected = await Drive(original);
        if (dynamicPublished is false) File.Delete(Path.Combine(minified, "Microsoft.CSharp.dll"));

        Minify(level);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var parameters = module.GetTypes().SelectMany(t => t.Methods).SelectMany(m => m.Parameters.Cast<ICustomAttributeProvider>().Append(m.MethodReturnType)).ToList();
            // reflection's own binder reads [ParamArray] too (Type.InvokeMember, Activator.CreateInstance)
            Assert.IsTrue(parameters.Any(p => HasAttribute(p, "ParamArrayAttribute")));
            Assert.AreEqual(dynamicPublished, parameters.Any(p => HasAttribute(p, "DynamicAttribute")));
            // and [Extension], to find the extension methods of a dynamic receiver - the names are the level's
            if (level == Level.Default)
            {
                var describe = module.GetType("Bit.Minifier.Tests.Library.LedgerExtensions").Methods.Single(m => m.Name == "Describe");
                Assert.AreEqual(dynamicPublished, HasAttribute(describe, "ExtensionAttribute"));
            }
        }
        if (dynamicPublished) CollectionAssert.AreEqual(expected, await Drive(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void KeepsAnEmbeddedPdb(Level level)
    {
        var libraryPath = Path.Combine(minified, Library + ".dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true, ReadSymbols = true }))
        {
            assembly.Write(libraryPath, new WriterParameters { WriteSymbols = true, SymbolWriterProvider = new Mono.Cecil.Cil.EmbeddedPortablePdbWriterProvider() });
        }
        File.Delete(Path.ChangeExtension(libraryPath, ".pdb"));
        var expected = FailureStackTrace(original);

        var results = Minify(level);

        Assert.IsTrue(results.Any(r => r.Name == Library));
        Assert.IsFalse(File.Exists(Path.ChangeExtension(libraryPath, ".pdb")));
        using (var pe = new PEReader(File.OpenRead(libraryPath)))
        {
            Assert.IsTrue(pe.ReadDebugDirectory().Any(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb));
        }
        var actual = FailureStackTrace(minified);
        Assert.HasCount(expected.Length, actual);
        for (int i = 0; i < expected.Length; i++) Assert.AreEqual(LineOf(expected[i]), LineOf(actual[i]));
    }

    [TestMethod]
    public void RemovesTheMapOfAnEarlierRun()
    {
        var map = Path.Combine(root, "bit-minifier.map");
        File.WriteAllText(map, "stale");

        var results = Minify(assemblies: ["Not.There"], mapFile: map);

        Assert.IsEmpty(results);
        Assert.IsFalse(File.Exists(map));
    }

    [TestMethod]
    public async Task MinifiesEveryAssemblyInTheFolderWhenNoneIsNamed()
    {
        var expected = await Exercise(original);

        var results = new AssemblyMinifier(new MinifierOptions { Directory = minified }).Run();

        CollectionAssert.IsSubsetOf(new[] { Library, Friend }, results.Select(r => r.Name).ToList());
        // the runtime's ReadyToRun images can't be written back, so they are skipped
        CollectionAssert.DoesNotContain(results.Select(r => r.Name).ToList(), "System.Private.CoreLib");
        // a type-forwarding facade has no names of its own, but its assembly attributes go like everything else
        CollectionAssert.Contains(results.Select(r => r.Name).ToList(), "System.Runtime");
        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    public async Task KeepsBehaviourAndPublicNames()
    {
        var expected = await Exercise(original);
        var stackTrace = FailureStackTrace(original);

        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [] }).Run();

        CollectionAssert.AreEqual(expected, await Exercise(minified));
        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // public API, and what serializers see
            foreach (var name in new[] { "Calculator", "AddAsync", "Fail", "Label", "Total", "Point", "Length", "Box`1", "Shade" }) CollectionAssert.Contains(names, name);
            // everything else is short
            foreach (var name in new[] { "Record", "ShiftAmount", "ThrowFromHelper", "_history", "_limit", "Select" }) CollectionAssert.DoesNotContain(names, name);
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.IsPrivate && m.HasCustomAttributes is false).SelectMany(m => m.Parameters).Any(p => p.Name.Length > 0));
            // what the app itself may read of its own metadata stays: Assembly.GetCustomAttribute is an About page
            Assert.IsTrue(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "AssemblyCompanyAttribute"));
            Assert.IsTrue(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "AssemblyFileVersionAttribute"));
            // only on generic parameters, where DI compares them
            Assert.IsTrue(HasAttribute(module.GetType("Bit.Minifier.Tests.Library.Box`1").GenericParameters[0], "DynamicallyAccessedMembersAttribute"));
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).SelectMany(m => m.Parameters).Any(p => HasAttribute(p, "DynamicallyAccessedMembersAttribute")));
        }
        // the public frame keeps its name, and every frame its line
        var actual = FailureStackTrace(minified);
        Assert.HasCount(stackTrace.Length, actual);
        StringAssert.Contains(actual[1], "Calculator.Fail()");
        for (int i = 0; i < actual.Length; i++) Assert.AreEqual(LineOf(stackTrace[i]), LineOf(actual[i]));
    }

    [TestMethod]
    public void KeepsNamesStringLiteralsMention()
    {
        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [] }).Run();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        // "total is {0}" in Calculator.ThrowFromHelper names nothing, but GetMethod(nameof(ShiftAmount)) would: see Reflected
        CollectionAssert.Contains(AllNames(module), "ReflectedByName");
        CollectionAssert.Contains(AllNames(module), "_reflectedField");
        // a compiled model, say, reaches backing fields this way
        CollectionAssert.Contains(AllNames(module), "<Note>k__BackingField");
    }

    [TestMethod]
    public void BackingFieldsAStringNamesKeepTheirNames()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);
        CollectionAssert.Contains(names, "<Note>k__BackingField");
        CollectionAssert.DoesNotContain(names, "<Label>k__BackingField");
    }

    [TestMethod]
    public void OnlyAggressiveTouchesPublicNames()
    {
        var map = Path.Combine(root, "bit-minifier.map");

        Minify(mapFile: map);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);
        foreach (var name in new[] { "Calculator", "AddAsync", "Doubles", "Tally", "Shade", "GetAsync", "NoteAccess", "Shapes", "Mensuration", "Crate", "Count", "IComponent", "get_HistoryCount", "add_Added" })
        {
            CollectionAssert.Contains(names, name);
        }
        // namespaces, event metadata and generic parameter names stay
        Assert.IsTrue(module.Types.Where(t => t.Name.StartsWith('<') is false).All(t => t.Namespace.Length > 0));
        CollectionAssert.AreEquivalent(new[] { "Added", "Recorded" }, module.GetType("Bit.Minifier.Tests.Library.Calculator").Events.Select(e => e.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "T" }, module.GetType("Bit.Minifier.Tests.Library.Box`1").GenericParameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(module.GetTypes().SelectMany(t => t.GenericParameters).Any(p => p.Name == "TSelf"));
        // and so do public parameter names and the attributes only aggressive removes
        var count = module.GetType("Bit.Minifier.Tests.Shelf.Crate").Methods.Single(m => m.Name == "Count");
        CollectionAssert.AreEqual(new[] { "rows", "columns" }, count.Parameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(module.GetTypes().SelectMany(t => t.Methods).SelectMany(m => m.Parameters).Any(p => HasAttribute(p, "EnumeratorCancellationAttribute")));

        var lines = File.ReadAllLines(map).Select(l => l.Split('\t')).ToList();
        Assert.IsFalse(lines.Any(l => l[1] == "G"));
        // a renamed top-level type stays in its namespace
        foreach (var line in lines.Where(l => l[1] == "T" && l[2].Contains('/') is false))
        {
            Assert.AreEqual(NamespaceOf(line[2]), NamespaceOf(line[3]));
        }

        static string NamespaceOf(string name) => name.Contains('.') ? name[..name.LastIndexOf('.')] : "";
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void KeepsWhatResourceManagerReads(Level level)
    {
        Minify(level);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        Assert.IsTrue(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "NeutralResourcesLanguageAttribute"));
        // the internal type an embedded resource is named after keeps its full name
        Assert.AreEqual("Bit.Minifier.Tests.Library", module.GetTypes().Single(t => t.Name == "Glossary").Namespace);
        Assert.AreEqual("Bit.Minifier.Tests.Library.Glossary.txt", module.Resources.Single().Name);
    }

    [TestMethod]
    [DataRow(null, Level.Default)]
    [DataRow("--aggressive", Level.Aggressive)]
    public async Task CommandLinePicksTheLevel(string? option, Level level)
    {
        var expected = await Drive(original);
        string[] args = option is null ? [minified, Library, Friend] : [minified, option, Library, Friend];

        var (exitCode, output) = RunCommandLine(args);

        Assert.AreEqual(0, exitCode, output);
        StringAssert.Contains(output, "2 assemblies in");
        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // both levels take the internal names; only aggressive takes the public ones
            CollectionAssert.DoesNotContain(names, "Record");
            Assert.AreEqual(level == Level.Default, names.Contains("Calculator"));
        }
        CollectionAssert.AreEqual(expected, await Drive(minified));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("--aggressive")]
    public void MinifiesAFileThatIsNotNamedAfterTheAssemblyInIt(string? option)
    {
        // the folder holds files; nothing says a file carries the name of the assembly inside it
        const string file = "Renamed.Library";
        foreach (var extension in new[] { ".dll", ".pdb" })
        {
            File.Move(Path.Combine(minified, Library + extension), Path.Combine(minified, file + extension));
        }
        var before = new FileInfo(Path.Combine(minified, file + ".dll")).Length;
        string[] args = option is null ? [minified, file, Friend] : [minified, option, file, Friend];

        var (exitCode, output) = RunCommandLine(args);

        Assert.AreEqual(0, exitCode, output);
        // the file is the one replaced - and reading it a second time as an assembly of its own would have left
        // every reference into it pointing at that other copy
        StringAssert.Contains(output, "2 assemblies in");
        Assert.DoesNotContain("BITMIN001", output);
        Assert.IsLessThan(before, new FileInfo(Path.Combine(minified, file + ".dll")).Length);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, file + ".dll"));
        Assert.AreEqual(Library, module.Assembly.Name.Name);
        // the friend still reaches it: its reference names the assembly, which the file name never was
        using var friend = ModuleDefinition.ReadModule(Path.Combine(minified, Friend + ".dll"));
        CollectionAssert.Contains(friend.AssemblyReferences.Select(r => r.Name).ToList(), Library);
    }

    [TestMethod]
    [DataRow("--agressive")]
    // the level it named is the default now, and the switch is gone with it
    [DataRow("--super-aggressive")]
    [DataRow("--map")]
    [DataRow("--map --aggressive")]
    public void CommandLineRejectsWhatItDoesNotKnow(string options)
    {
        var before = Snapshot(minified);

        var (exitCode, output) = RunCommandLine([minified, .. options.Split(' ')]);

        Assert.AreEqual(2, exitCode);
        StringAssert.StartsWith(output, "usage: ");
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    public void TheSameFolderGivesTheSameNamesEveryTime()
    {
        var backup = Directory.CreateDirectory(Path.Combine(root, "backup")).FullName;
        foreach (var file in Directory.GetFiles(minified)) File.Copy(file, Path.Combine(backup, Path.GetFileName(file)));
        var first = Path.Combine(root, "first.map");
        var second = Path.Combine(root, "second.map");

        new AssemblyMinifier(new MinifierOptions { Directory = minified, Aggressive = true, MapFile = first }).Run();
        foreach (var file in Directory.GetFiles(minified)) File.Delete(file);
        foreach (var file in Directory.GetFiles(backup)) File.Copy(file, Path.Combine(minified, Path.GetFileName(file)));
        new AssemblyMinifier(new MinifierOptions { Directory = minified, Aggressive = true, MapFile = second }).Run();

        // nothing a publish produces may depend on the order a folder happens to be enumerated in: a map read
        // against a release has to be the map that release's own publish would have written, wherever it ran
        Assert.IsNotEmpty(File.ReadAllLines(first));
        CollectionAssert.AreEqual(File.ReadAllLines(first), File.ReadAllLines(second));
    }

    [TestMethod]
    [DataRow(new string[0], "usage: ")]
    [DataRow(new[] { "map", "trace", "extra" }, "usage: ")]
    [DataRow(new[] { "not-a-map" }, "no map at ")]
    public void CommandLineDecodeWantsAMapAndAtMostATrace(string[] rest, string expected)
    {
        var (exitCode, output) = RunCommandLine(["--decode", .. rest]);

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, expected);
    }

    [TestMethod]
    public void CommandLineDecodeWantsAStackTraceThatIsThere()
    {
        var map = Path.Combine(root, "bit-minifier.map");
        File.WriteAllLines(map, ["App\tT\tApp.Services.Worker\t_a"]);

        var (exitCode, output) = RunCommandLine(["--decode", map, Path.Combine(root, "not-a-trace")]);

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "no stack trace at ");
    }

    [TestMethod]
    public void CommandLineWantsTheDirectoryFirst()
    {
        var before = Snapshot(minified);

        var (exitCode, output) = RunCommandLine(["--aggressive", minified]);

        Assert.AreEqual(2, exitCode);
        StringAssert.StartsWith(output, "usage: ");
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("--aggressive")]
    public void CommandLineReportsAFailureAsAWarning(string? option)
    {
        var map = Path.Combine(root, "bit-minifier.map");
        var before = Snapshot(minified);
        // nothing to read at all: whatever goes wrong, the publish goes on with the trimmed assemblies
        var missing = Path.Combine(root, "not-a-folder");
        string[] args = option is null ? [missing, "--map", map] : [missing, "--map", map, option];

        var (exitCode, output) = RunCommandLine(args);

        // MSBuild shows it as a warning
        Assert.AreEqual(0, exitCode, output);
        StringAssert.StartsWith(output, "Bit.Minifier : warning BITMIN001: ");
        StringAssert.Contains(output, "The assemblies were left unminified.");
        CollectionAssert.AreEqual(before, Snapshot(minified));
        Assert.IsFalse(File.Exists(map));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("--aggressive")]
    public void CommandLineWarnsAboutAnAssemblyItCouldNotReadAndMinifiesTheRest(string? option)
    {
        // a pdb that can't be read costs that one assembly its names, not the whole publish its savings
        File.WriteAllBytes(Path.Combine(minified, Library + ".pdb"), [1, 2, 3]);
        var libraryBefore = new FileInfo(Path.Combine(minified, Library + ".dll")).Length;
        string[] args = option is null ? [minified, Library, Friend] : [minified, option, Library, Friend];

        var (exitCode, output) = RunCommandLine(args);

        Assert.AreEqual(0, exitCode, output);
        StringAssert.Contains(output, "warning BITMIN001: left unminified: " + Library);
        // the other one was minified, and the report counts it alone
        StringAssert.Contains(output, "1 assembly in");
        Assert.AreEqual(libraryBefore, new FileInfo(Path.Combine(minified, Library + ".dll")).Length);
    }

    [TestMethod]
    public void AggressiveRenamesPublicNamesToo()
    {
        var map = Path.Combine(root, "bit-minifier.map");

        Minify(Level.Aggressive, mapFile: map);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);
        foreach (var name in new[] { "Calculator", "AddAsync", "Doubles", "Multiplier", "SumWhere", "Tally", "Shade", "GetAsync", "NoteAccess", "Shapes", "Mensuration", "Record", "_history", "InternalSquare", "IComponent" })
        {
            CollectionAssert.DoesNotContain(names, name, name);
        }
        // types that attribute blobs name by string, too
        Assert.IsFalse(module.GetTypes().Any(t => t.Name is "Flavor" or "Satchel`1" or "Lattice"));
        Assert.IsTrue(module.GetTypes().Any(t => t.Name == "Knot"));
        // properties keep their names, their accessors don't: metadata ties them together
        var historyCount = module.GetTypes().SelectMany(t => t.Properties).Single(p => p.Name == "HistoryCount");
        Assert.AreNotEqual("get_HistoryCount", historyCount.GetMethod.Name);
        CollectionAssert.DoesNotContain(names, "get_HistoryCount");
        // the assembly metadata an app may read of itself, which only this level takes
        Assert.IsFalse(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name is "AssemblyCompanyAttribute" or "AssemblyFileVersionAttribute" or "AssemblyProductAttribute"));
        // except the version, the one an app has to have something to show
        Assert.IsTrue(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "AssemblyInformationalVersionAttribute"));
        // and the InternalsVisibleTo the runtime reads to let a friend touch an internal member
        Assert.IsTrue(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "InternalsVisibleToAttribute"));
        // event metadata goes, generic parameters get short names
        Assert.IsFalse(module.GetTypes().Any(t => t.HasEvents));
        Assert.IsFalse(module.GetTypes().SelectMany(t => t.GenericParameters).Any(p => p.Name == "TSelf"));
        // and the attributes only the compiler reads
        Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Any(m => HasAttribute(m, "EnumeratorCancellationAttribute") || m.Parameters.Any(p => HasAttribute(p, "EnumeratorCancellationAttribute"))));

        var lines = File.ReadAllLines(map);
        // a renamed type leaves its namespace behind, unless a string names it (Type.GetType("Bit.Minifier.Tests.Library.Plugin"))
        var crate = lines.Single(l => l.StartsWith($"{Library}\tT\tBit.Minifier.Tests.Shelf.Crate\t", StringComparison.Ordinal)).Split('\t')[3];
        Assert.IsFalse(crate.Contains('.'));
        Assert.AreEqual("", module.GetType(crate).Namespace);
        // public parameters lose their names, unless a string mentions them
        Assert.IsTrue(module.GetType(crate).Methods.Where(m => m.IsConstructor is false).SelectMany(m => m.Parameters).All(p => p.Name.Length == 0));
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Library}\tT\tBit.Minifier.Tests.Library.Calculator\tBit.Minifier.Tests.Library._", StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Library}\tG\t", StringComparison.Ordinal)));
        Assert.IsTrue(lines.All(l => l.Split('\t').Length == 4));
    }

    [TestMethod]
    public void AggressiveKeepsWhatIsReachedByName()
    {
        Minify(Level.Aggressive);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // named by a string (Type.GetType, GetMethod, nameof, a record's ToString), virtual, or a property
            foreach (var name in new[] { "Plugin", "Hello", "ReflectedByName", "_reflectedField", "Describe", "Point", "Dark", "Shift", "Label", "Total", "X", "Length", "<Note>k__BackingField" })
            {
                CollectionAssert.Contains(names, name);
            }
            Assert.AreEqual("Bit.Minifier.Tests.Library", module.GetTypes().Single(t => t.Name == "Plugin").Namespace);
            // Blazor components, and the types of their parameters, which a prerendering server names
            CollectionAssert.Contains(names, "Widget");
            CollectionAssert.Contains(names, "Palette");
            Assert.AreEqual("Bit.Minifier.Tests.Library", module.GetTypes().Single(t => t.Name == "Palette").Namespace);
            // reached by reflection conventions no string spells out
            foreach (var name in new[] { "Florin", "AbacusException", "ShouldSerializeMemo" }) CollectionAssert.Contains(names, name);
            // a public field an attribute's named argument sets: the blob names it
            CollectionAssert.Contains(names, "Heat");
            // a name a longer string mentions, in any script
            CollectionAssert.Contains(names, "Περίμετρος");
            Assert.AreEqual("multiplicand", module.GetTypes().SelectMany(t => t.Methods).Single(m => m.DeclaringType.IsInterface && m.Parameters.Count == 1 && m.ReturnType.MetadataType == MetadataType.Int32).Parameters[0].Name);
            // public instance fields go when nothing serializes fields by default
            CollectionAssert.DoesNotContain(names, "Tally");
        }
        using var friend = ModuleDefinition.ReadModule(Path.Combine(minified, Friend + ".dll"));
        CollectionAssert.Contains(AllNames(friend), "RunAsync");
    }

    [TestMethod]
    public void AggressiveKeepsPublicFieldsWhenNewtonsoftJsonIsPublished()
    {
        File.WriteAllBytes(Path.Combine(minified, "Newtonsoft.Json.dll"), []);

        Minify(Level.Aggressive);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        CollectionAssert.Contains(AllNames(module), "Tally");
    }

    [TestMethod]
    public void AggressiveKeepsTheMethodAnAsyncEntryPointLeadsTo()
    {
        // an exe's entry point is the generated <Main>, which the WebAssembly host follows to Main by name
        var libraryPath = Path.Combine(minified, Library + ".dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true, ReadSymbols = true }))
        {
            var wrapper = assembly.MainModule.GetType("Bit.Minifier.Tests.Library.Startup").Methods.Single(m => m.Name == "Wrapper");
            wrapper.Name = "<Main>";
            assembly.MainModule.EntryPoint = wrapper;
            assembly.Write(libraryPath, new WriterParameters { WriteSymbols = true });
        }

        Minify(Level.Aggressive);

        using var module = ModuleDefinition.ReadModule(libraryPath);
        var startup = module.EntryPoint.DeclaringType;
        Assert.AreEqual("<Main>", module.EntryPoint.Name);
        Assert.IsTrue(startup.Methods.Any(m => m.Name == "Main"));
    }

    [TestMethod]
    public async Task AggressiveKeepsThePublicNamesAnUnminifiedAssemblyUses()
    {
        var expected = await Exercise(original);
        var driven = await Drive(original);

        Minify(Level.Aggressive, assemblies: [Library]);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            foreach (var name in new[] { "Calculator", "AddAsync", "Tally", "InternalSquare" }) CollectionAssert.Contains(names, name);
            CollectionAssert.DoesNotContain(names, "Record");
        }
        CollectionAssert.AreEqual(expected, await Exercise(minified));
        CollectionAssert.AreEqual(driven, await Drive(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void RenamedMembersNeverTakeANameOfTheirOwnType(Level level)
    {
        Minify(level);

        foreach (var name in new[] { Library, Friend })
        {
            using var module = ModuleDefinition.ReadModule(Path.Combine(minified, name + ".dll"));
            foreach (var type in module.GetTypes())
            {
                // no two members of one type share a name, whatever their kind: reflection looks a name up, not a
                // kind. Overloads are the exception, and so is the field the compiler backs an event with.
                var events = type.Events.Select(e => e.Name).ToList();
                var own = type.Fields.Select(f => f.Name).Where(f => events.Contains(f) is false)
                    .Concat(events).Concat(type.Properties.Select(p => p.Name)).Concat(type.NestedTypes.Select(n => n.Name))
                    .Concat(type.Methods.Select(m => m.Name).Distinct()).ToList();
                var duplicates = own.GroupBy(m => m).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.IsEmpty(duplicates, $"{type.FullName}: {string.Join(", ", duplicates)}");

                // and none takes the name of a field, property or nested type of a type it inherits from, which
                // reflection finds through the derived type as well
                var data = type.Fields.Select(f => f.Name).Where(f => events.Contains(f) is false)
                    .Concat(type.Properties.Select(p => p.Name)).Concat(type.NestedTypes.Select(n => n.Name)).ToHashSet();
                for (var t = type.BaseType is null ? null : type.BaseType.Resolve(); t is not null; t = t.BaseType is null ? null : t.BaseType.Resolve())
                {
                    var inherited = t.Fields.Select(f => f.Name).Concat(t.Properties.Select(p => p.Name)).Concat(t.NestedTypes.Select(n => n.Name)).Where(data.Contains).ToList();
                    Assert.IsEmpty(inherited, $"{type.FullName} vs {t.FullName}: {string.Join(", ", inherited)}");
                }
            }
        }
    }

    [TestMethod]
    // in both orders, since the assemblies are minified one after the other: a base type may well be renamed
    // after the type that derives from it, and its assembly's references still spell the names it had
    [DataRow(Level.Default, false)]
    [DataRow(Level.Default, true)]
    [DataRow(Level.Aggressive, false)]
    [DataRow(Level.Aggressive, true)]
    public void RenamedMembersNeverTakeANameOfABaseTypeOfAnotherAssembly(Level level, bool friendFirst)
    {
        Minify(level, assemblies: friendFirst ? [Friend, Library] : [Library, Friend]);

        using var after = new Fixtures(minified);
        using var before = new Fixtures(original);
        // renaming adds and reorders nothing, so the fields, methods and nested types of the minified assemblies
        // line up with the originals one by one - which is what says whether a name is a rename's doing
        var was = new Dictionary<IMemberDefinition, string>();
        foreach (var (type, source) in after.Types.Zip(before.Types))
        {
            foreach (var (member, earlier) in Renamable(type).Zip(Renamable(source))) was[member] = earlier.Name;
        }

        foreach (var type in after.Types)
        {
            for (var t = type.BaseType?.Resolve(); t is not null; t = t.BaseType?.Resolve())
            {
                var inherited = Members(t).ToLookup(m => m.Name);
                foreach (var member in Members(type))
                {
                    var shared = inherited[member.Name].ToList();
                    // a name both sides had all along - an override, an overload, a constructor - is nobody's rename
                    if (shared.Count == 0 || shared.Any(b => Was(b) == Was(member))) continue;
                    Assert.Fail($"{type.FullName}.{member.Name} (was {Was(member)}) takes a name of {t.FullName} (was {string.Join(", ", shared.Select(Was))})");
                }
            }
        }

        string Was(IMemberDefinition member) => was.GetValueOrDefault(member, member.Name);

        // properties and events are only ever removed, never renamed, so they need no counterpart to line up with
        static IEnumerable<IMemberDefinition> Renamable(Mono.Cecil.TypeDefinition type)
            => type.Fields.Cast<IMemberDefinition>().Concat(type.Methods).Concat(type.NestedTypes);

        // generated names are a world of their own: they hold a '<', so no shortened name ever lands on one
        static IEnumerable<IMemberDefinition> Members(Mono.Cecil.TypeDefinition type)
            => Renamable(type).Concat(type.Properties).Concat(type.Events).Where(m => m.Name.StartsWith('<') is false);
    }

    [TestMethod]
    public void AggressiveKeepsWhatASatelliteAssemblyNames()
    {
        // the resources of a culture live in a folder of their own, and are named after the type they belong to
        var satellite = Path.Combine(Directory.CreateDirectory(Path.Combine(minified, "fa")).FullName, Library + ".resources.dll");
        using (var resources = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Library + ".resources", new Version(1, 0)), Library, ModuleKind.Dll))
        {
            resources.MainModule.Resources.Add(new EmbeddedResource("Bit.Minifier.Tests.Library.Ledger.resources", Mono.Cecil.ManifestResourceAttributes.Public, new byte[] { 1 }));
            resources.Write(satellite);
        }

        Minify(Level.Aggressive);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        CollectionAssert.Contains(AllNames(module), "Ledger");
    }

    [TestMethod]
    public void AggressiveKeepsThePublicNamesAnUnminifiedAssemblyReachesThroughAForwarder()
    {
        const string Facade = "Bit.Minifier.Tests.Facade";
        const string Reader = "Bit.Minifier.Tests.Reader";
        // the facade forwards a type of the library, and the reader only ever names the facade
        using (var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Facade, new Version(1, 0)), Facade, ModuleKind.Dll))
        {
            var library = new AssemblyNameReference(Library, new Version(1, 0));
            facade.MainModule.AssemblyReferences.Add(library);
            facade.MainModule.ExportedTypes.Add(new Mono.Cecil.ExportedType("Bit.Minifier.Tests.Library", "Ledger", facade.MainModule, library) { IsForwarder = true });
            facade.Write(Path.Combine(minified, Facade + ".dll"));
        }
        using (var reader = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Reader, new Version(1, 0)), Reader, ModuleKind.Dll))
        {
            var scope = new AssemblyNameReference(Facade, new Version(1, 0));
            reader.MainModule.AssemblyReferences.Add(scope);
            var holder = new Mono.Cecil.TypeDefinition("", "Holder", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Class, reader.MainModule.TypeSystem.Object);
            holder.Fields.Add(new Mono.Cecil.FieldDefinition("ledger", Mono.Cecil.FieldAttributes.Public, new Mono.Cecil.TypeReference("Bit.Minifier.Tests.Library", "Ledger", reader.MainModule, scope)));
            reader.MainModule.Types.Add(holder);
            reader.Write(Path.Combine(minified, Reader + ".dll"));
        }

        var results = Minify(Level.Aggressive, assemblies: [Library, Friend, Facade]);

        Assert.IsNotEmpty(results);
        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        // the reader reaches Ledger by name, so it keeps it, and the forwarder keeps leading there
        CollectionAssert.Contains(AllNames(module), "Ledger");
        using var forwarded = ModuleDefinition.ReadModule(Path.Combine(minified, Facade + ".dll"));
        Assert.AreEqual("Bit.Minifier.Tests.Library.Ledger", forwarded.ExportedTypes.Single().FullName);
    }

    /// <summary>The minifier of the last <see cref="Minify"/>, for what it has to say besides its results.</summary>
    private AssemblyMinifier minifier = default!;

    private IReadOnlyList<MinifiedAssembly> Minify(Level level = Level.Default, bool keepNullable = false, string[]? assemblies = null, string? mapFile = null, string[]? full = null, string[]? own = null)
    {
        minifier = new AssemblyMinifier(new MinifierOptions
        {
            Directory = minified,
            Assemblies = assemblies ?? [Library, Friend],
            FullyMinified = full ?? [Library, Friend],
            OwnAssemblies = own ?? [],
            Aggressive = level == Level.Aggressive,
            KeepNullable = keepNullable,
            MapFile = mapFile,
        });
        return minifier.Run();
    }

    private async Task<List<string>> Exercise(string directory)
    {
        var (library, friend) = Load(directory);
        var results = new List<string>();

        var calculator = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Calculator")!)!;
        results.Add($"add {await Call<int>(calculator, "AddAsync", 5)} {await Call<int>(calculator, "AddAsync", 2)}");
        results.Add($"history {Get(calculator, "HistoryCount")} total {Get(calculator, "Total")}");
        results.Add($"doubles {string.Join(",", ((IEnumerable)Invoke(calculator, "Doubles", 3)!).Cast<object>())}");
        results.Add($"count {string.Join(",", await Drain(Invoke(calculator, "CountAsync", 3, CancellationToken.None)!))}");
        results.Add($"multiplier {((Delegate)Invoke(calculator, "Multiplier", 3)!).DynamicInvoke(2)}");
        results.Add($"sum {Invoke(calculator, "SumWhere", new[] { 1, 2, 3 }, 2)}");
        var parse = new object?[] { "7", null };
        results.Add($"parse {calculator.GetType().GetMethod("TryParse")!.Invoke(calculator, parse)} {parse[1]}");
        calculator.GetType().GetProperty("Label")!.SetValue(calculator, "L");
        results.Add($"label {Get(calculator, "Label")}");
        results.Add($"reflection {Invoke(calculator, "CallByReflection")}");
        calculator.GetType().GetProperty("Note")!.SetValue(calculator, "noted");
        var noteAccess = library.GetType("Bit.Minifier.Tests.Library.NoteAccess")!;
        results.Add($"note {noteAccess.GetMethod("ReadThroughAccessor")!.Invoke(null, [calculator])} {noteAccess.GetMethod("ReadThroughReflection")!.Invoke(null, [calculator])}");

        var point = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Point")!, 3, 4)!;
        results.Add($"point {point} {Get(point, "Length")} {point.Equals(Activator.CreateInstance(point.GetType(), 3, 4))}");

        var box = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Box`1")!.MakeGenericType(typeof(string)))!;
        box.GetType().GetProperty("Value")!.SetValue(box, "boxed");
        results.Add($"box {await Call<string>(box, "GetAsync")}");
        results.Add($"shade {Enum.Parse(library.GetType("Bit.Minifier.Tests.Library.Shade")!, "Dark")}");
        results.Add($"flavors {library.GetType("Bit.Minifier.Tests.Library.Flavors")!.GetMethod("Describe")!.Invoke(null, null)}");
        results.Add($"expression {Invoke(Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Shapes")!)!, "ReadThroughExpression")}");
        results.Add($"generic math {library.GetType("Bit.Minifier.Tests.Library.Mensuration")!.GetMethod("MakeMeters")!.Invoke(null, [7])}");

        var friendCalculator = Activator.CreateInstance(friend.GetType("Bit.Minifier.Tests.Friend.FriendCalculator")!)!;
        results.Add($"friend square {Invoke(friendCalculator, "SquareThroughInternals", 3)}");
        results.Add($"friend add {await Call<int>(friendCalculator, "AddTwiceAsync", 2)}");
        results.Add($"friend sum {Invoke(friendCalculator, "SumWhere", new[] { 1, 2, 3 }, 2)}");
        results.Add($"friend half {Invoke(friendCalculator, "HalfThroughInternals", 9)}");
        return results;
    }

    // the tool's own entry point, as the MSBuild targets run it
    private static (int exitCode, string output) RunCommandLine(string[] args)
    {
        var (stdout, stderr) = (Console.Out, Console.Error);
        using var output = new StringWriter();
        Console.SetOut(output);
        Console.SetError(output);
        try
        {
            var exitCode = (int)typeof(AssemblyMinifier).Assembly.EntryPoint!.Invoke(null, [args])!;
            return (exitCode, output.ToString());
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
    }

    // through the driver, the one entry the aggressive level leaves the tests
    private async Task<List<string>> Drive(string directory)
        => await (Task<List<string>>)Driver(directory).GetMethod("RunAsync")!.Invoke(null, null)!;

    private string[] FailureStackTrace(string directory)
    {
        var stackTrace = (string)Driver(directory).GetMethod("FailureStackTrace")!.Invoke(null, null)!;
        return stackTrace.Split('\n').Select(l => l.Trim()).Where(l => l.Contains("Calculator.cs", StringComparison.Ordinal)).ToArray();
    }

    private Type Driver(string directory) => Load(directory).friend.GetType("Bit.Minifier.Tests.Friend.Driver", throwOnError: true)!;

    private static string LineOf(string frame) => frame[(frame.LastIndexOf(" in ", StringComparison.Ordinal) + 4)..];

    private (Assembly library, Assembly friend) Load(string directory)
    {
        var context = new DirectoryLoadContext(directory);
        contexts.Add(context);
        return (context.LoadFromAssemblyName(new AssemblyName(Library)), context.LoadFromAssemblyName(new AssemblyName(Friend)));
    }

    /// <summary>
    /// Both fixture assemblies of a folder, resolved against one another so that a base type in the other one
    /// is the very type definition this reads rather than a second copy of it.
    /// </summary>
    private sealed class Fixtures : IDisposable
    {
        private sealed class Resolver : DefaultAssemblyResolver
        {
            public void Register(AssemblyDefinition assembly) => RegisterAssembly(assembly);
        }

        private readonly Resolver resolver = new();
        private readonly List<AssemblyDefinition> assemblies = [];

        public Fixtures(string directory)
        {
            foreach (var path in resolver.GetSearchDirectories()) resolver.RemoveSearchDirectory(path);
            resolver.AddSearchDirectory(directory);
            foreach (var name in new[] { Library, Friend })
            {
                var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(directory, name + ".dll"), new ReaderParameters { InMemory = true, AssemblyResolver = resolver });
                resolver.Register(assembly);
                assemblies.Add(assembly);
            }
        }

        public List<Mono.Cecil.TypeDefinition> Types => assemblies.SelectMany(a => a.MainModule.GetTypes()).ToList();

        public void Dispose()
        {
            foreach (var assembly in assemblies) assembly.Dispose();
            resolver.Dispose();
        }
    }

    // the fixtures also sit next to the tests, so the default context must never be asked for them
    private sealed class DirectoryLoadContext(string directory) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name is not (Library or Friend)) return null;
            var path = Path.Combine(directory, name.Name + ".dll");
            var pdb = Path.ChangeExtension(path, ".pdb");
            using var assembly = new MemoryStream(File.ReadAllBytes(path));
            // an embedded pdb comes with the assembly
            using var symbols = File.Exists(pdb) ? new MemoryStream(File.ReadAllBytes(pdb)) : null;
            return LoadFromStream(assembly, symbols);
        }
    }

    private static object? Invoke(object target, string method, params object?[] arguments)
        => target.GetType().GetMethod(method)!.Invoke(target, arguments);

    private static object? Get(object target, string property) => target.GetType().GetProperty(property)!.GetValue(target);

    private static async Task<T> Call<T>(object target, string method, params object?[] arguments)
        => await (Task<T>)Invoke(target, method, arguments)!;

    private static async Task<List<int>> Drain(object asyncEnumerable)
    {
        var values = new List<int>();
        await foreach (var value in (IAsyncEnumerable<int>)asyncEnumerable) values.Add(value);
        return values;
    }

    private static List<string> AllNames(ModuleDefinition module)
        => module.GetTypes().SelectMany(t => new[] { t.Name }
            .Concat(t.Methods.Select(m => m.Name))
            .Concat(t.Fields.Select(f => f.Name))
            .Concat(t.Properties.Select(p => p.Name))).ToList();

    private static bool HasNullableMetadata(ModuleDefinition module)
        => module.GetTypes().Cast<ICustomAttributeProvider>()
            .Concat(module.GetTypes().SelectMany(t => t.Properties))
            .Concat(module.GetTypes().SelectMany(t => t.Methods))
            .Any(p => HasAttribute(p, "NullableAttribute") || HasAttribute(p, "NullableContextAttribute"));

    private static bool HasAttribute(ICustomAttributeProvider provider, string name)
        => provider.CustomAttributes.Any(a => a.AttributeType.Name == name);

    private static List<string> Snapshot(string directory)
        => Directory.GetFiles(directory).Order().Select(f => $"{Path.GetFileName(f)}:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f)))}").ToList();

    private static DebugInfo ReadDebugInfo(string directory, string name)
    {
        using var pe = new PEReader(File.OpenRead(Path.Combine(directory, name + ".dll")));
        var metadata = pe.GetMetadataReader();
        var codeView = pe.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
        var codeViewData = pe.ReadCodeViewDebugDirectoryData(codeView);

        using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.Combine(directory, name + ".pdb")));
        var pdb = provider.GetMetadataReader();
        var id = new BlobContentId(pdb.DebugMetadataHeader!.Id);

        int methods = 0, points = 0, outside = 0;
        foreach (var handle in pdb.MethodDebugInformation)
        {
            var info = pdb.GetMethodDebugInformation(handle);
            if (info.SequencePointsBlob.IsNil) continue;
            var definition = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(MetadataTokens.GetRowNumber(handle)));
            var size = definition.RelativeVirtualAddress == 0 ? 0 : pe.GetMethodBody(definition.RelativeVirtualAddress).Size;
            methods++;
            foreach (var point in info.GetSequencePoints())
            {
                points++;
                if (point.Offset >= size) outside++;
            }
        }

        var locals = pdb.LocalVariables.Select(h => pdb.GetString(pdb.GetLocalVariable(h).Name)).Order().ToList();
        return new DebugInfo(id.Guid == codeViewData.Guid && id.Stamp == codeView.Stamp, codeViewData.Path, methods, points, outside, locals, pdb.GetTableRowCount(TableIndex.StateMachineMethod));
    }

    private sealed record DebugInfo(bool IdMatches, string PdbPath, int MethodsWithSequencePoints, int SequencePoints, int SequencePointsOutsideBody, List<string> LocalNames, int StateMachineMethods);
}

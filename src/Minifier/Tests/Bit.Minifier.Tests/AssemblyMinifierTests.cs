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
    SuperAggressive,
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

        var generatedTypes = module.GetTypes().Where(t => t.Name.StartsWith('<') && t.IsNested).ToList();
        Assert.IsNotEmpty(generatedTypes);
        Assert.IsTrue(generatedTypes.All(t => HasAttribute(t, "CompilerGeneratedAttribute")));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void KeepNullableKeepsWhatNullabilityInfoContextReads(Level level)
    {
        var expected = Nullability(original);

        Minify(level, keepNullable: true);

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
    public void ShortensGeneratedNamesAndKeepsHandWrittenOnes()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);

        string[] handWritten = ["AddAsync", "Doubles", "CountAsync", "Multiplier", "SumWhere", "Record", "ShiftAmount", "ThrowFromHelper", "_history", "InternalCounter", "InternalSquare", "Label", "Total"];
        foreach (var name in handWritten)
        {
            CollectionAssert.Contains(names, name);
            Assert.IsFalse(names.Any(n => n.StartsWith($"<{name}>", StringComparison.Ordinal)), $"<{name}> was not shortened");
        }

        // the kind marker stays, so debuggers still recognize what each generated member is
        Assert.IsTrue(names.Any(n => n.StartsWith("<a>d__", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.EndsWith(">k__BackingField", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.Contains(">g__Twice|", StringComparison.Ordinal)));

        // hoisted locals and captured variables keep the names the debugger shows
        Assert.IsTrue(names.Contains("offset") || names.Any(n => n.StartsWith("<offset>", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.StartsWith("<before>5__", StringComparison.Ordinal)));
        Assert.IsTrue(names.Contains("<>4__this"));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
    public async Task MinifiedCodeBehavesLikeTheOriginal(Level level)
    {
        var expected = await Drive(original);
        // reflection by public names, which only the lower levels keep
        var exercised = level == Level.SuperAggressive ? null : await Exercise(original);

        Minify(level);

        CollectionAssert.AreEqual(expected, await Drive(minified));
        if (exercised is not null) CollectionAssert.AreEqual(exercised, await Exercise(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
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
        if (level == Level.Default) CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
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
    [DataRow(Level.SuperAggressive)]
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
    public void AggressiveRenamesInternalsOnlyWhenEveryFriendIsMinified()
    {
        Minify(Level.Aggressive, assemblies: [Library, Friend]);

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
    public async Task AggressiveKeepsInternalsWhenAFriendIsNotMinified()
    {
        Minify(Level.Aggressive, assemblies: [Library]);

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
    public void LeavesTheFolderUntouchedWhenAReferenceWouldBreak()
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

        var error = Assert.ThrowsExactly<MinifierException>(() => Minify(Level.Aggressive, assemblies: [Library]));

        StringAssert.Contains(error.Message, Friend);
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
    public void PutsTheOriginalsBackWhenReplacingThemFails(Level level)
    {
        // only Windows refuses to move a file that is open
        if (OperatingSystem.IsWindows() is false) Assert.Inconclusive("Needs a file that can't be moved.");
        var map = Path.Combine(root, "bit-minifier.map");
        var before = Snapshot(minified);

        // the library is replaced first, then the friend's pdb can't be
        using (File.Open(Path.Combine(minified, Friend + ".pdb"), FileMode.Open, FileAccess.Read, FileShare.None))
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
    public void MinifiesAsOftenAsItIsRun()
    {
        Minify();
        var once = Snapshot(minified);

        Minify();

        // a second pass finds nothing left to strip, and the short names map onto themselves
        CollectionAssert.AreEqual(once, Snapshot(minified));
    }

    [TestMethod]
    public async Task OtherLibrariesKeepWhatTheyMayReadAtRuntime()
    {
        var expected = await Exercise(original);

        Minify(full: []);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // EF Core finds backing fields by name, Newtonsoft.Json skips fields by [CompilerGenerated]
            CollectionAssert.Contains(names, "<Label>k__BackingField");
            Assert.IsTrue(module.GetTypes().SelectMany(t => t.Fields).Where(f => f.Name.EndsWith("k__BackingField", StringComparison.Ordinal)).All(f => HasAttribute(f, "CompilerGeneratedAttribute")));
            // the rest still goes
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Any(m => HasAttribute(m, "CompilerGeneratedAttribute")));
            Assert.IsFalse(names.Any(n => n.StartsWith("<AddAsync>", StringComparison.Ordinal)));
            Assert.IsFalse(HasNullableMetadata(module));
        }

        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
    public void KeepsWhatEfCoreReadsInOtherLibrariesWhenItIsPublished(Level level)
    {
        File.WriteAllBytes(Path.Combine(minified, "Microsoft.EntityFrameworkCore.dll"), []);

        Minify(level, full: [Friend]);

        // EF Core tells required columns apart by the nullable metadata, and finds backing fields by name
        using (var library = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            Assert.IsTrue(HasNullableMetadata(library));
            CollectionAssert.Contains(AllNames(library), "<Label>k__BackingField");
            CollectionAssert.Contains(AllNames(library), "_limit");
            // what EF Core doesn't read still goes
            if (level != Level.Default) CollectionAssert.DoesNotContain(AllNames(library), "_history");
        }
        using var friend = ModuleDefinition.ReadModule(Path.Combine(minified, Friend + ".dll"));
        Assert.IsFalse(friend.GetTypes().Cast<ICustomAttributeProvider>().Concat(friend.GetTypes().SelectMany(t => t.Methods)).Any(p => HasAttribute(p, "NullableContextAttribute")));
    }

    [TestMethod]
    public void AggressiveRenamesWhatOnlyNullableMetadataIsLeftOn()
    {
        Minify(Level.Aggressive, keepNullable: true);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        Assert.IsTrue(HasNullableMetadata(module));
        // nullable metadata doesn't make a name observable: these go just as they do without it
        foreach (var name in new[] { "Record", "Select", "_history", "Satchel`1", "Pallet" }) CollectionAssert.DoesNotContain(AllNames(module), name);
        var select = module.GetType("Bit.Minifier.Tests.Library.Box`1").Methods.Single(m => m.IsPrivate && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name.StartsWith("Func", StringComparison.Ordinal));
        Assert.AreEqual("", select.Parameters[0].Name);
    }

    [TestMethod]
    [DataRow(Level.Aggressive, false)]
    [DataRow(Level.Aggressive, true)]
    [DataRow(Level.SuperAggressive, true)]
    public async Task AggressiveKeepsWhatTheDynamicBinderReads(Level level, bool dynamicPublished)
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
        }
        if (dynamicPublished) CollectionAssert.AreEqual(expected, await Drive(minified));
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
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
        var facade = Snapshot(minified).Single(f => f.StartsWith("System.Runtime.dll:", StringComparison.Ordinal));

        var results = new AssemblyMinifier(new MinifierOptions { Directory = minified }).Run();

        CollectionAssert.IsSubsetOf(new[] { Library, Friend }, results.Select(r => r.Name).ToList());
        // the runtime's ReadyToRun images can't be written back, so they are skipped
        CollectionAssert.DoesNotContain(results.Select(r => r.Name).ToList(), "System.Private.CoreLib");
        // nothing to change in a type-forwarding facade, so it is not rewritten
        CollectionAssert.DoesNotContain(results.Select(r => r.Name).ToList(), "System.Runtime");
        CollectionAssert.Contains(Snapshot(minified), facade);
        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    public async Task AggressiveKeepsBehaviourAndPublicNames()
    {
        var expected = await Exercise(original);
        var stackTrace = FailureStackTrace(original);

        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [], Aggressive = true }).Run();

        CollectionAssert.AreEqual(expected, await Exercise(minified));
        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // public API, and what serializers see
            foreach (var name in new[] { "Calculator", "AddAsync", "Fail", "Label", "Total", "Point", "Length", "Box`1", "Shade" }) CollectionAssert.Contains(names, name);
            // everything else is short
            foreach (var name in new[] { "Record", "ShiftAmount", "ThrowFromHelper", "_history", "_limit", "Select" }) CollectionAssert.DoesNotContain(names, name);
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.IsPrivate && m.HasCustomAttributes is false).SelectMany(m => m.Parameters).Any(p => p.Name.Length > 0));
            Assert.IsFalse(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "AssemblyCompanyAttribute"));
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
    public void AggressiveKeepsNamesStringLiteralsMention()
    {
        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [], Aggressive = true }).Run();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        // "total is {0}" in Calculator.ThrowFromHelper names nothing, but GetMethod(nameof(ShiftAmount)) would: see Reflected
        CollectionAssert.Contains(AllNames(module), "ReflectedByName");
        CollectionAssert.Contains(AllNames(module), "_reflectedField");
        // EF Core's compiled model reaches backing fields this way
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
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    public void OnlySuperAggressiveTouchesPublicNames(Level level)
    {
        var map = Path.Combine(root, "bit-minifier.map");

        Minify(level, mapFile: map);

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
        // and so do public parameter names and the attributes only super aggressive removes
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
        // the default level renames generated names only
        if (level == Level.Default) Assert.IsTrue(lines.All(l => l[2].Split(["::", "/"], StringSplitOptions.None).Last().StartsWith('<')), string.Join(Environment.NewLine, lines.Select(l => l[2])));

        static string NamespaceOf(string name) => name.Contains('.') ? name[..name.LastIndexOf('.')] : "";
    }

    [TestMethod]
    [DataRow(Level.Default)]
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
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
    [DataRow("--super-aggressive", Level.SuperAggressive)]
    public async Task CommandLinePicksTheLevel(string? option, Level level)
    {
        var expected = await Drive(original);
        string[] args = option is null ? [minified, Library, Friend] : [minified, option, Library, Friend];

        var (exitCode, output) = RunCommandLine(args);

        Assert.AreEqual(0, exitCode, output);
        StringAssert.Contains(output, "Bit.Minifier: 2 assemblies");
        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            Assert.AreEqual(level == Level.Default, names.Contains("Record"));
            Assert.AreEqual(level != Level.SuperAggressive, names.Contains("Calculator"));
        }
        CollectionAssert.AreEqual(expected, await Drive(minified));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("--aggressive")]
    [DataRow("--super-aggressive")]
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
        // the file is the one replaced, so it is the one the tool writes and reports - and reading it a second
        // time as an assembly of its own would have left every reference into it pointing at that other copy
        StringAssert.Contains(output, $"Bit.Minifier: {file} ");
        StringAssert.Contains(output, "Bit.Minifier: 2 assemblies");
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
    [DataRow("--super-aggressive")]
    public void CommandLineReportsAFailureAsAWarning(string? option)
    {
        var map = Path.Combine(root, "bit-minifier.map");
        // the default level renames nothing another assembly can see, so only a file that can't be read fails it
        File.WriteAllBytes(Path.Combine(minified, Library + ".pdb"), [1, 2, 3]);
        var before = Snapshot(minified);
        string[] args = option is null ? [minified, "--map", map, Library, Friend] : [minified, "--map", map, option, Library, Friend];

        var (exitCode, output) = RunCommandLine(args);

        // MSBuild shows it as a warning, and the publish goes on with the trimmed assemblies
        Assert.AreEqual(0, exitCode, output);
        StringAssert.StartsWith(output, "Bit.Minifier : warning BITMIN001: ");
        StringAssert.Contains(output, "The assemblies were left unminified.");
        CollectionAssert.AreEqual(before, Snapshot(minified));
        Assert.IsFalse(File.Exists(map));
    }

    [TestMethod]
    public void SuperAggressiveRenamesPublicNamesToo()
    {
        var map = Path.Combine(root, "bit-minifier.map");

        Minify(Level.SuperAggressive, mapFile: map);

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
    public void SuperAggressiveKeepsWhatIsReachedByName()
    {
        Minify(Level.SuperAggressive);

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
    public void SuperAggressiveKeepsPublicFieldsWhenNewtonsoftJsonIsPublished()
    {
        File.WriteAllBytes(Path.Combine(minified, "Newtonsoft.Json.dll"), []);

        Minify(Level.SuperAggressive);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        CollectionAssert.Contains(AllNames(module), "Tally");
    }

    [TestMethod]
    public void SuperAggressiveKeepsTheMethodAnAsyncEntryPointLeadsTo()
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

        Minify(Level.SuperAggressive);

        using var module = ModuleDefinition.ReadModule(libraryPath);
        var startup = module.EntryPoint.DeclaringType;
        Assert.AreEqual("<Main>", module.EntryPoint.Name);
        Assert.IsTrue(startup.Methods.Any(m => m.Name == "Main"));
    }

    [TestMethod]
    public async Task SuperAggressiveKeepsThePublicNamesAnUnminifiedAssemblyUses()
    {
        var expected = await Exercise(original);
        var driven = await Drive(original);

        Minify(Level.SuperAggressive, assemblies: [Library]);

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
    [DataRow(Level.Aggressive)]
    [DataRow(Level.SuperAggressive)]
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
    [DataRow(Level.Aggressive, false)]
    [DataRow(Level.Aggressive, true)]
    [DataRow(Level.SuperAggressive, false)]
    [DataRow(Level.SuperAggressive, true)]
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
    public void SuperAggressiveKeepsWhatASatelliteAssemblyNames()
    {
        // the resources of a culture live in a folder of their own, and are named after the type they belong to
        var satellite = Path.Combine(Directory.CreateDirectory(Path.Combine(minified, "fa")).FullName, Library + ".resources.dll");
        using (var resources = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Library + ".resources", new Version(1, 0)), Library, ModuleKind.Dll))
        {
            resources.MainModule.Resources.Add(new EmbeddedResource("Bit.Minifier.Tests.Library.Ledger.resources", Mono.Cecil.ManifestResourceAttributes.Public, new byte[] { 1 }));
            resources.Write(satellite);
        }

        Minify(Level.SuperAggressive);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        CollectionAssert.Contains(AllNames(module), "Ledger");
    }

    [TestMethod]
    public void SuperAggressiveKeepsThePublicNamesAnUnminifiedAssemblyReachesThroughAForwarder()
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

        var results = Minify(Level.SuperAggressive, assemblies: [Library, Friend, Facade]);

        Assert.IsNotEmpty(results);
        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        // the reader reaches Ledger by name, so it keeps it, and the forwarder keeps leading there
        CollectionAssert.Contains(AllNames(module), "Ledger");
        using var forwarded = ModuleDefinition.ReadModule(Path.Combine(minified, Facade + ".dll"));
        Assert.AreEqual("Bit.Minifier.Tests.Library.Ledger", forwarded.ExportedTypes.Single().FullName);
    }

    private IReadOnlyList<MinifiedAssembly> Minify(Level level = Level.Default, bool keepNullable = false, string[]? assemblies = null, string? mapFile = null, string[]? full = null)
        => new AssemblyMinifier(new MinifierOptions
        {
            Directory = minified,
            Assemblies = assemblies ?? [Library, Friend],
            FullyMinified = full ?? [Library, Friend],
            Aggressive = level == Level.Aggressive,
            SuperAggressive = level == Level.SuperAggressive,
            KeepNullable = keepNullable,
            MapFile = mapFile,
        }).Run();

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

    // through the driver, the one entry the super aggressive level leaves the tests
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

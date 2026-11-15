using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that every component of every package has a root class of its own. The root class is also the prefix of every
/// class the component renders and styles (bit-xxx-*), so two components sharing one share every rule written for it:
/// BitStepper's flex row on bit-stp and bit-stp-hrz once reached the BitSwipeTrap inside BitNavPanel, and narrowed and
/// shifted the whole panel. A component that is a part of another one's family carries a root under that one's
/// prefix (BitGridItem's bit-grd-itm, every loader's bit-ldn-*), named in <see cref="IsFamilyPart"/>.
/// </summary>
/// <remarks>
/// The roots are read off <c>RootElementClass</c> for every <see cref="BitComponentBase"/>, and off the markup for the
/// components that write theirs there (<see cref="_markupRoots"/>). The stylesheets are read as well: a rule a
/// component's stylesheet writes under another component's prefix reaches that other component wherever it is
/// rendered, which is the same bleed as a shared root, so it is allowed only for a part of the component's family or
/// for a component it is built out of (<see cref="_stylesheetReach"/>).
/// </remarks>
[TestClass]
public class BitRootClassContractTests
{
    private static readonly string[] _packages = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.AI"];

    // A root that extends another component's root, and the component it is a part of. The loaders are listed by
    // IsFamilyPart: each one's root extends the bit-ldn of the BitLoading shell they all render into.
    private static readonly Dictionary<string, string> _familyParts = new()
    {
        ["BitGridItem"] = "BitGrid",
    };

    // The components that write their root class in their markup rather than through RootElementClass, with the file
    // that writes it: they are not BitComponentBase (BitErrorBoundary, the legacy data grid and its paginator), or are
    // the shell a family renders into (BitLoading, whose bit-ldn every loader's root carries beside its own).
    private static readonly (string Type, string Class, string[] Source)[] _markupRoots =
    [
        ("BitErrorBoundary", "bit-erb", ["Bit.BlazorUI.Extras", "Components", "ErrorBoundary", "BitErrorBoundary.razor.cs"]),
        ("BitLoading", "bit-ldn", ["Bit.BlazorUI", "Components", "Progress", "Loading", "Base", "BitLoadingBase.cs"]),
        ("BitDataGridLegacy", "bit-qkg", ["Bit.BlazorUI.Legacy", "Components", "DataGrid", "BitDataGridLegacy.razor.cs"]),
        ("BitDataGridLegacyPaginator", "bit-dgp", ["Bit.BlazorUI.Legacy", "Components", "DataGrid", "Pagination", "BitDataGridLegacyPaginator.razor"]),
    ];

    // The components whose classes a component's stylesheet may write rules for besides its own family's: the ones it
    // renders and restyles in place (the Extras and AI components are built out of core ones), or hosts in content it
    // lays out. Anything else under another component's prefix is a collision.
    private static readonly Dictionary<string, string[]> _stylesheetReach = new()
    {
        ["BitAccordionList"] = ["BitAccordion"],
        ["BitAiArtifactView"] = ["BitAiDataTable"],
        ["BitAiContextMeter"] = ["BitProgress"],
        ["BitAiMessageActions"] = ["BitAiChatMessage"],
        ["BitAiPromptInput"] = ["BitButton"],
        ["BitAvatarGroup"] = ["BitPersona"],
        ["BitCarousel"] = ["BitCarouselItem"],
        ["BitDialog"] = ["BitModal"],
        ["BitMessage"] = ["BitLink"],
        ["BitMessageBox"] = ["BitButton", "BitModal"],
        ["BitNavPanel"] = ["BitNav", "BitSearchBox"],
        ["BitPivot"] = ["BitPivotItem"],
        ["BitPopConfirm"] = ["BitCallout"],
        ["BitSwiper"] = ["BitSwiperItem"],
    };

    private static readonly Type[] _typeArguments = [typeof(object), typeof(int), typeof(string), typeof(double), typeof(DateTimeOffset)];

    // Discovering the roots constructs every component of the four packages, so it is done once for every test here.
    private static readonly Lazy<List<(string Type, string Class)>> _roots = new(ReadRootClasses);

    private static readonly Regex _stylesheetClass = new(@"\.(bit-[a-z0-9-]+)", RegexOptions.Compiled);

    [TestMethod]
    public void EveryComponentShouldHaveARootClassOfItsOwn()
    {
        var roots = _roots.Value;

        // Guards the discovery itself: a reflection change that found nothing would otherwise pass vacuously.
        Assert.IsTrue(roots.Count > 150, $"Only {roots.Count} root classes were discovered.");

        var failures = roots.GroupBy(r => r.Class)
                            .Where(g => g.Count() > 1)
                            .Select(g => $"{g.Key} is the root class of {string.Join(", ", g.Select(r => r.Type))}.")
                            .ToList();

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void NoRootClassShouldExtendAnotherComponentsUnlessItIsPartOfItsFamily()
    {
        var roots = _roots.Value;
        var failures = new List<string>();

        foreach (var (type, cls) in roots)
        {
            foreach (var (otherType, otherCls) in roots)
            {
                if (cls.StartsWith(otherCls + "-", StringComparison.Ordinal) is false) continue;
                if (IsFamilyPart(type, otherType)) continue;

                failures.Add($"{type}'s root class {cls} sits under the prefix of {otherType} ({otherCls}).");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void EveryMarkupRootShouldBeWrittenWhereItIsListed()
    {
        var failures = new List<string>();

        foreach (var (type, cls, source) in _markupRoots)
        {
            var text = SourceFiles.Read(source);

            // The class as a whole word of a literal: "bit-erb", $"bit-qkg {Class}", class="bit-dgp".
            if (Regex.IsMatch(text, $@"(?<=[""\s]){Regex.Escape(cls)}(?=[""\s])") is false)
            {
                failures.Add($"{type}'s root class {cls} is not written in {Path.Combine(source)}.");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void EveryStylesheetShouldBelongToAComponentWithAKnownRoot()
    {
        var known = _roots.Value.Select(r => r.Type).ToHashSet();

        // A component with a stylesheet of its own and a root the test cannot see would escape every check here: it
        // goes in _markupRoots.
        var failures = EnumerateComponentStylesheets().Select(s => s.Component)
                                                      .Distinct()
                                                      .Where(component => known.Contains(component) is false)
                                                      .Select(component => $"{component} has a stylesheet but no root class this test knows of; list it in _markupRoots.")
                                                      .ToList();

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void NoStylesheetShouldStyleAnotherComponentsClassesUnlessItBuildsOnIt()
    {
        var roots = _roots.Value;
        var failures = new SortedSet<string>(StringComparer.Ordinal);
        var stylesheets = 0;

        foreach (var (component, path) in EnumerateComponentStylesheets())
        {
            stylesheets++;
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(path));
            _stylesheetReach.TryGetValue(component, out var reach);

            foreach (Match match in _stylesheetClass.Matches(stylesheet))
            {
                // A class put together with an interpolation (.bit-ico-#{$name}) is read up to it.
                var cls = match.Groups[1].Value.TrimEnd('-');
                var owner = OwnerOf(cls, roots);

                if (owner is null || owner == component) continue;
                if (IsFamilyPart(owner, component) || IsFamilyPart(component, owner)) continue;
                if (reach is not null && reach.Contains(owner)) continue;

                failures.Add($"{Path.GetFileName(path)} styles .{cls}, a class of {owner}.");
            }
        }

        // Guards the discovery itself, as above.
        Assert.IsTrue(stylesheets > 150, $"Only {stylesheets} component stylesheets were discovered.");
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    private static bool IsFamilyPart(string part, string family)
    {
        if (_familyParts.TryGetValue(part, out var owner) && owner == family) return true;

        return family == "BitLoading" && _loaders.Value.Contains(part);
    }

    private static readonly Lazy<HashSet<string>> _loaders = new(() =>
        LoadableTypes.Of(typeof(BitLoadingBase).Assembly)
                     .Where(t => t.IsAbstract is false && typeof(BitLoadingBase).IsAssignableFrom(t))
                     .Select(NameOf)
                     .ToHashSet());

    // The component whose prefix a class is under: the one with the longest root the class is or extends, so a part of
    // a family (bit-grd-itm-spn) is BitGridItem's rather than BitGrid's.
    private static string? OwnerOf(string cls, List<(string Type, string Class)> roots)
    {
        (string Type, string Class)? best = null;

        foreach (var root in roots)
        {
            if (cls != root.Class && cls.StartsWith(root.Class + "-", StringComparison.Ordinal) is false) continue;
            if (best is null || root.Class.Length > best.Value.Class.Length) best = root;
        }

        return best?.Type;
    }

    // Every stylesheet under a package's Components folder, with the component it is kept for: the one of its file name.
    private static IEnumerable<(string Component, string Path)> EnumerateComponentStylesheets()
    {
        foreach (var package in _packages)
        {
            var folder = SourceFiles.GetDirectory(SourceFiles.GetProjectFolder(package), "Components");

            foreach (var path in Directory.EnumerateFiles(folder, "*.scss", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(path);

                yield return (name[..name.IndexOf('.')], path);
            }
        }
    }

    private static List<(string Type, string Class)> ReadRootClasses()
    {
        var assemblies = new[] { typeof(BitButton).Assembly, typeof(BitAppShell).Assembly, typeof(Bit.BlazorUI.Legacy.BitRichTextEditorLegacy).Assembly, typeof(BitAiChat).Assembly };
        var property = typeof(BitComponentBase).GetProperty("RootElementClass", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var roots = new List<(string, string)>();

        foreach (var type in assemblies.SelectMany(LoadableTypes.Of))
        {
            if (type.IsAbstract || typeof(BitComponentBase).IsAssignableFrom(type) is false) continue;

            var closed = Close(type);
            Assert.IsNotNull(closed, $"{type.Name} could not be closed over any of the test's type arguments.");

            // A constructed instance runs the property initializers (BitCalendar's root is one); a constructor that
            // cannot run without a host falls back to an instance with none of them.
            object instance;
            try
            {
                instance = Activator.CreateInstance(closed)!;
            }
            catch (TargetInvocationException)
            {
                instance = RuntimeHelpers.GetUninitializedObject(closed);
            }

            var cls = (string?)property.GetValue(instance);
            Assert.IsFalse(string.IsNullOrWhiteSpace(cls), $"{type.Name} has no root class.");

            roots.Add((NameOf(type), cls!));
        }

        roots.AddRange(_markupRoots.Select(r => (r.Type, r.Class)));

        return roots;
    }

    private static Type? Close(Type type)
    {
        if (type.IsGenericTypeDefinition is false) return type;

        // A parameter constrained to a type of the library (BitMap's provider) is tried with the concrete types that
        // satisfy its constraints, ahead of the plain ones.
        IEnumerable<Type[]> combinations = [[]];
        foreach (var parameter in type.GetGenericArguments())
        {
            var constraints = parameter.GetGenericParameterConstraints().Where(c => c != typeof(ValueType)).ToArray();
            var candidates = constraints.Length == 0
                ? _typeArguments
                : LoadableTypes.Of(constraints[0].Assembly)
                    .Where(t => t.IsAbstract is false && t.IsGenericTypeDefinition is false && constraints.All(c => c.IsAssignableFrom(t)))
                    .Concat(_typeArguments)
                    .ToArray();

            combinations = combinations.SelectMany(c => candidates.Select(t => c.Append(t).ToArray()));
        }

        foreach (var arguments in combinations)
        {
            try
            {
                return type.MakeGenericType(arguments);
            }
            catch (ArgumentException)
            {
                // A constraint the arguments do not meet; the next combination is tried.
            }
        }

        return null;
    }

    private static string NameOf(Type type) => type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
}

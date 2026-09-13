using System.Reflection;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The API reference, reflected off the shipped Bit.Bmotion assembly.
/// <para>
/// Its whole claim is that it cannot drift from the library, so the tests are about the mechanism
/// rather than the contents: every listed type must be fetchable, the default values must be read
/// off real instances (in an animation library the defaults <i>are</i> the behaviour), and the
/// documentation must actually be there. That last one is a deployment property, not a code one -
/// the XML file lives next to the assembly, and a publish profile that drops it turns every summary
/// in the reference into null without failing anything.
/// </para>
/// </summary>
[TestClass]
public class ApiCatalogTests
{
    private static readonly string[] KnownKinds =
        ["Component", "Interface", "Enum", "Delegate", "Static class", "Class", "Struct", "Record", "Attribute"];

    [TestMethod]
    public void Types_ListsThePublicSurfaceOfTheLibrary()
    {
        var types = BmotionApiCatalog.Types;

        Assert.IsTrue(types.Length >= 20, $"Only {types.Length} public types were found.");

        foreach (var type in types)
        {
            Assert.AreNotEqual(string.Empty, type.Name.Trim());
            CollectionAssert.Contains(KnownKinds, type.Kind, $"'{type.Name}' is a '{type.Kind}'.");
            // Compiler-generated names and raw arity markers would be noise in a reference an agent
            // reads; a generic spelled out as BmotionPresenceGroup<TItem> is exactly right.
            Assert.IsFalse(type.Name.Contains('`'), $"'{type.Name}' still carries its arity marker.");
            Assert.IsFalse(type.Name.Contains("<>", StringComparison.Ordinal), $"'{type.Name}' is compiler-generated.");
        }

        var names = types.Select(type => type.Name).ToArray();

        CollectionAssert.AreEquivalent(names.Distinct().ToArray(), names, "A type is listed twice.");
    }

    [TestMethod]
    [DataRow("Bm")]
    [DataRow("Bmotion")]
    [DataRow("BmSpring")]
    [DataRow("BmTween")]
    [DataRow("BmInertia")]
    [DataRow("BmotionAnimatePresence")]
    [DataRow("BmVariants")]
    [DataRow("BmDrag")]
    [DataRow("BmScrollTimeline")]
    [DataRow("BmotionAnimateService")]
    public void Types_IncludeEveryTypeTheToolDescriptionsTellAnAgentToAskFor(string name)
    {
        Assert.IsTrue(BmotionApiCatalog.Types.Any(type => type.Name == name),
                      $"GetBmotionApiDetails advertises '{name}', which is not in the list.");
    }

    /// <summary>
    /// The listing exists to pick a name to fetch with. A name it offers that the fetch cannot
    /// resolve sends an agent round a loop with no way out.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_AnswersForEveryTypeTheListingOffers()
    {
        var unfetchable = BmotionApiCatalog.Types
            .Where(type => BmotionApiCatalog.GetTypeDetails(type.Name) is null)
            .Select(type => type.Name)
            .ToArray();

        Assert.AreEqual(0, unfetchable.Length, $"Listed but not fetchable: {string.Join(", ", unfetchable)}.");
    }

    [TestMethod]
    public void GetTypeDetails_ResolvesTheNameHoweverItIsWritten()
    {
        var canonical = BmotionApiCatalog.GetTypeDetails("BmSpring");

        Assert.IsNotNull(canonical);
        Assert.AreSame(canonical, BmotionApiCatalog.GetTypeDetails("bmspring"));
        Assert.AreSame(canonical, BmotionApiCatalog.GetTypeDetails("  BmSpring  "));
        Assert.AreSame(canonical, BmotionApiCatalog.GetTypeDetails("Bit.Bmotion.BmSpring"));
    }

    [TestMethod]
    [DataRow("Nonexistent")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void GetTypeDetails_WhatIsNotAPublicType_IsNull(string? name)
    {
        Assert.IsNull(BmotionApiCatalog.GetTypeDetails(name!));
    }

    /// <summary>
    /// The reason the reference is reflected rather than written down: whether Damping is 10 or 20
    /// is the difference between the motion an agent wrote and the motion it meant.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_ReadsDefaultValuesOffARealInstance()
    {
        var spring = BmotionApiCatalog.GetTypeDetails("BmSpring")!;

        var actual = new BmSpring();

        foreach (var (name, expected) in new[]
        {
            (nameof(BmSpring.Stiffness), actual.Stiffness),
            (nameof(BmSpring.Damping), actual.Damping),
            (nameof(BmSpring.Mass), actual.Mass),
        })
        {
            var member = spring.Members.Single(entry => entry.Name == name);

            Assert.AreEqual(expected.ToString(System.Globalization.CultureInfo.InvariantCulture), member.Default,
                            $"BmSpring.{name} is documented as defaulting to '{member.Default}'.");
        }

        // A tween's duration is the one number people assume; it has to be the library's own.
        Assert.AreEqual(new BmTween().Duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        BmotionApiCatalog.GetTypeDetails("BmTween")!.Members.Single(member => member.Name == "Duration").Default);
    }

    [TestMethod]
    public void GetTypeDetails_AComponent_ListsItsBlazorParameters()
    {
        var component = BmotionApiCatalog.GetTypeDetails("Bmotion")!;

        Assert.AreEqual("Component", component.Kind);

        var parameters = component.Members.Where(member => member.Kind == "Parameter").Select(member => member.Name).ToArray();

        foreach (var expected in new[] { "Initial", "Animate", "Exit", "Transition", "ChildContent" })
        {
            CollectionAssert.Contains(parameters, expected, $"<Bmotion> has no '{expected}' parameter in the reference.");
        }

        // Parameters come first: they are what a caller writes, and the rest is implementation.
        Assert.AreEqual("Parameter", component.Members[0].Kind);
    }

    /// <summary>
    /// A component's inherited Bmotion parameters belong in its reference; the members it gets from
    /// ComponentBase do not - they are noise an agent has to read past.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_AComponent_DoesNotCarryTheFrameworksOwnMembers()
    {
        var component = BmotionApiCatalog.GetTypeDetails("Bmotion")!;

        foreach (var framework in new[] { "SetParametersAsync", "StateHasChanged", "OnInitialized", "InvokeAsync" })
        {
            Assert.IsFalse(component.Members.Any(member => member.Name == framework),
                           $"'{framework}' comes from ComponentBase and does not belong in the reference.");
        }
    }

    [TestMethod]
    public void GetTypeDetails_AnEnum_ListsEveryMemberAsAnEnumValue()
    {
        var ease = BmotionApiCatalog.GetTypeDetails("BmEase")!;

        Assert.AreEqual("Enum", ease.Kind);
        Assert.IsTrue(ease.Members.All(member => member.Kind == "EnumValue"));

        CollectionAssert.AreEquivalent(Enum.GetNames<BmEase>(), ease.Members.Select(member => member.Name).ToArray());

        // The underlying constant is reported, so BmEase.Linear can be told from BmEase.Out.
        Assert.IsTrue(ease.Members.All(member => member.Default is not null));
    }

    [TestMethod]
    public void GetTypeDetails_AStaticFacade_ListsItsFactoryMethodsWithSignatures()
    {
        var bm = BmotionApiCatalog.GetTypeDetails("Bm")!;

        Assert.AreEqual("Static class", bm.Kind);

        foreach (var factory in new[] { "To", "Spring", "Tween" })
        {
            var methods = bm.Members.Where(member => member.Kind == "Method" && member.Name == factory).ToArray();

            Assert.AreNotEqual(0, methods.Length, $"Bm.{factory} is missing from the reference.");
            Assert.IsTrue(methods.All(method => method.Signature is not null && method.Signature.StartsWith('(')),
                          $"Bm.{factory} is listed without a parameter list.");
        }
    }

    /// <summary>
    /// The documentation comes from the XML file the library build emits next to the assembly. If a
    /// publish drops it, every summary here becomes null and the reference degrades to bare names
    /// without anything failing.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_TheXmlDocumentation_IsDeployedAlongsideTheAssembly()
    {
        Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, "Bit.Bmotion.xml")),
                      "Bit.Bmotion.xml is not next to the assembly, so the reference has no prose at all.");

        var documented = BmotionApiCatalog.Types.Count(type => string.IsNullOrWhiteSpace(type.Summary) is false);

        Assert.IsTrue(documented > BmotionApiCatalog.Types.Length / 2,
                      $"Only {documented} of {BmotionApiCatalog.Types.Length} types carry a summary.");

        StringAssert.Contains(BmotionApiCatalog.GetTypeDetails("BmSpring")!.Summary ?? string.Empty, "spring",
                              StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Every hit costs a reflection walk plus an XML lookup per member, and the search index asks
    /// for all of them at once.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_IsCachedPerType()
    {
        Assert.AreSame(BmotionApiCatalog.GetTypeDetails("Bmotion"), BmotionApiCatalog.GetTypeDetails("Bmotion"));
    }

    [TestMethod]
    public void GetTypeDetails_IsSafeToAskForFromManyThreadsAtOnce()
    {
        var names = BmotionApiCatalog.Types.Select(type => type.Name).ToArray();

        var results = names.AsParallel().WithDegreeOfParallelism(8)
            .Select(name => BmotionApiCatalog.GetTypeDetails(name))
            .ToArray();

        Assert.IsTrue(results.All(details => details is not null));
    }

    [TestMethod]
    public void FriendlyName_SpellsTypesTheWayCSharpDoes()
    {
        Assert.AreEqual("double?", BmotionApiCatalog.FriendlyName(typeof(double?)));
        Assert.AreEqual("string[]", BmotionApiCatalog.FriendlyName(typeof(string[])));
        Assert.AreEqual("void", BmotionApiCatalog.FriendlyName(typeof(void)));
        Assert.AreEqual("bool", BmotionApiCatalog.FriendlyName(typeof(bool)));
        Assert.AreEqual("int", BmotionApiCatalog.FriendlyName(typeof(int)));
        Assert.AreEqual("object", BmotionApiCatalog.FriendlyName(typeof(object)));
        Assert.AreEqual("Dictionary<string, int>", BmotionApiCatalog.FriendlyName(typeof(Dictionary<string, int>)));
        Assert.AreEqual("Task<string[]>", BmotionApiCatalog.FriendlyName(typeof(Task<string[]>)));
    }

    /// <summary>
    /// Every member of every type, walked once. A reflection or documentation-id mistake on one
    /// exotic member (a generic method, an operator, a ref parameter) throws rather than degrading,
    /// and would take the whole tool down for the type it is on.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_EveryMemberOfEveryType_IsDescribedWithoutThrowing()
    {
        foreach (var type in BmotionApiCatalog.Types)
        {
            var details = BmotionApiCatalog.GetTypeDetails(type.Name)!;

            Assert.AreEqual(type.Name, details.Name);
            Assert.AreEqual(type.Kind, details.Kind);
            Assert.IsNotNull(details.FullName);
            Assert.IsNotNull(details.Implements);

            foreach (var member in details.Members)
            {
                Assert.AreNotEqual(string.Empty, member.Name.Trim(), $"{type.Name} has an unnamed member.");
                Assert.IsFalse(member.Name.Contains('<'), $"{type.Name}.{member.Name} is compiler-generated.");
            }
        }
    }

    /// <summary>
    /// The reference is prose an agent reads, not markup. Flattening that leaves tags behind would
    /// put "&lt;see cref=..." into the middle of a sentence.
    /// </summary>
    [TestMethod]
    public void GetTypeDetails_TheDocumentation_IsFlattenedToProse()
    {
        foreach (var type in BmotionApiCatalog.Types)
        {
            var details = BmotionApiCatalog.GetTypeDetails(type.Name)!;

            foreach (var text in details.Members.Select(member => member.Summary)
                                        .Concat([details.Summary, details.Remarks])
                                        .Where(text => text is not null))
            {
                foreach (var tag in new[] { "<see ", "<paramref", "<para>", "<c>", "<summary>", "</summary>" })
                {
                    Assert.IsFalse(text!.Contains(tag, StringComparison.Ordinal),
                                   $"{type.Name} carries raw '{tag}' into its documentation: {text}");
                }
            }
        }
    }

    [TestMethod]
    public void GetTypeDetails_MarksTheEditorRequiredParameters()
    {
        // Not every version of the library has one; when it does, the flag has to survive.
        var required = BmotionApiCatalog.Types
            .Select(type => BmotionApiCatalog.GetTypeDetails(type.Name)!)
            .SelectMany(details => details.Members.Select(member => (details.Name, Member: member)))
            .Where(entry => entry.Member.Required)
            .ToArray();

        foreach (var (owner, member) in required)
        {
            var property = Type.GetType($"Bit.Bmotion.{owner}, Bit.Bmotion")?.GetProperty(member.Name);

            if (property is null) continue;

            Assert.IsTrue(property.IsDefined(typeof(Microsoft.AspNetCore.Components.EditorRequiredAttribute), inherit: true),
                          $"{owner}.{member.Name} is marked required but is not [EditorRequired].");
        }
    }
}

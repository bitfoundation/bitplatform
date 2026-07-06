using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.Brouter.Generators.Tests.GeneratorTestHarness;

namespace Bit.Brouter.Generators.Tests;

[TestClass]
public class BrouterRoutesGeneratorTests
{
    [TestMethod]
    public void Page_directive_with_constraint_generates_a_typed_builder()
    {
        var (source, asm) = Run(("Counter.razor", """
            @page "/counter/{start:int}"
            <h1>Counter</h1>
            """));

        StringAssert.Contains(source, "public static string CounterByStart(int @start");
        Assert.AreEqual("/counter/42", Invoke(asm, "CounterByStart", 42, Type.Missing));
    }

    [TestMethod]
    public void Nested_Broute_tags_compose_the_full_template()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Path="/users">
                    <Broute Path="/{id:int}/edit">
                        <Content>edit</Content>
                    </Broute>
                </Broute>
            </Brouter>
            """));

        Assert.AreEqual("/users/7/edit", Invoke(asm, "UsersEditById", 7, Type.Missing));
        Assert.AreEqual("/users", Invoke(asm, "Users", Type.Missing));
    }

    [TestMethod]
    public void Named_route_names_the_method_and_emits_a_Names_constant()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Name="user" Path="/users/{id:int}" />
            </Brouter>
            """));

        Assert.AreEqual("/users/42", Invoke(asm, "User", 42, Type.Missing));
        Assert.AreEqual("user", NameConstant(asm, "User"));
    }

    [TestMethod]
    public void Optional_parameter_is_omittable()
    {
        var (_, asm) = Run(("Profile.razor", """
            @page "/profile/{username?}"
            """));

        Assert.AreEqual("/profile", Invoke(asm, "ProfileByUsername", Type.Missing, Type.Missing));
        Assert.AreEqual("/profile/saleh", Invoke(asm, "ProfileByUsername", "saleh", Type.Missing));
    }

    [TestMethod]
    public void CatchAll_splits_and_escapes_per_segment()
    {
        var (_, asm) = Run(("Files.razor", """
            @page "/files/{**path}"
            """));

        Assert.AreEqual("/files", Invoke(asm, "Files", Type.Missing, Type.Missing));
        Assert.AreEqual("/files/docs/a%20b", Invoke(asm, "Files", "docs/a b", Type.Missing));
    }

    [TestMethod]
    public void Values_are_escaped_and_formatted_invariantly()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Path="/tag/{name}" />
                <Broute Path="/flag/{on:bool}" />
            </Brouter>
            """));

        Assert.AreEqual("/tag/c%23%20rocks", Invoke(asm, "TagByName", "c# rocks", Type.Missing));
        Assert.AreEqual("/flag/true", Invoke(asm, "FlagByOn", true, Type.Missing));
    }

    [TestMethod]
    public void Query_argument_appends_with_a_question_mark_either_way()
    {
        var (_, asm) = Run(("Home.razor", """
            @page "/home"
            """));

        Assert.AreEqual("/home?tab=1", Invoke(asm, "Home", "tab=1"));
        Assert.AreEqual("/home?tab=1", Invoke(asm, "Home", "?tab=1"));
    }

    [TestMethod]
    public void Root_page_generates_Root()
    {
        var (_, asm) = Run(("Index.razor", """
            @page "/"
            """));

        Assert.AreEqual("/", Invoke(asm, "Root", Type.Missing));
    }

    [TestMethod]
    public void Group_routes_add_no_segments_and_dynamic_paths_are_skipped()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Group Path="">
                    <Broute Path="/inside" />
                </Broute>
                <Broute Path="@dynamicPath">
                    <Broute Path="/unknowable" />
                </Broute>
            </Brouter>
            """));

        Assert.AreEqual("/inside", Invoke(asm, "Inside", Type.Missing));
        // Neither the dynamic route nor its child may generate anything.
        Assert.IsFalse(Methods(asm).Any(m => m.Name.Contains("Unknowable", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Duplicate_templates_across_files_generate_once_preferring_the_named_one()
    {
        var (_, asm) = Run(
            ("Page.razor", """
                @page "/users/{id:int}"
                """),
            ("Routes.razor", """
                <Brouter>
                    <Broute Name="user" Path="/users/{id:int}" Guard="X" />
                </Brouter>
                """));

        var methods = Methods(asm).Where(m => m.Name is "User" or "UsersById").ToArray();
        Assert.AreEqual(1, methods.Length);
        Assert.AreEqual("User", methods[0].Name);
    }

    [TestMethod]
    public void Redirect_routes_and_wildcard_templates_are_not_generated()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Path="/" RedirectTo="/home" />
                <Broute Path="/files/**" />
                <Broute Path="/home" />
            </Brouter>
            """));

        var names = Methods(asm).Select(m => m.Name).ToArray();
        CollectionAssert.Contains(names, "Home");
        Assert.IsFalse(names.Contains("Root"));
        Assert.IsFalse(names.Any(n => n.Contains("Files")));
    }

    [TestMethod]
    public void Duplicate_template_with_conflicting_names_reports_a_diagnostic()
    {
        var (_, asm, diagnostics) = RunWithDiagnostics(("Routes.razor", """
            <Brouter>
                <Broute Name="first" Path="/users/{id:int}" />
                <Broute Name="second" Path="/users/{id:int}" />
            </Brouter>
            """));

        var diagnostic = diagnostics.Single(d => d.Id == "BRT001");
        StringAssert.Contains(diagnostic.GetMessage(), "first");
        StringAssert.Contains(diagnostic.GetMessage(), "second");
        // The first declaration still wins deterministically.
        Assert.AreEqual("/users/7", Invoke(asm, "First", 7, Type.Missing));
        Assert.AreEqual("first", NameConstant(asm, "First"));
    }

    [TestMethod]
    public void A_named_route_owns_its_method_name_over_an_unnamed_lookalike()
    {
        var (_, asm) = Run(("Routes.razor", """
            <Brouter>
                <Broute Path="/counter" />
                <Broute Name="counter" Path="/counter/{init:int}" />
            </Brouter>
            """));

        // The explicit name wins Counter(...); the unnamed literal route gets the suffix.
        Assert.AreEqual("/counter/5", Invoke(asm, "Counter", 5, Type.Missing));
        Assert.AreEqual("/counter", Invoke(asm, "Counter2", Type.Missing));
    }

    [TestMethod]
    public void RouteAttribute_directives_are_discovered()
    {
        var (_, asm) = Run(("Legacy.razor", """
            @attribute [Route("/legacy/{id:guid}")]
            """));

        var id = Guid.NewGuid();
        Assert.AreEqual($"/legacy/{id}", Invoke(asm, "LegacyById", id, Type.Missing));
    }
}

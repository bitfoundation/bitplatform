using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ConstraintRegistryTests
{
    private static BrouterRouteConstraint Slug() =>
        new BrouterTypeRouteConstraint<string>((string s, out string r) =>
        {
            r = s;
            return s.Length >= 3 && s.All(c => char.IsLetterOrDigit(c) || c == '-');
        });

    [TestMethod]
    public void Custom_constraint_from_registry_is_resolved_during_parsing()
    {
        var registry = new BrouterConstraintRegistry();
        registry.Register("slug", Slug());

        var result = BrouterTemplateParser.ParseTemplate("/posts/{name:slug}", registry);

        Assert.AreEqual(1, result.TemplateSegments[1].Constraints.Length);
        Assert.AreEqual("slug", result.TemplateSegments[1].Constraints[0].Name);
    }

    [TestMethod]
    public void Built_in_constraints_are_available_without_registration()
    {
        var registry = new BrouterConstraintRegistry();

        var result = BrouterTemplateParser.ParseTemplate("/users/{id:int}", registry);

        Assert.AreEqual(1, result.TemplateSegments[1].Constraints.Length);
    }

    [TestMethod]
    public void Registering_a_built_in_name_throws()
    {
        var registry = new BrouterConstraintRegistry();

        Assert.ThrowsExactly<InvalidOperationException>(() => registry.Register("int", Slug()));
    }

    [TestMethod]
    public void Registering_a_duplicate_custom_name_throws()
    {
        var registry = new BrouterConstraintRegistry();
        registry.Register("slug", Slug());

        Assert.ThrowsExactly<InvalidOperationException>(() => registry.Register("slug", Slug()));
    }

    [TestMethod]
    public void Custom_constraints_are_isolated_between_registries()
    {
        var withSlug = new BrouterConstraintRegistry();
        withSlug.Register("slug", Slug());
        var withoutSlug = new BrouterConstraintRegistry();

        // The registry that has it resolves it...
        _ = BrouterTemplateParser.ParseTemplate("/posts/{name:slug}", withSlug);

        // ...a sibling container that never registered it does not (no process-wide leakage).
        Assert.ThrowsExactly<ArgumentException>(
            () => BrouterTemplateParser.ParseTemplate("/posts/{name:slug}", withoutSlug));
    }

    [TestMethod]
    public void Unregister_removes_a_custom_constraint()
    {
        var registry = new BrouterConstraintRegistry();
        registry.Register("slug", Slug());

        Assert.IsTrue(registry.Unregister("slug"));

        Assert.ThrowsExactly<ArgumentException>(
            () => BrouterTemplateParser.ParseTemplate("/posts/{name:slug}", registry));
    }

    [TestMethod]
    public void Unknown_constraint_throws()
    {
        var registry = new BrouterConstraintRegistry();

        Assert.ThrowsExactly<ArgumentException>(
            () => BrouterTemplateParser.ParseTemplate("/posts/{name:nope}", registry));
    }
}

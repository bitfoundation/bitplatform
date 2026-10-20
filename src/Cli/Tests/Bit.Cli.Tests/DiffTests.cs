using Bit.Cli.Projects;
using Bit.Cli.Templates;

namespace Bit.Cli.Tests;

[TestClass]
public class DiffTests
{
    [TestMethod]
    public void BitJson_Should_GiveTheVersionNameAndOptions()
    {
        using var host = new Infrastructure.TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """
            {
                "template": "bit-bp",
                "version": "10.8.0",
                "name": "Contoso",
                "options": { "database": "PostgreSQL", "aspire": "False", "sample": true, "url": 5 }
            }
            """);

        var record = TemplateDiff.ReadProject(host.WorkingDirectory);

        Assert.AreEqual("10.8.0", record.Version);
        Assert.AreEqual("Contoso", record.Name);
        CollectionAssert.AreEquivalent(new Dictionary<string, string> { ["database"] = "PostgreSQL", ["aspire"] = "False", ["sample"] = "true" }, record.Options.ToDictionary());

        var (arguments, _) = TemplateDiff.TemplateArguments(TemplateManifest.Embedded, record.Options);
        CollectionAssert.AreEqual(new[] { "--database", "PostgreSQL", "--aspire", "false", "--sample", "true" }, arguments);

        File.Delete(Path.Combine(host.WorkingDirectory, "bit.json"));
        Assert.IsNull(TemplateDiff.ReadProject(host.WorkingDirectory).Version);
    }

    [TestMethod]
    public void TheSummary_Should_ShowThePasswordTheReadmeNames()
    {
        Assert.AreEqual("40817263", NewWorkflow.ReadSeedPassword("pre-confirmed accounts whose password is **`40817263`**:"));
        Assert.IsNull(NewWorkflow.ReadSeedPassword("## Welcome!"));
    }

    [TestMethod]
    public void EachVersion_Should_GetOnlyTheOptionsItHas()
    {
        var options = new Dictionary<string, string> { ["database"] = "postgresql", ["madeUp"] = "true", ["module"] = "Nope" };

        var (arguments, unknown) = TemplateDiff.TemplateArguments(TemplateManifest.Embedded, options);

        CollectionAssert.AreEqual(new[] { "--database", "PostgreSQL" }, arguments);
        CollectionAssert.AreEquivalent(new[] { "--madeUp true", "--module Nope" }, unknown);
    }
}

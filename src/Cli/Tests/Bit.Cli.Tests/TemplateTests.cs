using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class TemplateTests
{
    [TestMethod]
    public void TheEmbeddedManifest_Should_DescribeTheTemplateParameters()
    {
        var manifest = TemplateManifest.Embedded;

        Assert.AreEqual("bit-bp", manifest.ShortName);
        Assert.AreEqual("Boilerplate", manifest.SourceName);
        Assert.IsNull(manifest.Find("helpUrl"));

        var database = manifest.Find("database")!;
        Assert.AreEqual(TemplateParameterType.Choice, database.Type);
        Assert.AreEqual("Sqlite", database.DefaultValue);
        CollectionAssert.IsSubsetOf(new[] { "Sqlite", "SqlServer", "PostgreSQL", "MySql", "Other" }, database.Choices.Select(c => c.Value).ToArray());

        Assert.AreEqual(TemplateParameterType.Bool, manifest.Find("aspire")!.Type);
        Assert.AreEqual(TemplateParameterType.String, manifest.Find("apiServerUrl")!.Type);
    }

    [TestMethod]
    public void EveryParameter_Should_HaveACleanLabel()
    {
        foreach (var parameter in TemplateManifest.Embedded.Parameters)
        {
            var label = TemplateLabels.For(parameter);

            Assert.IsFalse(string.IsNullOrWhiteSpace(label), parameter.Name);
            Assert.IsFalse(label.Contains('?'), $"{parameter.Name}: {label}");
            Assert.IsFalse(label.StartsWith("Add ", StringComparison.Ordinal), $"{parameter.Name}: {label}");
        }
    }

    [TestMethod]
    public void TheReadme_Should_ListEveryTemplateOption()
    {
        var readme = File.ReadAllText(TestHost.RepositoryFile("src", "Cli", "README.md"));
        var missing = TemplateManifest.Embedded.Parameters.Where(p => readme.Contains($"`--{p.Name}`", StringComparison.Ordinal) is false).Select(p => p.Name).ToList();

        Assert.IsEmpty(missing, "src/Cli/README.md's options table misses: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void Set_Should_NormalizeChoicesAndBooleans()
    {
        var selection = new TemplateSelection(TemplateManifest.Embedded);

        Assert.IsNull(selection.Set("database", "postgresql"));
        Assert.AreEqual("PostgreSQL", selection.Database);

        Assert.IsNull(selection.Set("redis", ""));
        Assert.IsTrue(selection.IsTrue("redis"));

        Assert.IsNull(selection.Set("aspire", "False"));
        Assert.IsFalse(selection.Aspire);

        StringAssert.Contains(selection.Set("database", "Oracle"), "Sqlite");
        StringAssert.Contains(selection.Set("redis", "maybe"), "true or false");
        StringAssert.Contains(selection.Set("nope", "1"), "no 'nope' option");
    }

    [TestMethod]
    public void OnlyNonDefaultValues_Should_ReachTheCommandLines()
    {
        var selection = new TemplateSelection(TemplateManifest.Embedded);
        selection.Set("database", "Sqlite");
        selection.Set("module", "Admin");
        selection.Set("redis", "true");
        selection.Set("aspire", "false");

        CollectionAssert.AreEqual(new[] { "--module", "Admin", "--aspire", "false", "--redis", "true" }, selection.ToTemplateArguments().ToArray());
        CollectionAssert.AreEqual(new[] { "--module", "Admin", "--aspire", "false", "--redis" }, selection.ToCommandLineTokens().ToArray());
    }

    [TestMethod]
    [DataRow("Contoso.Shop")]
    [DataRow("MyApp")]
    [DataRow("My_App2")]
    [DataRow("A.B.C")]
    public void ValidNames_Should_Pass(string name)
    {
        Assert.IsNull(ProjectName.Validate(name));
    }

    [TestMethod]
    [DataRow("", "Enter a name")]
    [DataRow("my-app", "Dashes")]
    [DataRow("my app", "Dashes and spaces")]
    [DataRow("1App", "must start with a letter")]
    [DataRow("Contoso.class", "keyword")]
    [DataRow("System.Shop", "clashes")]
    [DataRow("Contoso..Shop", "two dots")]
    [DataRow(".Contoso", "start or end with a dot")]
    public void InvalidNames_Should_SayWhy(string name, string reason)
    {
        StringAssert.Contains(ProjectName.Validate(name), reason);
    }

    [TestMethod]
    [DataRow("my-app", "MyApp")]
    [DataRow("contoso shop", "ContosoShop")]
    [DataRow("contoso.shop", "Contoso.Shop")]
    [DataRow("123", "App123")]
    [DataRow("class", "Class")]
    [DataRow("system.x", "MySystem.X")]
    [DataRow("---", "MyApp")]
    public void Suggest_Should_TurnAnythingIntoAValidName(string input, string expected)
    {
        var suggestion = ProjectName.Suggest(input);

        Assert.AreEqual(expected, suggestion);
        Assert.IsNull(ProjectName.Validate(suggestion));
    }

    [TestMethod]
    public void Workloads_Should_FollowThePlatformsAndTheHost()
    {
        CollectionAssert.AreEqual(new[] { "wasm-tools" }, Platforms.Workloads([Platform.Web], HostOs.Windows).ToArray());
        CollectionAssert.AreEqual(new[] { "wasm-tools", "maui-android" }, Platforms.Workloads([Platform.Web, Platform.Android], HostOs.Linux).ToArray());
        CollectionAssert.AreEqual(new[] { "wasm-tools", "maui" }, Platforms.Workloads([Platform.Web, Platform.Android], HostOs.Windows).ToArray());
        CollectionAssert.AreEqual(new[] { "wasm-tools", "maui" }, Platforms.Workloads([Platform.Web, Platform.Windows], HostOs.Windows).ToArray());
        CollectionAssert.AreEqual(new[] { "wasm-tools", "maui" }, Platforms.Workloads([Platform.Ios], HostOs.MacOS).ToArray());
    }

    [TestMethod]
    public void TheTemplate_Should_SayWhichNodeAndAspireItNeeds()
    {
        Assert.AreEqual(24, TemplateRequirements.Embedded.NodeMajor);
        Assert.IsNotNull(TemplateRequirements.Embedded.Aspire);

        Assert.AreEqual(24, TemplateRequirements.ParseNodeMajor("{ \"engines\": { \"node\": \">=24\" } }"));
        Assert.AreEqual(22, TemplateRequirements.ParseNodeMajor("{ \"engines\": { \"node\": \"^22.11\" } }"));
        Assert.IsNull(TemplateRequirements.ParseNodeMajor("{ \"devDependencies\": {} }"));
        Assert.AreEqual(new Version(13, 6, 0), TemplateRequirements.ParseAspire("<Project Sdk=\"Aspire.AppHost.Sdk/13.6.0\">"));
        Assert.IsNull(TemplateRequirements.ParseAspire("<Project Sdk=\"Microsoft.NET.Sdk\">"));
    }

    [TestMethod]
    public void TheBuild_Should_CoverTheWholeSolutionOnceANativeAppIsPicked()
    {
        Assert.AreEqual("Contoso.Web.slnf", Platforms.BuildPath("Contoso", [Platform.Web]));
        Assert.AreEqual("Contoso.slnx", Platforms.BuildPath("Contoso", [Platform.Web, Platform.Android]));
        Assert.AreEqual("Contoso.slnx", Platforms.BuildPath("Contoso", [Platform.Web, Platform.Windows]));
        Assert.AreEqual("Contoso.slnx", Platforms.BuildPath("Contoso", [Platform.Ios]));
    }

    [TestMethod]
    public void ParsePlatforms_Should_AcceptListsAndRejectWhatTheHostCantBuild()
    {
        var (platforms, error) = Commands.SharedOptions.ParsePlatforms(["web,android", "windows"], HostOs.Windows);
        Assert.IsNull(error);
        CollectionAssert.AreEquivalent(new[] { Platform.Web, Platform.Android, Platform.Windows }, platforms.ToArray());

        Assert.IsNotNull(Commands.SharedOptions.ParsePlatforms(["ios"], HostOs.Windows).Error);
        Assert.IsNotNull(Commands.SharedOptions.ParsePlatforms(["tizen"], HostOs.Linux).Error);
        CollectionAssert.AreEquivalent(new[] { Platform.Web }, Commands.SharedOptions.ParsePlatforms([], HostOs.Linux).Platforms.ToArray());
    }

    [TestMethod]
    public void DiagnosticCodes_Should_ComeOutOfToolOutputWithoutTheText()
    {
        var output = """
            C:\Users\jane\Contoso\Contoso.Server.Api.csproj : error NU1301: Unable to load the service index for source https://nuget.contoso.local/v3/index.json.
            D:\x\y.csproj(10,5): error MSB3073: The command "npm install" exited with code 1.
            Program.cs(3,1): error CS0246: The type or namespace name 'Foo' could not be found
            Program.cs(3,1): error CS0246: The type or namespace name 'Bar' could not be found
            C:\Program Files\dotnet\sdk\10.0.401\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.ImportWorkloads.targets(38,5): error NETSDK1147: To build this project, the following workloads must be installed: wasm-tools
            """;

        CollectionAssert.AreEqual(new[] { "NU1301", "MSB3073", "CS0246", "NETSDK1147" }, DiagnosticCodes.Extract(output).ToArray());
        StringAssert.Contains(DiagnosticCodes.FirstErrorLine(output), "NU1301");
        Assert.IsNull(DiagnosticCodes.FirstErrorLine("Restore complete."));
    }
}

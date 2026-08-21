using System.Reflection;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The built-in splash reference. Its parameters are reflected off the shipped assembly rather
/// than listed by hand, for one concrete reason: <c>AutoReload</c> flipped to <c>false</c> in
/// v-10-6-0, and an agent still answering "true" from memory is the cause behind most "updates no
/// longer apply themselves" reports. These tests pin that the reflection still happens - and that
/// the defaults it reports are the ones the component really has.
/// </summary>
[TestClass]
public class ProgressCatalogTests
{
    [TestMethod]
    public void Parameters_AreExactlyTheComponentsParameters()
    {
        var reflected = typeof(BswupProgress)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(reflected, BswupProgressCatalog.Parameters.Select(p => p.Name).ToArray(),
            "the reference is meant to be the assembly's own answer, with nothing added or left out");
    }

    [TestMethod]
    [DataRow("AutoReload", "false")]
    [DataRow("ShowLogs", "false")]
    [DataRow("ShowAssets", "false")]
    [DataRow("HideApp", "false")]
    [DataRow("AutoHide", "false")]
    [DataRow("AppContainer", "\"#app\"")]
    public void Parameters_ReportTheDefaultTheShippedComponentHas(string name, string expected)
    {
        var parameter = BswupProgressCatalog.Parameters.Single(p => p.Name == name);

        Assert.AreEqual(expected, parameter.Default);
    }

    [TestMethod]
    public void AutoReload_IsReportedAsOffWithTheVersionItChangedIn()
    {
        var autoReload = BswupProgressCatalog.Parameters.Single(parameter => parameter.Name == "AutoReload");

        Assert.AreEqual("false", autoReload.Default);
        Assert.AreEqual("bool", autoReload.Type);
        StringAssert.Contains(autoReload.Summary, "v-10-6-0",
            "the answer has to carry the change, not just the current value - that is what corrects a remembered default");
    }

    [TestMethod]
    public void Parameters_AreOrderedTheWayTheyAreWorthReading()
    {
        var names = BswupProgressCatalog.Parameters.Select(parameter => parameter.Name).ToArray();

        Assert.AreEqual("AutoReload", names[0], "the one that changed leads");
        Assert.IsTrue(Array.IndexOf(names, "ChildContent") > Array.IndexOf(names, "ShowAssets"));
    }

    [TestMethod]
    public void Parameters_CarryTheContextNeededToWriteTheTag()
    {
        foreach (var parameter in BswupProgressCatalog.Parameters)
        {
            Assert.IsTrue(parameter.VerifiedFromSource, parameter.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(parameter.Type), parameter.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(parameter.Summary), $"{parameter.Name} has no description");
        }
    }

    [TestMethod]
    public void Elements_AreTheIdsACustomSplashHasToRender()
    {
        var ids = BswupProgressCatalog.ProgressUi.Elements.Select(element => element.Id).ToArray();

        foreach (var expected in new[]
        {
            "bit-bswup", "bit-bswup-progress-bar", "bit-bswup-percent", "bit-bswup-assets",
            "bit-bswup-error", "bit-bswup-error-message", "bit-bswup-error-details", "bit-bswup-error-retry",
            "bit-bswup-reload", "bit-bswup-reload-status",
        })
        {
            CollectionAssert.Contains(ids, expected);
        }

        Assert.IsTrue(BswupProgressCatalog.ProgressUi.Elements.All(element => string.IsNullOrWhiteSpace(element.Role) is false));
    }

    [TestMethod]
    public void Elements_TheComponentRendersItselfAreMarkedAsSuch()
    {
        // Rendering your own copy of these shadows the working button.
        var componentOwned = BswupProgressCatalog.ProgressUi.Elements
            .Where(element => element.RenderedByComponent)
            .Select(element => element.Id)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { "bit-bswup-reload", "bit-bswup-reload-status" }, componentOwned);
    }

    [TestMethod]
    public void ProgressUi_NamesTheScriptAndStylesheetThePageNeeds()
    {
        var requires = string.Join("\n", BswupProgressCatalog.ProgressUi.Requires);

        StringAssert.Contains(requires, "bit-bswup.progress.css");
        StringAssert.Contains(requires, "bit-bswup.progress.js");
    }

    [TestMethod]
    public void ProgressUi_CarriesTheRuntimeConfigCallAndTheCaveats()
    {
        StringAssert.Contains(BswupProgressCatalog.ProgressUi.RuntimeConfig, "BitBswupProgress.config");

        var notes = string.Join("\n", BswupProgressCatalog.ProgressUi.Notes);

        StringAssert.Contains(notes, "FIRST-INSTALL only");
        StringAssert.Contains(notes, "Content-Security-Policy");
        StringAssert.Contains(notes, "index.html", "the standalone-WebAssembly caveat is the one people hit");
    }
}

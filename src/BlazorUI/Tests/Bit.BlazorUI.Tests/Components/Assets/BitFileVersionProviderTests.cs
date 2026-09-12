using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Assets;

/// <summary>
/// An app hosted under a sub-path renders its asset paths with that base in them, so the provider retries a
/// path it cannot find under the web root with the base taken off. What counts as being under the base is
/// whole segments: /appsettings.css is not a file of an app served from /app, and reading it as one makes the
/// retry find an unrelated settings.css and stamp its hash on a path pointing somewhere else entirely.
/// </summary>
[TestClass]
public class BitFileVersionProviderTests
{
    private string webRoot = default!;
    private PhysicalFileProvider fileProvider = default!;

    [TestInitialize]
    public void SetupWebRoot()
    {
        webRoot = Path.Combine(Path.GetTempPath(), $"bit-file-version-{Guid.NewGuid():N}");

        Directory.CreateDirectory(Path.Combine(webRoot, "styles"));

        File.WriteAllText(Path.Combine(webRoot, "settings.css"), ".a{color:red}");
        File.WriteAllText(Path.Combine(webRoot, "styles", "site.css"), ".b{color:blue}");

        fileProvider = new PhysicalFileProvider(webRoot);
    }

    [TestCleanup]
    public void CleanupWebRoot()
    {
        fileProvider?.Dispose();

        if (webRoot is null) return;

        try
        {
            Directory.Delete(webRoot, recursive: true);
        }
        catch (IOException) { }
    }

    [TestMethod]
    public void APathUnderTheBaseShouldBeHashedWithTheBaseTakenOff()
    {
        var versioned = Append("/app/styles/site.css", "/app");

        Assert.IsTrue(versioned.StartsWith("/app/styles/site.css?v=sha256-", StringComparison.Ordinal), versioned);
    }

    [TestMethod]
    public void ABaseWrittenWithATrailingSlashShouldStillMatchBySegments()
    {
        var versioned = Append("/app/styles/site.css", "/app/");

        Assert.IsTrue(versioned.StartsWith("/app/styles/site.css?v=sha256-", StringComparison.Ordinal), versioned);
    }

    [TestMethod]
    public void APathMerelySharingThePrefixOfTheBaseShouldNotBeReadAsBaseRelative()
    {
        // The regression: with the base taken off by characters this is "settings.css", which does exist -
        // and the path would come back carrying the hash of a file that is not the one it points at.
        Assert.AreEqual("/appsettings.css", Append("/appsettings.css", "/app"));
    }

    [TestMethod]
    public void ARelativePathShouldNotBeReadAsBaseRelative()
    {
        Assert.AreEqual("appsettings.css", Append("appsettings.css", "/app"));
    }

    private string Append(string path, string pathBase)
    {
        return BitFileVersionProvider.AppendFileVersion(fileProvider, new PathString(pathBase), path);
    }
}

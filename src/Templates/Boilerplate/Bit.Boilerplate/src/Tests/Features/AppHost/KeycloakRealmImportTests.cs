using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.ApplicationModel;

namespace Boilerplate.Tests.Features.AppHost;

/// <summary>
/// <c>dev-realm.json</c> seeds accounts with well-known passwords - <c>test</c> / <c>123456</c> is in <c>g-admin</c>,
/// which <c>AppJwtSecureDataFormat</c> expands into every <see cref="Boilerplate.Shared.Infrastructure.Services.AppFeatures"/>
/// value - and an OAuth client whose secret is committed in this repository. It is a development fixture.
/// <para>
/// It used to reach <c>aspire publish</c> output. Every other development-only resource in the app host
/// (<c>clientwebwasm</c>, <c>mailpit</c>, the dev tunnels, <c>clientwindows</c>, the MAUI heads) sits inside
/// <c>if (builder.ExecutionContext.IsRunMode)</c>; Keycloak did not, so the published application model carried the
/// container with <c>--import-realm</c> and a bind mount of this folder, while the published app received
/// <c>KEYCLOAK_HTTP</c> and therefore registered its OIDC scheme against it.
/// </para>
/// <para>
/// The control below is the half that makes this test mean something: run mode must still import the realm, or the
/// assertion would also pass if <c>WithRealmImport</c> were deleted outright.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class KeycloakRealmImportTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>Where Keycloak reads realm files from, and therefore where <c>WithRealmImport</c> puts them.</summary>
    private const string ImportPath = "/opt/keycloak/data/import";

    [TestMethod]
    public async Task TheDevRealm_Should_BeImportedInRunModeAndNeverInPublishMode()
    {
        Assert.IsTrue(await RealmIsImported(runMode: true),
            "Run mode must still import the development realm - otherwise `aspire run` gives you a Keycloak with no "
            + "users and the publish-mode assertion below would pass for the wrong reason.");

        Assert.IsFalse(await RealmIsImported(runMode: false),
            "The development realm reaches `aspire publish` output. Its accounts have well-known passwords, so the "
            + "import has to stay inside `if (builder.ExecutionContext.IsRunMode)`.");
    }

    /// <summary>
    /// Builds the app host's application model in the requested mode and reports whether the keycloak resource ends up
    /// mounting the realm folder. Only the model is built - nothing is started and no container is touched.
    /// </summary>
    private async Task<bool> RealmIsImported(bool runMode)
    {
        string[] args = runMode
            ? []
            : ["--operation", "publish", "--publisher", "manifest",
               "--output-path", Path.Combine(Path.GetTempPath(), $"aspire-manifest-{Guid.NewGuid():N}.json")];

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Program>(args, TestContext.CancellationToken);

        Assert.AreEqual(runMode, builder.ExecutionContext.IsRunMode,
            "The app host did not start in the mode this test asked for, so neither assertion would mean anything.");

        var keycloak = builder.Resources.Single(resource => resource.Name is "keycloak");

        // WithRealmImport stages the folder through WithContainerFiles, not through a bind mount - the mount this
        // resource does carry is WithDataVolume's, at /opt/keycloak/data. The manifest renders both as bindMounts,
        // which is how this reached published output in the first place.
        // WithRealmImport lands in a DIFFERENT annotation per mode - run mode stages the folder through
        // WithContainerFiles, publish mode emits a plain bind mount - so both shapes have to be looked for. Checking
        // only the run-mode one passes against the very bug this test exists to catch, which is how it was written
        // the first time. The data volume also mounts /opt/keycloak/data, hence the exact-path comparison.
        return keycloak.Annotations.Any(annotation => annotation switch
        {
            ContainerFileSystemCallbackAnnotation files => files.DestinationPath is ImportPath,
            ContainerMountAnnotation mount => mount.Target is ImportPath,
            _ => false
        });
    }
}

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.ApplicationModel;

namespace Boilerplate.Tests.Features.AppHost;

[TestClass, TestCategory("UnitTest")]
public class ContainerRuntimeTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task WithoutAContainerRuntime_Containers_Should_HaveNoNamedVolumes()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Program>(["--DcpPublisher:ContainerRuntime=bit-missing-container-runtime"], TestContext.CancellationToken);

        var containers = builder.Resources.OfType<ContainerResource>().ToArray();

        Assert.Contains("keycloak", containers.Select(container => container.Name).ToArray(), "The app host's containers must stay in the model.");

        var withVolumes = containers.Where(container => container.Annotations.OfType<ContainerMountAnnotation>().Any(mount => mount.Type is ContainerMountType.Volume)).Select(container => container.Name).ToArray();

        Assert.IsEmpty(withVolumes, $"A named volume never gets ready without a container runtime, and Aspire then ends the whole run: {string.Join(", ", withVolumes)}.");
    }
}

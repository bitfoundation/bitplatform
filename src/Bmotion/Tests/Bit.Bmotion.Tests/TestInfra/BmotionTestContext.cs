using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// A bUnit <see cref="TestContext"/> pre-wired with the Bmotion engine, layout registry and animate
/// service, all backed by a <see cref="FakeBmotionInterop"/> so component render/interaction logic
/// can be exercised without a browser. Access the fake through <see cref="Interop"/>.
/// </summary>
internal class BmotionTestContext : Bunit.TestContext
{
    public FakeBmotionInterop Interop { get; } = new();

    /// <summary>Library options exposed to components; mutate before rendering to adjust policy.</summary>
    public BitBmotionOptions Options { get; } = new();

    public BmotionTestContext()
    {
        Services.AddLogging();
        Services.AddSingleton<IBmotionInterop>(Interop);
        Services.AddSingleton(Options);
        Services.AddScoped<BmotionAnimationEngine>();
        Services.AddScoped<BmotionLayoutRegistry>();
        Services.AddScoped<BmotionAnimateService>();
    }

    public BmotionAnimationEngine Engine => Services.GetRequiredService<BmotionAnimationEngine>();
}

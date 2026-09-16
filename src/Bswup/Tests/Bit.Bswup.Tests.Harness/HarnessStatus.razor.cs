namespace Bit.Bswup.Tests.Harness;

public partial class HarnessStatus
{
#if NET9_0_OR_GREATER
    private string RendererName => RendererInfo.Name;
#else
    // RendererInfo arrived in .NET 9; the suites fall back to data-platform on net8.0.
    private string RendererName => "unknown";
#endif
}

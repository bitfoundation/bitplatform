namespace Bit.Bheadless;

/// <summary>Thrown by <c>ShouldBeAccessible</c> assertions.</summary>
public sealed class BitBhA11yException(BitBhA11yReport report) : Exception(report.ToString())
{
    /// <summary>The failing report.</summary>
    public BitBhA11yReport Report { get; } = report;
}

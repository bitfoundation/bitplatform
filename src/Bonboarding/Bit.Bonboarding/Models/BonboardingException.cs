namespace Bit.Bonboarding;

/// <summary>Raised when the library cannot continue a tour.</summary>
public sealed class BonboardingException : Exception
{
    public BonboardingException(string message) : base(message) { }

    public BonboardingException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>The tour that failed, when known.</summary>
    public string? TourId { get; init; }

    /// <summary>The step that failed, when known.</summary>
    public string? StepId { get; init; }
}

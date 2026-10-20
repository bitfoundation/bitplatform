using System.Text.Json.Serialization;

namespace Bit.Bonboarding;

/// <summary>A width/height pair in CSS pixels.</summary>
public readonly record struct BonboardingSize(double Width, double Height)
{
    public static readonly BonboardingSize Empty = default;

    [JsonIgnore]
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

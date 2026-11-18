namespace Bit.Bmage;

/// <summary>Which colour channels a per-channel operation affects.</summary>
[Flags]
public enum BmageColorChannels
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    Rgb = Red | Green | Blue,
    All = Rgb | Alpha,
}

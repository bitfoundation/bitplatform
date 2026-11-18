namespace Bit.Bmage;

/// <summary>A property of an item (ISO/IEC 23008-12, the HEIF image file format): its type and payload.</summary>
internal readonly record struct BmageHeifProperty(uint Type, int Offset, int Length);

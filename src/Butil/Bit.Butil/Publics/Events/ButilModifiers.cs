using System;

namespace Bit.Butil;

[Flags]
public enum ButilModifiers
{
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Meta = 4,
    Shift = 8
}

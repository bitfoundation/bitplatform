using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bit.BlazorUI;

public class NumUtils
{
    public static string ToInvariantString(double value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

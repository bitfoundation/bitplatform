using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public class ActionParameter
{
    public string Name { get; set; } = default!;

    public ITypeSymbol Type { get; set; } = default!;
}

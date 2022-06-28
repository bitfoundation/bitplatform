using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public class BitProperty
{
    public BitProperty(IPropertySymbol propertySymbol, bool isTwoWayBoundProperty)
    {
        PropertySymbol = propertySymbol;
        IsTwoWayBoundProperty = isTwoWayBoundProperty;
    }

    public IPropertySymbol PropertySymbol { get; set; }
    public bool IsTwoWayBoundProperty { get; set; }
}

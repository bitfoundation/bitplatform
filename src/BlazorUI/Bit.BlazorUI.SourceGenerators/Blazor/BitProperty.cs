using Microsoft.CodeAnalysis;

namespace Bit.BlazorUI.SourceGenerators.Blazor;

public class BitProperty(IPropertySymbol propertySymbol, bool resetClassBuilder, bool resetStyleBuilder)
{
    public IPropertySymbol PropertySymbol { get; set; } = propertySymbol;

    public bool ResetClassBuilder { get; set; } = resetClassBuilder;
    public bool ResetStyleBuilder { get; set; } = resetStyleBuilder;

    public bool IsTwoWayBoundProperty { get; set; }
}

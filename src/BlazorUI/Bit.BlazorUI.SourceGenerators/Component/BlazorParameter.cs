using Microsoft.CodeAnalysis;

namespace Bit.BlazorUI.SourceGenerators.Component;

public class BlazorParameter(IPropertySymbol propertySymbol, bool resetClassBuilder, bool resetStyleBuilder)
{
    public IPropertySymbol PropertySymbol { get; set; } = propertySymbol;

    public bool ResetClassBuilder { get; set; } = resetClassBuilder;
    public bool ResetStyleBuilder { get; set; } = resetStyleBuilder;

    public bool IsTwoWayBoundProperty { get; set; }
}

using Microsoft.CodeAnalysis;

namespace Bit.BlazorUI.SourceGenerators.Component;

public class BlazorParameter(IPropertySymbol propertySymbol, bool resetClassBuilder, bool resetStyleBuilder, bool isTwoWayBound)
{
    public IPropertySymbol PropertySymbol { get; set; } = propertySymbol;

    public bool IsTwoWayBound { get; set; } = isTwoWayBound;

    public bool ResetClassBuilder { get; set; } = resetClassBuilder;
    public bool ResetStyleBuilder { get; set; } = resetStyleBuilder;

    public string? CallOnSetMethodName { get; set; }
}

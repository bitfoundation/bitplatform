namespace Bit.Butil;

/// <summary>One entry of <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorUAData/brands">NavigatorUAData.brands</see>.</summary>
public class UserAgentBrand
{
    /// <summary>The brand name. The list deliberately carries a nonsense entry, to break naive matching.</summary>
    public string Brand { get; set; } = string.Empty;
    
    /// <summary>The brand's version.</summary>
    public string Version { get; set; } = string.Empty;
}

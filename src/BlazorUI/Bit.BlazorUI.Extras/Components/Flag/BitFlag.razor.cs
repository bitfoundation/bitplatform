namespace Bit.BlazorUI;

/// <summary>
/// BitFlag is a component to render the flag image of a country.
/// </summary>
public partial class BitFlag
{



    /// <summary>
    /// The aspect ratio of the flag image.
    /// </summary>
    [Parameter] public BitFlagAspectRatio? AspectRatio { get; set; }

    /// <summary>
    /// The country to render the flag.
    /// </summary>
    [Parameter] public BitCountry? Country { get; set; }

    /// <summary>
    /// The size of the flag image.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }



    protected override string RootElementClass => "bit-flg";

    protected override void RegisterCssClasses()
    {

    }
}

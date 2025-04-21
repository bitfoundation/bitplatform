namespace Bit.BlazorUI;

/// <summary>
/// BitFlag is a component that renders the flag image of a country.
/// </summary>
public partial class BitFlag : BitComponentBase
{
    /// <summary>
    /// The country to render the flag.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitCountry? Country { get; set; }

    /// <summary>
    /// The tooltip value of the flag element.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-flg";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Country is null
                                    ? string.Empty
                                    : $"background-image:url('_content/Bit.BlazorUI.Extras/flags/{Country?.Iso2}-flat-16.webp')");
    }
}

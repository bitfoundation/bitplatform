namespace Bit.BlazorUI;

public partial class BitGridLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-grd";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();


        StyleBuilder.Register(() => $"--bit-ldn-grd-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-grd-16:{Convert(16)}px");
        StyleBuilder.Register(() => $"--bit-ldn-grd-32:{Convert(32)}px");
        StyleBuilder.Register(() => $"--bit-ldn-grd-56:{Convert(56)}px");
    }
}

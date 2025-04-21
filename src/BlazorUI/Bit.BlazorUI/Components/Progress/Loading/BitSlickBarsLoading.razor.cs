namespace Bit.BlazorUI;

public partial class BitSlickBarsLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-sbr";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-sbr-2:{Convert(2)}px");
        StyleBuilder.Register(() => $"--bit-ldn-sbr-8:{Convert(8)}px");
    }
}

namespace Bit.BlazorUI;

public partial class BitSpinnerLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-spn";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-spn-3:{Convert(3)}px");
        StyleBuilder.Register(() => $"--bit-ldn-spn-6:{Convert(6)}px");
        StyleBuilder.Register(() => $"--bit-ldn-spn-18:{Convert(18)}px");
        StyleBuilder.Register(() => $"--bit-ldn-spn-37:{Convert(37)}px");
        StyleBuilder.Register(() => $"--bit-ldn-spn-40:{Convert(40)}px");
    }
}

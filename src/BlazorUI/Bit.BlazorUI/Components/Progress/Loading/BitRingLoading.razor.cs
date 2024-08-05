namespace Bit.BlazorUI;

public partial class BitRingLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-rng";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rng-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rng-64:{Convert(64)}px");
    }
}

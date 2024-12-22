namespace Bit.BlazorUI;

public partial class BitHeartLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-hrt";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-hrt-24:{Convert(24)}px");
        StyleBuilder.Register(() => $"--bit-ldn-hrt-28:{Convert(28)}px");
        StyleBuilder.Register(() => $"--bit-ldn-hrt-32:{Convert(32)}px");
        StyleBuilder.Register(() => $"--bit-ldn-hrt-40:{Convert(40)}px");
    }
}

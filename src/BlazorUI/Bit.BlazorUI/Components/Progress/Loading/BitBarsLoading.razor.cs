namespace Bit.BlazorUI;

public partial class BitBarsLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-bar";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-bar-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bar-16:{Convert(16)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bar-24:{Convert(24)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bar-32:{Convert(32)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bar-56:{Convert(56)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bar-64:{Convert(64)}px");
    }
}

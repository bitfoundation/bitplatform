namespace Bit.BlazorUI;

public partial class BitDualRingLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-dur";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-dur-6:{Convert(6)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dur-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dur-64:{Convert(64)}px");
    }
}

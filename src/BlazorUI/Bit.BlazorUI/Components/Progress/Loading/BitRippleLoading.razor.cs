namespace Bit.BlazorUI;

public partial class BitRippleLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-rpl";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rpl-4:{Convert(4)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rpl-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rpl-36:{Convert(36)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rpl-80:{Convert(80)}px");
    }
}

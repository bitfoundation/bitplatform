namespace Bit.BlazorUI;

public partial class BitRollingDashesLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-rld";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rld-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rld-15:{Convert(15)}px");
    }
}

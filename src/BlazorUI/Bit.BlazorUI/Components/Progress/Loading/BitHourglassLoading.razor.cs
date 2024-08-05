namespace Bit.BlazorUI;

public partial class BitHourglassLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-hgl";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-hgl-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-hgl-32:{Convert(32)}px");
    }
}

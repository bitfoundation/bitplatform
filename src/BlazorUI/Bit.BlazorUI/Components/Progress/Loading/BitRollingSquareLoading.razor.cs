namespace Bit.BlazorUI;

public partial class BitRollingSquareLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-rsq";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rsq-4:{Convert(4)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rsq-20:{Convert(20)}px");
    }
}

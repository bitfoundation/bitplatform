namespace Bit.BlazorUI;

public partial class BitOrbitingDotsLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-ord";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-ord-6:{Convert(6)}px");
        StyleBuilder.Register(() => $"--bit-ldn-ord-25:{Convert(25)}px");
    }
}

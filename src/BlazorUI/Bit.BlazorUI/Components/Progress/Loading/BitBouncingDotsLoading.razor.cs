namespace Bit.BlazorUI;

public partial class BitBouncingDotsLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-bnd";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-bnd-6:{Convert(6)}px");
        StyleBuilder.Register(() => $"--bit-ldn-bnd-15:{Convert(15)}px");
    }
}

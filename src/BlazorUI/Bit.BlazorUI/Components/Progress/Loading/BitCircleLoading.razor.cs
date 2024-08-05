namespace Bit.BlazorUI;

public partial class BitCircleLoading : BitLoadingBase
{
    protected override int OriginalSize { get; set; } = 64;

    protected override string RootElementClass => "bit-ldn-cir";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-cir-8:{Convert(8)}px");
    }
}

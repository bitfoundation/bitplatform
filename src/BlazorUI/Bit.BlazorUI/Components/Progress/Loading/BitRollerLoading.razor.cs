namespace Bit.BlazorUI;

public partial class BitRollerLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-rol";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rol-4:{Convert(4)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-12:{Convert(12)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-17:{Convert(17)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-24:{Convert(24)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-32:{Convert(32)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-40:{Convert(40)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-48:{Convert(48)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-56:{Convert(56)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-63:{Convert(63)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-68:{Convert(68)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-71:{Convert(71)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rol-72:{Convert(72)}px");
    }
}

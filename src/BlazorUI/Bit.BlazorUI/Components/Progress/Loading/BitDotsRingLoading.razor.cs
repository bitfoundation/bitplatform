namespace Bit.BlazorUI;

public partial class BitDotsRingLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-dor";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-dor-6:{Convert(6)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-7:{Convert(7)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-11:{Convert(11)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-22:{Convert(22)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-37:{Convert(37)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-52:{Convert(52)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-62:{Convert(62)}px");
        StyleBuilder.Register(() => $"--bit-ldn-dor-66:{Convert(66)}px");
    }
}

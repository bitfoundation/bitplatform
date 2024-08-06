namespace Bit.BlazorUI;

public partial class BitEllipsisLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-elp";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-elp-8:{Convert(8)}px");
        StyleBuilder.Register(() => $"--bit-ldn-elp-13:{Convert(13)}px");
        StyleBuilder.Register(() => $"--bit-ldn-elp-24:{Convert(24)}px");
        StyleBuilder.Register(() => $"--bit-ldn-elp-32:{Convert(32)}px");
        StyleBuilder.Register(() => $"--bit-ldn-elp-33:{Convert(33)}px");
        StyleBuilder.Register(() => $"--bit-ldn-elp-56:{Convert(56)}px");
    }
}

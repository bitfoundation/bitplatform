namespace Bit.BlazorUI;

public partial class BitXBoxLoading : BitLoadingBase
{
    protected override string RootElementClass => "bit-ldn-xbx";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-xbx-3:{Convert(3)}px");
        StyleBuilder.Register(() => $"--bit-ldn-xbx-25f:{Convert(0.25 * OriginalSize)}px");
        StyleBuilder.Register(() => $"--bit-ldn-xbx-50f:{Convert(0.50 * OriginalSize)}px");
        StyleBuilder.Register(() => $"--bit-ldn-xbx-125f:{Convert(0.125 * OriginalSize)}px");
        StyleBuilder.Register(() => $"--bit-ldn-xbx-75f:{Convert(0.75 * OriginalSize)}px");
    }
}

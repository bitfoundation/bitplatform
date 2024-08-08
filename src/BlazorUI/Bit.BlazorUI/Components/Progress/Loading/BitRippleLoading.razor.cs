namespace Bit.BlazorUI;

public partial class BitRippleLoading : BitLoadingBase
{
    [Parameter] public string? Label { get; set; }
    [Parameter] public BitLabelPosition? LabelPosition { get; set; }

    protected override string RootElementClass => "bit-ldn-rpl";

    protected override void RegisterCssStyles()
    {
        base.RegisterCssStyles();

        StyleBuilder.Register(() => $"--bit-ldn-rpl-4:{Convert(4)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rpl-36:{Convert(36)}px");
        StyleBuilder.Register(() => $"--bit-ldn-rpl-72:{Convert(72)}px");
    }
}

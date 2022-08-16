
namespace Bit.BlazorUI;

public partial class BitSplitButton
{
    private BitSplitButtonItem Item = new();

    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public IEnumerable<BitSplitButtonItem> Items { get; set; } = new List<BitSplitButtonItem>();

    /// <summary>
    /// The style of button, Possible values: Primary | Standard
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    protected override string RootElementClass => "splt-btn";

    protected override async Task OnInitializedAsync()
    {
        var item = Items.FirstOrDefault(i => i.DefaultIsSelected);

        if (item is not null)
        {
            Item = item;
        }
        else
        {
            Item = Items.First();
        }

        await base.OnInitializedAsync();
    }

    private void HandleItemOnClick(BitSplitButtonItem item)
    {
        if (item.IsEnabled is false || IsEnabled is false) return;

        Item = item;

        if (item.OnClick is not null)
        {
            item.OnClick.Invoke();
        }
    }
}

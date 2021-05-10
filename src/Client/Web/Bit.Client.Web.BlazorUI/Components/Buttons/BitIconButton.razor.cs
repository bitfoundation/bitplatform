using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitIconButton
    {
        [Parameter] public string AriaDescription { get; set; }
        [Parameter] public bool AriaHidden { get; set; }
        [Parameter] public string AriaLabel { get; set; }
        [Parameter] public string IconName { get; set; }
        [Parameter] public string ToolTip { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-ico-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                        ? string.Empty
                                        : $"{RootElementClass}-{VisualClassRegistrar()}");
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }
    }
}

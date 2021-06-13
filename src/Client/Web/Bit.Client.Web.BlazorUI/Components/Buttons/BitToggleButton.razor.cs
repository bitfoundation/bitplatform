using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggleButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        private int? tabIndex;

        [Parameter] public bool AllowDisabledFocus { get; set; } = true;
        [Parameter] public string? AriaDescription { get; set; }
        [Parameter] public bool AriaHidden { get; set; }
        [Parameter] public string? AriaLabel { get; set; }
        [Parameter] public bool Checked { get; set; } = true;
        [Parameter] public string? CheckedIconName { get; set; }
        [Parameter] public string? UnCheckedIconName { get; set; }
        [Parameter] public string? ChekedLabel { get; set; }
        [Parameter] public string? UnChekedLabel { get; set; }        
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter]
        public ButtonStyle ButtonStyle
        {
            get => buttonStyle;
            set
            {
                buttonStyle = value;
                ClassBuilder.Reset();
            }
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                Checked = !Checked;
                StateHasChanged();
                await OnClick.InvokeAsync(e);
            }
        }

        protected override string RootElementClass => "bit-tgl-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                           ? string.Empty
                                           : ButtonStyle == ButtonStyle.Primary
                                               ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                               : $"{RootElementClass}-standard-{VisualClassRegistrar()}");
        }

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                tabIndex = AllowDisabledFocus ? null : -1;
            }

            await base.OnInitializedAsync();
        }
    }
}

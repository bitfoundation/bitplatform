using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        [Inject]
        private IJSRuntime JsRuntime { get; set; }
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;
        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private void HideMessageBar(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }
        private async Task ShowAert()
        {
            await JsRuntime.InvokeVoidAsync("alert", "some thing happened");
        }
    }
}

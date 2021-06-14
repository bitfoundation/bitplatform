using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private void MessageBarButton(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }

        private string SpinButtunValue;
        public void HandleSpinButtonValueChange(string value)
        {
            SpinButtunValue = value;
        }
    }
}

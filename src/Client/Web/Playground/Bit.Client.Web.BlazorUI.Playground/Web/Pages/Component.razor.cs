using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxIndeterminate = true;
        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;
        private string ShowPasswordText = "show";

        private void MessageBarButton(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }

        private void ChangeInputType(MouseEventArgs args)
        {
            InputType = InputType == TextFieldType.Text ? TextFieldType.Password : TextFieldType.Text;
            ShowPasswordText = InputType == TextFieldType.Text ? "hide" : "show";
        }
    }
}

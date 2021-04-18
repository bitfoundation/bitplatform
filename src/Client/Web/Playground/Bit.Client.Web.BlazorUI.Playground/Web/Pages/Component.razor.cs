using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxIndeterminate = true;
        private bool _isMessageBarHidden = false;
        private TextFieldType _inputType = TextFieldType.Password;
        private string _showPasswordText = "show";
        private void MessageBarButton(MouseEventArgs args)
        {
            _isMessageBarHidden = true;
        }
        private void ChangeInputType(MouseEventArgs args)
        {
            _inputType = _inputType == TextFieldType.Text ? TextFieldType.Password : TextFieldType.Text;
            _showPasswordText = _inputType == TextFieldType.Text ? "hide" : "show";
        }
    }
}

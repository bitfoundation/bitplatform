using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Index
    {
        private bool IsCheckBoxIndeterminate = true;
        private bool _isMessageBarHidden = false;
        private TextFieldType _inputType = TextFieldType.Password;
        private string _showPasswordText = "show";
        private List<ChoiceItem> _choiceItems = new List<ChoiceItem>();
       
        protected override async Task OnInitializedAsync()
        {
            _choiceItems.Add(new ChoiceItem()
            {
                Text = "Option1",
                Value = "1"
            });
            _choiceItems.Add(new ChoiceItem()
            {
                Text = "Option2",
                Value = "2"
            });
        }

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

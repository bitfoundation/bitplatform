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

        #region PivotSamples

        public string OverridePivotSelectedKey { get; set; } = "1";
        public string SelectedPivotItemKey { get; set; } = "1";
        public BitPivotItem BitPivotItem { get; set; }
        public ComponentVisibility PivotItemVisibility { get; set; }
        public BitPivotItem SelectedPivotKey { get; set; } = new BitPivotItem { ItemKey = "Foo" };

        public void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        public void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == ComponentVisibility.Visible ? ComponentVisibility.Collapsed : ComponentVisibility.Visible;
        }

        #endregion 

        private void HideMessageBar(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }
    }
}

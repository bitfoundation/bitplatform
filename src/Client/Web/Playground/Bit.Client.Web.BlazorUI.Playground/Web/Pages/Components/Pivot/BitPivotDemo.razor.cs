namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pivot
{
    public partial class BitPivotDemo
    {
        private string SelectedKey = "Foo";
        private BitPivotItem SelectedPivotItem;
        private string OverridePivotSelectedKey = "1";
        private ComponentVisibility PivotItemVisibility;

        private void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        private void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == ComponentVisibility.Visible ? ComponentVisibility.Collapsed : ComponentVisibility.Visible;
        }
    }
}

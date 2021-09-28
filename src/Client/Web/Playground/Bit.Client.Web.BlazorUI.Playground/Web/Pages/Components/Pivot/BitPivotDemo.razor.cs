namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pivot
{
    public partial class BitPivotDemo
    {
        private string SelectedKey = "Foo";
        private BitPivotItem SelectedPivotItem;
        private string OverridePivotSelectedKey = "1";
        private BitComponentVisibility PivotItemVisibility;

        private void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        private void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
        }
    }
}

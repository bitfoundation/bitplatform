namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pivot
{
    public partial class BitPivotDemo
    {
        public string OverridePivotSelectedKey { get; set; } = "1";
        public ComponentVisibility PivotItemVisibility { get; set; }
        public BitPivotItem SelectedPivotKey { get; set; } = new BitPivotItem { ItemKey = "Foo" };
        public BitPivotItem BitPivotItem { get; set; }

        public void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        public void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == ComponentVisibility.Visible ? ComponentVisibility.Collapsed : ComponentVisibility.Visible;
        }
    }
}

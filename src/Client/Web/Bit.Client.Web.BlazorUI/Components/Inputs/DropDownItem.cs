namespace Bit.Client.Web.BlazorUI
{
    public class DropDownItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool IsDisabled { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public DropDownItemType ItemType { get; set; } = DropDownItemType.Normal;
    }
}

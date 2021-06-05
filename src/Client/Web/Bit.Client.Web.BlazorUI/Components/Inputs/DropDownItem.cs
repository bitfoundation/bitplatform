namespace Bit.Client.Web.BlazorUI
{
    public class DropDownItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsSelected { get; set; }
        public DropDownItemType ItemType { get; set; } = DropDownItemType.Normal;
    }
}

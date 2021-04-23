namespace Bit.Client.Web.BlazorUI
{
    public class ChoiceItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool IsDisabled { get; set; } = false;
        public bool IsChecked { get; set; } = false;
    }
}

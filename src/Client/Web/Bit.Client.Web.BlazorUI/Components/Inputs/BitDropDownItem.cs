namespace Bit.Client.Web.BlazorUI
{
    public class BitDropDownItem
    {
        /// <summary>
        /// Text to render for this item
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Value of this item
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Whether or not this item is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether or not this item is selected
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// The type of this item, Refers to the dropdown separator
        /// </summary>
        public BitDropDownItemType ItemType { get; set; } = BitDropDownItemType.Normal;
    }
}

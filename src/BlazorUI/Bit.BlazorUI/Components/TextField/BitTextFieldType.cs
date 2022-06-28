namespace Bit.Client.Web.BlazorUI
{
    public enum BitTextFieldType
    {
        /// <summary>
        /// The TextField characters are shown as text
        /// </summary>
        Text,

        /// <summary>
        /// The TextField characters are masked
        /// </summary>
        Password,

        /// <summary>
        /// The TextField characters are number
        /// </summary>
        Number,

        /// <summary>
        /// The TextField act as an email input
        /// </summary>
        Email,

        /// <summary>
        /// The TextField act as a tel input
        /// </summary>
        Tel,

        /// <summary>
        /// The TextField act as a url input
        /// </summary>
        Url
    }
}

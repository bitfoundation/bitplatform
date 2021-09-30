namespace Bit.Client.Web.BlazorUI
{
    public enum BitNavMode
    {
        /// <summary>
        /// selected key changes will be handled inside the component
        /// </summary>
        Automatic,

        /// <summary>
        /// selected key changes will be sent back to the parent component and the component won't change its value
        /// </summary>
        Manual
    }
}

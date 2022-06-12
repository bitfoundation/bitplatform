namespace Bit.Client.Web.BlazorUI;

public enum BitComponentVisibility
{
    /// <summary>
    /// Show content of the component
    /// </summary>
    Visible = 0,

    /// <summary>
    /// Hide content of the component, though the space it takes on the page remains
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// Hide content of the component, though the space it takes on the page is gone
    /// </summary>
    Collapsed = 2
}

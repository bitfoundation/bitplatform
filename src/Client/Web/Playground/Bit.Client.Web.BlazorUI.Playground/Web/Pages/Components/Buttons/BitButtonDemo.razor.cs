namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitButtonDemo
    {
        private bool TogglePrimaryButtonChecked;
        private bool ToggleStandardButtonChecked;
        private bool ToggleDisabledButtonChecked;
        private bool ToggleButtonChecked;

        private bool ToggleButtonForOnChange = true;
        private bool OnToggleButtonChanged = true;

        private bool ToggleButtonTwoWayValue = true;
        private bool ToggleButtonTwoWayValue2 = true;

        private void ToggleButtonChanged(bool newValue)
        {
            OnToggleButtonChanged = newValue;
        }
    }
}

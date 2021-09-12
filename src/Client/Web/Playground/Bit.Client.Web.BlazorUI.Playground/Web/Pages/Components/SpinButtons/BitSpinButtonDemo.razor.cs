namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SpinButtons
{
    public partial class BitSpinButtonDemo
    {
        private double BasicSpinButtonValue = 5;
        private double BasicSpinButtonDisableValue = 20;
        private double SpinButtonWithCustomHandlerValue = 14;
        private double SpinButtonWithLabelAboveValue = 7;
        private double BitSpinButtonBindValue = 8;
        private double BitSpinButtonValueChanged = 16;
        
        private void HandleSpinButtonValueChange(double value)
        {
            SpinButtonWithCustomHandlerValue = value;
        }

        private void HandleControlledSpinButtonValueChange(double value)
        {
            BitSpinButtonValueChanged = value;
        }
    }
}

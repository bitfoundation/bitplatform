using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.SpinButtons
{
    public class BitSpinButtonTestModel
    {
        [Range(6, 18)]
        public double Value { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.NumericTextField
{
    public class BitNumericTextFieldTestModel
    {
        [Range(6, 18)]
        public double? Value { get; set; }
    }
}

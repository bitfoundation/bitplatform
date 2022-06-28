using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.Pickers
{
    public class BitDatePickerTestModel
    {
        [Required]
        public DateTimeOffset? Value { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.Checkboxes
{
    public class BitCheckboxTestModel
    {
        [Range(typeof(bool), "false", "false")]
        public bool Value { get; set; }
    }
}

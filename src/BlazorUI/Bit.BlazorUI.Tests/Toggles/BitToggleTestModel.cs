using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.Toggles
{
    public class BitToggleTestModel
    {
        [Range(typeof(bool), "false", "false")]
        public bool Value { get; set; }
    }
}

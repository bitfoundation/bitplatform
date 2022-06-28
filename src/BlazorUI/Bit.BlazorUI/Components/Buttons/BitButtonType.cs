using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI
{
    [DefaultValue(Button)]
    public enum BitButtonType
    {
        Button,
        Submit,
        Reset
    }

    public static class BitButtonTypeExtensions
    {
        public static string GetValue(this BitButtonType bitButtonType)
        {
            return bitButtonType switch
            {
                BitButtonType.Button => "button",
                BitButtonType.Submit => "submit",
                BitButtonType.Reset => "reset",
                _ => string.Empty,
            };
        }
    }
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI;

[DefaultValue(Button)]
public enum BitButtonType
{
    Button,
    Submit,
    Reset
}

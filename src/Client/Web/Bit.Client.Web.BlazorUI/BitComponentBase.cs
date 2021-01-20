using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class BitComponentBase : ComponentBase
    {
        [CascadingParameter]
        public Theme Theme { get; set; }

        [CascadingParameter]
        public Visual Visual { get; set; }

        public string VisualClass => Visual == Visual.Cupertino ? "cupertino" : Visual == Visual.Material ? "material" : "fluent";

        public string EnabledClass => IsEnabled ? "enabled" : "disabled";

        [Parameter]
        public bool IsEnabled { get; set; } = true;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                if (parameter.Name is nameof(IsEnabled))
                    IsEnabled = (bool)parameter.Value;
                else if (parameter.Name is nameof(Visual))
                    Visual = (Visual)parameter.Value;
                else if (parameter.Name is nameof(Theme))
                    Theme = (Theme)parameter.Value;
            }

            return base.SetParametersAsync(ParameterView.Empty);
        }
    }
}

using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class BitComponentBase : ComponentBase
    {
        [CascadingParameter] public Theme Theme { get; set; }

        [CascadingParameter] public Visual Visual { get; set; }

        [Parameter] public string Style { get; set; }

        [Parameter] public string Class { get; set; }

        [Parameter] public bool IsEnabled { get; set; } = true;

        public string EnabledClass => IsEnabled ? "enabled" : "disabled";

        public string VisualClass => Visual == Visual.Cupertino ? "cupertino" : Visual == Visual.Material ? "material" : "fluent";

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(IsEnabled):
                        IsEnabled = (bool)parameter.Value;
                        break;

                    case nameof(Visual):
                        Visual = (Visual)parameter.Value;
                        break;

                    case nameof(Theme):
                        Theme = (Theme)parameter.Value;
                        break;

                    case nameof(Style):
                        Style = (string)parameter.Value;
                        break;

                    case nameof(Class):
                        Class = (string)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(ParameterView.Empty);
        }
    }
}

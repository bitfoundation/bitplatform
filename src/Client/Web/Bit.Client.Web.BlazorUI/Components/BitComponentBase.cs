using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class BitComponentBase : ComponentBase
    {
        [CascadingParameter] public Theme Theme { get; set; }

        [CascadingParameter] public Visual Visual { get; set; }

        [Parameter] public bool IsEnabled { get; set; } = true;

        [Parameter]
        public string Style
        {
            get
            {
                var prefix = string.IsNullOrEmpty(_style) ? "" : ";";
                return $"{_style}{prefix}{GetVisibilityStyle()}";
            }
            set => _style = value;
        }

        [Parameter] public string Class { get; set; }

        [Parameter] public ComponentVisibility Visibility { get; set; }

        public string EnabledClass => IsEnabled ? "enabled" : "disabled";

        public string VisualClass => Visual == Visual.Cupertino ? "cupertino" : Visual == Visual.Material ? "material" : "fluent";

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Visual):
                        Visual = (Visual)parameter.Value;
                        break;

                    case nameof(Theme):
                        Theme = (Theme)parameter.Value;
                        break;

                    case nameof(IsEnabled):
                        IsEnabled = (bool)parameter.Value;
                        break;

                    case nameof(Style):
                        Style = (string)parameter.Value;
                        break;

                    case nameof(Class):
                        Class = (string)parameter.Value;
                        break;

                    case nameof(Visibility):
                        Visibility = (ComponentVisibility)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(ParameterView.Empty);
        }

        private string _style;

        private string GetVisibilityStyle()
        {
            return Visibility == ComponentVisibility.Hidden
                    ? "visibility:hidden"
                    : Visibility == ComponentVisibility.Collapsed
                        ? "display:none"
                        : "";
        }
    }
}

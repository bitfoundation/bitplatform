using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public class BitComponentBase : ComponentBase
    {
        public ElementReference Element { get; internal set; }

        [CascadingParameter] public Theme Theme { get; set; }

        [CascadingParameter] public Visual Visual { get; set; }

        [Parameter] public string Style { get; set; }

        [Parameter] public string Class { get; set; }

        [Parameter] public bool IsEnabled { get; set; } = true;

        [Parameter] public ComponentVisibility Visibility { get; set; }

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

        protected ElementStyleContainer ElementStyleContainer { get; private set; } = new ElementStyleContainer();

        protected ElementClassContainer ElementClassContainer { get; private set; } = new ElementClassContainer();

        protected virtual string GetElementStyle()
        {
            var stylePostfix = string.IsNullOrEmpty(Style) ? "" : ";";
            var elementStyle = ElementStyleContainer.Value;
            var elementStylePostfix = string.IsNullOrEmpty(elementStyle) ? "" : ";";
            var visibilityStyle = Visibility == ComponentVisibility.Hidden
                                    ? "visibility:hidden"
                                    : Visibility == ComponentVisibility.Collapsed
                                        ? "display:none"
                                        : string.Empty;
            return $"{Style}{stylePostfix}{elementStyle}{elementStylePostfix}{visibilityStyle}";
        }

        protected virtual string GetElementClass()
        {
            var enabledClass = IsEnabled ? "enabled" : "disabled";
            var visualClass = Visual == Visual.Cupertino
                                ? "cupertino"
                                : Visual == Visual.Material
                                    ? "material"
                                    : "fluent";
            return $"{Class} {enabledClass} {visualClass} {ElementClassContainer.Value}";
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLabel
    {
        private bool isRequired;

        [Parameter] public string For { get; set; }

        [Parameter]
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override string RootElementClass => "bit-label";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsRequired ? "required" : string.Empty);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;

                    case nameof(IsRequired):
                        IsRequired = (bool)parameter.Value;
                        break;

                    case nameof(For):
                        For = (string)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}

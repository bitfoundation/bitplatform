using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLabel
    {
        [Parameter] public string For { get; set; }

        [Parameter] public bool IsRequired { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-label");

            if (IsRequired)
            {
                ElementClassContainer.Add("required");
            }

            return base.GetElementClass();
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

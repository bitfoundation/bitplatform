using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private ChoiceGroupContext _context;
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter]
        public string Value
        {
            get => Value;
            set
            {
                Value = value;
                ValueChange();
            }
        }

        private void ValueChange()
        {

        }

        [CascadingParameter] private ChoiceGroupContext? CascadedContext { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;
                    case nameof(Name):
                        Name = (string)parameter.Value;
                        break;
                    case nameof(Value):
                        Value = (string)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            var changeEventCallback = EventCallback.Factory.CreateBinder<string>(this, value => Value = value, Value);
            _context = new ChoiceGroupContext(CascadedContext, Name, changeEventCallback);
        }
    }
}

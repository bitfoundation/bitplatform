using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private ChoiceGroupContext _context;
        private event EventHandler<ChangeEventArgs> ChangeEvent;

        [Parameter] public string Name { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public List<ChoiceItem> Items { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Items):
                        Items = (List<ChoiceItem>)parameter.Value;
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

        protected override async Task OnInitializedAsync()
        {
            ChangeEvent += HandleChange;
            _context = new ChoiceGroupContext(Name, ChangeEvent);
        }
        private void HandleChange(object? sender, ChangeEventArgs e)
        {
            var option = (BitChoiceOption)sender;
            Items.ForEach(item => item.IsChecked = item.Value.Equals(option.Value));
            StateHasChanged();
        }
    }
}

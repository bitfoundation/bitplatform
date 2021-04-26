using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceOption : IDisposable
    {
        private string _id = Guid.NewGuid().ToString();
        private bool isChecked = false;

        [Parameter] public string Text { get; set; }
        [Parameter] public string Name { get; set; }
        [Parameter] public string Value { get; set; }

        [Parameter]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

        [CascadingParameter] protected BitChoiceGroup ChoiceGroup { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Text):
                        Text = (string)parameter.Value;
                        break;
                    case nameof(Name):
                        Name = (string)parameter.Value;
                        break;
                    case nameof(Value):
                        Value = (string)parameter.Value;
                        break;
                    case nameof(IsChecked):
                        IsChecked = (bool)parameter.Value;
                        break;
                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<ChangeEventArgs>)parameter.Value;
                        break;
                    case nameof(ChoiceGroup):
                        ChoiceGroup = (BitChoiceGroup)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }

        protected override Task OnInitializedAsync()
        {
            if (ChoiceGroup is not null)
            {
                ChoiceGroup.RegisterOption(this);
                if (string.IsNullOrEmpty(Name))
                    Name = ChoiceGroup.Name;
            }
            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-choice-option";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsChecked is true ? "checked" : string.Empty);
        }

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (ChoiceGroup is not null)
                    await ChoiceGroup.ChangeSelection(this);
                await OnClick.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                await OnChange.InvokeAsync(e);
            }
        }

        internal void SetOptionCheckedStatus(bool status)
        {
            IsChecked = status;
            StateHasChanged();
        }

        public void Dispose()
        {
            if (ChoiceGroup is not null)
                ChoiceGroup.UnregisterRadio(this);
        }
    }
}

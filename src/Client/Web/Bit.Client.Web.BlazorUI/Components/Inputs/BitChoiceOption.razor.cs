using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Components.Inputs;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceOption
    {
        internal ChoiceGroupContext? Context { get; private set; }

        [Parameter] public string Text { get; set; }
        [Parameter] public string Name { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public bool IsChecked { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

        [CascadingParameter] private ChoiceGroupContext? CascadedContext { get; set; }

        public string CheckedClass => IsChecked ? "checked" : "";
        public string Id = Guid.NewGuid().ToString();

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
                }
            }

            return base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            Context = string.IsNullOrEmpty(Name) ? CascadedContext : CascadedContext?.FindContextInAncestors(Name);

            if (Context == null)
            {
                throw new InvalidOperationException($"{GetType()} must have an ancestor {typeof(BitChoiceGroup)} " +
                                                    $"with a matching 'Name' property, if specified.");
            }
        }

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-choice-option");
            if (!IsEnabled)
            {
                ElementClassContainer.Add("disabled");
            }
            return base.GetElementClass();
        }

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                IsChecked = true;
                await OnClick.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                await OnChange.InvokeAsync(e);
                await CascadedContext.ChangeEventCallback.InvokeAsync(e);
            }
        }
    }
}
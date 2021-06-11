using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceOption : IDisposable
    {
        private bool isChecked;

        [Parameter] public string? Text { get; set; }

        [Parameter] public string? Name { get; set; }

        [Parameter] public string? Value { get; set; }

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

        [CascadingParameter] protected BitChoiceGroup? ChoiceGroup { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

            foreach (var parameter in parametersDictionary!)
            {
                switch (parameter.Key)
                {
                    case nameof(Text):
                        Text = (string)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(Name):
                        Name = (string)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(Value):
                        Value = (string)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(IsChecked):
                        IsChecked = (bool)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(OnChange):
                        OnChange = (EventCallback<ChangeEventArgs>)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(ChoiceGroup):
                        ChoiceGroup = (BitChoiceGroup)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;
                }
            }
            return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary));
        }

        protected override Task OnInitializedAsync()
        {
            if (ChoiceGroup is not null)
            {
                ChoiceGroup.RegisterOption(this);
                if (Name.HasNoValue())
                {
                    Name = ChoiceGroup.Name;
                }
            }
            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-cho";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsChecked is false
                ? string.Empty
                : $"{RootElementClass}-checked-{VisualClassRegistrar()}");
        }

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (ChoiceGroup is not null)
                {
                    await ChoiceGroup.ChangeSelection(this);
                }
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

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (ChoiceGroup is not null)
            {
                ChoiceGroup.UnregisterOption(this);
            }

            _disposed = true;
        }
    }
}

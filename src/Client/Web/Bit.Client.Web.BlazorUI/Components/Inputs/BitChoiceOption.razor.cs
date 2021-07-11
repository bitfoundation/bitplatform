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

        /// <summary>
        /// ChoiceOption content, It can be a text
        /// </summary>
        [Parameter] public string? Text { get; set; }

        /// <summary>
        /// This value is used to group each ChoiceGroupOption into the same logical ChoiceGroup
        /// </summary>
        [Parameter] public string? Name { get; set; }

        /// <summary>
        /// Value of selected ChoiceOption
        /// </summary>
        [Parameter] public string? Value { get; set; }

        /// <summary>
        /// Whether or not the option is checked
        /// </summary>
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

        /// <summary>
        /// Callback for when the ChoiceOption clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Callback for when the option has been changed
        /// </summary>
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

        [CascadingParameter] protected BitChoiceGroup? ChoiceGroup { get; set; }

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

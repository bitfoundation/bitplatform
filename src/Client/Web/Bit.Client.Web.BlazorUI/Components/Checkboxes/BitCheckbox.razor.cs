using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        private bool isIndeterminate;
        private BoxSide boxSide;
        private bool isChecked;

        public ElementReference CheckboxElement { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public bool IsChecked
        {
            get => isChecked; set
            {
                isChecked = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public BoxSide BoxSide
        {
            get => boxSide; set
            {
                boxSide = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                isIndeterminate = value;
                JSRuntime.SetProperty(CheckboxElement, "indeterminate", value);
                ClassBuilder.Reset();
            }
        }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<bool> OnChange { get; set; }

        protected override string RootElementClass => "bit-checkbox-container";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsIndeterminate ? "indeterminate" : string.Empty);
            ClassBuilder.Register(() => IsChecked ? "checked" : string.Empty);
            ClassBuilder.Register(() => BoxSide == BoxSide.End ? "box-side-end" : "box-side-start");
        }

        protected async Task HandleCheckboxClick(MouseEventArgs e)
        {
            if (IsEnabled is false) return;

            if (IsIndeterminate)
            {
                IsIndeterminate = false;
            }
            else
            {
                IsChecked = !IsChecked;
            }

            await OnChange.InvokeAsync(IsChecked);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.SetProperty(CheckboxElement, "indeterminate", IsIndeterminate);
            }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}

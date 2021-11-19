using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivotItem : IDisposable
    {
        /// <summary>
        /// The parent BitPivot component instance
        /// </summary>
        [CascadingParameter] public BitPivot? Pivot { get; set; }

        /// <summary>
        /// The content of the pivot item, It can be Any custom tag or a text 
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// The content of the pivot item header, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? HeaderFragment { get; set; }

        /// <summary>
        /// The content of the pivot item can be Any custom tag or a text, If HeaderContent provided value of this parameter show, otherwise use ChildContent
        /// </summary>
        [Parameter] public RenderFragment? BodyFragment { get; set; }

        /// <summary>
        /// The text of the pivot item header, The text displayed of each pivot link
        /// </summary>
        [Parameter] public string? HeaderText { get; set; }

        /// <summary>
        /// The icon name for the icon shown next to the pivot link
        /// </summary>
        [Parameter] public BitIcon? IconName { get; set; }

        /// <summary>
        /// Defines an optional item count displayed in parentheses just after the linkText
        /// </summary>
        [Parameter] public int? ItemCount { get; set; }

        /// <summary>
        /// A required key to uniquely identify a pivot item
        /// </summary>
        [Parameter] public string? Key { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                ClassBuilder.Reset();
            }
        }

        internal void SetState(bool status)
        {
            IsSelected = status;
            StateHasChanged();
        }

        protected override Task OnInitializedAsync()
        {
            if (Pivot is not null)
            {
                Pivot.RegisterItem(this);
            }
            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-pvt-itm";
        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsSelected ? $"{RootElementClass}-selcted-{VisualClassRegistrar()}" : string.Empty);
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

            if (Pivot is not null)
            {
                Pivot.UnregisterItem(this);
            }

            _disposed = true;
        }

        private void HandleButtonClick()
        {
            if (IsEnabled is false) return;

            Pivot?.SelectItem(this);
        }
    }
}

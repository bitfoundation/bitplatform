using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDropDown
    {
        private string focusClass = "";
        private string expandClass = "";
        private DropDownItem selectedItem ;

        [Parameter] public List<DropDownItem> Items { get; set; } = new List<DropDownItem>();
        [Parameter] public string Placeholder { get; set; }
        
        [Parameter]
        public DropDownItem SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        public string FocusClass
        {
            get => focusClass;
            set
            {
                focusClass = value;
                ClassBuilder.Reset();
            }
        }
        public string ExpandClass
        {
            get => expandClass;
            set
            {
                expandClass = value;
                ClassBuilder.Reset();
            }
        }

        protected override string RootElementClass => "bit-drp";
        protected override void RegisterComponentClasses()
        {
            //ClassBuilder.Register(() => IsChecked is true ?
            //    $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => string.IsNullOrWhiteSpace(FocusClass)
                ? string.Empty
                : $"{RootElementClass}-{FocusClass}-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => string.IsNullOrWhiteSpace(ExpandClass)
                ? string.Empty
                : $"{RootElementClass}-{ExpandClass}-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => SelectedItem is null
                ? string.Empty
                : $"{RootElementClass}-{"hasValue"}-{VisualClassRegistrar()}");
        }

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "focused";
                await OnClick.InvokeAsync(e);
            }
        }
       
    }
}

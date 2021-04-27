using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLabel
    {
        private bool isRequired;

        [Parameter] public string For { get; set; }

        [Parameter]
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override string RootElementClass => "bit-lbl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);
        }
    }
}

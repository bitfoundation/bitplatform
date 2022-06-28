using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI
{
    public partial class BitLabel
    {
        private bool isRequired;

        /// <summary>
        /// This attribute specifies which form element a label is bound to
        /// </summary>
        [Parameter] public string? For { get; set; }

        /// <summary>
        /// Whether the associated field is required or not, it shows a star above of it
        /// </summary>
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

        /// <summary>
        /// The content of label, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override string RootElementClass => "bit-lbl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);
        }
    }
}

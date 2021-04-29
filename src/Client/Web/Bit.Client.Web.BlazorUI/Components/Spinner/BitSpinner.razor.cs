using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinner
    {
        [Parameter] public SpinnerLabelPosition LabelPosition { get; set; }
        [Parameter] public SpinnerSize Size { get; set; }
        [Parameter] public string Label { get; set; }

        protected override string RootElementClass => "bit-spinner";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => GetClassSize());
            ClassBuilder.Register(() => GetClassLabelPosition());
        }

        private string GetClassSize()
        {
            string classSize = $"{RootElementClass}-medium-{VisualClassRegistrar()}";

            switch (Size)
            {
                case SpinnerSize.XSmall:
                    classSize = $"{RootElementClass}-xSmall-{VisualClassRegistrar()}";
                    break;
                case SpinnerSize.Small:
                    classSize = $"{RootElementClass}-small-{VisualClassRegistrar()}";
                    break;
                case SpinnerSize.Large:
                    classSize = $"{RootElementClass}-large-{VisualClassRegistrar()}";
                    break;
            }

            return classSize;
        }

        private string GetClassLabelPosition()
        {
            string classLabelPosition = $"{RootElementClass}-bottom-{VisualClassRegistrar()}";

            switch (LabelPosition)
            {
                case SpinnerLabelPosition.Top:
                    classLabelPosition = $"{RootElementClass}-top-{VisualClassRegistrar()}";
                    break;
                case SpinnerLabelPosition.Left:
                    classLabelPosition = $"{RootElementClass}-left-{VisualClassRegistrar()}";
                    break;
                case SpinnerLabelPosition.Right:
                    classLabelPosition = $"{RootElementClass}-right-{VisualClassRegistrar()}";
                    break;
            }

            return classLabelPosition;
        }
    }
}

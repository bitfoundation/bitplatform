using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinner
    {
        [Parameter] public SpinnerLabelPosition LabelPosition { get; set; }
        [Parameter] public SpinnerSize Size { get; set; }
        [Parameter] public string Label { get; set; }

        protected override string RootElementClass => "bit-spn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(GetClassSize);
            ClassBuilder.Register(GetClassLabelPosition);
        }

        private string GetClassSize()
        {
            string classSize = string.Empty;

            switch (Size)
            {
                case SpinnerSize.XSmall:
                    classSize = "xSmall";
                    break;
                case SpinnerSize.Small:
                    classSize = "small";
                    break;
                case SpinnerSize.Medium:
                    classSize = "medium";
                    break;
                case SpinnerSize.Large:
                    classSize = "large";
                    break;
            }

            return $"{RootElementClass}-{classSize}-{VisualClassRegistrar()}";
        }

        private string GetClassLabelPosition()
        {
            string classLabelPosition = string.Empty;

            switch (LabelPosition)
            {
                case SpinnerLabelPosition.Top:
                    classLabelPosition = "top";
                    break;
                case SpinnerLabelPosition.Left:
                    classLabelPosition = "left";
                    break;
                case SpinnerLabelPosition.Right:
                    classLabelPosition = "right";
                    break;
                case SpinnerLabelPosition.Bottom:
                    classLabelPosition = "bottom";
                    break;
            }

            return $"{RootElementClass}-{classLabelPosition}-{VisualClassRegistrar()}";
        }
    }
}

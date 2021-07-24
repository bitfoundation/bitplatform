using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinner
    {
        /// <summary>
        /// The position of the label in regards of the spinner animation
        /// </summary>
        [Parameter] public SpinnerLabelPosition LabelPosition { get; set; }

        /// <summary>
        /// The size of spinner to render
        /// </summary>
        [Parameter] public SpinnerSize Size { get; set; }

        /// <summary>
        /// The label to show next to the spinner. Label updates will be announced to the screen readers
        /// </summary>
        [Parameter] public string? Label { get; set; }

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

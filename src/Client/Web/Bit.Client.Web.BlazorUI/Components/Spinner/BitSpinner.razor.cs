using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinner
    {
        /// <summary>
        /// Politeness setting for label update announcement.
        /// </summary>
        [Parameter] public BitSpinnerAriaLive AriaLive { get; set; } = BitSpinnerAriaLive.Polite;

        /// <summary>
        /// The position of the label in regards to the spinner animation
        /// </summary>
        [Parameter] public BitSpinnerLabelPosition LabelPosition { get; set; }

        /// <summary>
        /// The size of spinner to render
        /// </summary>
        [Parameter] public BitSpinnerSize Size { get; set; }

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
                case BitSpinnerSize.XSmall:
                    classSize = "xSmall";
                    break;

                case BitSpinnerSize.Small:
                    classSize = "small";
                    break;

                case BitSpinnerSize.Medium:
                    classSize = "medium";
                    break;

                case BitSpinnerSize.Large:
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
                case BitSpinnerLabelPosition.Top:
                    classLabelPosition = "top";
                    break;

                case BitSpinnerLabelPosition.Left:
                    classLabelPosition = "left";
                    break;

                case BitSpinnerLabelPosition.Right:
                    classLabelPosition = "right";
                    break;

                case BitSpinnerLabelPosition.Bottom:
                    classLabelPosition = "bottom";
                    break;
            }

            return $"{RootElementClass}-{classLabelPosition}-{VisualClassRegistrar()}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitProgressIndicator
    {
        private double percentComplete;
        private bool PercentCompleteHasBeenSet;

        /// <summary>
        /// Label to display above the component
        /// </summary>
        [Parameter] public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Height of the ProgressIndicator
        /// </summary>
        [Parameter] public int BarHeight { get; set; } = 2;

        /// <summary>
        /// Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead
        /// </summary>
        [Parameter]
        public double PercentComplete
        {
            get => percentComplete;
            set => percentComplete = Normalize(value);
        }

        /// <summary>
        /// Text describing or supplementing the operation
        /// </summary>
        [Parameter] public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Text alternative of the progress status, used by screen readers for reading the value of the progress
        /// </summary>
        [Parameter] public string AriaValueText { get; set; } = string.Empty;

        /// <summary>
        /// Whether or not to hide the progress state
        /// </summary>
        [Parameter] public bool IsProgressHidden { get; set; }

        /// <summary>
        /// A custom template for progress track
        /// </summary>
        [Parameter] public RenderFragment<BitProgressIndicator>? ProgressTemplate { get; set; }

        public string LabelId { get; set; } = string.Empty;
        public string DescriptionId { get; set; } = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
            LabelId = $"progress-indicator{UniqueId}-label";
            DescriptionId = $"progress-indicator{UniqueId}-description";

            await base.OnParametersSetAsync();
        }
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            PercentCompleteHasBeenSet = false;

            var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;
            foreach (var parameter in parametersDictionary!)
            {
                switch (parameter.Key)
                {
                    case nameof(PercentComplete):
                        PercentCompleteHasBeenSet = true;
                        PercentComplete = (double)parameter.Value;
                        break;
                    case nameof(Label):
                        Label = (string)parameter.Value;
                        break;
                    case nameof(BarHeight):
                        BarHeight = (int)parameter.Value;
                        break;
                    case nameof(Description):
                        Description = (string)parameter.Value;
                        break;
                }
            }
            await base.SetParametersAsync(parameters);
        }

        protected override string RootElementClass => "bit-pi";
        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => PercentCompleteHasBeenSet ? string.Empty
                                                : $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}");
        }

        private static double Normalize(double value) => Math.Clamp(value, 0, 100);
    }
}

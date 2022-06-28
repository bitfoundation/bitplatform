using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI
{
    public partial class BitRating
    {
        private bool isReadOnly;

        /// <summary>
        /// A flag to mark rating control as readOnly
        /// </summary>
        [Parameter]
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                isReadOnly = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard
        /// </summary>
        [Parameter] public bool AllowZeroStars { get; set; }

        /// <summary>
        /// Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers. 
        /// Placeholder {0} is the current rating and placeholder {1} is the max: for example, 
        /// "Select {0} of {1} stars". (To set the label for the control as a whole, use getAriaLabel or aria-label.)
        /// </summary>
        [Parameter] public string? AriaLabelFormat { get; set; }

        /// <summary>
        /// Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise)
        /// </summary>
        [Parameter] public int Max { get; set; } = 5;

        /// <summary>
        /// Custom icon name for selected rating elements, If unset, default will be the FavoriteStarFill icon
        /// </summary>
        [Parameter] public BitIconName Icon { get; set; } = BitIconName.FavoriteStarFill;

        /// <summary>
        /// Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon
        /// </summary>
        [Parameter] public BitIconName UnselectedIcon { get; set; } = BitIconName.FavoriteStar;

        /// <summary>
        /// Default value. Must be a number between min and max. 
        /// Only provide this if the CurrentValue is an uncontrolled component; otherwise, use the Value property.
        /// </summary>
        [Parameter] public double? DefaultValue { get; set; }

        /// <summary>
        /// Size of rating
        /// </summary>
        [Parameter] public BitRatingSize Size { get; set; }

        /// <summary>
        /// Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.
        /// </summary>
        [Parameter] public Func<double, double, string>? GetAriaLabel { get; set; }

        /// <summary>
        /// Callback that is called when the rating has changed
        /// </summary>
        [Parameter] public EventCallback<double> OnChange { get; set; }

        protected override async Task OnInitializedAsync()
        {
            OnCurrentValueChanged += HandleOnCurrentValueChanged;

            if (DefaultValue.HasValue)
            {
                CurrentValue = DefaultValue.Value;
            }

            CurrentValue = Math.Min(Math.Max(CurrentValue, (AllowZeroStars ? 0 : 1)), Max);
            
            await base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-rating";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadOnly
                                                ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                                : string.Empty);

            ClassBuilder.Register(() => Size == BitRatingSize.Large
                                                ? $"{RootElementClass}-large-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-small-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => ValueInvalid is true
                                                ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
                                                : string.Empty);
        }

        private double GetPercentageOf(int starNumber)
        {
            double fullRating = Math.Ceiling(CurrentValue);
            double fullStar = 100;

            if (starNumber == CurrentValue)
            {
                fullStar = 100;
            }
            else if (starNumber == fullRating)
            {
                fullStar = 100 * (CurrentValue % 1);
            }
            else if (starNumber > fullRating)
            {
                fullStar = 0;
            }

            return fullStar;
        }

        private async Task HandleClick(int index)
        {
            if ((AllowZeroStars is false && index == 0) ||
                IsReadOnly is true ||
                IsEnabled is false ||
                ValueChanged.HasDelegate is false) return;

            CurrentValue = index;
            await OnChange.InvokeAsync(CurrentValue);
        }

        private void HandleOnCurrentValueChanged(object? sender, EventArgs args) => ClassBuilder.Reset();

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, out double result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            if (double.TryParse(value, out var parsedValue))
            {
                result = parsedValue;
                validationErrorMessage = null;
                return true;
            }

            result = default;
            validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnCurrentValueChanged -= HandleOnCurrentValueChanged;
            }

            base.Dispose(disposing);
        }
    }
}

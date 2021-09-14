using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRating
    {
        private bool isReadOnly;
        private int ratingValue;
        private bool ValueHasBeenSet;

        private string[] RatingColorClasses { get; set; } = Array.Empty<string>();

        public string[] RatingIcons { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers. 
        /// Placeholder {0} is the current rating and placeholder {1} is the max: for example, 
        /// "Select {0} of {1} stars". (To set the label for the control as a whole, use getAriaLabel or aria-label.)
        /// </summary>
        [Parameter] public string? AriaLabelFormat { get; set; }

        /// <summary>
        /// Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard
        /// </summary>
        [Parameter] public bool AllowZeroStars { get; set; }

        /// <summary>
        /// Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise)
        /// </summary>
        [Parameter] public int Max { get; set; } = 5;

        /// <summary>
        /// Custom icon name for selected rating elements, If unset, default will be the FavoriteStarFill icon
        /// </summary>
        [Parameter] public string Icon { get; set; } = "FavoriteStarFill";

        /// <summary>
        /// Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon
        /// </summary>
        [Parameter] public string UnselectedIcon { get; set; } = "FavoriteStar";

        /// <summary>
        /// Size of rating
        /// </summary>
        [Parameter] public RatingSize Size { get; set; }

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
        /// Current rating value. Must be a number between min (0 if AllowZeroStars is true, 1 otherwise) and max.
        /// </summary>
        [Parameter]
        public int Value
        {
            get => ratingValue;
            set
            {
                if (value == ratingValue) return;
                ratingValue = value;

                FillRating(ratingValue);

                ClassBuilder.Reset();
                _ = ValueChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Callback that is called when the rating value changed
        /// </summary>
        [Parameter] public EventCallback<int> ValueChanged { get; set; }

        /// <summary>
        /// Callback that is called when the rating has changed
        /// </summary>
        [Parameter] public EventCallback<int> OnChange { get; set; }

        private string? _colorClass;
        private int _min;

        protected override string RootElementClass => "bit-rating";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadOnly
                                                ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                                : string.Empty);
            ClassBuilder.Register(() => Size == RatingSize.Large
                                                ? $"{RootElementClass}-large-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-small-{VisualClassRegistrar()}");
        }

        protected override async Task OnInitializedAsync()
        {
            _colorClass = $"{RootElementClass}-dark-{VisualClassRegistrar()}";

            _min = AllowZeroStars ? 0 : 1;
            Max = Max > _min ? Max : _min;

            RatingColorClasses = new string[Max + 1];
            RatingIcons = new string[Max + 1];

            FillRating(Value > 0 ? Value : _min);

            await base.OnInitializedAsync();
        }

        private async Task HandleClick(int index)
        {
            if ((_min == 1 && index == 0) || IsReadOnly is true || IsEnabled is false || ValueChanged.HasDelegate is false) return;

            Value = index;

            await OnChange.InvokeAsync(Value);
        }

        private void FillRating(int index)
        {
            if (RatingIcons.Length == 0 || RatingColorClasses.Length == 0) return;

            if (AllowZeroStars is false && index == 0)
            {
                index = 1;
            }

            EmptyRating();

            for (var item = 0; item < index; item++)
            {
                RatingIcons![item] = Icon;
                RatingColorClasses![item] = _colorClass!;
            }
        }

        private void EmptyRating()
        {
            Array.Fill(RatingIcons!, UnselectedIcon);
            Array.Fill(RatingColorClasses!, _colorClass);
        }

        private string UniqId(string prefix)
        {
            string randomNum = new Random().Next(10000, 99999).ToString("D5");
            return $"{prefix}{randomNum}";
        }
    }
}

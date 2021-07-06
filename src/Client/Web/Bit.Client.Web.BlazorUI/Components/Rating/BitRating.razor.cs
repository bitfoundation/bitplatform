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

        [Parameter] public bool AllowZeroStars { get; set; }

        [Parameter] public int Max { get; set; } = 5;

        [Parameter] public string Icon { get; set; } = "FavoriteStarFill";

        [Parameter] public string UnselectedIcon { get; set; } = "FavoriteStar";

        [Parameter] public RatingSize Size { get; set; }

        [Parameter]
        public bool IsReadonly
        {
            get => isReadOnly;
            set
            {
                isReadOnly = value;
                ClassBuilder.Reset();
            }
        }

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

        [Parameter] public EventCallback<int> ValueChanged { get; set; }

        [Parameter] public EventCallback<int> OnChange { get; set; }

        private string? _colorClass;
        private int _min;

        protected override string RootElementClass => "bit-rating";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadonly
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
            if ((_min == 1 && index == 0) || IsReadonly is true || IsEnabled is false || ValueChanged.HasDelegate is false) return;

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
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRating
    {
        [Parameter] public bool AllowZeroStars { get; set; }
        [Parameter] public int DefaultRating { get; set; }
        [Parameter] public int Max { get; set; } = 5;
        [Parameter] public string Icon { get; set; } = "FavoriteStarFill";
        [Parameter] public string UnselectedIcon { get; set; } = "FavoriteStar";
        [Parameter] public RatingSize Size { get; set; }
        [Parameter] public EventCallback<int> ValueChanged { get; set; }
        [Parameter] public EventCallback<int> OnChange { get; set; }

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
                ratingValue = value;
                ClassBuilder.Reset();

                _ = ValueChanged.InvokeAsync(value);
            }
        }

        public string[] RatingColorClass { get; set; }
        public string[] RatingIcon { get; set; }

        private string colorClass { get; set; } = "bit-rating-dark-fluent";
        private int min { get; set; }

        private bool isReadOnly;
        private int ratingValue;

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
            min = AllowZeroStars == true ? 0 : 1;
            Max = Max > min ? Max : min;
            DefaultRating = DefaultRating > 0 ? DefaultRating : min;
            ratingValue = DefaultRating > 0 ? DefaultRating : ratingValue;

            RatingColorClass = new string[Max + 1];
            RatingIcon = new string[Max + 1];

            FillRating(DefaultRating);

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleClick(int index)
        {
            FillRating(index);

            if (IsEnabled is false) return;
            Value = index;

            await OnChange.InvokeAsync(index);
        }

        private void FillRating(int index)
        {
            EmptyRating();

            for (var item = 0; item < index; item++)
            {
                RatingIcon[item] = Icon;
                RatingColorClass[item] = colorClass;
            }
        }

        private void EmptyRating()
        {
            Array.Fill(RatingIcon, UnselectedIcon);
            Array.Fill(RatingColorClass, colorClass);
        }
    }
}

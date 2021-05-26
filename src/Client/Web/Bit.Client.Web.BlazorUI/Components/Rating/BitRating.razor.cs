using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRating
    {

        private bool isReadOnly;
        private int ratingValue;
        private string colorClass;
        private int min;

        public string[] RatingColorClass { get; set; }
        public string[] RatingIcon { get; set; }
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
                ClassBuilder.Reset();
                _ = ValueChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<int> ValueChanged { get; set; }
        [Parameter] public EventCallback<int> OnChange { get; set; }

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
            colorClass = $"{RootElementClass}-dark-{VisualClassRegistrar()}";

            min = AllowZeroStars == true ? 0 : 1;
            Max = Max > min ? Max : min;

            RatingColorClass = new string[Max + 1];
            RatingIcon = new string[Max + 1];

            FillRating(Value > 0 ? Value : min);

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleClick(int index)
        {
            if ((min == 1 && index == 0) || IsReadonly is true || IsEnabled is false || ValueChanged.HasDelegate is false) return;

            Value = index;

            FillRating(index);

            await OnChange.InvokeAsync(Value);
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

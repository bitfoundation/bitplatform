using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

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

        public string[] RatingColorClass { get; set; }
        public string[] RatingIcon { get; set; }

        private string colorClass { get; set; } = "bit-rating-dark-fluent";
        private int min { get; set; }

        private bool isReadOnly;

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

            RatingColorClass = new string[Max + 1];
            RatingIcon = new string[Max + 1];

            FillRating(DefaultRating);

            await base.OnInitializedAsync();
        }

        protected virtual void HandleClick(int index)
        {
            FillRating(index);
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

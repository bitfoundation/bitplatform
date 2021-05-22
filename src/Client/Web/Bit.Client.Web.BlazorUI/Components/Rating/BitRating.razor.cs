using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRating
    {
        private bool isReadOnly;

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

        private string[] RatingColorClasses = Array.Empty<string>();

        private string[] RatingIcons = Array.Empty<string>();

        private const string COLOR_CLASS = "bit-rating-dark-fluent";

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
            int min = AllowZeroStars == true ? 0 : 1;
            Max = Max > min ? Max : min;
            DefaultRating = DefaultRating > 0 ? DefaultRating : min;

            RatingColorClasses = new string[Max + 1];
            RatingIcons = new string[Max + 1];

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
                RatingIcons[item] = Icon;
                RatingColorClasses[item] = COLOR_CLASS;
            }
        }

        private void EmptyRating()
        {
            Array.Fill(RatingIcons, UnselectedIcon);
            Array.Fill(RatingColorClasses, COLOR_CLASS);
        }
    }
}

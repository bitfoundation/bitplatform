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
        [Parameter] public string Icon { get; set; } = "FavoriteStar";
        [Parameter] public string FillIcon { get; set; } = "FavoriteStarFill";
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

        public string[] RatingColor { get; set; }
        public string[] RatingIcon { get; set; }

        private string color { get; set; } = "#1B1A19";
        private int min { get; set; }

        private bool isReadOnly;

        protected override string RootElementClass => "";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => Size == RatingSize.Large ? "rating-star-large" : "rating-star-small");
            ClassBuilder.Register(() => IsReadonly ? "readonly" : string.Empty);
        }

        protected override async Task OnInitializedAsync()
        {
            min = AllowZeroStars == true ? 0 : 1;
            Max = Max > min ? Max : min;
            DefaultRating = DefaultRating > 0 ? DefaultRating : min;

            RatingColor = new string[Max + 1];
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
                RatingIcon[item] = FillIcon;
                RatingColor[item] = color;
            }
        }

        private void EmptyRating()
        {
            Array.Fill(RatingIcon, Icon);
            Array.Fill(RatingColor, color);
        }
    }
}

using Bit.Websites.Careers.Shared.Dtos.Account;

namespace Bit.Websites.Careers.Client.Pages
{
    public partial class HomePage
    {
        private BitCarousel? carousel;

        private List<IntroBoxDto> SuggestedArticles;
        private List<IntroBoxDto> SuggestedVideos;
        private List<CareerDto> Careers;

        private void GoNext() => carousel?.GoNext();

        protected override async Task OnInitAsync()
        {
            SuggestedArticles = new List<IntroBoxDto>
            {
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "تاثیر Chat GTP رو محتوای سایت‌های Bit",
                    ImgUrl = "/images/main-page/sample-img.png",
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Article,
                    StudyTime = "۱۰"
                },
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "تاثیر Chat GTP رو محتوای سایت‌های Bit",
                    ImgUrl = "/images/main-page/sample-img.png",    
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Article,
                    StudyTime = "۱۰"
                },
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "تاثیر Chat GTP رو محتوای سایت‌های Bit",
                    ImgUrl = "/images/main-page/sample-img.png",
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Article,
                    StudyTime = "۱۰"
                },

            };

            SuggestedVideos = new List<IntroBoxDto>
            {
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "درباره bit platform ",
                    ImgUrl = "/images/main-page/sample-img.png",
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Video,
                    WatchingTime = "۱۰"
                },
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "درباره bit platform ",
                    ImgUrl = "/images/main-page/sample-img.png",
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Video,
                    WatchingTime = "۱۰"
                },
                new IntroBoxDto
                {
                    Id = 1,
                    Subject = "درباره bit platform ",
                    ImgUrl = "/images/main-page/sample-img.png",
                    AuthorName = "سارا نیازی",
                    AuthorAvatarUrl = "/images/main-page/sample-author-avatar.svg",
                    UploadDate = "۲۲ مهر ۱۴۰۰",
                    Type = IntroBoxType.Video,
                    WatchingTime = "۱۰"
                },

            };

            Careers = new List<CareerDto> {
                new CareerDto
                {
                    Id = 1,
                    Title = "(Product Manager) مدیر پروژه",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },
                new CareerDto
                {
                    Id = 1,
                    Title = " (Scrum Master) اسکرام مستر",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },
                new CareerDto
                {
                    Id = 1,
                    Title = "(Developer) برنامه نویس",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },
                new CareerDto
                {
                    Id = 1,
                    Title = "(Product Manager) مدیر پروژه",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },
                new CareerDto
                {
                    Id = 1,
                    Title = " (Scrum Master) اسکرام مستر",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },
                new CareerDto
                {
                    Id = 1,
                    Title = "(Developer) برنامه نویس",
                    ImgUrl = "/images/main-page/product-manager-img.svg"
                },

            };

            await base.OnInitAsync();
        }
    }
}

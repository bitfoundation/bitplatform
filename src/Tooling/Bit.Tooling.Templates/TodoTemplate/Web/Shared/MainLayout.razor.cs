using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Shared
{
    public partial class MainLayout
    {
        public bool IsSignedInUser { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;


            if (AuthenticationStateTask.Result.User.Identity != null)
            {
                IsSignedInUser = user.Identity!.IsAuthenticated;
            }

            await base.OnInitializedAsync();
        }
    }
}

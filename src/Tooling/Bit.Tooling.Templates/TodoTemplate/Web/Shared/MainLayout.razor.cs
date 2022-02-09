using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Shared
{
    public partial class MainLayout
    {
        public bool IsSignedInUser { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateTask;
          
            if (AuthenticationStateTask.Result.User.Identity != null)
            {
                IsSignedInUser = authState.User.Identity!.IsAuthenticated;
            }

            await base.OnInitializedAsync();
        }
    }
}

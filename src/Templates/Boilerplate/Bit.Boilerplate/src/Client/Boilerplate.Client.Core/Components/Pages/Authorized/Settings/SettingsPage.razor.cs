﻿//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SettingsPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Settings)];
    protected override string? Subtitle => string.Empty;


    [Parameter] public string? Section { get; set; }


    [AutoInject] private IUserController userController = default!;


    private UserDto? user;
    private bool isLoading;
    private string? profileImageUrl;
    private string? openedAccordion;


    protected override async Task OnInitAsync()
    {
        openedAccordion = Section?.ToLower();

        isLoading = true;

        try
        {
            user = user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", JsonSerializerOptions.GetTypeInfo<UserDto>(), CurrentCancellationToken)))!;

            var accessToken = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessToken);
            profileImageUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProfileImage?access_token={accessToken}").ToString();
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }
}

using Microsoft.AspNetCore.Components.Forms;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public partial class ProfileSection
{
    private bool isSaving;
    private bool isUploading;
    private SettingsProfile? seededProfile;
    private BitFileUpload fileUploadRef = default!;
    private EditUserRequestDto editUserDto = new();
    private EditContext editContext = new(new EditUserRequestDto());

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    private string? ProfileImageUrl => Page.Profile?.HasPicture is true ? SettingsDemoData.ProfilePhotoUrl : null;

    protected override void OnInitialized()
    {
        Page.ProfileChanged += OnProfileChanged;
        Seed();
    }

    private void OnProfileChanged()
    {
        Seed();
        StateHasChanged();
    }

    private void Seed()
    {
        if (Page.Profile is not { } profile || ReferenceEquals(profile, seededProfile))
            return;

        seededProfile = profile;

        var draft = store.ProfileDraft;

        editUserDto = new()
        {
            FullName = draft?.FullName ?? profile.FullName,
            BirthDate = draft is null ? profile.BirthDate : draft.BirthDate,
            Gender = draft is null ? profile.Gender : draft.Gender
        };

        editContext = new(editUserDto);
        editContext.OnFieldChanged += (_, _) => store.EditProfile(new() { FullName = editUserDto.FullName, BirthDate = editUserDto.BirthDate, Gender = editUserDto.Gender });
    }

    private async Task SaveProfile()
    {
        if (isSaving || Page.Profile is null)
            return;

        isSaving = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            seededProfile = Page.Profile with { FullName = editUserDto.FullName ?? string.Empty, BirthDate = editUserDto.BirthDate, Gender = editUserDto.Gender };
            Page.UpdateProfile(seededProfile);

            await Page.SnackBar!.Success(AppLocalizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)]);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RemoveProfileImage()
    {
        if (isSaving || Page.Profile is null)
            return;

        isSaving = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            Page.UpdateProfile(Page.Profile with { HasPicture = false });
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandleOnFilesChange(BitFileInfo[] files)
    {
        if (Page.Profile is null || files.Length == 0)
            return;

        isUploading = true;
        StateHasChanged();

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            Page.UpdateProfile(Page.Profile with { HasPicture = true });
        }
        finally
        {
            isUploading = false;
            await fileUploadRef.Reset();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        Page.ProfileChanged -= OnProfileChanged;

        await base.DisposeAsync();
    }
}

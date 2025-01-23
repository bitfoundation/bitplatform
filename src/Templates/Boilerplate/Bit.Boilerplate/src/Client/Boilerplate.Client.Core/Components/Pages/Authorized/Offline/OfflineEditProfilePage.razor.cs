using Boilerplate.Client.Core.Data;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Offline;

public partial class OfflineEditProfilePage
{
    protected override string? Title => Localizer[nameof(AppStrings.ProfileTitle)];
    protected override string? Subtitle => string.Empty;

    [AutoInject] IDbContextFactory<OfflineDbContext> dbContextFactory = default!;

    private bool isSaving;
    private bool isLoading = true;
    private UserDto user = new();
    private readonly EditUserDto userToEdit = new();

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        try
        {
            await LoadEditProfileData();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadEditProfileData()
    {
        user = await GetCurrentUser() ?? new();

        user.Patch(userToEdit);
    }

    private async Task<UserDto> GetCurrentUser()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(CurrentCancellationToken);

        return await dbContext.Users.FirstAsync(CurrentCancellationToken);
    }

    private async Task DoSave()
    {
        if (isSaving) return;

        isSaving = true;

        try
        {
            userToEdit.Patch(user);

            await using var dbContext = await dbContextFactory.CreateDbContextAsync(CurrentCancellationToken);
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }
}

using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>The profile fields the settings page shows, in place of the app's user DTO.</summary>
public sealed record SettingsProfile(string FullName, string Email, string? PhoneNumber, DateTimeOffset? BirthDate, Gender? Gender, bool HasPicture);

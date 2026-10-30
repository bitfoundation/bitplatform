using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public sealed record SettingsProfile(string FullName, string Email, string? PhoneNumber, DateTimeOffset? BirthDate, Gender? Gender, bool HasPicture);

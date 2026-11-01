namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>A signed-in session as the sessions section shows it, in place of the app's session DTO.</summary>
public sealed record SettingsSession(Guid Id,
                                     string Address,
                                     string? Ip,
                                     string Device,
                                     string ImageUrl,
                                     DateTimeOffset LastSeen,
                                     bool IsPrivileged,
                                     bool IsNotificationAllowed,
                                     string? OAuthClientName = null);

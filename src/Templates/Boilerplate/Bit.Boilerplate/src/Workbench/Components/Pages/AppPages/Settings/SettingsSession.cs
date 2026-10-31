namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public sealed record SettingsSession(Guid Id,
                                     string Address,
                                     string? Ip,
                                     string Device,
                                     string ImageUrl,
                                     DateTimeOffset LastSeen,
                                     bool IsPrivileged,
                                     bool IsNotificationAllowed,
                                     string? OAuthClientName = null);

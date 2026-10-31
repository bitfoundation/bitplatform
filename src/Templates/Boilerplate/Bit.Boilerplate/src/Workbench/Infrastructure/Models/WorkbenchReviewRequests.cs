using System.ComponentModel;

namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewReplyRequest(string Text, string? Agent);

public sealed record WorkbenchReviewStatusRequest(string Status, string? Agent);

public sealed record WorkbenchReviewTasksRequest(WorkbenchReviewTask[] Tasks, string? Agent);

public sealed record WorkbenchFlowRequest(string Name, WorkbenchFlowScreenRequest[] Screens, WorkbenchFlowLinkRequest[]? Links, string? Id, string? Agent);

public sealed record WorkbenchFlowScreenRequest(
    [property: Description("A short id of your own for the screen, which the links use, like \"list\" or \"add-dialog\".")] string Key,
    [property: Description("The page's key, as in /workbench/<key>.")] string Page,
    [property: Description("The view's query string as in a Workbench link (device, theme, culture, data, auth and so on), or empty.")] string? Query,
    [property: Description("What the screen shows, in the user's language.")] string? Note,
    [property: Description("The visible texts of the buttons, links or items to tap in order to bring the page into the state the screen shows, like [\"Add building\"] for the dialog that button opens. Empty for the page as it opens.")] string[]? Taps);

public sealed record WorkbenchFlowLinkRequest(
    [property: Description("The key of the screen the arrow leaves.")] string From,
    [property: Description("The key of the screen the arrow points at.")] string To,
    [property: Description("What the user does, in the user's language, like \"Tap Add building\".")] string? Label,
    [property: Description("The visible text of the button, link or item on the From screen that leads to the To screen. The board marks it and starts the arrow there.")] string? Tap);

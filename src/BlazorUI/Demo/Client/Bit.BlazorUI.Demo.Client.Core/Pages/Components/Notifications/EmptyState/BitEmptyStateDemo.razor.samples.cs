namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.EmptyState;

public partial class BitEmptyStateDemo
{
    private readonly string example1RazorCode = @"
<BitEmptyState />

<BitEmptyState Title=""No messages"" Description=""When someone writes to you, it shows up here."" />";

    private readonly string example2RazorCode = @"
<BitEmptyState IconName=""@BitIconName.Inbox""
               Title=""Your inbox is empty""
               Description=""When someone writes to you, it shows up here."" />

<BitEmptyState Title=""No pictures yet"" Description=""The pictures you upload are kept here."">
    <IconTemplate>
        <svg width=""72"" height=""56"" viewBox=""0 0 72 56"" aria-hidden=""true"">
            <rect x=""2"" y=""2"" width=""68"" height=""52"" rx=""6"" fill=""none"" stroke=""currentColor"" stroke-width=""3"" />
            <circle cx=""22"" cy=""20"" r=""6"" fill=""currentColor"" />
            <path d=""M6 48 L26 30 L38 40 L50 26 L66 44 L66 50 L6 50 Z"" fill=""currentColor"" opacity=""0.5"" />
        </svg>
    </IconTemplate>
</BitEmptyState>";

    private readonly string example3RazorCode = @"
@if (tasks.Count == 0)
{
    <BitEmptyState IconName=""@BitIconName.TaskManager""
                   Title=""No tasks yet""
                   Description=""Add a task to start planning the project."">
        <BitButton OnClick=""AddTask"" IconName=""@BitIconName.Add"">Add a task</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Href=""/components/basiclist"">Learn more</BitButton>
    </BitEmptyState>
}
else
{
    <div>@tasks.Count task(s) added.</div>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => tasks.Clear()"">Clear the tasks</BitButton>
}";
    private readonly string example3CsharpCode = @"
private readonly List<string> tasks = [];

private void AddTask() => tasks.Add($""Task {tasks.Count + 1}"");";

    private readonly string example4RazorCode = @"
<BitEmptyState IconName=""@BitIconName.SearchIssue"">
    <TitleTemplate>No results for <i>""blue widget""</i></TitleTemplate>
    <DescriptionTemplate>
        Check the spelling, or <BitLink Href=""/products"">browse every product</BitLink> instead.
    </DescriptionTemplate>
</BitEmptyState>";

    private readonly string example5RazorCode = @"
<BitEmptyState HeadingLevel=""2""
               IconName=""@BitIconName.Archive""
               Title=""No archived projects""
               Description=""Projects you archive are kept here, out of the way of the ones you work on."" />";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""AddPerson"" IconName=""@BitIconName.AddFriend"">Add a person</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => people.Clear()"">Clear</BitButton>

<BitBasicList Items=""people"" Style=""height: auto; border: 1px solid var(--bit-clr-brd-sec)"">
    <EmptyTemplate>
        <BitEmptyState IconName=""@BitIconName.People"" Title=""No people"" Description=""The people you add are listed here."" />
    </EmptyTemplate>
    <RowTemplate Context=""person"">
        <div style=""padding: 0.5rem 1rem"">@person</div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example6CsharpCode = @"
private readonly List<string> people = [];

private void AddPerson() => people.Add($""Person {people.Count + 1}"");";

    private readonly string example7RazorCode = @"
<BitEmptyState IconName=""@BitIconName.Filter""
               Title=""Nothing matches these filters""
               Description=""Clear a filter to see more.""
               Style=""--bit-EmptyState-align: start;"">
    <BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Clear the filters</BitButton>
</BitEmptyState>";

    private readonly string example8RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitEmptyState Icon=""@BitIconInfo.Fa(""solid folder-open"")"" Title=""This folder is empty"" />

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitEmptyState Icon=""@BitIconInfo.Bi(""cart-x"")"" Title=""Your cart is empty"" />";

    private readonly string example9RazorCode = @"
<style>
    .custom-root {
        border-radius: 1rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-icon {
        padding: 0.75rem;
        border-radius: 50%;
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
    }

    .custom-title {
        letter-spacing: 0.05rem;
        text-transform: uppercase;
    }
</style>

<BitEmptyState IconName=""@BitIconName.CloudUpload""
               Title=""Drop files here""
               Description=""Or pick them from your computer.""
               Style=""--bit-EmptyState-icon-color: var(--bit-clr-pri); --bit-EmptyState-title-font-size: var(--bit-tpg-fs-xl);"" />

<BitEmptyState IconName=""@BitIconName.Ringer""
               Title=""No notifications""
               Description=""You are all caught up.""
               Classes=""@(new() { Root = ""custom-root"", Icon = ""custom-icon"", Title = ""custom-title"" })"" />

<BitEmptyState IconName=""@BitIconName.Calendar""
               Title=""No events today""
               Styles=""@(new() { Root = ""border: 1px dashed var(--bit-clr-brd-pri); border-radius: 0.5rem;"", Description = ""font-style: italic;"" })""
               Description=""Enjoy the free time."" />";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">
    <BitEmptyState Dir=""BitDir.Rtl""
                   IconName=""@BitIconName.Inbox""
                   Title=""صندوق پیام شما خالی است""
                   Description=""وقتی کسی برای شما پیام بفرستد، اینجا نمایش داده می‌شود.""
                   Style=""--bit-EmptyState-align: start;"">
        <BitButton>نوشتن پیام</BitButton>
    </BitEmptyState>
</div>";
}

using Bit.Butil;
using Bit.Websites.Platform.Shared.Dtos.CreateProject;

namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public partial class CreateProjectGetIt
{
    [AutoInject] private Clipboard clipboard = default!;

    private bool isLinkCopied;
    private string? recordedLine;

    private string MailLink
    {
        get
        {
            var answers = Answers;
            var subject = $"Create {answers.DisplayName} with bit Boilerplate";
            var body = $"Open this on your computer to create {answers.DisplayName}. Your answers are already filled in: {answers.ShareLink}";

            return $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
        }
    }

    private async Task CopyLink()
    {
        await clipboard.WriteText(Answers.ShareLink);
        isLinkCopied = true;
    }

    private void RecordCopiedLine()
    {
        var line = Answers.LineWithoutName;

        if (line == recordedLine)
            return;

        recordedLine = line;
        _ = RecordCreatedProject(line);
    }

    private async Task RecordCreatedProject(string command)
    {
        try
        {
            await HttpClient.PostAsJsonAsync("api/CreateProject/Created", new ProjectCreatedDto { Command = command }, AppJsonContext.Default.ProjectCreatedDto);
        }
        catch (KnownException)
        {
        }
        catch (HttpRequestException)
        {
        }
    }

    private static string ComputerNeed(CreateProjectAnswers answers) => answers.Computer switch
    {
        CreateProjectComputer.Windows => $"A Windows 10 or 11 computer with {answers.MemoryGb} GB of memory and {answers.DiskGb} GB of free space.",
        CreateProjectComputer.MacOs => $"A Mac with {answers.MemoryGb} GB of memory and {answers.DiskGb} GB of free space.",
        _ => $"A Linux computer with {answers.MemoryGb} GB of memory and {answers.DiskGb} GB of free space."
    };

    private static IEnumerable<string> PlanSteps(CreateProjectAnswers answers)
    {
        yield return "Installs Node.js, Git and VS Code with Claude Code, each only if it's missing.";

        if (answers.NeedsContainers)
        {
            yield return answers.Computer switch
            {
                CreateProjectComputer.Windows => "Installs Podman, a free container engine that runs your database while you build. Windows may ask once to allow it.",
                CreateProjectComputer.MacOs => "Installs Colima, a free container engine that runs your database while you build.",
                _ => "Installs Docker Engine, which runs your database while you build."
            };
        }

        yield return $"Creates {answers.CodeName} with its history in Git, builds it and prepares its database.";

        if (answers.HasGitHubRepo)
            yield return "Creates a private GitHub repository in your account. Your browser opens once so you can sign in.";

        yield return "Starts your Workbench, your landing page and your app, and opens them in your browser.";

        if (answers.Devices.Count > 0)
            yield return $"Then sets up the {CreateProjectNames.ListOf(answers.Devices)} tools while you look around.";

        yield return "Opens your project in VS Code, with Claude Code ready for your first prompt.";
        yield return "If a step fails, the others carry on, and the end shows the exact line that fixes it.";
    }
}

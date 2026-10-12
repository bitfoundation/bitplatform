using Bit.Butil;
using Bit.Websites.Platform.Shared.Dtos.CreateProject;

namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public partial class CreateProjectCommandBar
{
    [AutoInject] private Clipboard clipboard = default!;

    private string? copiedCommand;

    [Parameter, EditorRequired] public ProjectSetup Setup { get; set; } = default!;

    private async Task CopyCommand()
    {
        var command = Setup.ToCommand();

        await clipboard.WriteText(command);

        if (command == copiedCommand)
            return;

        copiedCommand = command;
        _ = RecordCreatedProject(command);
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
}

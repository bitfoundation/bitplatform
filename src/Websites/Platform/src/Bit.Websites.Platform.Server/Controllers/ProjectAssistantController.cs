using System.ClientModel;
using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;

namespace Bit.Websites.Platform.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ProjectAssistantController : AppControllerBase
{
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private TelegramBotService telegramBotService = default!;

    private const string NoAnswer = "The project assistant couldn't answer that. Try again, or pick the options below.";

    [HttpPost]
    [EnableRateLimiting("ProjectAssistant")]
    public async Task<ProjectAssistantReply> Chat(ProjectAssistantRequest request, CancellationToken cancellationToken)
    {
        var chatClient = serviceProvider.GetService<IChatClient>()
            ?? throw new DomainLogicException("The project assistant isn't available right now. Pick the options below instead.");

        var recorded = false;

        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(async (string topic, string details) =>
                {
                    if (recorded)
                        return;

                    recorded = true;
                    await telegramBotService.SendProjectAssistantNote(topic, details, request.Summary, cancellationToken);
                }, name: "RecordForTeam", description: "Records, for the bit platform team, something the user wants that the template doesn't have, or an important point that stays unclear. Parameters: topic (a few words), details (what the user wants and why)")
            ]
        };

        configuration.GetSection("AppSettings:ChatOptions").Bind(chatOptions);

        ChatResponse<ProjectAssistantReply> response;

        try
        {
            response = await chatClient.GetResponseAsync<ProjectAssistantReply>(
                [new(ChatRole.System, ProjectAssistant.SystemPrompt), new(ChatRole.User, ProjectAssistant.UserTurn(request))],
                chatOptions,
                cancellationToken: cancellationToken);
        }
        catch (Exception exp) when (exp is ClientResultException or HttpRequestException)
        {
            throw new DomainLogicException(NoAnswer, exp);
        }

        if (response.TryGetResult(out var answer) is false || string.IsNullOrWhiteSpace(answer.Reply))
            throw new DomainLogicException(NoAnswer);

        return new()
        {
            Reply = answer.Reply.Trim(),
            Summary = answer.Summary?.Trim() ?? "",
            Options = ProjectAssistant.Normalize(answer.Options)
        };
    }

    [HttpPost]
    [EnableRateLimiting("MessageSubmit")]
    public async Task<IActionResult> Created(ProjectCreatedDto dto, CancellationToken cancellationToken)
    {
        await telegramBotService.SendProjectCreatedMessage(dto.Command!, dto.Summary, cancellationToken);
        return Ok();
    }
}

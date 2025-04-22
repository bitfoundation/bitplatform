namespace Boilerplate.Server.Api.Models.Chatbot;

public class SystemPrompt
{
    public Guid Id { get; set; }

    public PromptKind PromptKind { get; set; }

    [Required]
    public string? Markdown { get; set; }
}

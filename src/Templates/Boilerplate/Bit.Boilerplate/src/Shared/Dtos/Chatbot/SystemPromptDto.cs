namespace Boilerplate.Shared.Dtos.Chatbot;

public class SystemPromptDto
{
    public PromptKind PromptKind { get; set; }

    [Required]
    public string? Markdown { get; set; }

    public byte[] Version { get; set; } = [];
}

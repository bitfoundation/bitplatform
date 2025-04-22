namespace Boilerplate.Shared.Dtos.Chatbot;

public class UpdateSystemPromptDto
{
    public PromptKind Kind { get; set; }

    [Required]
    public string? Markdown { get; set; }
}

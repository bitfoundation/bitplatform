namespace Bit.Websites.Platform.Api;

public class AppSettings
{
    public TelegramBotSettings TelegramBotSettings { get; set; } = default!;
}

public class TelegramBotSettings
{
    public string Token { get; set; }
    public string[] ChatIds { get; set; }
}

namespace _10x_cards.Services;

public interface IEmailSender
{
    Task SendMagicLinkAsync(string email, string magicLinkUrl);
}

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendMagicLinkAsync(string email, string magicLinkUrl)
    {
        logger.LogInformation("Magic link for {Email}: {Url}", email, magicLinkUrl);
        return Task.CompletedTask;
    }
}

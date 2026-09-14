using Microsoft.Extensions.AI;

namespace _10x_cards.Tests.Fakes;

public class FakeChatClient : IChatClient
{
    public string? ResponseText { get; set; } = "[]";

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var message = new ChatMessage(ChatRole.Assistant, ResponseText);
        return Task.FromResult(new ChatResponse(message));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

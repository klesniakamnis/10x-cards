using _10x_cards.Services;
using _10x_cards.Tests.Fakes;

namespace _10x_cards.Tests.Services;

public class OpenAiFlashcardGeneratorTests
{
    private readonly FakeChatClient _fakeChatClient = new();
    private readonly OpenAiFlashcardGenerator _generator;

    public OpenAiFlashcardGeneratorTests()
    {
        _generator = new OpenAiFlashcardGenerator(_fakeChatClient);
    }

    [Fact]
    public async Task ValidSingleItem_ReturnsOneProposal()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q1","answer":"A1"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
        Assert.Equal("Q1", result[0].Question);
        Assert.Equal("A1", result[0].Answer);
    }

    [Fact]
    public async Task ValidMultipleItems_ReturnsAll()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q1","answer":"A1"},{"question":"Q2","answer":"A2"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task MarkdownFencedJson_StripsAndParses()
    {
        _fakeChatClient.ResponseText = "```json\n[{\"question\":\"Q\",\"answer\":\"A\"}]\n```";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
    }

    [Fact]
    public async Task BareMarkdownFence_StripsAndParses()
    {
        _fakeChatClient.ResponseText = "```\n[{\"question\":\"Q\",\"answer\":\"A\"}]\n```";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
    }

    [Fact]
    public async Task EmptyArray_ReturnsEmptyList()
    {
        _fakeChatClient.ResponseText = "[]";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullResponseText_ThrowsInvalidOperationException()
    {
        _fakeChatClient.ResponseText = null;
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _generator.GenerateFlashcardsAsync("source text"));
    }

    [Fact]
    public async Task EmptyQuestion_FilteredOut()
    {
        _fakeChatClient.ResponseText = """[{"question":"","answer":"A1"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Empty(result);
    }

    [Fact]
    public async Task EmptyAnswer_FilteredOut()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q1","answer":""}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullQuestion_FilteredOut()
    {
        _fakeChatClient.ResponseText = """[{"question":null,"answer":"A1"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtraFields_Ignored()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q","answer":"A","extra":"x"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
    }

    [Fact]
    public async Task CaseInsensitiveFields_Parsed()
    {
        _fakeChatClient.ResponseText = """[{"Question":"Q","Answer":"A"}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
    }

    [Fact]
    public async Task MalformedResponse_ThrowsInvalidOperationException()
    {
        _fakeChatClient.ResponseText = "not json at all";
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _generator.GenerateFlashcardsAsync("source text"));
    }

    [Fact]
    public async Task TruncatedJson_ThrowsInvalidOperationException()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q","answer""";
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _generator.GenerateFlashcardsAsync("source text"));
    }

    [Fact]
    public async Task MixedValidAndInvalid_ReturnsOnlyValid()
    {
        _fakeChatClient.ResponseText = """[{"question":"Q1","answer":"A1"},{"question":"","answer":""}]""";
        var result = await _generator.GenerateFlashcardsAsync("source text");
        Assert.Single(result);
        Assert.Equal("Q1", result[0].Question);
    }
}

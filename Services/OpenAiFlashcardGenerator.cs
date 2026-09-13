using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace _10x_cards.Services;

public partial class OpenAiFlashcardGenerator(IChatClient chatClient) : IFlashcardGenerator
{
    private const string SystemPrompt = """
        You are an educational flashcard generator. Given a text, extract the key concepts and generate question/answer pairs suitable for spaced repetition study.

        Rules:
        - Focus on understanding and core concepts, not trivia or minor details.
        - Questions should test comprehension, not just recall of exact wording.
        - Answers should be concise but complete.
        - Generate in the same language as the input text.
        - Output ONLY a JSON array of objects with "question" and "answer" fields. No other text.

        Example output:
        [{"question": "What is X?", "answer": "X is ..."}]
        """;

    public async Task<List<FlashcardProposal>> GenerateFlashcardsAsync(string sourceText, CancellationToken cancellationToken = default)
    {
        var response = await chatClient.GetResponseAsync(
            [
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, sourceText)
            ],
            cancellationToken: cancellationToken);

        var content = response.Text ?? "";
        content = StripMarkdownFences(content);

        try
        {
            var items = JsonSerializer.Deserialize<List<JsonFlashcard>>(content, JsonOptions);
            if (items is null)
                return [];

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.Question) && !string.IsNullOrWhiteSpace(i.Answer))
                .Select(i => new FlashcardProposal(i.Question!, i.Answer!))
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse flashcard proposals from LLM response. Raw content: {content[..Math.Min(content.Length, 200)]}", ex);
        }
    }

    private static string StripMarkdownFences(string content)
    {
        content = content.Trim();
        var match = MarkdownFenceRegex().Match(content);
        return match.Success ? match.Groups[1].Value.Trim() : content;
    }

    [GeneratedRegex(@"^```(?:json)?\s*\n?(.*?)\n?\s*```$", RegexOptions.Singleline)]
    private static partial Regex MarkdownFenceRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record JsonFlashcard
    {
        public string? Question { get; init; }
        public string? Answer { get; init; }
    }
}

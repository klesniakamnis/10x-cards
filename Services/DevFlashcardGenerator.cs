namespace _10x_cards.Services;

public class DevFlashcardGenerator(ILogger<DevFlashcardGenerator> logger) : IFlashcardGenerator
{
    public async Task<List<FlashcardProposal>> GenerateFlashcardsAsync(string sourceText, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DevFlashcardGenerator: returning hardcoded proposals (stub mode)");

        await Task.Delay(2000, cancellationToken);

        return
        [
            new("What is spaced repetition?",
                "A learning technique where reviews are scheduled at increasing intervals to optimize long-term memory retention."),
            new("How does the Leitner system work?",
                "Cards are sorted into boxes based on how well the learner knows them. Correctly answered cards advance to the next box with longer review intervals; incorrect ones return to the first box."),
            new("What is the forgetting curve?",
                "A concept by Hermann Ebbinghaus showing that memory retention decays exponentially over time without reinforcement."),
            new("Why are flashcards effective for learning?",
                "They leverage active recall and spaced repetition, two evidence-based techniques that strengthen memory consolidation."),
        ];
    }
}

namespace _10x_cards.Services;

public interface IFlashcardGenerator
{
    Task<List<FlashcardProposal>> GenerateFlashcardsAsync(string sourceText, CancellationToken cancellationToken = default);
}

public record FlashcardProposal(string Question, string Answer);

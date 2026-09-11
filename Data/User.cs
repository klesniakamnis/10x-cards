namespace _10x_cards.Data;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Flashcard> Flashcards { get; set; } = [];
}

namespace _10x_cards.Data;

public class Flashcard
{
    public Guid Id { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public FlashcardSource Source { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

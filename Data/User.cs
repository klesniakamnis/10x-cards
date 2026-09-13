using System.ComponentModel.DataAnnotations;

namespace _10x_cards.Data;

public class User : IHasTimestamps
{
    public Guid Id { get; set; }
    [MaxLength(320)]
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Flashcard> Flashcards { get; set; } = [];
}

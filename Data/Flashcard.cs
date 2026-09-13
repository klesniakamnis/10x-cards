using System.ComponentModel.DataAnnotations;

namespace _10x_cards.Data;

public class Flashcard : IHasTimestamps
{
    public Guid Id { get; set; }
    [MaxLength(5000)]
    public required string Question { get; set; }
    [MaxLength(5000)]
    public required string Answer { get; set; }
    public required FlashcardSource Source { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public double EasinessFactor { get; set; } = 2.5;
    public int Interval { get; set; }
    public int Repetitions { get; set; }
    public DateTime NextReviewDate { get; set; }

    public User User { get; set; } = null!;
}

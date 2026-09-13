namespace _10x_cards.Data;

public class MagicLinkToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public required string Email { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _10x_cards.Data;

namespace _10x_cards.Endpoints;

public static class FlashcardEndpoints
{
    public static WebApplication MapFlashcardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/flashcards");

        group.MapPost("/", async (CreateFlashcardRequest request, ApplicationDbContext db, HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return Results.Json(new { error = "Question is required." }, statusCode: 400);

            if (request.Question.Length > 5000)
                return Results.Json(new { error = "Question must not exceed 5,000 characters." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Answer))
                return Results.Json(new { error = "Answer is required." }, statusCode: 400);

            if (request.Answer.Length > 5000)
                return Results.Json(new { error = "Answer must not exceed 5,000 characters." }, statusCode: 400);

            if (!Enum.TryParse<FlashcardSource>(request.Source, ignoreCase: true, out var source))
                return Results.Json(new { error = "Invalid source value." }, statusCode: 400);

            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var flashcard = new Flashcard
            {
                Id = Guid.NewGuid(),
                Question = request.Question,
                Answer = request.Answer,
                Source = source,
                UserId = userId
            };

            db.Flashcards.Add(flashcard);
            await db.SaveChangesAsync();

            return Results.Json(
                new FlashcardResponse(flashcard.Id, flashcard.Question, flashcard.Answer, flashcard.Source.ToString(), flashcard.CreatedAt),
                statusCode: 201);
        });

        return app;
    }
}

public record CreateFlashcardRequest(string Question, string Answer, string Source);

public record FlashcardResponse(Guid Id, string Question, string Answer, string Source, DateTime CreatedAt);

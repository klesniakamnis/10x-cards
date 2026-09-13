using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _10x_cards.Data;
using _10x_cards.Services;

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
                new FlashcardResponse(flashcard.Id, flashcard.Question, flashcard.Answer, flashcard.Source.ToString(), flashcard.CreatedAt, flashcard.UpdatedAt),
                statusCode: 201);
        });

        group.MapGet("/", async (ApplicationDbContext db, HttpContext httpContext) =>
        {
            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var flashcards = await db.Flashcards
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FlashcardResponse(f.Id, f.Question, f.Answer, f.Source.ToString(), f.CreatedAt, f.UpdatedAt))
                .ToListAsync();

            return Results.Ok(flashcards);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateFlashcardRequest request, ApplicationDbContext db, HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return Results.Json(new { error = "Question is required." }, statusCode: 400);

            if (request.Question.Length > 5000)
                return Results.Json(new { error = "Question must not exceed 5,000 characters." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Answer))
                return Results.Json(new { error = "Answer is required." }, statusCode: 400);

            if (request.Answer.Length > 5000)
                return Results.Json(new { error = "Answer must not exceed 5,000 characters." }, statusCode: 400);

            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var flashcard = await db.Flashcards.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (flashcard is null)
                return Results.Json(new { error = "Flashcard not found." }, statusCode: 404);

            flashcard.Question = request.Question;
            flashcard.Answer = request.Answer;
            await db.SaveChangesAsync();

            return Results.Ok(new FlashcardResponse(flashcard.Id, flashcard.Question, flashcard.Answer, flashcard.Source.ToString(), flashcard.CreatedAt, flashcard.UpdatedAt));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db, HttpContext httpContext) =>
        {
            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var flashcard = await db.Flashcards.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (flashcard is null)
                return Results.Json(new { error = "Flashcard not found." }, statusCode: 404);

            db.Flashcards.Remove(flashcard);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        group.MapGet("/due", async (ApplicationDbContext db, HttpContext httpContext) =>
        {
            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            var flashcards = await db.Flashcards
                .Where(f => f.UserId == userId && f.NextReviewDate <= now)
                .OrderBy(f => f.NextReviewDate)
                .Select(f => new FlashcardResponse(f.Id, f.Question, f.Answer, f.Source.ToString(), f.CreatedAt, f.UpdatedAt))
                .ToListAsync();

            return Results.Ok(flashcards);
        });

        group.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, ApplicationDbContext db, HttpContext httpContext) =>
        {
            if (request.Grade < 0 || request.Grade > 5)
                return Results.Json(new { error = "Grade must be between 0 and 5." }, statusCode: 400);

            var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var flashcard = await db.Flashcards.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (flashcard is null)
                return Results.Json(new { error = "Flashcard not found." }, statusCode: 404);

            var result = Sm2Service.Calculate(flashcard.EasinessFactor, flashcard.Interval, flashcard.Repetitions, request.Grade);

            flashcard.EasinessFactor = result.EasinessFactor;
            flashcard.Interval = result.Interval;
            flashcard.Repetitions = result.Repetitions;
            flashcard.NextReviewDate = result.NextReviewDate;

            await db.SaveChangesAsync();

            return Results.Ok(new FlashcardResponse(flashcard.Id, flashcard.Question, flashcard.Answer, flashcard.Source.ToString(), flashcard.CreatedAt, flashcard.UpdatedAt));
        });

        return app;
    }
}

public record CreateFlashcardRequest(string Question, string Answer, string Source);

public record UpdateFlashcardRequest(string Question, string Answer);

public record ReviewRequest(int Grade);

public record FlashcardResponse(Guid Id, string Question, string Answer, string Source, DateTime CreatedAt, DateTime UpdatedAt);

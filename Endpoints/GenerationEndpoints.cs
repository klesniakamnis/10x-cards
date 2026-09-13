using _10x_cards.Services;

namespace _10x_cards.Endpoints;

public static class GenerationEndpoints
{
    public static WebApplication MapGenerationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/generation");

        group.MapPost("/", async (GenerationRequest request, IFlashcardGenerator generator, ILogger<Program> logger) =>
        {
            if (string.IsNullOrWhiteSpace(request.SourceText))
                return Results.Json(new { error = "Source text is required." }, statusCode: 400);

            if (request.SourceText.Length > 10_000)
                return Results.Json(new { error = "Source text must not exceed 10,000 characters." }, statusCode: 400);

            try
            {
                var proposals = await generator.GenerateFlashcardsAsync(request.SourceText);
                return Results.Ok(new GenerationResponse(proposals));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Flashcard generation failed");
                return Results.Json(new { error = "Generation failed. Please try again." }, statusCode: 500);
            }
        });

        return app;
    }
}

public record GenerationRequest(string SourceText);

public record GenerationResponse(List<FlashcardProposal> Proposals);

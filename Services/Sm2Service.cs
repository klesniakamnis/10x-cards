namespace _10x_cards.Services;

public static class Sm2Service
{
    public static Sm2Result Calculate(double easinessFactor, int interval, int repetitions, int grade)
    {
        if (grade < 0 || grade > 5)
            throw new ArgumentOutOfRangeException(nameof(grade), "Grade must be between 0 and 5.");

        if (grade >= 3)
        {
            interval = repetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(interval * easinessFactor)
            };
            repetitions++;
        }
        else
        {
            repetitions = 0;
            interval = 1;
        }

        easinessFactor += 0.1 - (5 - grade) * (0.08 + (5 - grade) * 0.02);
        if (easinessFactor < 1.3)
            easinessFactor = 1.3;

        var nextReviewDate = DateTime.UtcNow.AddDays(interval);

        return new Sm2Result(easinessFactor, interval, repetitions, nextReviewDate);
    }
}

public record Sm2Result(double EasinessFactor, int Interval, int Repetitions, DateTime NextReviewDate);

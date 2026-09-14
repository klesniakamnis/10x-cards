using _10x_cards.Services;

namespace _10x_cards.Tests.Services;

public class Sm2ServiceTests
{
    private const double EfTolerance = 0.001;
    private static readonly TimeSpan DateTolerance = TimeSpan.FromSeconds(5);

    private static void AssertResult(Sm2Result result, double expectedEf, int expectedInterval, int expectedReps)
    {
        Assert.Equal(expectedEf, result.EasinessFactor, EfTolerance);
        Assert.Equal(expectedInterval, result.Interval);
        Assert.Equal(expectedReps, result.Repetitions);

        var expectedDate = DateTime.UtcNow.AddDays(expectedInterval);
        Assert.InRange(result.NextReviewDate, expectedDate - DateTolerance, expectedDate + DateTolerance);
    }

    [Fact]
    public void FirstReview_Correct_ReturnsInterval1()
    {
        var result = Sm2Service.Calculate(2.5, 0, 0, 4);
        AssertResult(result, 2.5, 1, 1);
    }

    [Fact]
    public void SecondReview_Correct_ReturnsInterval6()
    {
        var result = Sm2Service.Calculate(2.5, 1, 1, 4);
        AssertResult(result, 2.5, 6, 2);
    }

    [Fact]
    public void ThirdReview_Correct_ReturnsInterval15()
    {
        // round(6 * 2.5) = 15
        var result = Sm2Service.Calculate(2.5, 6, 2, 4);
        AssertResult(result, 2.5, 15, 3);
    }

    [Fact]
    public void FourthReview_Correct_ReturnsInterval38()
    {
        // round(15 * 2.5) = 38
        var result = Sm2Service.Calculate(2.5, 15, 3, 4);
        AssertResult(result, 2.5, 38, 4);
    }

    [Fact]
    public void PerfectRecall_Grade5_IncreasesEf()
    {
        // EF: 2.5 + 0.1 - (5-5)*(0.08+(5-5)*0.02) = 2.5 + 0.1 = 2.6
        var result = Sm2Service.Calculate(2.5, 0, 0, 5);
        AssertResult(result, 2.6, 1, 1);
    }

    [Fact]
    public void HardCorrect_Grade3_DecreasesEf()
    {
        // EF: 2.5 + 0.1 - (5-3)*(0.08+(5-3)*0.02) = 2.5 + 0.1 - 2*(0.08+2*0.02) = 2.5 + 0.1 - 2*0.12 = 2.5 - 0.14 = 2.36
        var result = Sm2Service.Calculate(2.5, 0, 0, 3);
        AssertResult(result, 2.36, 1, 1);
    }

    [Fact]
    public void Fail_Grade2_ResetsIntervalAndReps()
    {
        // EF: 2.5 + 0.1 - (5-2)*(0.08+(5-2)*0.02) = 2.5 + 0.1 - 3*(0.08+3*0.02) = 2.5 + 0.1 - 3*0.14 = 2.5 - 0.32 = 2.18
        var result = Sm2Service.Calculate(2.5, 15, 3, 2);
        AssertResult(result, 2.18, 1, 0);
    }

    [Fact]
    public void CompleteBlackout_Grade0_LargeEfDrop()
    {
        // EF: 2.5 + 0.1 - (5-0)*(0.08+(5-0)*0.02) = 2.5 + 0.1 - 5*(0.08+5*0.02) = 2.5 + 0.1 - 5*0.18 = 2.5 - 0.8 = 1.7
        var result = Sm2Service.Calculate(2.5, 6, 2, 0);
        AssertResult(result, 1.7, 1, 0);
    }

    [Fact]
    public void EfFloor_ClampedTo1Point3()
    {
        // EF: 1.3 + 0.1 - 5*0.18 = 1.3 + 0.1 - 0.9 = 0.5 → clamped to 1.3
        var result = Sm2Service.Calculate(1.3, 1, 0, 0);
        AssertResult(result, 1.3, 1, 0);
    }

    [Fact]
    public void GradeBelowRange_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sm2Service.Calculate(2.5, 0, 0, -1));
    }

    [Fact]
    public void GradeAboveRange_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sm2Service.Calculate(2.5, 0, 0, 6));
    }
}

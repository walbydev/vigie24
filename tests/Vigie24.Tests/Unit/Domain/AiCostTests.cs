namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

/// <summary>
/// Tests for the AI call cost calculation (AiCost).
/// </summary>
public class AiCostTests
{
    [Fact]
    public void Calculate_WithGemmaPrices_ReturnsExpectedCost()
    {
        // Gemma-4-31B: 0.20 CHF / M input tokens, 0.40 CHF / M output tokens
        // 1,500 input tokens + 300 output tokens
        var cost = AiCost.Calculate(1500, 300, 0.20m, 0.40m);

        Assert.Equal(0.00042m, cost);
    }

    [Fact]
    public void Calculate_WithNoTokens_ReturnsZero()
    {
        var cost = AiCost.Calculate(0, 0, 0.20m, 0.40m);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void Calculate_WithOneMillionTokens_ReturnsPricesExactly()
    {
        // 1M input tokens + 1M output tokens = input price + output price
        var cost = AiCost.Calculate(1_000_000, 1_000_000, 0.20m, 0.40m);

        Assert.Equal(0.60m, cost);
    }

    [Fact]
    public void Calculate_WithLargeUsage_ScalesLinearly()
    {
        // 100 articles/day × (1,500 input + 300 output) ≈ 180k input + 30k output tokens
        var cost = AiCost.Calculate(180_000, 30_000, 0.20m, 0.40m);

        Assert.Equal(0.048m, cost);
    }
}

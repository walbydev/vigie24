namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

/// <summary>
/// Tests du calcul de coût des appels IA (AiCost).
/// </summary>
public class AiCostTests
{
    [Fact]
    public void Calculate_WithGemmaPrices_ReturnsExpectedCost()
    {
        // Gemma-4-31B : 0.20 CHF / M tokens entrée, 0.40 CHF / M tokens sortie
        // 1 500 tokens entrée + 300 tokens sortie
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
        // 1M tokens d'entrée + 1M tokens de sortie = prix d'entrée + prix de sortie
        var cost = AiCost.Calculate(1_000_000, 1_000_000, 0.20m, 0.40m);

        Assert.Equal(0.60m, cost);
    }

    [Fact]
    public void Calculate_WithLargeUsage_ScalesLinearly()
    {
        // 100 articles/jour × (1 500 entrée + 300 sortie) ≈ 180k tokens entrée + 30k sortie
        var cost = AiCost.Calculate(180_000, 30_000, 0.20m, 0.40m);

        Assert.Equal(0.048m, cost);
    }
}

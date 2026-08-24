namespace Vigie24.Domain;

/// <summary>
/// Computes the cost in CHF of AI calls, from token consumption
/// and prices per million tokens (Infomaniak AI rates, CHF excl. VAT).
/// </summary>
public static class AiCost
{
    /// <summary>
    /// Cost in CHF of one analysis: input tokens × input price + output tokens × output price.
    /// </summary>
    /// <param name="promptTokens">Input tokens (usage.prompt_tokens)</param>
    /// <param name="completionTokens">Output tokens (usage.completion_tokens)</param>
    /// <param name="inputPricePerMillion">Input price in CHF per million tokens</param>
    /// <param name="outputPricePerMillion">Output price in CHF per million tokens</param>
    public static decimal Calculate(
        int promptTokens,
        int completionTokens,
        decimal inputPricePerMillion,
        decimal outputPricePerMillion)
        => promptTokens * inputPricePerMillion / 1_000_000m
         + completionTokens * outputPricePerMillion / 1_000_000m;
}

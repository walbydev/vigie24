namespace Vigie24.Domain;

/// <summary>
/// Calcul du coût en CHF des appels IA, à partir de la consommation en tokens
/// et des prix par million de tokens (tarifs Infomaniak AI, CHF hors TVA).
/// </summary>
public static class AiCost
{
    /// <summary>
    /// Coût en CHF d'une analyse : tokens d'entrée × prix entrée + tokens de sortie × prix sortie.
    /// </summary>
    /// <param name="promptTokens">Tokens d'entrée (usage.prompt_tokens)</param>
    /// <param name="completionTokens">Tokens de sortie (usage.completion_tokens)</param>
    /// <param name="inputPricePerMillion">Prix d'entrée en CHF par million de tokens</param>
    /// <param name="outputPricePerMillion">Prix de sortie en CHF par million de tokens</param>
    public static decimal Calculate(
        int promptTokens,
        int completionTokens,
        decimal inputPricePerMillion,
        decimal outputPricePerMillion)
        => promptTokens * inputPricePerMillion / 1_000_000m
         + completionTokens * outputPricePerMillion / 1_000_000m;
}

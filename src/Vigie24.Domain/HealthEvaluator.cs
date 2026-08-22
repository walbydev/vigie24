namespace Vigie24.Domain;

/// <summary>Niveau de santé global du système.</summary>
public enum HealthLevel
{
    /// <summary>Tout fonctionne : ingestion récente et fournisseur IA joignable.</summary>
    Ok,

    /// <summary>Fonctionnement dégradé : ingestion ancienne (mais pas critique).</summary>
    Degraded,

    /// <summary>Hors ligne : pas d'ingestion depuis longtemps ou fournisseur IA injoignable.</summary>
    Offline,
}

/// <summary>
/// Logique pure d'évaluation de l'état de santé (sans infrastructure, testable).
/// </summary>
public static class HealthEvaluator
{
    /// <summary>
    /// Évalue le niveau de santé à partir de la fraîcheur de l'ingestion et de
    /// la disponibilité du fournisseur IA.
    /// </summary>
    /// <param name="lastIngestionUtc">Date UTC du dernier article ingéré (null = aucun).</param>
    /// <param name="providerHealthy">Le fournisseur IA répond-il ?</param>
    /// <param name="okThreshold">Âge max pour un état « Opérationnel ».</param>
    /// <param name="degradedThreshold">Âge max pour un état « Dégradé » (au-delà : hors ligne).</param>
    public static HealthLevel Evaluate(
        DateTime? lastIngestionUtc,
        bool providerHealthy,
        TimeSpan okThreshold,
        TimeSpan degradedThreshold)
    {
        // Fournisseur injoignable → hors ligne, quelle que soit la fraîcheur des données.
        if (!providerHealthy)
            return HealthLevel.Offline;

        // Aucun article jamais ingéré → rien ne prouve que le système fonctionne.
        if (lastIngestionUtc is not { } last)
            return HealthLevel.Offline;

        var age = DateTime.UtcNow - last;
        if (age > degradedThreshold)
            return HealthLevel.Offline;
        if (age > okThreshold)
            return HealthLevel.Degraded;
        return HealthLevel.Ok;
    }

    /// <summary>Formate un âge en français : « il y a 12 min », « il y a 3 h 05 », « il y a 2 j ».</summary>
    public static string FormatAge(TimeSpan age)
    {
        if (age < TimeSpan.FromMinutes(1))
            return "à l'instant";
        if (age < TimeSpan.FromHours(1))
            return $"il y a {(int)age.TotalMinutes} min";
        if (age < TimeSpan.FromDays(1))
            return $"il y a {(int)age.TotalHours} h {age.Minutes:D2}";
        return $"il y a {(int)age.TotalDays} j";
    }
}

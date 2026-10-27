using Vigie24.Domain;
using Vigie24.Domain.Models;

namespace Vigie24.Infrastructure.Queries;

/// <summary>
/// Source selection shared by the worker's polling cycle and its recovery
/// probe. Kept in Infrastructure (same pattern as <c>ArticleFiltering</c>) so
/// the EF expressions stay in one place and can be unit-tested against an
/// InMemory context.
/// </summary>
public static class SourceFiltering
{
    /// <summary>
    /// Sources the worker must ingest this cycle: active, and not excluded by
    /// editorial policy (buying guides, affiliate reviews — see
    /// <see cref="SourceEditorialPolicy"/>).
    /// </summary>
    public static IQueryable<Source> Ingestible(this IQueryable<Source> sources)
        => sources.Where(s => s.IsActive
                              && s.EditorialPolicy != SourceEditorialPolicy.Commercial);

    /// <summary>
    /// Auto-disabled sources eligible for the 24 h recovery probe. Sources
    /// excluded by editorial policy are never probed: re-activating them would
    /// contradict the exclusion.
    /// </summary>
    public static IQueryable<Source> Probeable(this IQueryable<Source> sources, DateTime since)
        => sources.Where(s => !s.IsActive
                              && s.DisabledReason != null
                              && s.EditorialPolicy != SourceEditorialPolicy.Commercial
                              && (s.DisabledAt ?? s.UpdatedAt) < since);
}

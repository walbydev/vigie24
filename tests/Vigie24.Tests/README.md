# Tests Vigie24

Suite de tests complète pour le projet Vigie24.

## 📊 Résultats

**65 tests** - **100% de réussite** (372ms d'exécution)

## 📁 Structure des tests

```
tests/Vigie24.Tests/
├── Unit/
│   ├── Domain/
│   │   └── DomainTests.cs           # 8 tests (Article, Source, Category, Tag)
│   ├── Services/
│   │   └── AiServiceTests.cs        # 8 tests (Factory, Settings)
│   └── Worker/
│       └── WorkerTests.cs           # 22 tests (FeedItem, Scoring, Processing, CategoryTag)
├── Functional/
│   └── ScoringTests.cs              # 15 tests (Algorithmes de scoring, Workflows)
├── Integration/
│   ├── DbContextTests.cs            # 6 tests (EF Core InMemory)
│   └── PostgreSqlIntegrationTests.cs.disabled  # 7 tests (PostgreSQL réel - à activer)
├── appsettings.test.json
├── test_data.sql
├── Vigie24.Tests.csproj
└── README.md
```

## 🧪 Détails des tests

### **Unitaires (38 tests)** ✅

#### Domain (8 tests)
- Article: création, valeurs par défaut, validation
- Source: création, IsActive par défaut
- Category: création, slugs
- Tag: création, timestamps

#### Services (8 tests)
- AiServiceFactory: Infomaniak, DeepSeek, gestion erreurs
- AiServiceSettings: validation, sérialisation JSON

#### Worker (22 tests)
- FeedItemTests (4): FeedItem, ArticleFeedItem
- ScoringTests (4): récence, contenu, algorithmique
- ArticleProcessingTests (10): publication, URLs, timestamps
- CategoryTagTests (4): validation slugs, SourceKind

### **Fonctionnels (15 tests)** ✅

#### ScoringFunctionalTests (10 tests)
- ArticleWithHighScore_ShouldBePublished
- ArticleWithLowScore_ShouldNotBePublished
- RankingScore_ShouldReflectEditorialAndAlgorithmic
- RecentArticle_ShouldHaveHigherRecencyScore
- ArticleWithMultipleTags_ShouldHaveRichContent
- ArticleLanguageCode_ShouldBeValid
- SourceWithMultipleFeeds_ShouldHandleDuplicates
- CategoryWithArticles_ShouldCalculateAverageScore
- ArticleWithLongContent_ShouldScoreHigher
- ActiveSources_ShouldBeProcessed

#### WorkflowFunctionalTests (5 tests)
- FullIngestionWorkflow_ShouldProcessSources
- ArticleCreationWorkflow_ShouldValidateRequiredFields
- ScoreCalculationWorkflow_ShouldBeConsistent
- CategoryAssignmentWorkflow_ShouldMapCorrectly
- TagExtractionWorkflow_ShouldIdentifyKeywords

### **Intégration (6 tests)** ✅

#### DbContextTests (6 tests)
- SaveAndRetrieveArticle (EF Core InMemory)
- GetActiveSources
- GetArticlesByCategory
- GetRecentArticles (tri)
- BulkInsertArticles (50 articles)
- GetArticlesWithHighScore (filtrage)

### **Intégration PostgreSQL (7 tests) - Désactivé** ⏸️

Tests avec PostgreSQL réel via Testcontainers (fichier `.disabled`):
- SaveAndRetrieveArticle_WithPostgreSQL
- BulkInsert_WithPostgreSQL
- ComplexQuery_WithPostgreSQL
- SourceWithArticles_WithPostgreSQL
- UpdateArticle_WithPostgreSQL
- DeleteArticle_WithPostgreSQL

**Pour activer**: Renommer `PostgreSqlIntegrationTests.cs.disabled` en `PostgreSqlIntegrationTests.cs`

## 🚀 Exécution

### Tous les tests
```bash
dotnet test tests/Vigie24.Tests/
```

### Par catégorie
```bash
# Unitaires
dotnet test --filter "FullyQualifiedName~Unit"

# Fonctionnels
dotnet test --filter "FullyQualifiedName~Functional"

# Intégration
dotnet test --filter "FullyQualifiedName~Integration"

# Par classe
dotnet test --filter "FullyQualifiedName~DomainTests"
dotnet test --filter "FullyQualifiedName~WorkerTests"
dotnet test --filter "FullyQualifiedName~ScoringFunctionalTests"
```

### Avec sortie détaillée
```bash
dotnet test --verbosity normal
dotnet test --logger "console;verbosity=detailed"
```

### Avec couverture de code (si Coverlet installé)
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Bonnes pratiques

### Naming Convention
```csharp
// Format: MethodName_Scenario_ExpectedResult
[Fact]
public void ArticleWithHighScore_ShouldBePublished()
{
    // ...
}
```

### Arrange-Act-Assert
```csharp
[Fact]
public void TestName()
{
    // Arrange
    var input = ...;
    var expected = ...;

    // Act
    var result = _service.Process(input);

    // Assert
    Assert.Equal(expected, result);
}
```

### Theory vs Fact
- **Fact**: Test unique
- **Theory**: Test avec multiples jeux de données
```csharp
[Theory]
[InlineData(90, 10)]
[InlineData(75, 8)]
public void RankingScore_ShouldReflectEditorialAndAlgorithmic(decimal score, int expectedRange)
{
    // ...
}
```

## 🔄 Intégration Continue

### GitHub Actions
```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Run tests
        run: dotnet test --no-build --verbosity normal
```

### Gitea Actions
```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Run tests
        run: dotnet test --verbosity normal
```

## 🎯 Prochaines étapes

### Tests fonctionnels (Priorité haute)
- [ ] Tests API endpoints (Web)
- [ ] Tests du Worker avec mocks réalistes
- [ ] Tests de l'AI Service avec réponses simulées

### Tests d'intégration (Priorité moyenne)
- [ ] Activer et corriger tests PostgreSQL (Testcontainers)
- [ ] Tests avec Docker Compose (services réels)
- [ ] Tests de performance et charge

### Tests E2E (Priorité basse)
- [ ] Tests du workflow complet RSS → IA → Base
- [ ] Tests de l'interface Web (Playwright)
- [ ] Tests de monitoring et alertes

### Couverture de code
- [ ] Intégrer Coverlet
- [ ] Définir seuil minimum (80%)
- [ ] Générer rapports HTML

## 📈 Métriques

| Métrique | Valeur | Cible |
|----------|--------|-------|
| **Total tests** | 65 | 100+ |
| **Succès** | 100% | 100% |
| **Durée** | 372ms | < 1s |
| **Couverture** | TBD | 80% |
| **Tests unitaires** | 38 | 50+ |
| **Tests fonctionnels** | 15 | 30+ |
| **Tests intégration** | 6 | 20+ |

## 🐛 Dépannage

### Tests PostgreSQL échouent
```bash
# Vérifier que Docker est en cours d'exécution
docker ps

# Activer les tests PostgreSQL
cd tests/Vigie24.Tests/Integration/
mv PostgreSqlIntegrationTests.cs.disabled PostgreSqlIntegrationTests.cs

# Exécuter uniquement les tests PostgreSQL
dotnet test --filter "FullyQualifiedName~PostgreSql"
```

### Erreurs de compilation
```bash
# Restaurer les packages
dotnet restore tests/Vigie24.Tests/

# Nettoyer et reconstruire
dotnet clean tests/Vigie24.Tests/
dotnet build tests/Vigie24.Tests/
```

## 📚 Ressources

- [xUnit.net Documentation](https://xunit.net/)
- [Testcontainers Documentation](https://testcontainers.com/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
- [Moq Documentation](https://github.com/devlooped/moq)

---

**Dernière mise à jour**: 2026-08-18  
**Maintenu par**: Philippe (philippe@walby.dev)

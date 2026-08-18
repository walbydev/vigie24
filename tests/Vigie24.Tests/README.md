# Tests Vigie24

Suite de tests complète pour le projet Vigie24.

## 📊 Résultats

**44 tests** - **100% de réussite** (672ms d'exécution)

## 📁 Structure des tests

```
tests/Vigie24.Tests/
├── Unit/
│   ├── Domain/
│   │   └── DomainTests.cs           # Tests Article, Source, Category, Tag
│   ├── Services/
│   │   └── AiServiceTests.cs        # Tests AI Factory & Settings
│   └── Worker/
│       └── WorkerTests.cs           # Tests Worker, Scoring, FeedItems
├── Integration/
│   └── DbContextTests.cs            # Tests EF Core avec InMemory DB
├── appsettings.test.json             # Configuration de test
├── test_data.sql                     # Données SQL de test
├── Vigie24.Tests.csproj              # Projet .NET 10
└── README.md                         # Ce fichier
```

## 🧪 Catégories de tests

### Tests Unitaires (38 tests)

#### Domain (8 tests)
- **ArticleTests**
  - `CreateArticle_WithValidData_ShouldSucceed` ✅
  - `Article_ShouldHaveDefaultValues` ✅
- **SourceTests**
  - `CreateSource_WithValidData_ShouldSucceed` ✅
  - `Source_ShouldHaveDefaultIsActiveTrue` ✅
- **CategoryTests**
  - `CreateCategory_WithValidData_ShouldSucceed` ✅
  - `Category_ShouldHaveDefaultValues` ✅
- **TagTests**
  - `CreateTag_WithValidData_ShouldSucceed` ✅
  - `Tag_ShouldHaveDefaultTimestamps` ✅

#### Services (8 tests)
- **AiServiceFactoryTests**
  - `CreateService_WithInfomaniakProvider_ShouldReturnService` ✅
  - `CreateService_WithDeepSeekProvider_ShouldReturnService` ✅
  - `CreateService_WithUnknownProvider_ShouldThrowException` ✅
  - `CreateService_WithEmptyProvider_ShouldThrowException` ✅
- **AiServiceSettingsTests**
  - `AiServiceSettings_ShouldHaveDefaults` ✅
  - `AiServiceSettings_ShouldAllowCustomValues` ✅
  - `AiServiceSettings_WithInvalidTimeout_ShouldAcceptButMayFailLater` ✅ (2x)

#### Worker (22 tests)
- **FeedItemTests** (4 tests)
  - `FeedItem_ShouldHaveDefaultValues` ✅
  - `FeedItem_ShouldSetProperties` ✅
  - `ArticleFeedItem_ShouldHaveDefaultValues` ✅
  - `ArticleFeedItem_ShouldSetProperties` ✅
- **ScoringTests** (3 tests)
  - `CalculateRecencyScore_WithRecentArticle_ShouldReturnHighScore` ✅
  - `CalculateRecencyScore_WithOldArticle_ShouldReturnLowScore` ✅
  - `CalculateContentScore_WithLongSummary_ShouldReturnHigherScore` ✅
  - `CalculateAlgorithmicScore_ShouldConsiderRecentArticles` ✅
- **ArticleProcessingTests** (10 tests)
  - `IsPublished_ShouldBeTrueWhenScoreAboveThreshold` ✅ (5x)
  - `Article_ShouldHaveValidUrlFormat` ✅
  - `Article_ShouldHaveValidTimestamps` ✅
  - `Article_ShouldSupportMultipleLanguages` ✅ (3x)
- **CategoryTagTests** (4 tests)
  - `CategorySlug_ShouldBeLowercase` ✅
  - `TagSlug_ShouldBeValidFormat` ✅
  - `SourceKind_ShouldHaveValidValues` ✅
  - `IngestionMethod_ShouldHaveValidValues` ✅

### Tests d'Intégration (6 tests)

#### DbContext (6 tests)
- `SaveAndRetrieveArticle_ShouldPersistData` ✅
- `GetActiveSources_ShouldReturnOnlyActiveSources` ✅
- `GetArticlesByCategory_ShouldReturnCorrectArticles` ✅
- `GetRecentArticles_ShouldOrderByPublishedAtDescending` ✅
- `BulkInsertArticles_ShouldHandleMultipleArticles` ✅
- `GetArticlesWithHighScore_ShouldFilterCorrectly` ✅

## 🚀 Exécution

### Tous les tests
```bash
cd tests/Vigie24.Tests
dotnet test
```

### Avec sortie détaillée
```bash
dotnet test --verbosity normal
```

### Par catégorie
```bash
# Domain uniquement
dotnet test --filter "FullyQualifiedName~Domain"

# Services uniquement
dotnet test --filter "FullyQualifiedName~Services"

# Worker uniquement
dotnet test --filter "FullyQualifiedName~Worker"

# Integration uniquement
dotnet test --filter "FullyQualifiedName~Integration"
```

### Avec couverture de code (si configuré)
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Ajouter de nouveaux tests

### Créer un nouveau test unitaire

1. Crée un fichier dans le bon dossier :
```
tests/Vigie24.Tests/Unit/[Category]/[ClassName]Tests.cs
```

2. Structure minimale :
```csharp
namespace Vigie24.Tests.Unit.[Category];

using Vigie24.Domain.Models;

public class [ClassName]Tests
{
    [Fact]
    public void TestMethod_ShouldDoSomething()
    {
        // Arrange
        var expected = "expected value";
        
        // Act
        var result = [method under test];
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Créer un nouveau test d'intégration

1. Utilise `IAsyncLifetime` pour setup/teardown :
```csharp
public class MyIntegrationTests : IAsyncLifetime
{
    private Vigie24DbContext _context = null!;
    
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")
            .Options;
        _context = new Vigie24DbContext(options);
    }
    
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }
}
```

## 🛠️ Bonnes pratiques

1. **Naming convention** : `MethodName_ShouldDoSomething_WhenCondition`
2. **Arrange-Act-Assert** : Toujours dans cet ordre
3. **Un seul fait par test** : Un test = une assertion principale
4. **Tests indépendants** : Pas de dépendance entre tests
5. **Données de test** : Crée des données dans le test, pas en static

## 📊 CI/CD

### GitHub Actions
```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Run tests
        run: dotnet test tests/Vigie24.Tests/
```

## 🎯 Prochaines étapes

- [ ] Tests fonctionnels des API endpoints (Web)
- [ ] Tests de performance et charge
- [ ] Tests d'intégration avec PostgreSQL réel (Testcontainers)
- [ ] Tests E2E du workflow complet
- [ ] Intégration Coverlet pour couverture de code
- [ ] Tests de non-régression avec snapshots

## 📈 Métriques

| Métrique | Valeur |
|----------|--------|
| Total tests | 44 |
| Succès | 100% |
| Durée moyenne | 672ms |
| Tests Domain | 8 |
| Tests Services | 8 |
| Tests Worker | 22 |
| Tests Integration | 6 |

---

**Dernière mise à jour** : 2026-08-18
**Dernière exécution** : 44/44 tests réussis

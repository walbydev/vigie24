# Tests Vigie24

Suite de tests unitaires pour le projet Vigie24.

## Structure des tests

```
tests/Vigie24.Tests/
├── Unit/
│   ├── Domain/
│   │   └── DomainTests.cs           # Tests des modèles (Article, Source, Category, Tag)
│   └── Services/
│       └── AiServiceTests.cs         # Tests des services IA (Factory, Settings)
├── appsettings.test.json             # Configuration de test
├── test_data.sql                     # Données de test SQL
├── Vigie24.Tests.csproj              # Projet de tests
└── README.md                         # Ce fichier
```

## Types de tests implémentés

### Tests Unitaires

**Couverture actuelle :**
- **Domain/DomainTests.cs** : 
  - `ArticleTests` - Création et valeurs par défaut des articles
  - `SourceTests` - Création et état des sources RSS
  - `CategoryTests` - Création et hiérarchie des catégories
  - `TagTests` - Création et timestamps des tags
- **Services/AiServiceTests.cs** :
  - `AiServiceFactoryTests` - Factory de services IA (infomaniak, deepseek, erreur)
  - `AiServiceSettingsTests` - Configuration et validation des paramètres IA

**Caractéristiques :**
- Exécution rapide (< 20ms pour 16 tests)
- Pas de dépendances externes (base de données, API)
- Tests déterministes et reproductibles

## Exécution des tests

### Tous les tests

```bash
cd tests/Vigie24.Tests
dotnet test
```

### Avec sortie détaillée

```bash
dotnet test --verbosity normal
```

### Avec couverture de code

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Par catégorie

```bash
# Tests de domaine uniquement
dotnet test --filter "FullyQualifiedName~Domain"

# Tests de services uniquement
dotnet test --filter "FullyQualifiedName~Services"
```

## Résultats actuels

```
Réussi!  - échec: 0, réussite: 16, ignorée(s): 0, total: 16, durée: 19 ms
```

### Détail des tests

| Test | Statut | Description |
|------|--------|-------------|
| `ArticleTests.CreateArticle_WithValidData_ShouldSucceed` | ✅ | Création d'article valide |
| `ArticleTests.Article_ShouldHaveDefaultValues` | ✅ | Valeurs par défaut correctes |
| `SourceTests.CreateSource_WithValidData_ShouldSucceed` | ✅ | Création de source valide |
| `SourceTests.Source_ShouldHaveDefaultIsActiveTrue` | ✅ | Source active par défaut |
| `CategoryTests.CreateCategory_WithValidData_ShouldSucceed` | ✅ | Création de catégorie valide |
| `CategoryTests.Category_ShouldHaveDefaultValues` | ✅ | Valeurs par défaut correctes |
| `TagTests.CreateTag_WithValidData_ShouldSucceed` | ✅ | Création de tag valide |
| `TagTests.Tag_ShouldHaveDefaultTimestamps` | ✅ | Timestamps par défaut |
| `AiServiceFactoryTests.CreateService_WithInfomaniakProvider_ShouldReturnService` | ✅ | Factory Infomaniak |
| `AiServiceFactoryTests.CreateService_WithDeepSeekProvider_ShouldReturnService` | ✅ | Factory DeepSeek |
| `AiServiceFactoryTests.CreateService_WithUnknownProvider_ShouldThrowException` | ✅ | Erreur provider inconnu |
| `AiServiceFactoryTests.CreateService_WithEmptyProvider_ShouldThrowException` | ✅ | Erreur provider vide |
| `AiServiceSettingsTests.AiServiceSettings_ShouldHaveDefaults` | ✅ | Valeurs par défaut |
| `AiServiceSettingsTests.AiServiceSettings_ShouldAllowCustomValues` | ✅ | Valeurs personnalisées |
| `AiServiceSettingsTests.AiServiceSettings_WithInvalidTimeout_ShouldAcceptButMayFailLater` | ✅ | Timeout invalide |

## Architecture des tests

### Projet de tests

- **Framework** : xUnit
- **Mocking** : Moq (prêt pour tests de services externes)
- **Target Framework** : .NET 10.0
- **Packages principaux** :
  - `xunit` 2.9.0
  - `Moq` 4.20.70
  - `Testcontainers` 3.9.0 (prêt pour tests d'intégration)
  - `Microsoft.EntityFrameworkCore.InMemory` 10.0.0

### Configuration

Les tests utilisent `appsettings.test.json` pour la configuration :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=vigie24_test;Username=postgres;Password=postgres"
  },
  "AiService": {
    "Provider": "infomaniak",
    "Model": "Qwen/Qwen3.5-122B-A10B-FP8"
  }
}
```

## Prochaines étapes

### Tests d'intégration (à implémenter)

- [ ] Tests avec PostgreSQL réel via Testcontainers
- [ ] Tests des migrations de base de données
- [ ] Tests des relations entre entités
- [ ] Tests des requêtes EF Core complexes

### Tests fonctionnels (à implémenter)

- [ ] Tests du Worker (ingestion RSS)
- [ ] Tests du traitement IA complet
- [ ] Tests des flux métier complets
- [ ] Tests de performance et charge

### Couverture de code

- [ ] Configurer Coverlet pour génération de rapports
- [ ] Définir seuils minimum de couverture
- [ ] Intégrer dans CI/CD

## Intégration Continue

### Exemple GitHub Actions

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
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
```

## Bonnes pratiques

1. **Nommer les tests clairement** : `MethodName_ShouldDoSomething_WhenCondition`
2. **Un seul assert par test** : Facilite le débogage
3. **Tests indépendants** : Chaque test doit pouvoir s'exécuter seul
4. **Données de test réalistes** : Mieux reflète la production
5. **Tester les cas d'erreur** : Ne pas tester que le "happy path"
6. **Maintenir la couverture** : Ajouter des tests pour chaque nouvelle fonctionnalité

## Dépannage

### Tests qui échouent

```bash
# Exécuter avec sortie détaillée
dotnet test --logger "console;verbosity=detailed"

# Exécuter un test spécifique
dotnet test --filter "FullyQualifiedName~ArticleTests.CreateArticle_WithValidData_ShouldSucceed"
```

### Problèmes de dépendances

```bash
# Nettoyer et restaurer
dotnet clean
dotnet restore

# Mettre à jour les packages
dotnet list package --outdated
dotnet update
```

## Ressources

- [xUnit Documentation](https://xunit.net/docs/)
- [Moq Documentation](https://github.com/devlooped/Moq)
- [Testcontainers Documentation](https://testcontainers.com/)
- [Entity Framework Testing](https://learn.microsoft.com/en-us/ef/core/testing/)

---

**Dernière mise à jour** : 2026-08-18  
**Statut** : ✅ 16/16 tests réussissants  
**Couverture** : Tests unitaires (Domain + Services)

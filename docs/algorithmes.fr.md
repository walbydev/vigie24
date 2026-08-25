# Vigie24 — Algorithmes

> Document compagnon : [architecture.fr.md](architecture.fr.md) décrit les
> composants et le flux de données. Ce document est la référence de toutes les
> formules et règles de rapprochement. Les constantes de scoring ci-dessous vivent
> dans `Vigie24.Domain.ArticleScoring` et sont la **source de vérité unique**
> (partagées par le Worker et l'indication « score précis » du Web).

## 1. Pipeline d'ingestion

Pour chaque source, à chaque cycle du worker (au plus `FeedReader:MaxItemsPerFeed`
éléments, défaut 15) :

1. **Récupération** — RSS/Atom via `SyndicationFeed`, YouTube via
   `https://www.youtube.com/feeds/videos.xml?channel_id=…`, Reddit via l'API JSON.
   Un `User-Agent` de navigateur est envoyé ; les dates invalides sont rejetées (un
   flux impossible à parser est ignoré, le worker ne plante jamais).
2. **Dédup exacte** — si un article avec la même `CanonicalUrl` existe déjà, il est ignoré.
3. **Dédup sémantique** — le nouvel élément est comparé par paires aux articles les
   plus récents des 7 derniers jours (plafonné par `FeedReader:MaxDuplicateCheckArticles`,
   défaut 10) via l'appel IA `IsDuplicateAsync`. Toute correspondance → ignoré.
4. **Analyse IA** — `ProcessArticleAsync(title, content, sourceUrl)` renvoie :
   résumé français, score éditorial (0–100), thèmes, hiérarchie de catégories,
   mots-clés, topic + stance de débat, score d'anxiété (0–10). L'usage de tokens est
   capturé pour le suivi des coûts.
5. **Illustration** — image du flux (`media:content`/`enclosure`), sinon scrapée de
   la page de l'article (`FeedReader:ScrapeArticleImages`, défaut true).
6. **Persistance** — l'article est ajouté au contexte *immédiatement* (avant tout
   `SaveChanges` intermédiaire) pour éviter une violation de clé étrangère sur
   `article_embeddings`. L'embedding (titre + résumé) est calculé de façon non
   bloquante (échec → avertissement).
7. **Scoring & publication** — voir sections 3 à 6. L'article est publié quand
   `EditorialScore ≥ 60` (seuil codé en dur).
8. **Rattachements** — tags (thèmes), lien source primaire, auteur, catégorie
   (section 8). Tous les `DateTime.Kind` sont normalisés en UTC avant l'enregistrement.

## 2. Détection des doublons

| Couche | Règle |
|---|---|
| Exacte | Même `CanonicalUrl` → ignoré |
| Sémantique | Comparaison IA par paires avec les articles des 7 derniers jours, plafonnée à `MaxDuplicateCheckArticles` (10) pour borner les appels IA (rate limits) |

## 3. Score éditorial (IA, 0–100)

Produit par le LLM (`editorial_score` du contrat de résultat) : valeur
informationnelle de l'article pour un public tech francophone. **Seuil de
publication : ≥ 60** (codé en dur).

## 4. Score algorithmique (déterministe, 0–100)

Moyenne de la fraîcheur et de la densité de contenu, chacune sur 0–100 :

```
Fraîcheur : 100 si ≤ 1 h, 80 si ≤ 6 h, 60 si ≤ 24 h, 40 si ≤ 48 h,
            puis max(0, 40 − (heures − 48) / 24 × 10)
Contenu  : 90 si résumé ≥ 50 mots, 70 si ≥ 30, 50 si ≥ 15, sinon 30

Algorithmique = (Fraîcheur + Contenu) / 2
```

## 5. Score d'engagement (0–100)

À partir des votes anonymes (échelle 0–5) et des clics. **0 s'il n'y a ni vote ni
clic** (le classement ne change pas tant que le public n'interagit pas) :

```
scoreVotes  = clamp(moyenneVotes / 5 × 100, 0, 100)      (0 sans vote)
scoreClics  = min(100, nombreClics × 10)                  (0 sans clic)
Engagement  = 0,6 × scoreVotes + 0,4 × scoreClics         (les votes pèsent plus)
```

## 6. Score de classement (0–100)

```
Classement = Éditorial × 0,55 + Algorithmique × 0,35 + Engagement × 0,10
```

Les trois poids sont des constantes (`EditorialWeight`, `AlgorithmicWeight`,
`EngagementWeight`). L'engagement démarre à 0 : un nouvel article est donc classé
uniquement sur la qualité éditoriale + algorithmique.

## 7. Score d'anxiété (0–10)

Mesure subjective et éditoriale de la propension du sujet à inquiéter un lecteur de
veille tech francophone (menaces, crises, alarmisme). Produit par le LLM
(`anxiety_score`), stocké à `null` s'il est hors bornes (garde : `0 ≤ x ≤ 10` — le
modèle renvoie parfois des valeurs hors contrat). L'UI expose un filtre
`maxAnxiety` (navigateur d'articles + RSS) ; les articles à `null` passent toujours.

## 8. Mappage de taxonomie

L'IA renvoie une hiérarchie `[parent, sous-catégorie]` (ex. `["IA", "Modèles LLM"]`).
`GetOrCreateCategoryAsync` la mappe sur la taxonomie fixe (8 parents, ~35
sous-catégories — `Vigie24.Domain.Taxonomy`), en renvoyant l'id de la sous-catégorie,
sinon celui du parent. Repli : mappage hérité par thème principal. Les catégories
hors taxonomie créées par le modèle sont réconciliées périodiquement par des scripts.

## 9. Débats

Les débats rapprochent des articles couvrant le même sujet avec des positions
opposées. Reconstruits à la fin de chaque cycle du worker à partir des articles des
**14 derniers jours** portant un topic + une stance (`RebuildDebatesAsync`).

### 9.1 Normalisation du topic (`DebateEngine.NormalizeTopic`)

Minuscules, sans diacritiques (table d'accents explicite — robuste sans ICU),
ponctuation → espaces, mots de 2 lettres écartés, stop-words supprimés,
**4 premiers mots significatifs conservés**. Exemple : `"Réglementation IA en
Europe"` → `"reglementation ia europe"`.

### 9.2 Normalisation de la stance (`DebateEngine.NormalizeStance`)

Valeurs acceptées et synonymes, ramenés à 4 positions canoniques :

| Canonique | Synonymes (correspondance par préfixe) |
|---|---|
| `pour` | favorable, soutien, pro… |
| `contre` | defavorable, opposition, anti… |
| `nuance` | nuançé, mitigé, prudent, reserve… |
| `neutre` | neutral, objectif, information… |

### 9.3 Règle de débat

Un débat existe quand **≥ 2 articles partagent le même topic normalisé ET qu'au
moins un `pour` + un `contre` sont présents**. `nuance` et `neutre` rejoignent un
débat mais ne le déclenchent pas. Les débats/liens périmés sont supprimés à la
reconstruction.

### 9.4 Fusion sémantique des topics isolés

Un topic à article unique (pas encore d'opposant) est rapproché des groupes de
débat existants par **similarité cosinus d'embedding ≥ 0,85**
(`MergeIsolatedGroupsAsync`) ; le groupe le plus proche gagne, ce qui peut créer
l'opposition manquante. Si le groupe fusionné n'a toujours pas de positions
opposées, il est écarté.

### 9.5 Embeddings

`GetEmbeddingAsync("«Titre» — «Résumé»")` → `bge-multilingual-gemma2`, **3584
dimensions**, stocké dans `article_embeddings.Vector`. Similarité = cosinus.

## 10. Suivi des coûts IA

L'usage de tokens par article (`PromptTokens`, `CompletionTokens`) est capturé
depuis les champs `usage` de l'API et stocké sur l'article. Coût en CHF, tarifs
Infomaniak (prix configurables par million de tokens) :

```
Coût = promptTokens × prixEntréeParMillion / 1_000_000
     + completionTokens × prixSortieParMillion / 1_000_000
```

Agrégé par jour/semaine sur la page `/couts` (`CostReportService`).

## 11. Évaluation de santé (`HealthEvaluator`)

| Niveau | Condition |
|---|---|
| `Ok` | Fournisseur IA joignable ET âge de la dernière ingestion ≤ `okThreshold` |
| `Degraded` | fournisseur joignable ET âge ≤ `degradedThreshold` |
| `Offline` | fournisseur injoignable, OU aucun article jamais ingéré, OU âge > `degradedThreshold` |

Le bloc de statuts de la page d'accueil utilise `FormatAge` pour une fraîcheur
lisible (« il y a 12 min », « il y a 3 h 05 », « il y a 2 j »).

## 12. Génération des flux RSS (`RssFeedService`)

`/feed` (global) et `/categories/{slug}/feed` partagent un même builder : filtres
appliqués via `ArticleFiltering.ApplyFilterCriteria` (même logique que le navigateur
web), tri par date décroissante, **plafonné à 50 éléments**. Les réponses portent
`Cache-Control: public, max-age=300`.

# Vigie24 — Politique éditoriale des sources

Vigie24 agrège l'actualité technologique francophone. Les sources candidates
sont examinées selon les critères ci-dessous. Aucun placement payant, aucun
échange de liens, aucun partenariat. Ajouter une source à Vigie24 n'accorde
rien et ne rapporte rien, hormis le lien vers les articles originaux.

## Retenues

- Médias d'actualité, presse spécialisée, sources institutionnelles et
  académiques.
- Blogs d'expert, newsletters et chaînes techniques dont la production
  principale est de l'information, non de la promotion.
- Chaînes YouTube et subreddits modérés par des humains (interrogés 2×/jour).

## Écartées

- **Contenu à finalité commerciale** : guides d'achat, comparatifs d'outils ou
  de services, « avis » ou « tests » d'un fournisseur, pages de présentation
  d'une offre — tout texte dont l'objectif principal est de provoquer un clic
  affilié ou une inscription. Indépendamment de la qualité rédactionnelle, de
  la longueur ou de l'audience.
- **Fermes de contenu générées par IA** : volume indifférencié, auteur unique,
  absence de responsabilité éditoriale assumée.
- **Flux de pure republication** : copies intégrales des articles d'un autre
  média.

## Deux niveaux d'application

1. **Plafond de score (IA)** : le prompt d'analyse plafonne `editorial_score` à
   45 (le seuil de publication est 60) pour tout contenu à visée commerciale,
   et impose d'en indiquer la raison dans `justification`. Cela attrape un
   article commercial publié par une source par ailleurs légitime.
2. **Exclusion de source (base)** : `sources."EditorialPolicy" = 'Commercial'`
   retire la source de l'interrogation — voir
   `Vigie24.Domain.SourceEditorialPolicy` et `scripts/exclude-commercial-sources.sql`.
   Colonne distincte de `IsActive` à dessein : la sonde de récupération à 24 h
   réactive les sources désactivées automatiquement, et ne doit jamais annuler
   une exclusion éditoriale.

## Répondre à une sollicitation

Les propositions de sources arrivent le plus souvent enveloppées dans une
demande de lien. La réponse est courte et n'ouvre pas de négociation :

- les sources sont retenues sur critères éditoriaux, sans contrepartie et sans
  logique de référencement ;
- Vigie24 n'affiche jamais le contenu d'un article : titre, résumé produit par
  l'IA à partir du flux, lien vers l'original, et la vignette du flux ;
- des conditions qui interdisent le résumé ou la vignette (par exemple
  « titres, extraits courts et liens seulement, sans images ») sont
  incompatibles avec le service ;
- les critères sont publiés ici, et ne se négocient pas au cas par cas.

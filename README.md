# FixedEngine

**FixedEngine** est une bibliothèque .NET Standard 2.0 pour les moteurs de jeux Unity, MonoGame, et toute stack C# nécessitant des calculs déterministes, rétro-faithful et compatibles export WebGL/HTML5.  
Elle fournit :
- Des entiers signés et non signés custom (`IntN`, `UIntN`) wrap hardware, avec taille paramétrable (ex: 8, 16, 24, 32 bits)
- Des types Fixed-Point génériques (`Fixed<TInt, TFrac>`, `UFixed<TUInt, TFrac>`) performants et bit-faithful (Q8.8, Q16.8, Q24.8, etc.)
- Des structures de math 2D (vecteurs, matrices, transforms) compatibles fixed et int custom
- Des outils d'interopérabilité, de lookup table, de tests unitaires exhaustifs, et des conversions/primitives fidèles au hardware rétro

## Objectif

Garantir **le même résultat binaire** sur toutes plateformes (Windows, Linux, Mac, WebGL, consoles)  
→ Idéal pour la physique déterministe, le netcode, les jeux compétitifs, ou la rétrocompatibilité console/émulation.

## Pourquoi FixedEngine ?

- **Déterminisme strict** : tous les calculs sont bit-faithful, aucun flottant caché, wrap/clamp à la volée, sans dépendance à la plateforme ou au compilateur.
- **Interopérabilité** : conçu pour s'intégrer en tant que bibliothèque dans Unity, MonoGame, Stride, Godot, Raylib...
- **Performance** : optimisé pour WebGL/HTML5 et plateformes embarquées (aucune allocation, branchless autant que possible).
- **Flexible** : supporte tout format de fixed-point (Qm.n), aliases simples, transformations 2D, LUT rétro.

## Fonctionnalités principales

- `IntN<TBits>`, `UIntN<TBits>` : entiers wrap hardware, sign-extend, conversions branchless
- `Fixed<TInt, TFrac>`, `UFixed<TUInt, TFrac>` : fixed-point signés/non-signés, arithmétique branchless, conversions LUT
- `Vec2`, `Mat2x2`, `Transform2D`, etc. : structures mathématiques 2D compatibles int/fixed
- Sérialisation/Parsing : JSON, hex, binaire, meta-infos (pour le tooling & network)
- Look-up tables (LUT) pour trigonométrie et fonctions math accélérées (optionnelles)
- Tests unitaires exhaustifs à venir

## Utilisation

**Installation**  
(Copier les sources, ou ajouter en tant que sous-module/sous-dossier, ou via NuGet à venir)


# FixedEngine

**FixedEngine** est une bibliothèque mathématique déterministe en C# (.NET Standard 2.0), conçue pour les moteurs de jeux comme **Unity**, **MonoGame** et toute stack .NET nécessitant des calculs rétros fidèles, optimisés pour le WebGL/HTML5, la simulation et le netcode.

## ✨ À propos

- **Déterminisme bit-faithful** : arithmétique sans flottant caché, wrap hardware, résultats identiques sur toutes plateformes
- **Types génériques** : entiers signés/non signés custom (`IntN`, `UIntN`), fixed-point (`Fixed<TInt, TFrac>`, `UFixed<TUInt, TFrac>`) avec précision paramétrable (Q8.8, Q16.8, Q24.8…)
- **Structures math 2D** : vecteurs, matrices, transforms compatibles fixed ou int custom
- **Interopérabilité** : intégré facilement à Unity, MonoGame, Godot, Raylib, et autres
- **Performance** : zéro allocation, branchless autant que possible, optimal pour la rétro-compatibilité hardware

## 🚧 Statut

**Le projet est en développement actif et n’est pas encore open source.**  
> **Aucune utilisation, redistribution ou contribution n’est autorisée sans l’accord explicite de l’auteur.**  
> Une licence open source (MIT ou Apache) sera appliquée dès que le projet sera stabilisé.

Voir [`UNLICENSED.md`](UNLICENSED.md) pour le détail.

## Fonctionnalités principales

- `IntN<TBits>`, `UIntN<TBits>` : entiers wrap/sign-extend hardware, conversion branchless, conversions hex/bin
- `Fixed<TInt, TFrac>`, `UFixed<TUInt, TFrac>` : fixed-point Q-format, arithmétique optimisée, conversions rétro, LUTs optionnelles
- Structures mathématiques 2D : `Vec2`, `Mat2x2`, `Transform2D`, versions signed/unsigned
- Sérialisation/Parsing : JSON, binaire, hexadécimal, formats méta (tooling, netcode, save)
- Tests unitaires exhaustifs


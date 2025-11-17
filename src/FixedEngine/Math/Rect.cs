using FixedEngine.Core;
using FixedEngine.Math;
using System;
using System.Linq;
public struct Rect<TInt, TFrac>
    where TInt : struct
    where TFrac : struct
{

    /* ==========================================
     * CONSTRUCTEURS & INSTANCES PRÉDÉFINIES
     * - Rect(min, max)
     * - FromMinMax(min, max)
     * - FromMinSize(min, size)
     * - FromCenterSize(center, size)
     * - FromXYWH(x, y, w, h)
     * - FromLTRB(left, top, right, bottom)
     * - FromPoints(pt1, pt2)
     * - FromCorners(corner1, corner2)
     * - Zero
     * - Unit
     * - One
     * - Infinite
     * - Empty
     * - ByteSize
     * ========================================== */
    #region --- Constructeurs & Instances prédéfinies ---

    // Principal : min + max (par convention, Min = coin inf. gauche, Max = coin sup. droit)
    public Rect(Vec2<TInt, TFrac> min, Vec2<TInt, TFrac> max)
    {
        Min = min;
        Max = max;
    }

    // Alias "FromMinMax" pour clarté d'intention
    public static Rect<TInt, TFrac> FromMinMax(Vec2<TInt, TFrac> min, Vec2<TInt, TFrac> max)
        => new Rect<TInt, TFrac>(min, max);

    // Création à partir de min + taille
    public static Rect<TInt, TFrac> FromMinSize(Vec2<TInt, TFrac> min, Vec2<TInt, TFrac> size)
        => new Rect<TInt, TFrac>(min, min + size);

    // Création à partir du centre et taille
    public static Rect<TInt, TFrac> FromCenterSize(Vec2<TInt, TFrac> center, Vec2<TInt, TFrac> size)
    {
        var half = size / (Fixed<TInt, TFrac>)2;
        return new Rect<TInt, TFrac>(center - half, center + half);
    }

    // Création à partir de (x, y, width, height)
    public static Rect<TInt, TFrac> FromXYWH(Fixed<TInt, TFrac> x, Fixed<TInt, TFrac> y, Fixed<TInt, TFrac> w, Fixed<TInt, TFrac> h)
        => new Rect<TInt, TFrac>(new Vec2<TInt, TFrac>(x, y), new Vec2<TInt, TFrac>(x + w, y + h));

    // Création à partir de bords gauche, haut, droit, bas (LTRB)
    public static Rect<TInt, TFrac> FromLTRB(Fixed<TInt, TFrac> left, Fixed<TInt, TFrac> top, Fixed<TInt, TFrac> right, Fixed<TInt, TFrac> bottom)
        => new Rect<TInt, TFrac>(new Vec2<TInt, TFrac>(left, bottom), new Vec2<TInt, TFrac>(right, top));

    // Création à partir de deux points quelconques (les remet dans l'ordre pour former le rect)
    public static Rect<TInt, TFrac> FromPoints(Vec2<TInt, TFrac> pt1, Vec2<TInt, TFrac> pt2)
        => new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Min(pt1.X, pt2.X),
                Fixed<TInt, TFrac>.Min(pt1.Y, pt2.Y)),
            new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Max(pt1.X, pt2.X),
                Fixed<TInt, TFrac>.Max(pt1.Y, pt2.Y))
        );

    // Alias : FromCorners = FromPoints (naming pour la compat API Unity/Godot)
    public static Rect<TInt, TFrac> FromCorners(Vec2<TInt, TFrac> corner1, Vec2<TInt, TFrac> corner2)
        => FromPoints(corner1, corner2);

    // Rect tout à zéro (point nul)
    public static readonly Rect<TInt, TFrac> Zero = new Rect<TInt, TFrac>(Vec2<TInt, TFrac>.Zero, Vec2<TInt, TFrac>.Zero);
    // Rect [0,0]–[1,1]
    public static readonly Rect<TInt, TFrac> Unit = new Rect<TInt, TFrac>(Vec2<TInt, TFrac>.Zero, Vec2<TInt, TFrac>.One);
    // Rect [0,0]–[One,One] (utile si One = 256, 65536, etc.)
    public static readonly Rect<TInt, TFrac> One = new Rect<TInt, TFrac>(Vec2<TInt, TFrac>.Zero, Vec2<TInt, TFrac>.One);
    // Rect infini (selon Fixed, à adapter selon la sémantique désirée)
    public static readonly Rect<TInt, TFrac> Infinite = new Rect<TInt, TFrac>(
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MinValue, Fixed<TInt, TFrac>.MinValue),
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MaxValue, Fixed<TInt, TFrac>.MaxValue));
    // Rect vide (alias de Zero)
    public static readonly Rect<TInt, TFrac> Empty = Zero;

    public static readonly int ByteSize = Vec2<TInt, TFrac>.ByteSize * 2;

    #endregion

    /* ==========================================
     * ACCÈS & PROPRIÉTÉS
     * - Min, Max
     * - Width, Height, Size
     * - Center
     * - X, Y, XMax, YMax
     * - Top, Bottom, Left, Right
     * - Area
     * - Diagonal
     * - AspectRatio
     * - IsEmpty
     * - IsValid
     * - BottomLeft, BottomRight, TopLeft, TopRight
     * - MinValue, MaxValue
     * - Indexeur d'accès aux coins
     * - Indexeur d'accès aux axes
     * ========================================== */
    #region --- Accès & Propriétés ---

    public Vec2<TInt, TFrac> Min;  // Coin inférieur gauche
    public Vec2<TInt, TFrac> Max;  // Coin supérieur droit

    public Fixed<TInt, TFrac> X => Min.X;
    public Fixed<TInt, TFrac> Y => Min.Y;
    public Fixed<TInt, TFrac> XMax => Max.X;
    public Fixed<TInt, TFrac> YMax => Max.Y;

    // Largeur, hauteur
    public Fixed<TInt, TFrac> Width => Max.X - Min.X;
    public Fixed<TInt, TFrac> Height => Max.Y - Min.Y;
    public Vec2<TInt, TFrac> Size => new Vec2<TInt, TFrac>(Width, Height);

    // Centre
    public Vec2<TInt, TFrac> Center =>
        new Vec2<TInt, TFrac>((Min.X + Max.X) / (Fixed<TInt, TFrac>)2, (Min.Y + Max.Y) / (Fixed<TInt, TFrac>)2);


    // Côtés
    public Fixed<TInt, TFrac> Left => Min.X;
    public Fixed<TInt, TFrac> Right => Max.X;
    public Fixed<TInt, TFrac> Bottom => Min.Y;
    public Fixed<TInt, TFrac> Top => Max.Y;

    // Aire
    public Fixed<TInt, TFrac> Area => Width * Height;

    // Diagonale du rect (utile pour debug, spatial hashing, distance max, etc.)
    public Vec2<TInt, TFrac> Diagonal => Max - Min;

    // Test si rect vide ou inversé (aire nulle ou négative)
    public bool IsEmpty => Width <= Fixed<TInt, TFrac>.Zero || Height <= Fixed<TInt, TFrac>.Zero;

    // Rectangle bien formé (Min <= Max sur les deux axes)
    public bool IsValid => Min.X <= Max.X && Min.Y <= Max.Y;

    // Rapport largeur/hauteur (pour layout, UI, etc.)
    public Fixed<TInt, TFrac> AspectRatio =>
        Height == Fixed<TInt, TFrac>.Zero
            ? Fixed<TInt, TFrac>.Zero
            : Width / Height;
    
    // Coins
    public Vec2<TInt, TFrac> BottomLeft => Min;
    public Vec2<TInt, TFrac> BottomRight => new Vec2<TInt, TFrac>(Max.X, Min.Y);
    public Vec2<TInt, TFrac> TopLeft => new Vec2<TInt, TFrac>(Min.X, Max.Y);
    public Vec2<TInt, TFrac> TopRight => Max;

    // Bornes extrêmes (utile pour clamp ou comparaisons de masse)
    public static readonly Rect<TInt, TFrac> MinValue = new Rect<TInt, TFrac>(
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MinValue, Fixed<TInt, TFrac>.MinValue),
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MinValue, Fixed<TInt, TFrac>.MinValue));

    public static readonly Rect<TInt, TFrac> MaxValue = new Rect<TInt, TFrac>(
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MaxValue, Fixed<TInt, TFrac>.MaxValue),
        new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MaxValue, Fixed<TInt, TFrac>.MaxValue));


    // Indexeur d'accès aux coins (pratique pour boucle, API générique, etc.)
    public Vec2<TInt, TFrac> this[int index] =>
        index switch
        {
            0 => BottomLeft,
            1 => BottomRight,
            2 => TopRight,
            3 => TopLeft,
            _ => throw new IndexOutOfRangeException()
        };

    public Fixed<TInt, TFrac> Axis(int axis)
    {
        return axis switch
        {
            0 => Width,
            1 => Height,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion

    /* ==========================================
     * OPÉRATEURS & ÉGALITÉ
     * - operator ==, !=
     * - Equals(object)
     * - Equals(Rect<TInt, TFrac>)
     * - GetHashCode()
     * ========================================== */
    #region --- Opérateurs & Égalité ---

    public static bool operator ==(Rect<TInt, TFrac> a, Rect<TInt, TFrac> b)
        => a.Min == b.Min && a.Max == b.Max;

    public static bool operator !=(Rect<TInt, TFrac> a, Rect<TInt, TFrac> b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is Rect<TInt, TFrac> other && this == other;

    public bool Equals(Rect<TInt, TFrac> other) => this == other;

    public override int GetHashCode()
        => Min.GetHashCode() ^ (Max.GetHashCode() << 1);

    #endregion

    /* ==========================================
     * HELPERS CLASSIQUES & MÉTHODES GÉOMÉTRIQUES
     * - Contains(point), Contains(rect)
     * - Intersects(rect)
     * - Encapsulate(point), Encapsulate(rect)
     * - Union(rect)
     * - Intersection(rect)
     * - Expand(amount), Contract(amount)
     * - Clamp(bounds)
     * - Moved(delta)
     * - Scaled(factor)
     * - SwapXY
     * - IsDegenerate
     * - Copy
     * ========================================== */
    #region --- Helpers classiques & Méthodes géométriques ---

    // Teste si le point est inclus (inclusif)
    public bool Contains(Vec2<TInt, TFrac> pt)
        => pt.X >= Min.X && pt.X <= Max.X && pt.Y >= Min.Y && pt.Y <= Max.Y;

    // Teste si le rectangle other est entièrement inclus dans ce rect
    public bool Contains(Rect<TInt, TFrac> other)
        => Contains(other.Min) && Contains(other.Max);

    // Teste si les deux rectangles se recoupent
    public bool Intersects(Rect<TInt, TFrac> other)
        => !(other.Min.X > Max.X || other.Max.X < Min.X
            || other.Min.Y > Max.Y || other.Max.Y < Min.Y);

    // Crée un rect qui englobe ce rect et le point pt
    public Rect<TInt, TFrac> Encapsulate(Vec2<TInt, TFrac> pt)
    {
        var min = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Min(Min.X, pt.X),
            Fixed<TInt, TFrac>.Min(Min.Y, pt.Y));
        var max = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Max(Max.X, pt.X),
            Fixed<TInt, TFrac>.Max(Max.Y, pt.Y));
        return new Rect<TInt, TFrac>(min, max);
    }

    // Crée un rect qui englobe ce rect et le rect other
    public Rect<TInt, TFrac> Encapsulate(Rect<TInt, TFrac> other)
    {
        var min = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Min(Min.X, other.Min.X),
            Fixed<TInt, TFrac>.Min(Min.Y, other.Min.Y));
        var max = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Max(Max.X, other.Max.X),
            Fixed<TInt, TFrac>.Max(Max.Y, other.Max.Y));
        return new Rect<TInt, TFrac>(min, max);
    }

    // Union : plus petit rect englobant this et other
    public Rect<TInt, TFrac> Union(Rect<TInt, TFrac> other) => Encapsulate(other);

    // Intersection : partie commune (rect vide si aucun recouvrement)
    public Rect<TInt, TFrac> Intersection(Rect<TInt, TFrac> other)
    {
        var min = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Max(Min.X, other.Min.X),
            Fixed<TInt, TFrac>.Max(Min.Y, other.Min.Y));
        var max = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Min(Max.X, other.Max.X),
            Fixed<TInt, TFrac>.Min(Max.Y, other.Max.Y));
        // Si pas de recouvrement : rect vide
        if (max.X < min.X || max.Y < min.Y)
            return Rect<TInt, TFrac>.Empty;
        return new Rect<TInt, TFrac>(min, max);
    }

    // Agrandit le rect (dans toutes les directions)
    public Rect<TInt, TFrac> Expand(Fixed<TInt, TFrac> amount)
        => new Rect<TInt, TFrac>(Min - new Vec2<TInt, TFrac>(amount, amount), Max + new Vec2<TInt, TFrac>(amount, amount));

    // Rétrécit le rect (dans toutes les directions)
    public Rect<TInt, TFrac> Contract(Fixed<TInt, TFrac> amount)
        => Expand(-amount);

    // Clamp ce rect à l’intérieur du rect bounds (si Min/Max sortent, ils sont repliés à bounds.Min/Max)
    public Rect<TInt, TFrac> Clamp(Rect<TInt, TFrac> bounds)
        => new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Max(Min.X, bounds.Min.X),
                Fixed<TInt, TFrac>.Max(Min.Y, bounds.Min.Y)),
            new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Min(Max.X, bounds.Max.X),
                Fixed<TInt, TFrac>.Min(Max.Y, bounds.Max.Y)));

    // Translate ce rect par delta
    public Rect<TInt, TFrac> Moved(Vec2<TInt, TFrac> delta)
        => new Rect<TInt, TFrac>(Min + delta, Max + delta);

    // Mise à l’échelle (autour du centre)
    public Rect<TInt, TFrac> Scaled(Fixed<TInt, TFrac> factor)
    {
        var c = Center;
        var size = Size * factor / (Fixed<TInt, TFrac>)2;
        return new Rect<TInt, TFrac>(c - size, c + size);
    }

    // Inverse X et Y sur Min et Max (utile pour transposer, layouts, etc.)
    public Rect<TInt, TFrac> SwapXY()
        => new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(Min.Y, Min.X),
            new Vec2<TInt, TFrac>(Max.Y, Max.X));

    // True si le rect est réduit à une ligne ou un point (aire nulle)
    public bool IsDegenerate =>
        Width == Fixed<TInt, TFrac>.Zero || Height == Fixed<TInt, TFrac>.Zero;

    // Renvoie une copie du Rect (chaînabilité, sécurité…)
    public Rect<TInt, TFrac> Copy()
        => new Rect<TInt, TFrac>(Min, Max);

    #endregion

    /* ==========================================
     * TRANSFORMATIONS, INTERPOLATIONS & GÉOMÉTRIE AVANCÉE
     * - Lerp(a, b, t)
     * - Transform(Mat2x2)
     * - TransformAffine(Mat2x2, Vec2)
     * - NormalizedToPoint(normalized)
     * - PointToNormalized(pt)
     * - Rotate90()
     * - Rotate180()
     * - Rotate270()
     * - SplitRows(n), SplitCols(n)
     * - MirrorHorizontal(), MirrorVertical()
     * - FlipX(), FlipY()
     * ========================================== */
    #region --- Transformations, Interpolations & Géométrie avancée ---

    // Interpolation linéaire entre deux rectangles (lerp sur min et max)
    public static Rect<TInt, TFrac> Lerp(Rect<TInt, TFrac> a, Rect<TInt, TFrac> b, UFixed<TInt, TFrac> t)
        => new Rect<TInt, TFrac>(
            Vec2<TInt, TFrac>.Lerp(a.Min, b.Min, t),
            Vec2<TInt, TFrac>.Lerp(a.Max, b.Max, t));

    // Transforme tous les coins du rectangle par une matrice (ex: rotation, scale…)
    public Rect<TInt, TFrac> Transform(Mat2x2<TInt, TFrac> mat)
    {
        var bl = mat * BottomLeft;
        var br = mat * BottomRight;
        var tl = mat * TopLeft;
        var tr = mat * TopRight;
        var min = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Min(Fixed<TInt, TFrac>.Min(bl.X, br.X), Fixed<TInt, TFrac>.Min(tl.X, tr.X)),
            Fixed<TInt, TFrac>.Min(Fixed<TInt, TFrac>.Min(bl.Y, br.Y), Fixed<TInt, TFrac>.Min(tl.Y, tr.Y)));
        var max = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Max(Fixed<TInt, TFrac>.Max(bl.X, br.X), Fixed<TInt, TFrac>.Max(tl.X, tr.X)),
            Fixed<TInt, TFrac>.Max(Fixed<TInt, TFrac>.Max(bl.Y, br.Y), Fixed<TInt, TFrac>.Max(tl.Y, tr.Y)));
        return new Rect<TInt, TFrac>(min, max);
    }

    // Transforme par matrice affine (mat, translation)
    public Rect<TInt, TFrac> TransformAffine(Mat2x2<TInt, TFrac> mat, Vec2<TInt, TFrac> translation)
    {
        var bl = mat * BottomLeft + translation;
        var br = mat * BottomRight + translation;
        var tl = mat * TopLeft + translation;
        var tr = mat * TopRight + translation;
        var min = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Min(Fixed<TInt, TFrac>.Min(bl.X, br.X), Fixed<TInt, TFrac>.Min(tl.X, tr.X)),
            Fixed<TInt, TFrac>.Min(Fixed<TInt, TFrac>.Min(bl.Y, br.Y), Fixed<TInt, TFrac>.Min(tl.Y, tr.Y)));
        var max = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Max(Fixed<TInt, TFrac>.Max(bl.X, br.X), Fixed<TInt, TFrac>.Max(tl.X, tr.X)),
            Fixed<TInt, TFrac>.Max(Fixed<TInt, TFrac>.Max(bl.Y, br.Y), Fixed<TInt, TFrac>.Max(tl.Y, tr.Y)));
        return new Rect<TInt, TFrac>(min, max);
    }

    // Convertit un point (0..1, 0..1) dans l’espace normalisé → dans le rect
    public Vec2<TInt, TFrac> NormalizedToPoint(Vec2<TInt, TFrac> normalized)
        => Min + normalized * Size;

    // Convertit un point global → coordonnées normalisées (0..1) dans le rect
    public Vec2<TInt, TFrac> PointToNormalized(Vec2<TInt, TFrac> pt)
        => new Vec2<TInt, TFrac>(
            (pt.X - Min.X) / Width,
            (pt.Y - Min.Y) / Height);

    // Rotation 90°, 180°, 270° (sur le centre, optionnel)
    public Rect<TInt, TFrac> Rotate90()
    {
        var c = Center;
        var sz = Size;
        return Rect<TInt, TFrac>.FromCenterSize(c, new Vec2<TInt, TFrac>(sz.Y, sz.X));
    }
    public Rect<TInt, TFrac> Rotate180() => this; // AABB: rect inchangé

    public Rect<TInt, TFrac> Rotate270()
    {
        var c = Center;
        var sz = Size;
        return Rect<TInt, TFrac>.FromCenterSize(c, new Vec2<TInt, TFrac>(sz.Y, sz.X));
    }

    // Découpage en n sous-rectangles horizontaux/verticaux (bonus, usage UI/layout)
    public Rect<TInt, TFrac>[] SplitRows(int n)
    {
        var sz = Height / (Fixed<TInt, TFrac>)n;
        var arr = new Rect<TInt, TFrac>[n];
        for (int i = 0; i < n; i++)
            arr[i] = Rect<TInt, TFrac>.FromMinSize(
                new Vec2<TInt, TFrac>(Min.X, Min.Y + sz * (Fixed<TInt, TFrac>)i),
                new Vec2<TInt, TFrac>(Width, sz));
        return arr;
    }
    public Rect<TInt, TFrac>[] SplitCols(int n)
    {
        var sz = Width / (Fixed<TInt, TFrac>)n;
        var arr = new Rect<TInt, TFrac>[n];
        for (int i = 0; i < n; i++)
            arr[i] = Rect<TInt, TFrac>.FromMinSize(
                new Vec2<TInt, TFrac>(Min.X + sz * (Fixed<TInt, TFrac>)i, Min.Y),
                new Vec2<TInt, TFrac>(sz, Height));
        return arr;
    }

    // Miroir horizontal (retourne un rect symétrique horizontalement autour du centre Y)
    public Rect<TInt, TFrac> MirrorHorizontal()
    {
        var cY = Center.Y;
        var minY = cY - (Max.Y - cY);
        var maxY = cY + (cY - Min.Y);
        return new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(Min.X, minY),
            new Vec2<TInt, TFrac>(Max.X, maxY));
    }

    // Miroir vertical (symétrie autour de X)
    public Rect<TInt, TFrac> MirrorVertical()
    {
        var cX = Center.X;
        var minX = cX - (Max.X - cX);
        var maxX = cX + (cX - Min.X);
        return new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(minX, Min.Y),
            new Vec2<TInt, TFrac>(maxX, Max.Y));
    }

    // FlipX : échange Min.X et Max.X
    public Rect<TInt, TFrac> FlipX()
        => new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(Max.X, Min.Y),
            new Vec2<TInt, TFrac>(Min.X, Max.Y));

    // FlipY : échange Min.Y et Max.Y
    public Rect<TInt, TFrac> FlipY()
        => new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(Min.X, Max.Y),
            new Vec2<TInt, TFrac>(Max.X, Min.Y));


    #endregion

    /* ==========================================
     * DÉBOGAGE, TOSTRING & SÉRIALISATION
     * - ToString()
     * - DebugString()
     * - ToArray()
     * - FromArray()
     * - ToBytes()
     * - FromBytes()
     * ========================================== */
    #region --- Débogage, ToString & Sérialisation ---

    public override string ToString() => $"Rect(Min: {Min}, Max: {Max})";

    // Version debug plus lisible (pour logs ou éditeurs custom)
    public string DebugString() =>
        $"Rect[Min=({Min.X}, {Min.Y}), Max=({Max.X}, {Max.Y}), Size=({Width}, {Height})]";

    // Conversion en tableau : [min.x, min.y, max.x, max.y]
    public Fixed<TInt, TFrac>[] ToArray() =>
        new[] { Min.X, Min.Y, Max.X, Max.Y };

    public static Rect<TInt, TFrac> FromArray(Fixed<TInt, TFrac>[] arr)
    {
        if (arr == null || arr.Length != 4)
            throw new ArgumentException("Array must have length 4.");
        return new Rect<TInt, TFrac>(
            new Vec2<TInt, TFrac>(arr[0], arr[1]),
            new Vec2<TInt, TFrac>(arr[2], arr[3]));
    }

    // Sérialisation brute en bytes (ex : pour interop low-level, buffer)
    public byte[] ToBytes()
    {
        var minBytes = Min.ToBytes();
        var maxBytes = Max.ToBytes();
        var result = new byte[minBytes.Length + maxBytes.Length];
        Buffer.BlockCopy(minBytes, 0, result, 0, minBytes.Length);
        Buffer.BlockCopy(maxBytes, 0, result, minBytes.Length, maxBytes.Length);
        return result;
    }

    public static Rect<TInt, TFrac> FromBytes(byte[] bytes)
    {
        int vecSize = bytes.Length / 2;
        var min = Vec2<TInt, TFrac>.FromBytes(bytes.Take(vecSize).ToArray());
        var max = Vec2<TInt, TFrac>.FromBytes(bytes.Skip(vecSize).ToArray());
        return new Rect<TInt, TFrac>(min, max);
    }

    #endregion



}


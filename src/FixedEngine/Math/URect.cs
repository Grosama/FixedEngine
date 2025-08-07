using FixedEngine.Math;
using System;
using System.Linq;

public struct URect<TUInt, TFrac>
    where TUInt : struct
    where TFrac : struct
{
    /* ==========================================
     * CONSTRUCTEURS & INSTANCES PRÉDÉFINIES
     * - URect(min, max)
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
     * - Empty
     * - ByteSize
     * ========================================== */
    #region --- Constructeurs & Instances prédéfinies ---

    public URect(UVec2<TUInt, TFrac> min, UVec2<TUInt, TFrac> max)
    {
        Min = min;
        Max = max;
    }

    public static URect<TUInt, TFrac> FromMinMax(UVec2<TUInt, TFrac> min, UVec2<TUInt, TFrac> max)
        => new URect<TUInt, TFrac>(min, max);

    public static URect<TUInt, TFrac> FromMinSize(UVec2<TUInt, TFrac> min, UVec2<TUInt, TFrac> size)
        => new URect<TUInt, TFrac>(min, min + size);

    public static URect<TUInt, TFrac> FromCenterSize(UVec2<TUInt, TFrac> center, UVec2<TUInt, TFrac> size)
    {
        var half = size / (UFixed<TUInt, TFrac>)2;
        return new URect<TUInt, TFrac>(center - half, center + half);
    }

    public static URect<TUInt, TFrac> FromXYWH(UFixed<TUInt, TFrac> x, UFixed<TUInt, TFrac> y, UFixed<TUInt, TFrac> w, UFixed<TUInt, TFrac> h)
        => new URect<TUInt, TFrac>(new UVec2<TUInt, TFrac>(x, y), new UVec2<TUInt, TFrac>(x + w, y + h));

    public static URect<TUInt, TFrac> FromLTRB(UFixed<TUInt, TFrac> left, UFixed<TUInt, TFrac> top, UFixed<TUInt, TFrac> right, UFixed<TUInt, TFrac> bottom)
        => new URect<TUInt, TFrac>(new UVec2<TUInt, TFrac>(left, bottom), new UVec2<TUInt, TFrac>(right, top));

    public static URect<TUInt, TFrac> FromPoints(UVec2<TUInt, TFrac> pt1, UVec2<TUInt, TFrac> pt2)
        => new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Min(pt1.X, pt2.X),
                UFixed<TUInt, TFrac>.Min(pt1.Y, pt2.Y)),
            new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Max(pt1.X, pt2.X),
                UFixed<TUInt, TFrac>.Max(pt1.Y, pt2.Y))
        );

    public static URect<TUInt, TFrac> FromCorners(UVec2<TUInt, TFrac> corner1, UVec2<TUInt, TFrac> corner2)
        => FromPoints(corner1, corner2);

    public static readonly URect<TUInt, TFrac> Zero = new URect<TUInt, TFrac>(UVec2<TUInt, TFrac>.Zero, UVec2<TUInt, TFrac>.Zero);
    public static readonly URect<TUInt, TFrac> Unit = new URect<TUInt, TFrac>(UVec2<TUInt, TFrac>.Zero, UVec2<TUInt, TFrac>.One);
    public static readonly URect<TUInt, TFrac> One = new URect<TUInt, TFrac>(UVec2<TUInt, TFrac>.Zero, UVec2<TUInt, TFrac>.One);
    public static readonly URect<TUInt, TFrac> Empty = Zero;

    public static readonly int ByteSize = UVec2<TUInt, TFrac>.ByteSize * 2;

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

    public UVec2<TUInt, TFrac> Min;  // Coin inférieur gauche
    public UVec2<TUInt, TFrac> Max;  // Coin supérieur droit

    public UFixed<TUInt, TFrac> X => Min.X;
    public UFixed<TUInt, TFrac> Y => Min.Y;
    public UFixed<TUInt, TFrac> XMax => Max.X;
    public UFixed<TUInt, TFrac> YMax => Max.Y;

    public UFixed<TUInt, TFrac> Width => Max.X - Min.X;
    public UFixed<TUInt, TFrac> Height => Max.Y - Min.Y;
    public UVec2<TUInt, TFrac> Size => new UVec2<TUInt, TFrac>(Width, Height);

    public UVec2<TUInt, TFrac> Center =>
        new UVec2<TUInt, TFrac>((Min.X + Max.X) / (UFixed<TUInt, TFrac>)2, (Min.Y + Max.Y) / (UFixed<TUInt, TFrac>)2);

    public UFixed<TUInt, TFrac> Left => Min.X;
    public UFixed<TUInt, TFrac> Right => Max.X;
    public UFixed<TUInt, TFrac> Bottom => Min.Y;
    public UFixed<TUInt, TFrac> Top => Max.Y;

    public UFixed<TUInt, TFrac> Area => Width * Height;

    public UVec2<TUInt, TFrac> Diagonal => Max - Min;

    public UFixed<TUInt, TFrac> AspectRatio =>
        Height == UFixed<TUInt, TFrac>.Zero
            ? UFixed<TUInt, TFrac>.Zero
            : Width / Height;

    // IsEmpty : width ou height à zéro (jamais négatif en unsigned)
    public bool IsEmpty => Width == UFixed<TUInt, TFrac>.Zero || Height == UFixed<TUInt, TFrac>.Zero;

    // Rectangle bien formé (Min <= Max sur les deux axes)
    public bool IsValid => Min.X <= Max.X && Min.Y <= Max.Y;

    public UVec2<TUInt, TFrac> BottomLeft => Min;
    public UVec2<TUInt, TFrac> BottomRight => new UVec2<TUInt, TFrac>(Max.X, Min.Y);
    public UVec2<TUInt, TFrac> TopLeft => new UVec2<TUInt, TFrac>(Min.X, Max.Y);
    public UVec2<TUInt, TFrac> TopRight => Max;

    public static readonly URect<TUInt, TFrac> MinValue = new URect<TUInt, TFrac>(
        new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero),
        new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero));

    public static readonly URect<TUInt, TFrac> MaxValue = new URect<TUInt, TFrac>(
        new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.AllOnes, UFixed<TUInt, TFrac>.AllOnes),
        new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.AllOnes, UFixed<TUInt, TFrac>.AllOnes));

    // Indexeur d'accès aux coins (0:BottomLeft, 1:BottomRight, 2:TopRight, 3:TopLeft)
    public UVec2<TUInt, TFrac> this[int index] =>
        index switch
        {
            0 => BottomLeft,
            1 => BottomRight,
            2 => TopRight,
            3 => TopLeft,
            _ => throw new IndexOutOfRangeException()
        };

    public UFixed<TUInt, TFrac> Axis(int axis)
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
     * - Equals(URect<TUInt, TFrac>)
     * - GetHashCode()
     * - ApproxEquals ()
     * ========================================== */
    #region --- Opérateurs & Égalité ---

    public static bool operator ==(URect<TUInt, TFrac> a, URect<TUInt, TFrac> b)
        => a.Min == b.Min && a.Max == b.Max;

    public static bool operator !=(URect<TUInt, TFrac> a, URect<TUInt, TFrac> b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is URect<TUInt, TFrac> other && this == other;

    public bool Equals(URect<TUInt, TFrac> other) => this == other;

    public override int GetHashCode()
        => Min.GetHashCode() ^ (Max.GetHashCode() << 1);

    public bool ApproxEquals(URect<TUInt, TFrac> other, UFixed<TUInt, TFrac> epsilon) =>
        Min.ApproxEquals(other.Min, epsilon) &&
        Max.ApproxEquals(other.Max, epsilon);

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

    public bool Contains(UVec2<TUInt, TFrac> pt)
        => pt.X >= Min.X && pt.X <= Max.X && pt.Y >= Min.Y && pt.Y <= Max.Y;

    public bool Contains(URect<TUInt, TFrac> other)
        => Contains(other.Min) && Contains(other.Max);

    public bool Intersects(URect<TUInt, TFrac> other)
        => !(other.Min.X > Max.X || other.Max.X < Min.X
            || other.Min.Y > Max.Y || other.Max.Y < Min.Y);

    public URect<TUInt, TFrac> Encapsulate(UVec2<TUInt, TFrac> pt)
    {
        var min = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Min(Min.X, pt.X),
            UFixed<TUInt, TFrac>.Min(Min.Y, pt.Y));
        var max = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Max(Max.X, pt.X),
            UFixed<TUInt, TFrac>.Max(Max.Y, pt.Y));
        return new URect<TUInt, TFrac>(min, max);
    }

    public URect<TUInt, TFrac> Encapsulate(URect<TUInt, TFrac> other)
    {
        var min = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Min(Min.X, other.Min.X),
            UFixed<TUInt, TFrac>.Min(Min.Y, other.Min.Y));
        var max = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Max(Max.X, other.Max.X),
            UFixed<TUInt, TFrac>.Max(Max.Y, other.Max.Y));
        return new URect<TUInt, TFrac>(min, max);
    }

    public URect<TUInt, TFrac> Union(URect<TUInt, TFrac> other) => Encapsulate(other);

    public URect<TUInt, TFrac> Intersection(URect<TUInt, TFrac> other)
    {
        var min = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Max(Min.X, other.Min.X),
            UFixed<TUInt, TFrac>.Max(Min.Y, other.Min.Y));
        var max = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Min(Max.X, other.Max.X),
            UFixed<TUInt, TFrac>.Min(Max.Y, other.Max.Y));
        // Si pas de recouvrement : rect vide
        if (max.X < min.X || max.Y < min.Y)
            return URect<TUInt, TFrac>.Empty;
        return new URect<TUInt, TFrac>(min, max);
    }

    public URect<TUInt, TFrac> Expand(UFixed<TUInt, TFrac> amount)
        => new URect<TUInt, TFrac>(
            Min - new UVec2<TUInt, TFrac>(amount, amount),
            Max + new UVec2<TUInt, TFrac>(amount, amount));

    public URect<TUInt, TFrac> Contract(UFixed<TUInt, TFrac> amount)
        => Expand(amount); // No signed, so Contract == Expand (no negative value in unsigned)

    public URect<TUInt, TFrac> Clamp(URect<TUInt, TFrac> bounds)
        => new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Max(Min.X, bounds.Min.X),
                UFixed<TUInt, TFrac>.Max(Min.Y, bounds.Min.Y)),
            new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Min(Max.X, bounds.Max.X),
                UFixed<TUInt, TFrac>.Min(Max.Y, bounds.Max.Y)));

    public URect<TUInt, TFrac> Moved(UVec2<TUInt, TFrac> delta)
        => new URect<TUInt, TFrac>(Min + delta, Max + delta);

    public URect<TUInt, TFrac> Scaled(UFixed<TUInt, TFrac> factor)
    {
        var c = Center;
        var size = Size * factor / (UFixed<TUInt, TFrac>)2;
        return new URect<TUInt, TFrac>(c - size, c + size);
    }

    public URect<TUInt, TFrac> SwapXY()
        => new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(Min.Y, Min.X),
            new UVec2<TUInt, TFrac>(Max.Y, Max.X));

    public bool IsDegenerate =>
        Width == UFixed<TUInt, TFrac>.Zero || Height == UFixed<TUInt, TFrac>.Zero;

    public URect<TUInt, TFrac> Copy()
        => new URect<TUInt, TFrac>(Min, Max);

    #endregion

    /* ==========================================
     * TRANSFORMATIONS, INTERPOLATIONS & GÉOMÉTRIE AVANCÉE
     * - Lerp(a, b, t)
     * - Transform(Mat2x2)
     * - TransformAffine(Mat2x2, UVec2)
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

    // Lerp sur min et max
    public static URect<TUInt, TFrac> Lerp(URect<TUInt, TFrac> a, URect<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
        => new URect<TUInt, TFrac>(
            UVec2<TUInt, TFrac>.Lerp(a.Min, b.Min, t),
            UVec2<TUInt, TFrac>.Lerp(a.Max, b.Max, t));

    // Transforme chaque coin par la matrice (pour les cas où tu veux du "rect rotatif" même en unsigned)
    public URect<TUInt, TFrac> Transform(UMat2x2<TUInt, TFrac> mat)
    {
        var bl = mat * BottomLeft;
        var br = mat * BottomRight;
        var tl = mat * TopLeft;
        var tr = mat * TopRight;
        var min = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.X, br.X), UFixed<TUInt, TFrac>.Min(tl.X, tr.X)),
            UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.Y, br.Y), UFixed<TUInt, TFrac>.Min(tl.Y, tr.Y)));
        var max = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.X, br.X), UFixed<TUInt, TFrac>.Max(tl.X, tr.X)),
            UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.Y, br.Y), UFixed<TUInt, TFrac>.Max(tl.Y, tr.Y)));
        return new URect<TUInt, TFrac>(min, max);
    }

    // Transformation affine : matrice + translation
    public URect<TUInt, TFrac> TransformAffine(UMat2x2<TUInt, TFrac> mat, UVec2<TUInt, TFrac> translation)
    {
        var bl = mat * BottomLeft + translation;
        var br = mat * BottomRight + translation;
        var tl = mat * TopLeft + translation;
        var tr = mat * TopRight + translation;
        var min = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.X, br.X), UFixed<TUInt, TFrac>.Min(tl.X, tr.X)),
            UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.Y, br.Y), UFixed<TUInt, TFrac>.Min(tl.Y, tr.Y)));
        var max = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.X, br.X), UFixed<TUInt, TFrac>.Max(tl.X, tr.X)),
            UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.Y, br.Y), UFixed<TUInt, TFrac>.Max(tl.Y, tr.Y)));
        return new URect<TUInt, TFrac>(min, max);
    }

    // NormalizedToPoint : (0..1, 0..1) dans le rect
    public UVec2<TUInt, TFrac> NormalizedToPoint(UVec2<TUInt, TFrac> normalized)
        => Min + normalized * Size;

    // PointToNormalized : coordonnées normalisées (0..1) dans le rect
    public UVec2<TUInt, TFrac> PointToNormalized(UVec2<TUInt, TFrac> pt)
        => new UVec2<TUInt, TFrac>(
            (pt.X - Min.X) / Width,
            (pt.Y - Min.Y) / Height);

    // Rotations 90°, 180°, 270° (autour du centre)
    public URect<TUInt, TFrac> Rotate90()
    {
        var c = Center;
        var sz = Size;
        return URect<TUInt, TFrac>.FromCenterSize(c, new UVec2<TUInt, TFrac>(sz.Y, sz.X));
    }

    public URect<TUInt, TFrac> Rotate180() => this; // Un rect aligné reste le même

    public URect<TUInt, TFrac> Rotate270()
    {
        var c = Center;
        var sz = Size;
        return URect<TUInt, TFrac>.FromCenterSize(c, new UVec2<TUInt, TFrac>(sz.Y, sz.X));
    }

    // SplitRows : découpe en n sous-rectangles horizontaux
    public URect<TUInt, TFrac>[] SplitRows(int n)
    {
        var sz = Height / (UFixed<TUInt, TFrac>)n;
        var arr = new URect<TUInt, TFrac>[n];
        for (int i = 0; i < n; i++)
            arr[i] = URect<TUInt, TFrac>.FromMinSize(
                new UVec2<TUInt, TFrac>(Min.X, Min.Y + sz * (UFixed<TUInt, TFrac>)i),
                new UVec2<TUInt, TFrac>(Width, sz));
        return arr;
    }

    // SplitCols : découpe en n sous-rectangles verticaux
    public URect<TUInt, TFrac>[] SplitCols(int n)
    {
        var sz = Width / (UFixed<TUInt, TFrac>)n;
        var arr = new URect<TUInt, TFrac>[n];
        for (int i = 0; i < n; i++)
            arr[i] = URect<TUInt, TFrac>.FromMinSize(
                new UVec2<TUInt, TFrac>(Min.X + sz * (UFixed<TUInt, TFrac>)i, Min.Y),
                new UVec2<TUInt, TFrac>(sz, Height));
        return arr;
    }

    public URect<TUInt, TFrac> MirrorHorizontal()
    {
        var cY = Center.Y;
        var minY = cY - (Max.Y - cY);
        var maxY = cY + (cY - Min.Y);
        return new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(Min.X, minY),
            new UVec2<TUInt, TFrac>(Max.X, maxY));
    }

    public URect<TUInt, TFrac> MirrorVertical()
    {
        var cX = Center.X;
        var minX = cX - (Max.X - cX);
        var maxX = cX + (cX - Min.X);
        return new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(minX, Min.Y),
            new UVec2<TUInt, TFrac>(maxX, Max.Y));
    }

    public URect<TUInt, TFrac> FlipX()
        => new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(Max.X, Min.Y),
            new UVec2<TUInt, TFrac>(Min.X, Max.Y));

    public URect<TUInt, TFrac> FlipY()
        => new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(Min.X, Max.Y),
            new UVec2<TUInt, TFrac>(Max.X, Min.Y));

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

    public override string ToString() => $"URect(Min: {Min}, Max: {Max})";

    public string DebugString() =>
        $"URect[Min=({Min.X}, {Min.Y}), Max=({Max.X}, {Max.Y}), Size=({Width}, {Height})]";

    // Conversion en tableau : [min.x, min.y, max.x, max.y]
    public UFixed<TUInt, TFrac>[] ToArray() =>
        new[] { Min.X, Min.Y, Max.X, Max.Y };

    public static URect<TUInt, TFrac> FromArray(UFixed<TUInt, TFrac>[] arr)
    {
        if (arr == null || arr.Length != 4)
            throw new ArgumentException("Array must have length 4.");
        return new URect<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(arr[0], arr[1]),
            new UVec2<TUInt, TFrac>(arr[2], arr[3]));
    }

    // Sérialisation brute en bytes
    public byte[] ToBytes()
    {
        var minBytes = Min.ToBytes();
        var maxBytes = Max.ToBytes();
        var result = new byte[minBytes.Length + maxBytes.Length];
        Buffer.BlockCopy(minBytes, 0, result, 0, minBytes.Length);
        Buffer.BlockCopy(maxBytes, 0, result, minBytes.Length, maxBytes.Length);
        return result;
    }

    public static URect<TUInt, TFrac> FromBytes(byte[] bytes)
    {
        int vecSize = bytes.Length / 2;
        var min = UVec2<TUInt, TFrac>.FromBytes(bytes.Take(vecSize).ToArray());
        var max = UVec2<TUInt, TFrac>.FromBytes(bytes.Skip(vecSize).ToArray());
        return new URect<TUInt, TFrac>(min, max);
    }

    #endregion


}

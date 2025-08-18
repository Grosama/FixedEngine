using FixedEngine.Math;
using System;
using System.Runtime.CompilerServices;

public struct UTransform2D<TUInt, TFrac>
    where TUInt : struct
    where TFrac : struct
{

    // Champs
    public UVec2<TUInt, TFrac> Position;
    public UFixed<TUInt, TFrac> Rotation;      // Angle en radians (wrap unsigned)
    public UVec2<TUInt, TFrac> Scale;
    public UVec2<TUInt, TFrac> Origin;

    /* ==========================================
     * 1. CONSTRUCTEURS & INSTANCES
     * ========================================== */
    #region --- Constructeurs & Instances ---

    public UTransform2D(
        UVec2<TUInt, TFrac> position,
        UFixed<TUInt, TFrac> rotation,
        UVec2<TUInt, TFrac> scale,
        UVec2<TUInt, TFrac> origin)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Origin = origin;
    }

    public UTransform2D(UVec2<TUInt, TFrac> position, UFixed<TUInt, TFrac> rotation, UVec2<TUInt, TFrac> scale)
        : this(position, rotation, scale, UVec2<TUInt, TFrac>.Zero) { }

    public UTransform2D(UVec2<TUInt, TFrac> position, UFixed<TUInt, TFrac> rotation)
        : this(position, rotation, UVec2<TUInt, TFrac>.One, UVec2<TUInt, TFrac>.Zero) { }

    public UTransform2D(UVec2<TUInt, TFrac> position)
        : this(position, UFixed<TUInt, TFrac>.Zero, UVec2<TUInt, TFrac>.One, UVec2<TUInt, TFrac>.Zero) { }

    // Identité : pas de translation, pas de rotation, scale 1, origin 0
    public static readonly UTransform2D<TUInt, TFrac> Identity = new UTransform2D<TUInt, TFrac>(
        UVec2<TUInt, TFrac>.Zero,
        UFixed<TUInt, TFrac>.Zero,
        UVec2<TUInt, TFrac>.One,
        UVec2<TUInt, TFrac>.Zero);

    #endregion

    /* ==========================================
     * PROPRIÉTÉS BONUS
     * - Matrix
     * - InverseMatrix
     * - (TransformMatrix pour moteurs externes)
     * ========================================== */
    #region --- Propriétés Bonus ---

    /// <summary>
    /// Renvoie la matrice 2x2 de rotation et scale (ne gère pas la translation).
    /// </summary>
    public UMat2x2<TUInt, TFrac> Matrix
    {
        get
        {
            // Génération d'une matrice de rotation+scale unsigned
            var cos = UFixed<TUInt, TFrac>.Cos(Rotation);
            var sin = UFixed<TUInt, TFrac>.Sin(Rotation);
            return new UMat2x2<TUInt, TFrac>(
                (UFixed<TUInt, TFrac>)cos * Scale.X, (UFixed<TUInt, TFrac>)sin * Scale.Y,
                (UFixed<TUInt, TFrac>)sin * Scale.X, (UFixed<TUInt, TFrac>)cos * Scale.Y
            );
        }
    }

    /// <summary>
    /// Renvoie la matrice inverse (unsigned : wrap possible si scale == 0).
    /// </summary>
    public UMat2x2<TUInt, TFrac> InverseMatrix
    {
        get
        {
            // Inverse d'une matrice de scale + rotation (unsigned only)
            var invScaleX = Scale.X == UFixed<TUInt, TFrac>.Zero ? UFixed<TUInt, TFrac>.Zero : UFixed<TUInt, TFrac>.One / Scale.X;
            var invScaleY = Scale.Y == UFixed<TUInt, TFrac>.Zero ? UFixed<TUInt, TFrac>.Zero : UFixed<TUInt, TFrac>.One / Scale.Y;
            var cos = UFixed<TUInt, TFrac>.Cos(Rotation);
            var sin = UFixed<TUInt, TFrac>.Sin(Rotation);
            // Rotation "inverse" = wrap unsigned
            return new UMat2x2<TUInt, TFrac>(
                (UFixed<TUInt, TFrac>)cos * invScaleX, (UFixed<TUInt, TFrac>)sin * invScaleY,
                (UFixed<TUInt, TFrac>)sin * invScaleX, (UFixed<TUInt, TFrac>)cos * invScaleY
            );
        }
    }

    // Propriété pour interop moteurs externes (Matrix4x4, etc.)
    // Ici, on retourne la matrice 2x3 (rotation+scale, translation)
    public UFixed<TUInt, TFrac>[,] TransformMatrix
    {
        get
        {
            var mat = Matrix;
            return new UFixed<TUInt, TFrac>[2, 3]
            {
                { mat.M11, mat.M12, Position.X },
                { mat.M21, mat.M22, Position.Y }
            };
        }
    }

    #endregion

    /* ==========================================
     * 3. OPÉRATEURS & ÉGALITÉ
     * ========================================== */
    #region --- Opérateurs & Égalité ---

    public static bool operator ==(UTransform2D<TUInt, TFrac> a, UTransform2D<TUInt, TFrac> b)
        => a.Position == b.Position
        && a.Rotation == b.Rotation
        && a.Scale == b.Scale
        && a.Origin == b.Origin;

    public static bool operator !=(UTransform2D<TUInt, TFrac> a, UTransform2D<TUInt, TFrac> b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is UTransform2D<TUInt, TFrac> other && this == other;

    public bool Equals(UTransform2D<TUInt, TFrac> other)
        => this == other;

    public override int GetHashCode()
        => Position.GetHashCode()
         ^ (Rotation.GetHashCode() << 1)
         ^ (Scale.GetHashCode() << 2)
         ^ (Origin.GetHashCode() << 3);

    /// <summary>
    /// Teste l’égalité à epsilon (utilise Delta sur tous les champs, branchless unsigned).
    /// </summary>
    public bool ApproxEquals(UTransform2D<TUInt, TFrac> other, UFixed<TUInt, TFrac> epsilon)
        => Position.ApproxEquals(other.Position, epsilon)
        && UFixed<TUInt, TFrac>.Delta(Rotation, other.Rotation) <= epsilon
        && Scale.ApproxEquals(other.Scale, epsilon)
        && Origin.ApproxEquals(other.Origin, epsilon);

    #endregion

    /* ==========================================
     * 4. HELPERS & TRANSFORMATIONS
     * ========================================== */
    #region --- Helpers & Transformations ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UVec2<TUInt, TFrac> TransformPoint(UVec2<TUInt, TFrac> pt)
    {
        // Applique d'abord le pivot, puis scale/rot, puis translation (unsigned wrap si overflow)
        var local = pt - Origin;
        var mat = Matrix;
        var rotatedScaled = mat * local;
        return rotatedScaled + Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UVec2<TUInt, TFrac> InverseTransformPoint(UVec2<TUInt, TFrac> pt)
    {
        // Applique l’inverse de la translation, puis l’inverse de scale/rot, puis restore le pivot
        var matInv = InverseMatrix;
        var translated = pt - Position;
        var local = matInv * translated;
        return local + Origin;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> Translated(UVec2<TUInt, TFrac> delta)
        => new UTransform2D<TUInt, TFrac>(Position + delta, Rotation, Scale, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> Rotated(UFixed<TUInt, TFrac> delta)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation + delta, Scale, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> Scaled(UVec2<TUInt, TFrac> scaleFactor)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale * scaleFactor, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> With(UVec2<TUInt, TFrac>? position = null,
                                           UFixed<TUInt, TFrac>? rotation = null,
                                           UVec2<TUInt, TFrac>? scale = null,
                                           UVec2<TUInt, TFrac>? origin = null)
        => new UTransform2D<TUInt, TFrac>(
            position ?? Position,
            rotation ?? Rotation,
            scale ?? Scale,
            origin ?? Origin);

    #endregion

    /* ==========================================
     * HELPERS UI, TOOLS & INTEROP
     * - Right, Up
     * - ApplyInverse
     * - WorldPivot
     * - WithX, WithY, WithScaleX, WithScaleY, WithOriginX, WithOriginY
     * - AsMatrix3x2Array
     * - ApproxEquals (epsilon)
     * ========================================== */
    #region --- Helpers UI, Tools & Interop ---

    /// <summary>
    /// Vecteur “Right” de ce transform (axe X local, unsigned)
    /// </summary>
    public UVec2<TUInt, TFrac> Right => Matrix * UVec2<TUInt, TFrac>.Right;


    /// <summary>
    /// Vecteur “Up” de ce transform (axe Y local, unsigned)
    /// </summary>
    public UVec2<TUInt, TFrac> Up => Matrix * UVec2<TUInt, TFrac>.Up;

    /// <summary>
    /// Applique l’inverse du transform à un point (world -> local)
    /// </summary>
    public UVec2<TUInt, TFrac> ApplyInverse(UVec2<TUInt, TFrac> pt)
        => InverseTransformPoint(pt);

    /// <summary>
    /// Position du pivot dans le repère monde (utile pour UI)
    /// </summary>
    public UVec2<TUInt, TFrac> WorldPivot => TransformPoint(Origin);

    // Helpers de modification "axis/scale/origin"
    public UTransform2D<TUInt, TFrac> WithX(UFixed<TUInt, TFrac> x)
        => new UTransform2D<TUInt, TFrac>(Position.WithX(x), Rotation, Scale, Origin);

    public UTransform2D<TUInt, TFrac> WithY(UFixed<TUInt, TFrac> y)
        => new UTransform2D<TUInt, TFrac>(Position.WithY(y), Rotation, Scale, Origin);

    public UTransform2D<TUInt, TFrac> WithScaleX(UFixed<TUInt, TFrac> sx)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale.WithX(sx), Origin);

    public UTransform2D<TUInt, TFrac> WithScaleY(UFixed<TUInt, TFrac> sy)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale.WithY(sy), Origin);

    public UTransform2D<TUInt, TFrac> WithOriginX(UFixed<TUInt, TFrac> ox)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale, Origin.WithX(ox));

    public UTransform2D<TUInt, TFrac> WithOriginY(UFixed<TUInt, TFrac> oy)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale, Origin.WithY(oy));

    /// <summary>
    /// Pour interop (interop UI / moteurs externes)
    /// Retourne la matrice 2x3 sous forme de tableau à plat [M11, M12, M21, M22, Tx, Ty]
    /// </summary>
    public UFixed<TUInt, TFrac>[] AsMatrix3x2Array()
    {
        var mat = Matrix;
        return new UFixed<TUInt, TFrac>[]
        {
            mat.M11, mat.M12,
            mat.M21, mat.M22,
            Position.X, Position.Y
        };
    }

    // ApproxEquals (epsilon) déjà en région égalité

    #endregion

    /* ==========================================
     * HELPERS AVANCÉS & CHAIN-CODING
     * - WithPosition, WithRotation, WithScale, WithOrigin
     * - Reset, Copy
     * - ApproxEquals
     * - ToArray, FromArray, ToBytes, FromBytes
     * ========================================== */
    #region --- Helpers avancés & Chain-coding ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> WithPosition(UVec2<TUInt, TFrac> pos)
        => new UTransform2D<TUInt, TFrac>(pos, Rotation, Scale, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> WithRotation(UFixed<TUInt, TFrac> rot)
        => new UTransform2D<TUInt, TFrac>(Position, rot, Scale, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> WithScale(UVec2<TUInt, TFrac> scale)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, scale, Origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> WithOrigin(UVec2<TUInt, TFrac> origin)
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale, origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Position = UVec2<TUInt, TFrac>.Zero;
        Rotation = UFixed<TUInt, TFrac>.Zero;
        Scale = UVec2<TUInt, TFrac>.One;
        Origin = UVec2<TUInt, TFrac>.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UTransform2D<TUInt, TFrac> Copy()
        => new UTransform2D<TUInt, TFrac>(Position, Rotation, Scale, Origin);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ApproxEquals(UTransform2D<TUInt, TFrac> other)
        => UVec2<TUInt, TFrac>.ApproxEquals(Position, other.Position, UFixed<TUInt, TFrac>.Epsilon)
        && UFixed<TUInt, TFrac>.Delta(Rotation, other.Rotation) <= UFixed<TUInt, TFrac>.Epsilon
        && UVec2<TUInt, TFrac>.ApproxEquals(Scale, other.Scale, UFixed<TUInt, TFrac>.Epsilon)
        && UVec2<TUInt, TFrac>.ApproxEquals(Origin, other.Origin, UFixed<TUInt, TFrac>.Epsilon);

    // Conversion à plat : [pos.x, pos.y, rot, scale.x, scale.y, origin.x, origin.y]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UFixed<TUInt, TFrac>[] ToArray()
        => new UFixed<TUInt, TFrac>[] { Position.X, Position.Y, Rotation, Scale.X, Scale.Y, Origin.X, Origin.Y };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UTransform2D<TUInt, TFrac> FromArray(UFixed<TUInt, TFrac>[] arr)
    {
        if (arr == null || arr.Length < 7)
            throw new ArgumentException("Array trop court pour FromArray.");
        return new UTransform2D<TUInt, TFrac>(
            new UVec2<TUInt, TFrac>(arr[0], arr[1]),
            arr[2],
            new UVec2<TUInt, TFrac>(arr[3], arr[4]),
            new UVec2<TUInt, TFrac>(arr[5], arr[6]));
    }

    // Sérialisation brute octets
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToBytes()
    {
        var posBytes = Position.ToBytes();
        var rotBytes = Rotation.ToBytes();
        var scaleBytes = Scale.ToBytes();
        var originBytes = Origin.ToBytes();
        var result = new byte[posBytes.Length + rotBytes.Length + scaleBytes.Length + originBytes.Length];
        int offset = 0;
        Buffer.BlockCopy(posBytes, 0, result, offset, posBytes.Length); offset += posBytes.Length;
        Buffer.BlockCopy(rotBytes, 0, result, offset, rotBytes.Length); offset += rotBytes.Length;
        Buffer.BlockCopy(scaleBytes, 0, result, offset, scaleBytes.Length); offset += scaleBytes.Length;
        Buffer.BlockCopy(originBytes, 0, result, offset, originBytes.Length);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UTransform2D<TUInt, TFrac> FromBytes(byte[] bytes)
    {
        int fixedSize = (UFixed<TUInt, TFrac>.IntBitsConst + 7) / 8;
        int vecSize = fixedSize * 2;

        if (bytes.Length < vecSize * 3 + fixedSize * 1)
            throw new ArgumentException("Byte array too short for UTransform2D.");

        // Extraction manuelle
        var posBytes = new byte[vecSize];
        var rotBytes = new byte[fixedSize];
        var scaleBytes = new byte[vecSize];
        var originBytes = new byte[vecSize];

        int offset = 0;
        Array.Copy(bytes, offset, posBytes, 0, vecSize); offset += vecSize;
        Array.Copy(bytes, offset, rotBytes, 0, fixedSize); offset += fixedSize;
        Array.Copy(bytes, offset, scaleBytes, 0, vecSize); offset += vecSize;
        Array.Copy(bytes, offset, originBytes, 0, vecSize);

        var pos = UVec2<TUInt, TFrac>.FromBytes(posBytes);
        var rot = UFixed<TUInt, TFrac>.FromBytes(rotBytes);
        var scale = UVec2<TUInt, TFrac>.FromBytes(scaleBytes);
        var origin = UVec2<TUInt, TFrac>.FromBytes(originBytes);

        return new UTransform2D<TUInt, TFrac>(pos, rot, scale, origin);
    }

    #endregion

    /* ==========================================
     * HIERARCHIE DE TRANSFORM (parent/child)
     * - ComposeHierarchy(parent, local)
     * - ToLocal(world, parent)
     * - ComposeChain(...)
     * ========================================== */
    #region --- Hierarchie de Transform ---

    /// <summary>
    /// Compose un transform local avec un transform parent pour obtenir le world.
    /// </summary>
    public static UTransform2D<TUInt, TFrac> ComposeHierarchy(
        UTransform2D<TUInt, TFrac> parent,
        UTransform2D<TUInt, TFrac> local)
    {
        // Combine la position, la rotation, le scale (branchless, unsigned)
        var parentMatrix = parent.Matrix;
        var pos = parent.TransformPoint(local.Position);
        var rot = parent.Rotation + local.Rotation; // wrap unsigned
        var scale = parent.Scale * local.Scale;
        var origin = parent.Origin + local.Origin; // simple addition (à ajuster selon besoin)
        return new UTransform2D<TUInt, TFrac>(pos, rot, scale, origin);
    }

    /// <summary>
    /// Passe d’un transform world à un local relatif à parent.
    /// </summary>
    public static UTransform2D<TUInt, TFrac> ToLocal(
        UTransform2D<TUInt, TFrac> world,
        UTransform2D<TUInt, TFrac> parent)
    {
        var invParent = parent.InverseMatrix;
        var pos = invParent * (world.Position - parent.Position);
        var rot = world.Rotation - parent.Rotation; // wrap unsigned
        var scale = world.Scale / parent.Scale; // branchless, safe si parent.Scale ≠ 0
        var origin = world.Origin - parent.Origin;
        return new UTransform2D<TUInt, TFrac>(pos, rot, scale, origin);
    }

    /// <summary>
    /// Compose une chaîne de transforms (de gauche à droite).
    /// </summary>
    public static UTransform2D<TUInt, TFrac> ComposeChain(params UTransform2D<TUInt, TFrac>[] chain)
    {
        if (chain == null || chain.Length == 0) return Identity;
        var t = chain[0];
        for (int i = 1; i < chain.Length; i++)
            t = ComposeHierarchy(t, chain[i]);
        return t;
    }

    #endregion


    /* ==========================================
     * 5. DEBUG, TOSTRING, SÉRIALISATION
     * ========================================== */
    #region --- Debug, ToString, Sérialisation ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => $"UTransform2D(Pos: {Position}, Rot: {Rotation}, Scale: {Scale}, Origin: {Origin})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string DebugString()
        => $"UTransform2D[\n  Pos={Position.DebugString()},\n  Rot={Rotation},\n  Scale={Scale.DebugString()},\n  Origin={Origin.DebugString()}\n]";

    #endregion
}

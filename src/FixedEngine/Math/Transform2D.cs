/* ==========================================
 * TRANSFORM2D<TInt, TFrac>
 * Transform affine 2D rétro-faithful, branchless, ultra-modulaire
 * - Position (Vec2)
 * - Rotation (Fixed, en radians)
 * - Scale (Vec2)
 * - Origin/Pivot (Vec2)
 * ========================================== */

using FixedEngine.Core;
using FixedEngine.Math;
using System;
using System.Linq;

public struct Transform2D<TInt, TFrac>
    // : IEquatable<Transform2D<TInt, TFrac>> // (optionnel)
    where TInt : struct
    where TFrac : struct
{

    public Vec2<TInt, TFrac> Position;
    public Fixed<TInt, TFrac> Rotation;      // Angle en radians (branchless, plus efficace pour rot)
    public Vec2<TInt, TFrac> Scale;
    public Vec2<TInt, TFrac> Origin;         // Pivot local (pour UI, rotations autour d'un point, etc.)

    /* ==========================================
     * 1. CONSTRUCTEURS & INSTANCES
     * ========================================== */
    #region --- Constructeurs & Instances ---

    // Constructeur principal
    public Transform2D(
        Vec2<TInt, TFrac> position,
        Fixed<TInt, TFrac> rotation,
        Vec2<TInt, TFrac> scale,
        Vec2<TInt, TFrac> origin)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Origin = origin;
    }

    // Instanciations rapides
    public static readonly Transform2D<TInt, TFrac> Identity = new Transform2D<TInt, TFrac>(
        Vec2<TInt, TFrac>.Zero,
        Fixed<TInt, TFrac>.Zero,
        Vec2<TInt, TFrac>.One,
        Vec2<TInt, TFrac>.Zero);

    // Constructeur sans origin (pivot = zero)
    public Transform2D(Vec2<TInt, TFrac> position, Fixed<TInt, TFrac> rotation, Vec2<TInt, TFrac> scale)
        : this(position, rotation, scale, Vec2<TInt, TFrac>.Zero)
    { }

    // Constructeur simple (position seulement)
    public Transform2D(Vec2<TInt, TFrac> position)
        : this(position, Fixed<TInt, TFrac>.Zero, Vec2<TInt, TFrac>.One, Vec2<TInt, TFrac>.Zero)
    { }

    #endregion

    /* ==========================================
     * PROPRIÉTÉS BONUS
     * - Matrix
     * - InverseMatrix
     * - (TransformMatrix pour moteurs externes)
     * ========================================== */
    #region --- Propriétés ---

    public bool IsIdentity =>
        Position == Vec2<TInt, TFrac>.Zero &&
        Rotation == Fixed<TInt, TFrac>.Zero &&
        Scale == Vec2<TInt, TFrac>.One &&
        Origin == Vec2<TInt, TFrac>.Zero;

    public static readonly int ByteSize =
    Vec2<TInt, TFrac>.ByteSize        // Position
    + Fixed<TInt, TFrac>.ByteSize     // Rotation
    + Vec2<TInt, TFrac>.ByteSize      // Scale
    + Vec2<TInt, TFrac>.ByteSize;     // Origin

    public Mat2x2<TInt, TFrac> Matrix
    {
        get
        {
            var cos = Fixed<TInt, TFrac>.Cos(Rotation);
            var sin = Fixed<TInt, TFrac>.Sin(Rotation);
                return new Mat2x2<TInt, TFrac>(
                cos * Scale.X, -sin * Scale.Y,
                sin * Scale.X, cos * Scale.Y
            );
        }
    }

    // Matrice inverse (utile pour world-to-local, etc.)
    public Mat2x2<TInt, TFrac> InverseMatrix => Matrix.Inverse();

    // Matrice 3x2 complète (scale, rot, translation)
    // Pour usage avec moteurs, buffers, shaders, etc.
    public float[,] TransformMatrix
    {
        get
        {
            var m = Matrix;
            // Translation = Position + pivot local transformé
            var tx = Position.X.Raw + Origin.X.Raw;
            var ty = Position.Y.Raw + Origin.Y.Raw;
            return new float[,]
            {
            { m.M11.ToFloat(), m.M12.ToFloat(), tx },
            { m.M21.ToFloat(), m.M22.ToFloat(), ty }
            };
        }
    }


    #endregion

    /* ==========================================
     * 3. OPÉRATEURS & ÉGALITÉ
     * ========================================== */
    #region --- Opérateurs & Égalité ---

    public static bool operator ==(Transform2D<TInt, TFrac> a, Transform2D<TInt, TFrac> b)
        => a.Position == b.Position && a.Rotation == b.Rotation && a.Scale == b.Scale && a.Origin == b.Origin;

    public static bool operator !=(Transform2D<TInt, TFrac> a, Transform2D<TInt, TFrac> b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is Transform2D<TInt, TFrac> other && this == other;

    public override int GetHashCode()
        => Position.GetHashCode() ^ (Rotation.GetHashCode() << 1) ^ (Scale.GetHashCode() << 2) ^ (Origin.GetHashCode() << 3);

    #endregion

    /* ==========================================
     * 4. HELPERS & TRANSFORMATIONS
     * ========================================== */
    #region --- Helpers & Transformations ---

    // Appliquer à un point
    public Vec2<TInt, TFrac> Apply(Vec2<TInt, TFrac> point)
    {
        // Décale par -Origin, scale, rotation, puis recentre +Position
        var p = point - Origin;
        p = new Vec2<TInt, TFrac>(p.X * Scale.X, p.Y * Scale.Y);
        p = p.Rotate(Rotation); // Nécessite Vec2.Rotate(angle) (déjà dans ton socle)
        return p + Position;
    }

    // Appliquer à un tableau de points
    public void ApplyInPlace(Vec2<TInt, TFrac>[] points)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = Apply(points[i]);
    }

    // Appliquer à un Rect (AABB conservatif)
    public Rect<TInt, TFrac> Apply(Rect<TInt, TFrac> rect)
    {
        var bl = Apply(rect.BottomLeft);
        var br = Apply(rect.BottomRight);
        var tl = Apply(rect.TopLeft);
        var tr = Apply(rect.TopRight);
        return Rect<TInt, TFrac>.FromPoints(bl, br).Encapsulate(tl).Encapsulate(tr);
    }

    // Compose (chaîne deux Transforms)
    public Transform2D<TInt, TFrac> Compose(Transform2D<TInt, TFrac> other)
    {
        // Compose dans l'ordre this -> other (autrement dit, "après" other)
        var pos = Apply(other.Position);
        var rot = Rotation + other.Rotation;
        var scale = new Vec2<TInt, TFrac>(Scale.X * other.Scale.X, Scale.Y * other.Scale.Y);
        var origin = Origin + other.Origin; // (optionnel, à ajuster selon ton usage)
        return new Transform2D<TInt, TFrac>(pos, rot, scale, origin);
    }

    // Inverse du transform
    public Transform2D<TInt, TFrac> Inverse()
    {
        var invScale = new Vec2<TInt, TFrac>(
            Fixed<TInt, TFrac>.One / Scale.X,
            Fixed<TInt, TFrac>.One / Scale.Y);
        var invRot = -Rotation;
        var invOrigin = -Origin;
        var invPos = (-Position).Rotate(invRot);
        return new Transform2D<TInt, TFrac>(invPos, invRot, invScale, invOrigin);
    }

    // Lerp linéaire entre deux transforms
    public static Transform2D<TInt, TFrac> Lerp(Transform2D<TInt, TFrac> a, Transform2D<TInt, TFrac> b, UFixed<TInt, TFrac> t)
        => new Transform2D<TInt, TFrac>(
            Vec2<TInt, TFrac>.Lerp(a.Position, b.Position, t),
            FixedMath.Lerp(a.Rotation, b.Rotation, t),
            Vec2<TInt, TFrac>.Lerp(a.Scale, b.Scale, t),
            Vec2<TInt, TFrac>.Lerp(a.Origin, b.Origin, t));

    #endregion

    /* ==========================================
     * HELPERS UI, TOOLS & INTEROP
     * - Right, Up
     * - Left, Down
     * - ApplyInverse
     * - WorldPivot
     * - WithX, WithY, WithScaleX, WithScaleY, WithOriginX, WithOriginY
     * - AsMatrix3x2Array
     * - ApproxEquals (epsilon)
     * ========================================== */
    #region --- HELPERS UI, TOOLS & INTEROP ---
    // Axe local "droite" (X local)
    public Vec2<TInt, TFrac> Right => Matrix * Vec2<TInt, TFrac>.Right;

    // Axe local "haut" (Y local)
    public Vec2<TInt, TFrac> Up => Matrix * Vec2<TInt, TFrac>.Up;


    public Vec2<TInt, TFrac> Left => Matrix * Vec2<TInt, TFrac>.Left;

    // Axe local "haut" (Y local)
    public Vec2<TInt, TFrac> Down => Matrix * Vec2<TInt, TFrac>.Down;

    // Transforme un point monde en local (inverse du Apply)
    public Vec2<TInt, TFrac> ApplyInverse(Vec2<TInt, TFrac> point)
    {
        var p = point - Position;
        p = p.Rotate(-Rotation);
        p = new Vec2<TInt, TFrac>(p.X / Scale.X, p.Y / Scale.Y);
        return p + Origin;
    }

    // Position du pivot (origin) en coordonnées monde
    public Vec2<TInt, TFrac> WorldPivot => Apply(Origin);

    // Setters immuables pour chaque champ "fin"
    public Transform2D<TInt, TFrac> WithX(Fixed<TInt, TFrac> x) => new Transform2D<TInt, TFrac>(Position.WithX(x), Rotation, Scale, Origin);
    public Transform2D<TInt, TFrac> WithY(Fixed<TInt, TFrac> y) => new Transform2D<TInt, TFrac>(Position.WithY(y), Rotation, Scale, Origin);
    public Transform2D<TInt, TFrac> WithScaleX(Fixed<TInt, TFrac> x) => new Transform2D<TInt, TFrac>(Position, Rotation, Scale.WithX(x), Origin);
    public Transform2D<TInt, TFrac> WithScaleY(Fixed<TInt, TFrac> y) => new Transform2D<TInt, TFrac>(Position, Rotation, Scale.WithY(y), Origin);
    public Transform2D<TInt, TFrac> WithOriginX(Fixed<TInt, TFrac> x) => new Transform2D<TInt, TFrac>(Position, Rotation, Scale, Origin.WithX(x));
    public Transform2D<TInt, TFrac> WithOriginY(Fixed<TInt, TFrac> y) => new Transform2D<TInt, TFrac>(Position, Rotation, Scale, Origin.WithY(y));

    // Export sous forme de matrice 3x2 pour moteurs externes (Unity, MonoGame, etc.)
    public float[] AsMatrix3x2Array()
    {
        var m = Matrix;
        return new[]
        {
        m.M11.ToFloat(), m.M12.ToFloat(),
        m.M21.ToFloat(), m.M22.ToFloat(),
        Position.X.ToFloat(), Position.Y.ToFloat()
        };
    }

    // Comparaison "approx" avec tolérance epsilon personnalisable
    public static bool ApproxEquals(Transform2D<TInt, TFrac> a, Transform2D<TInt, TFrac> b, Fixed<TInt, TFrac> epsilon)
        => Vec2<TInt, TFrac>.ApproxEquals(a.Position, b.Position, epsilon)
        && Fixed<TInt, TFrac>.Abs(a.Rotation - b.Rotation) <= epsilon
        && Vec2<TInt, TFrac>.ApproxEquals(a.Scale, b.Scale, epsilon)
        && Vec2<TInt, TFrac>.ApproxEquals(a.Origin, b.Origin, epsilon);

    #endregion

    /* ==========================================
     * HELPERS AVANCÉS & CHAIN-CODING
     * - WithPosition, WithRotation, WithScale, WithOrigin
     * - Reset, Copy
     * - ApproxEquals
     * - ToArray, FromArray, ToBytes, FromBytes
     * ========================================== */
    #region --- Helpers avancés & chain-coding ---

    public Transform2D<TInt, TFrac> WithPosition(Vec2<TInt, TFrac> pos)
        => new Transform2D<TInt, TFrac>(pos, Rotation, Scale, Origin);

    public Transform2D<TInt, TFrac> WithRotation(Fixed<TInt, TFrac> rot)
        => new Transform2D<TInt, TFrac>(Position, rot, Scale, Origin);

    public Transform2D<TInt, TFrac> WithScale(Vec2<TInt, TFrac> scale)
        => new Transform2D<TInt, TFrac>(Position, Rotation, scale, Origin);

    public Transform2D<TInt, TFrac> WithOrigin(Vec2<TInt, TFrac> origin)
        => new Transform2D<TInt, TFrac>(Position, Rotation, Scale, origin);

    // Reset à l’identité
    public Transform2D<TInt, TFrac> Reset()
        => Identity;

    // Copy (alias)
    public Transform2D<TInt, TFrac> Copy()
        => new Transform2D<TInt, TFrac>(Position, Rotation, Scale, Origin);


    public bool ApproxEquals(Transform2D<TInt, TFrac> other)
        => Vec2<TInt, TFrac>.ApproxEquals(Position, other.Position, Fixed<TInt, TFrac>.Epsilon)
        && Fixed<TInt, TFrac>.Abs(Rotation - other.Rotation) <= Fixed<TInt, TFrac>.Epsilon
        && Vec2<TInt, TFrac>.ApproxEquals(Scale, other.Scale, Fixed<TInt, TFrac>.Epsilon)
        && Vec2<TInt, TFrac>.ApproxEquals(Origin, other.Origin, Fixed<TInt, TFrac>.Epsilon);


    // Conversion en tableau (pour debug, export, tests)
    public Fixed<TInt, TFrac>[] ToArray() => new[]
    {
    Position.X, Position.Y,
    Rotation,
    Scale.X, Scale.Y,
    Origin.X, Origin.Y
};

    public static Transform2D<TInt, TFrac> FromArray(Fixed<TInt, TFrac>[] arr)
    {
        if (arr == null || arr.Length != 7)
            throw new ArgumentException("Array must have length 7.");
        return new Transform2D<TInt, TFrac>(
            new Vec2<TInt, TFrac>(arr[0], arr[1]),
            arr[2],
            new Vec2<TInt, TFrac>(arr[3], arr[4]),
            new Vec2<TInt, TFrac>(arr[5], arr[6]));
    }

    // Conversion en bytes (ultra-rapide pour save/load, net, etc.)
    public byte[] ToBytes()
    {
        var posBytes = Position.ToBytes();
        var rotBytes = Rotation.ToBytes();
        var scaleBytes = Scale.ToBytes();
        var originBytes = Origin.ToBytes();
        var result = new byte[posBytes.Length + rotBytes.Length + scaleBytes.Length + originBytes.Length];
        Buffer.BlockCopy(posBytes, 0, result, 0, posBytes.Length);
        Buffer.BlockCopy(rotBytes, 0, result, posBytes.Length, rotBytes.Length);
        Buffer.BlockCopy(scaleBytes, 0, result, posBytes.Length + rotBytes.Length, scaleBytes.Length);
        Buffer.BlockCopy(originBytes, 0, result, posBytes.Length + rotBytes.Length + scaleBytes.Length, originBytes.Length);
        return result;
    }

    public static Transform2D<TInt, TFrac> FromBytes(byte[] bytes)
    {
        int offset = 0;
        var pos = Vec2<TInt, TFrac>.FromBytes(bytes.Skip(offset).Take(Vec2<TInt, TFrac>.ByteSize).ToArray());
        offset += Vec2<TInt, TFrac>.ByteSize;

        var rot = Fixed<TInt, TFrac>.FromBytes(bytes.Skip(offset).Take(Fixed<TInt, TFrac>.ByteSize).ToArray());
        offset += Fixed<TInt, TFrac>.ByteSize;

        var scale = Vec2<TInt, TFrac>.FromBytes(bytes.Skip(offset).Take(Vec2<TInt, TFrac>.ByteSize).ToArray());
        offset += Vec2<TInt, TFrac>.ByteSize;

        var origin = Vec2<TInt, TFrac>.FromBytes(bytes.Skip(offset).Take(Vec2<TInt, TFrac>.ByteSize).ToArray());
        offset += Vec2<TInt, TFrac>.ByteSize;

        return new Transform2D<TInt, TFrac>(pos, rot, scale, origin);
    }
    #endregion

    /* ==========================================
     * HIERARCHIE DE TRANSFORM (parent/child)
     * - ComposeHierarchy(parent, local)
     * - ToLocal(world, parent)
     * - ComposeChain(...)
     * ========================================== */
    #region --- Helpers avancés & chain-coding ---
    public static Transform2D<TInt, TFrac> ComposeHierarchy(
    Transform2D<TInt, TFrac> parent,
    Transform2D<TInt, TFrac> local)
    {
        // Compose le parent et le local : parent * local
        var pos = parent.Apply(local.Position);
        var rot = parent.Rotation + local.Rotation;
        var scale = new Vec2<TInt, TFrac>(parent.Scale.X * local.Scale.X, parent.Scale.Y * local.Scale.Y);
        var origin = local.Origin; // à adapter si tu veux le pivot monde/parent
        return new Transform2D<TInt, TFrac>(pos, rot, scale, origin);
    }

    public static Transform2D<TInt, TFrac> ToLocal(
    Transform2D<TInt, TFrac> world,
    Transform2D<TInt, TFrac> parent)
    {
        // Applique l’inverse du parent au world : local = parent^-1 * world
        var inv = parent.Inverse();
        var pos = inv.Apply(world.Position);
        var rot = world.Rotation - parent.Rotation;
        var scale = new Vec2<TInt, TFrac>(world.Scale.X / parent.Scale.X, world.Scale.Y / parent.Scale.Y);
        var origin = world.Origin; // idem, à adapter si besoin
        return new Transform2D<TInt, TFrac>(pos, rot, scale, origin);
    }

    public static Transform2D<TInt, TFrac> ComposeChain(params Transform2D<TInt, TFrac>[] transforms)
    {
        var t = transforms[0];
        for (int i = 1; i < transforms.Length; i++)
            t = ComposeHierarchy(t, transforms[i]);
        return t;
    }
    #endregion

    /* ==========================================
     * 5. DEBUG, TOSTRING, SÉRIALISATION
     * ========================================== */
    #region --- Debug, ToString, Sérialisation ---

    public override string ToString() =>
        $"Transform2D(Pos: {Position}, Rot: {Rotation}, Scale: {Scale}, Origin: {Origin})";

    // Ajoute ToArray, FromArray, ToBytes, FromBytes, etc. selon besoin

    #endregion


}

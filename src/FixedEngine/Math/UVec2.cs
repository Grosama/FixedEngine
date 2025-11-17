using FixedEngine.Core;
using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    /// <summary>
    /// Vecteur 2D générique pour UFixed<TUInt, TFrac>
    /// </summary>
    public struct UVec2<TUInt, TFrac>
        where TUInt : struct
        where TFrac : struct
    {

        public  UFixed<TUInt, TFrac> X;
        public  UFixed<TUInt, TFrac> Y;

        /* ==========================================
         * CONSTRUCTEURS & INSTANCES
         * - UVec2(x, y)
         * - FromRaw(rawX, rawY)
         * - FromInt(intX, intY)
         * - FromFloat(floatX, floatY)
         * - MinValue / MaxValue
         * - ByteSize
         * ========================================== */
        #region --- CONSTRUCTEURS & INSTANCES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2(UFixed<TUInt, TFrac> x, UFixed<TUInt, TFrac> y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromRaw(uint rawX, uint rawY)
        {
            return new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.FromRaw(rawX),
                UFixed<TUInt, TFrac>.FromRaw(rawY)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromInt(int intX, int intY)
        {
            return new UVec2<TUInt, TFrac>(
                new UFixed<TUInt, TFrac>((uint)intX),
                new UFixed<TUInt, TFrac>((uint)intY)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromFloat(float floatX, float floatY)
        {
            return new UVec2<TUInt, TFrac>(
                new UFixed<TUInt, TFrac>(floatX),
                new UFixed<TUInt, TFrac>(floatY)
            );
        }

        public static readonly UVec2<TUInt, TFrac> MinValue = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Zero,
            UFixed<TUInt, TFrac>.Zero
        );

        public static readonly UVec2<TUInt, TFrac> MaxValue = new UVec2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.AllOnes,
            UFixed<TUInt, TFrac>.AllOnes
        );

        /// <summary>
        /// Nombre d’octets utilisés par ce vecteur (pour du packing).
        /// </summary>
        public static int ByteSize => 2 * ((UFixed<TUInt, TFrac>.IntBitsConst + 7) / 8);

        #endregion

        /* ===================
         * CONSTANTES
         * Zero
         * One
         * Up
         * Right
         * =================== */
        #region --- CONSTANTES ---

        public static readonly UVec2<TUInt, TFrac> Zero = new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero);
        public static readonly UVec2<TUInt, TFrac> One = new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.One, UFixed<TUInt, TFrac>.One);

        // Directions cardinales (unsigned : pas de -1, donc conventions “Up = (0,1)” etc)
        public static readonly UVec2<TUInt, TFrac> Up = new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.One);
        public static readonly UVec2<TUInt, TFrac> Right = new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.One, UFixed<TUInt, TFrac>.Zero);

        #endregion

        // =====================
        // == ACCÈS/DECONSTRUCTEUR  ==
        // =====================
        #region --- ACCES / DECONSTRUCTEUR ---

        /// <summary>
        /// Accès par index : [0] → X, [1] → Y. Aucun bound-check signed.
        /// </summary>
        public UFixed<TUInt, TFrac> this[int index]
        {
            get
            {
                // Pas de bounds check, laisse throw IndexOutOfRange si abus
                return index == 0 ? X
                     : index == 1 ? Y
                     : throw new IndexOutOfRangeException("Index must be 0 (X) or 1 (Y) for UVec2.");
            }
        }

        /// <summary>
        /// Déconstruction (syntaxe : `var (x, y) = uvec;`)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out UFixed<TUInt, TFrac> x, out UFixed<TUInt, TFrac> y)
        {
            x = X;
            y = Y;
        }

        #endregion


        /* ===================
         * OPERATEURS VECTORIELS
         * +, -, *, /
         * Multiplication / Division scalaire
         * ==, !=
         * =================== */
        #region --- OPERATEURS VECTORIELS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator +(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator -(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator *(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(a.X * b.X, a.Y * b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator /(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(a.X / b.X, a.Y / b.Y);

        // --- Multiplication / Division par scalaire (UFixed)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator *(UVec2<TUInt, TFrac> v, UFixed<TUInt, TFrac> k)
            => new UVec2<TUInt, TFrac>(v.X * k, v.Y * k);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator *(UFixed<TUInt, TFrac> k, UVec2<TUInt, TFrac> v)
            => new UVec2<TUInt, TFrac>(k * v.X, k * v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> operator /(UVec2<TUInt, TFrac> v, UFixed<TUInt, TFrac> k)
            => new UVec2<TUInt, TFrac>(v.X / k, v.Y / k);

        // --- Comparaisons vectorielles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => a.X == b.X && a.Y == b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => a.X != b.X || a.Y != b.Y;

        #endregion

        /* ===================
         * COMPARAISONS (égalité stricte)
         * Equals
         * GetHashCode
         * =================== */
        #region --- COMPARAISONS (égalité stricte) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UVec2<TUInt, TFrac> other)
            => X == other.X && Y == other.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is UVec2<TUInt, TFrac> v && Equals(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => X.GetHashCode() ^ (Y.GetHashCode() << 1);

        #endregion

        /* ===================
         * PROPRIÉTÉS VECTORIELLES
         * SqrMagnitude
         * Magnitude
         * Normalized
         * IsNormalized
         * IsZero
         * =================== */
        #region --- PROPRIÉTÉS VECTORIELLES (unsigned) ---

        /// <summary>
        /// Somme des carrés des composantes (X² + Y²).
        /// </summary>
        public UFixed<TUInt, TFrac> SqrMagnitude
            => X * X + Y * Y;

        /// <summary>
        /// Norme euclidienne (longueur du vecteur).
        /// </summary>
        public UFixed<TUInt, TFrac> Magnitude
            => FixedMath.Sqrt(SqrMagnitude);

        /// <summary>
        /// Vecteur normalisé (de norme 1). Si le vecteur est nul, retourne (0,0).
        /// </summary>
        public UVec2<TUInt, TFrac> Normalized
        {
            get
            {
                var mag = Magnitude;
                if (mag == UFixed<TUInt, TFrac>.Zero)
                    return Zero;
                return this / mag;
            }
        }

        /// <summary>
        /// True si la norme est 1 à epsilon près (utile pour vérification unitaires).
        /// </summary>
        public bool IsNormalized
             => SqrMagnitude == UFixed<TUInt, TFrac>.One;

        /// <summary>
        /// True si le vecteur est exactement nul.
        /// </summary>
        public bool IsZero
            => X == UFixed<TUInt, TFrac>.Zero && Y == UFixed<TUInt, TFrac>.Zero;

        #endregion

        /* ===================
         * MÉTHODES VECTORIELLES
         * Dot
         * Distance
         * Lerp
         * Min
         * Max
         * Clamp
         * =================== */
        #region --- MÉTHODES VECTORIELLES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Dot(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => a.X * b.X + a.Y * b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Distance(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => (a - b).Magnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> Lerp(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
            => a + (b - a) * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> Min(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Min(a.X, b.X),
                UFixed<TUInt, TFrac>.Min(a.Y, b.Y)
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> Max(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Max(a.X, b.X),
                UFixed<TUInt, TFrac>.Max(a.Y, b.Y)
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> Clamp(UVec2<TUInt, TFrac> v, UVec2<TUInt, TFrac> min, UVec2<TUInt, TFrac> max)
            => new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Clamp(v.X, min.X, max.X),
                UFixed<TUInt, TFrac>.Clamp(v.Y, min.Y, max.Y)
            );

        #endregion

        /* ===================
         * MÉTHODES VECTORIELLES AVANCÉES
         * Cross
         * AngleTo
         * PerpendicularRight, PerpendicularLeft
         * Rotate
         * FromAngle
         * ToAngle
         * ProjectOnto
         * ClampMagnitude
         * MoveTowards
         * ApproxEquals
         * Angle
         * AngleDeg
         * Scale
         * With
         * =================== */
        #region --- MÉTHODES VECTORIELLES AVANCÉES ---

        // ⚠️ Certaines fonctions ont un sens limité ou nul en unsigned (pas de négatif)
        // N’implémente que ce qui reste cohérent ou utile

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Cross(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
        {
            // Le "signed area" n’a pas de sens ici, mais on garde la formule pour compat :
            // (Attention, wrap unsigned : l’utilisateur doit savoir ce qu’il fait)
            return a.X * b.Y - a.Y * b.X;
        }

        // Les notions d’angles et de rotation sur unsigned sont à manier avec précaution,
        // Ici on propose juste l’API, mais toute opération qui devrait produire un négatif va wrap.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> AngleTo(UVec2<TUInt, TFrac> other) =>

            // Atan2 unsigned : c’est au caller d’interpréter le wrap
            new UFixed<TUInt, TFrac>(FixedMath.Atan2(this.Y, this.X) - FixedMath.Atan2(other.Y, other.X));


        // Perpendicular : retourne un "vecteur tourné de 90°", wrap unsigned
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> PerpendicularRight()
            => new UVec2<TUInt, TFrac>(this.Y, UFixed<TUInt, TFrac>.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> PerpendicularLeft()
            => new UVec2<TUInt, TFrac>(UFixed<TUInt, TFrac>.Zero, this.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> Rotate(UFixed<TUInt, TFrac> angle)
        {
            // On suppose que UFixedMath.Cos/Sin existent, et que l’angle est en fixed
            var cos = UFixed<TUInt, TFrac>.Cos(angle);
            var sin = UFixed<TUInt, TFrac>.Sin(angle);
            // Produit vectoriel "classique", wrap unsigned :
            return new UVec2<TUInt, TFrac>(
                X * (UFixed<TUInt, TFrac>)cos - Y * (UFixed<TUInt, TFrac>)sin,
                X * (UFixed<TUInt, TFrac>)sin + Y * (UFixed<TUInt, TFrac>)cos
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromAngle(UFixed<TUInt, TFrac> angle)
        {
            var cos = UFixed<TUInt, TFrac>.Cos(angle);
            var sin = UFixed<TUInt, TFrac>.Sin(angle);
            return new UVec2<TUInt, TFrac>((UFixed<TUInt, TFrac>)cos, (UFixed<TUInt, TFrac>)sin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> ToAngle()
            => new UFixed<TUInt, TFrac>(FixedMath.Atan2(Y, X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> ProjectOnto(UVec2<TUInt, TFrac> target)
        {
            var dot = Dot(this, target);
            var mag2 = target.SqrMagnitude;
            if (mag2 == UFixed<TUInt, TFrac>.Zero)
                return Zero;
            var factor = dot / mag2;
            return target * factor;
        }

        public UVec2<TUInt, TFrac> ClampMagnitude(UFixed<TUInt, TFrac> maxLength)
        {
            var (len, dir) = LengthAndDir(this);
            return len > maxLength ? dir * maxLength : this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> MoveTowards(
            UVec2<TUInt, TFrac> current,
            UVec2<TUInt, TFrac> target,
            UFixed<TUInt, TFrac> maxDelta)
        {
            var delta = target - current;
            var (dist, dir) = LengthAndDir(delta);
            if (dist <= maxDelta || dist == UFixed<TUInt, TFrac>.Zero)
                return target;
            return current + dir * maxDelta;
        }

        public bool ApproxEquals(UVec2<TUInt, TFrac> other, UFixed<TUInt, TFrac> epsilon) =>
            UFixed<TUInt, TFrac>.Delta(X, other.X) <= epsilon &&
            UFixed<TUInt, TFrac>.Delta(Y, other.Y) <= epsilon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ApproxEquals(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b, UFixed<TUInt, TFrac> epsilon)
            => (a - b).SqrMagnitude <= epsilon * epsilon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Angle(UVec2<TUInt, TFrac> from, UVec2<TUInt, TFrac> to)
            => new UFixed<TUInt, TFrac>(FixedMath.Acos(Dot(from.Normalized, to.Normalized)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> AngleDeg(UVec2<TUInt, TFrac> from, UVec2<TUInt, TFrac> to)
        {
            var rad = FixedMath.Acos(Dot(from.Normalized, to.Normalized));
            var radToDegQ = FixedMath.RadToDegQ<TFrac>();
            return new UFixed<TUInt, TFrac>((rad * radToDegQ) >> UFixed<TUInt, TFrac>.FracBitsConst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> Scale(UVec2<TUInt, TFrac> a, UVec2<TUInt, TFrac> b)
            => new UVec2<TUInt, TFrac>(a.X * b.X, a.Y * b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> With(UFixed<TUInt, TFrac> x, UFixed<TUInt, TFrac> y)
            => new UVec2<TUInt, TFrac>(x, y);

        #endregion

        /* ===================
         * HELPERS & UTILS VECTORIELS
         * Swap
         * SqrDistance
         * ManhattanDistance
         * ChebyshevDistance 
         * ToArray
         * FromArray
         * =================== */
        #region --- HELPERS & UTILS VECTORIELS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> Swap()
            => new UVec2<TUInt, TFrac>(Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> SqrDistance(UVec2<TUInt, TFrac> other)
        {
            var dx = (X > other.X) ? X - other.X : other.X - X;
            var dy = (Y > other.Y) ? Y - other.Y : other.Y - Y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> ManhattanDistance(UVec2<TUInt, TFrac> other)
        {
            var dx = (X > other.X) ? X - other.X : other.X - X;
            var dy = (Y > other.Y) ? Y - other.Y : other.Y - Y;
            return dx + dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> ChebyshevDistance(UVec2<TUInt, TFrac> other)
        {
            var dx = (X > other.X) ? X - other.X : other.X - X;
            var dy = (Y > other.Y) ? Y - other.Y : other.Y - Y;
            return UFixed<TUInt, TFrac>.Max(dx, dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac>[] ToArray()
            => new UFixed<TUInt, TFrac>[] { X, Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromArray(UFixed<TUInt, TFrac>[] arr)
        {
            if (arr == null || arr.Length < 2)
                throw new ArgumentException("Le tableau doit contenir au moins 2 éléments.");
            return new UVec2<TUInt, TFrac>(arr[0], arr[1]);
        }

        #endregion

        /* ==========================================
         * AUTRES HELPERS
         * - Copy()
         * - SwapXY()
         * - WithX(x), WithY(y)
         * ========================================== */
        #region --- AUTRES HELPERS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> Copy() => new UVec2<TUInt, TFrac>(X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> SwapXY() => new UVec2<TUInt, TFrac>(Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> WithX(UFixed<TUInt, TFrac> x) => new UVec2<TUInt, TFrac>(x, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UVec2<TUInt, TFrac> WithY(UFixed<TUInt, TFrac> y) => new UVec2<TUInt, TFrac>(X, y);

        #endregion

        /* ===================
         * FONCTIONS VECTORIELLES MUTATIF
         * Set
         * Normalize
         * ClampMagnitudeMut
         * PerpendicularRightMut
         * PerpendicularLeftMut
         * =================== */
        #region --- FONCTIONS VECTORIELLES MUTATIF ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(UFixed<TUInt, TFrac> x, UFixed<TUInt, TFrac> y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var mag = Magnitude;
            if (mag == UFixed<TUInt, TFrac>.Zero)
                return;
            X /= mag;
            Y /= mag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClampMagnitudeMut(UFixed<TUInt, TFrac> maxLength)
        {
            var mag = Magnitude;
            if (mag > maxLength)
            {
                var scale = maxLength / mag;
                X *= scale;
                Y *= scale;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PerpendicularRightMut()
        {
            var oldX = X;
            X = Y;
            Y = UFixed<TUInt, TFrac>.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PerpendicularLeftMut()
        {
            var oldX = X;
            X = UFixed<TUInt, TFrac>.Zero;
            Y = oldX;
        }
        #endregion

        /* ===================
         * ACCES OCTETS
         * ToBytes
         * FromBytes
         * =================== */
        #region --- ACCES OCTETS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            var xBytes = X.ToBytes();
            var yBytes = Y.ToBytes();
            byte[] result = new byte[xBytes.Length + yBytes.Length];
            Buffer.BlockCopy(xBytes, 0, result, 0, xBytes.Length);
            Buffer.BlockCopy(yBytes, 0, result, xBytes.Length, yBytes.Length);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UVec2<TUInt, TFrac> FromBytes(byte[] bytes)
        {
            int size = (UFixed<TUInt, TFrac>.IntBitsConst + 7) / 8;
            if (bytes.Length < 2 * size)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {2 * size} éléments.");
            var xBytes = new byte[size];
            var yBytes = new byte[size];
            Array.Copy(bytes, 0, xBytes, 0, size);
            Array.Copy(bytes, size, yBytes, 0, size);
            var x = UFixed<TUInt, TFrac>.FromBytes(xBytes);
            var y = UFixed<TUInt, TFrac>.FromBytes(yBytes);
            return new UVec2<TUInt, TFrac>(x, y);
        }

        #endregion

        /* ===================
         * CONVERSION/TOSTRING
         * =================== */
        #region --- CONVERSION/TOSTRING ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"({X}, {Y})";
        public string DebugString() => $"UVec2<{typeof(TUInt).Name},{typeof(TFrac).Name}> (X={X.DebugString()}, Y={Y.DebugString()})";


        #endregion

        /* ===================
         * HELPERS INTERNES
         * =================== */
        #region --- HELPERS INTERNES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (UFixed<TUInt, TFrac> len, UVec2<TUInt, TFrac> dir)
            LengthAndDir(in UVec2<TUInt, TFrac> v)
        {
            var len2 = v.SqrMagnitude;
            if (len2 == UFixed<TUInt, TFrac>.Zero)
                return (UFixed<TUInt, TFrac>.Zero, v);
            var len = FixedMath.Sqrt(len2);
            return (len, v / len);
        }

        #endregion
    }
}
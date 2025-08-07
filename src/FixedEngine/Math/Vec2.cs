using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    /// <summary>
    /// Vecteur 2D générique en fixed-point déterministe (compatible FixedEngine).
    /// </summary>
    public struct Vec2<TInt, TFrac>
        where TInt : struct // IntN<TBits> ou UIntN<TBits>
        where TFrac : struct // B0-B32
    {
        public Fixed<TInt, TFrac> X;
        public Fixed<TInt, TFrac> Y;

        /* ==========================================
         * CONSTRUCTEURS & INSTANCES
         * - Vec2(x, y)
         * - FromRaw(rawX, rawY)
         * - FromInt(intX, intY)
         * - FromFloat(floatX, floatY)
         * - MinValue / MaxValue
         * - ByteSize
         * ========================================== */
        #region ----- CONSTRUCTEURS -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2(Fixed<TInt, TFrac> x, Fixed<TInt, TFrac> y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2(float x, float y)
        {
            X = new Fixed<TInt, TFrac>(x);
            Y = new Fixed<TInt, TFrac>(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2(int x, int y)
        {
            X = new Fixed<TInt, TFrac>(x);
            Y = new Fixed<TInt, TFrac>(y);
        }

        // Valeurs extrêmes pour le typage générique et les algos globaux
        public static readonly Vec2<TInt, TFrac> MinValue = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MinValue, Fixed<TInt, TFrac>.MinValue);
        public static readonly Vec2<TInt, TFrac> MaxValue = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.MaxValue, Fixed<TInt, TFrac>.MaxValue);

        public static readonly int ByteSize = Fixed<TInt, TFrac>.ByteSize * 2;
        #endregion

        /* ===================
         * CONSTANTES
         * Zero
         * One
         * Up
         * Down
         * Left
         * Right
         * =================== */
        #region ----- CONSTANTES ----
        public static readonly Vec2<TInt, TFrac> Zero = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.Zero);
        public static readonly Vec2<TInt, TFrac> One = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.One, Fixed<TInt, TFrac>.One);
        public static readonly Vec2<TInt, TFrac> Up = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.One);
        public static readonly Vec2<TInt, TFrac> Down = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Zero, -Fixed<TInt, TFrac>.One);
        public static readonly Vec2<TInt, TFrac> Left = new Vec2<TInt, TFrac>(-Fixed<TInt, TFrac>.One, Fixed<TInt, TFrac>.Zero);
        public static readonly Vec2<TInt, TFrac> Right = new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.One, Fixed<TInt, TFrac>.Zero);
        #endregion

        // =====================
        // == ACCÈS/DECONSTRUCTEUR  ==
        // =====================
        #region ----- ACCÈS/DECONSTRUCTEUR -----
        public Fixed<TInt, TFrac> this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                    default: throw new ArgumentOutOfRangeException(nameof(i), "Index doit être 0 (X) ou 1 (Y).");
                }
            }
            set
            {
                switch (i)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(i), "Index doit être 0 (X) ou 1 (Y).");
                }
            }
        }

        public void Deconstruct(out Fixed<TInt, TFrac> x, out Fixed<TInt, TFrac> y)
        {
            x = X;
            y = Y;
        }
        #endregion

        /* ===================
         * OPERATEURS VECTORIELS
         * +, -, *, /
         * Négation
         * Multiplication / Division scalaire
         * ==, !=
         * =================== */
        #region ----- OPERATEURS VECTORIELS -----
        // Addition
        public static Vec2<TInt, TFrac> operator +(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => new Vec2<TInt, TFrac>(a.X + b.X, a.Y + b.Y);

        // Soustraction
        public static Vec2<TInt, TFrac> operator -(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => new Vec2<TInt, TFrac>(a.X - b.X, a.Y - b.Y);

        // Négation
        public static Vec2<TInt, TFrac> operator -(Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(-v.X, -v.Y);

        // Multiplication composant à composant
        public static Vec2<TInt, TFrac> operator *(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => new Vec2<TInt, TFrac>(a.X * b.X, a.Y * b.Y);

        // Multiplication scalaire
        public static Vec2<TInt, TFrac> operator *(Vec2<TInt, TFrac> v, Fixed<TInt, TFrac> s)
            => new Vec2<TInt, TFrac>(v.X * s, v.Y * s);

        public static Vec2<TInt, TFrac> operator *(Fixed<TInt, TFrac> s, Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(s * v.X, s * v.Y);

        // Division scalaire
        public static Vec2<TInt, TFrac> operator /(Vec2<TInt, TFrac> v, Fixed<TInt, TFrac> s)
            => new Vec2<TInt, TFrac>(v.X / s, v.Y / s);

        public static bool operator ==(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => !(a == b);
        #endregion

        /* ===================
         * COMPARAISONS (égalité stricte)
         * Equals
         * GetHasCode
         * =================== */
        #region ----- COMPARAISONS (égalité stricte) ------

        public bool Equals(Vec2<TInt, TFrac> other)
            => this.X == other.X && this.Y == other.Y;

        public override bool Equals(object obj)
            => obj is Vec2<TInt, TFrac> v && Equals(v);

        public override int GetHashCode()
            => X.GetHashCode() ^ Y.GetHashCode();
        #endregion

        /* ===================
         * PROPRIÉTÉS VECTORIELLES
         * SqrMagnitude
         * Magnitude
         * Normalized
         * IsNormalized
         * IsZero
         * =================== */
        #region ----- PROPRIÉTÉS VECTORIELLES (Unity-style) -----

        /// <summary>
        /// Somme des carrés des composantes (X² + Y²). Utilisé pour éviter les sqrt en physique.
        /// </summary>
        public Fixed<TInt, TFrac> SqrMagnitude => X * X + Y * Y;

        /// <summary>
        /// Norme euclidienne (longueur du vecteur).
        /// </summary>
        public Fixed<TInt, TFrac> Magnitude => FixedMath.Sqrt(SqrMagnitude);

        /// <summary>
        /// Vecteur normalisé (de norme 1), ou (0,0) si le vecteur est nul.
        /// </summary>
        public Vec2<TInt, TFrac> Normalized =>
            (Magnitude == Fixed<TInt, TFrac>.Zero) ? this : this / Magnitude;

        // True si le vecteur est (quasi) unitaire à Epsilon près
        public bool IsNormalized =>
            Fixed<TInt, TFrac>.Abs(SqrMagnitude - Fixed<TInt, TFrac>.One) <= Fixed<TInt, TFrac>.Epsilon;

        public bool IsZero => X == Fixed<TInt, TFrac>.Zero && Y == Fixed<TInt, TFrac>.Zero;

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
        #region ----- MÉTHODES VECTORIELLES -----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Dot(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => a.X * b.X + a.Y * b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Distance(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => (a - b).Magnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Lerp(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b, UFixed<TInt, TFrac> t)
            => a + (b - a) * (Fixed<TInt, TFrac>) t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Min(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Min(a.X, b.X),
                Fixed<TInt, TFrac>.Min(a.Y, b.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Max(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Max(a.X, b.X),
                Fixed<TInt, TFrac>.Max(a.Y, b.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Clamp(Vec2<TInt, TFrac> v, Vec2<TInt, TFrac> min, Vec2<TInt, TFrac> max)
            => new Vec2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Clamp(v.X, min.X, max.X),
                Fixed<TInt, TFrac>.Clamp(v.Y, min.Y, max.Y));

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
         * SignedAngle
         * Sign
         * AngleDeg
         * Scale
         * With
         * =================== */
        #region ----- MÉTHODES VECTORIELLES AVANCÉES -----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Cross(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
            => a.X * b.Y - a.Y * b.X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> AngleTo(Vec2<TInt, TFrac> other)
            => new Fixed<TInt, TFrac>(FixedMath.Atan2(
                this.Y * other.X - this.X * other.Y,
                this.X * other.X + this.Y * other.Y));

        // Static Perpendicular (droite canonique)
        public static Vec2<TInt, TFrac> Perpendicular(Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(-v.Y, v.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> PerpendicularRight()
            => new Vec2<TInt, TFrac>(-Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> PerpendicularLeft()
            => new Vec2<TInt, TFrac>(Y, -X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> Rotate(Fixed<TInt, TFrac> angle)
        {
            var cos = Fixed<TInt, TFrac>.Cos(angle);
            var sin = Fixed<TInt, TFrac>.Sin(angle);
            return new Vec2<TInt, TFrac>(
                X * cos - Y * sin,
                X * sin + Y * cos
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> FromAngle(Fixed<TInt, TFrac> angle)
        {
            var cos = Fixed<TInt, TFrac>.Cos(angle);
            var sin = Fixed<TInt, TFrac>.Sin(angle);
            return new Vec2<TInt, TFrac>(cos, sin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> ToAngle()
            => new Fixed<TInt, TFrac>(FixedMath.Atan2(Y, X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> Reflect(Vec2<TInt, TFrac> normal)
        {
            var dot = Dot(this, normal);
            return this - normal * dot * new Fixed<TInt, TFrac>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public  Vec2<TInt, TFrac> ProjectOnto(Vec2<TInt, TFrac> target)
        {
            var dot = Dot(this, target);
            var mag2 = target.SqrMagnitude;
            if (mag2 == Fixed<TInt, TFrac>.Zero)
                return Zero;
            var factor = dot / mag2;
            return target * factor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> ClampMagnitude(Fixed<TInt, TFrac> maxLength)
        {
            var (len, dir) = LengthAndDir(this);
            return len > maxLength ? dir * maxLength : this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> MoveTowards(
            Vec2<TInt, TFrac> current,
            Vec2<TInt, TFrac> target,
            Fixed<TInt, TFrac> maxDelta)
        {
            var delta = target - current;
            var (dist, dir) = LengthAndDir(delta);
            if (dist <= maxDelta || dist == Fixed<TInt, TFrac>.Zero)
                return target;
            return current + dir * maxDelta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ApproxEquals(Vec2<TInt, TFrac> other, Fixed<TInt, TFrac> epsilon)
            => (this - other).SqrMagnitude < epsilon * epsilon; // AVANT : MagnitudeSquared()

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ApproxEquals(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b, Fixed<TInt, TFrac> epsilon)
        {
            return Fixed<TInt, TFrac>.Abs(a.X - b.X) <= epsilon
                && Fixed<TInt, TFrac>.Abs(a.Y - b.Y) <= epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Angle(Vec2<TInt, TFrac> from, Vec2<TInt, TFrac> to)
            => new Fixed<TInt, TFrac>(FixedMath.Acos(Dot(from.Normalized, to.Normalized)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> SignedAngle(Vec2<TInt, TFrac> from, Vec2<TInt, TFrac> to)
        {
            var unsigned = Angle(from, to);
            var sign = Fixed<TInt, TFrac>.Sign(Cross(from, to));
            return unsigned * sign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> AngleDeg(Vec2<TInt, TFrac> from, Vec2<TInt, TFrac> to)
        {
            var rad = new Fixed<TInt, TFrac>(FixedMath.Acos(Dot(from.Normalized, to.Normalized)));
            // Conversion rad→deg : angleDeg = angleRad * (180/π)
            var radToDegQ = FixedMath.RadToDegQ<TFrac>();
            return new Fixed<TInt, TFrac>((int)((long)rad.Raw * radToDegQ >> Fixed<TInt, TFrac>.FracBitsConst));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Scale(Vec2<TInt, TFrac> a, Vec2<TInt, TFrac> b)
        {
            return new Vec2<TInt, TFrac>(
                a.X * b.X,
                a.Y * b.Y
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> With(Fixed<TInt, TFrac> x, Fixed<TInt, TFrac> y)
        {
            return new Vec2<TInt, TFrac>(x, y);
        }



        #endregion

        /* ===================
         * HELPERS & UTILS VECTORIELS
         * Abs
         * Sign
         * Swap
         * SqrDistance
         * ManhattanDistance
         * ChebyshevDistance 
         * ToArray
         * FromArray
         * =================== */
        #region ----- HELPERS & UTILS VECTORIELS -----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> Abs()
            => new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Abs(X), Fixed<TInt, TFrac>.Abs(Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> Sign()
            => new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Sign(X), Fixed<TInt, TFrac>.Sign(Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2<TInt, TFrac> Swap()
            => new Vec2<TInt, TFrac>(Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> SqrDistance(Vec2<TInt, TFrac> other)
            => (this - other).SqrMagnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> ManhattanDistance(Vec2<TInt, TFrac> other)
            => Fixed<TInt, TFrac>.Abs(X - other.X) + Fixed<TInt, TFrac>.Abs(Y - other.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> ChebyshevDistance(Vec2<TInt, TFrac> other)
            => Fixed<TInt, TFrac>.Max(Fixed<TInt, TFrac>.Abs(X - other.X), Fixed<TInt, TFrac>.Abs(Y - other.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac>[] ToArray()
            => new Fixed<TInt, TFrac>[] { X, Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> FromArray(Fixed<TInt, TFrac>[] arr)
        {
            if (arr == null || arr.Length < 2)
                throw new ArgumentException("Le tableau doit contenir au moins 2 éléments.");
            return new Vec2<TInt, TFrac>(arr[0], arr[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Floor(Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Floor(v.X), Fixed<TInt, TFrac>.Floor(v.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Ceil(Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Ceil(v.X), Fixed<TInt, TFrac>.Ceil(v.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2<TInt, TFrac> Round(Vec2<TInt, TFrac> v)
            => new Vec2<TInt, TFrac>(Fixed<TInt, TFrac>.Round(v.X), Fixed<TInt, TFrac>.Round(v.Y));


        #endregion

        /* ==========================================
         * AUTRES HELPERS
         * - Copy()
         * - SwapXY()
         * - WithX(x), WithY(y)
         * ========================================== */
        #region --- AUTRES HELPERS ---

        // Copie du vecteur (utile pour chaîner ou du pattern immutable)
        public Vec2<TInt, TFrac> Copy() => new Vec2<TInt, TFrac>(X, Y);

        // Échange X et Y (utile pour UI, géométrie, shaders…)
        public Vec2<TInt, TFrac> SwapXY() => new Vec2<TInt, TFrac>(Y, X);

        // Setter fonctionnel d'un axe
        public Vec2<TInt, TFrac> WithX(Fixed<TInt, TFrac> x) => new Vec2<TInt, TFrac>(x, Y);
        public Vec2<TInt, TFrac> WithY(Fixed<TInt, TFrac> y) => new Vec2<TInt, TFrac>(X, y);

        #endregion

        /* ===================
         * FONCTIONS VECTORIELLES MUTATIF
         * Set
         * Normalize
         * ClampMagnitudeMut
         * PerpendicularRightMut
         * PerpendicularLeftMut
         * AbsMut
         * SignMut
         * =================== */
        #region ----- FONCTIONS VECTORIELLES MUTATIF -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Fixed<TInt, TFrac> x, Fixed<TInt, TFrac> y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var mag = Magnitude;
            if (mag == Fixed<TInt, TFrac>.Zero)
                return;
            X /= mag;
            Y /= mag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(Vec2<TInt, TFrac> scale)
        {
            X *= scale.X;
            Y *= scale.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClampMagnitudeMut(Fixed<TInt, TFrac> maxLength)
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
            X = -Y;
            Y = oldX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PerpendicularLeftMut()
        {
            var oldX = X;
            X = Y;
            Y = -oldX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AbsMut()
        {
            X = Fixed<TInt, TFrac>.Abs(X);
            Y = Fixed<TInt, TFrac>.Abs(Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SignMut()
        {
            X = Fixed<TInt, TFrac>.Sign(X);
            Y = Fixed<TInt, TFrac>.Sign(Y);
        }
        #endregion

        /* ===================
         * ACCES OCTETS
         * ToBytes
         * FromBytes
         * =================== */
        #region ----- ACCES OCTETS -----

        public byte[] ToBytes()
        {
            var xBytes = X.ToBytes();
            var yBytes = Y.ToBytes();
            byte[] result = new byte[xBytes.Length + yBytes.Length];
            Buffer.BlockCopy(xBytes, 0, result, 0, xBytes.Length);
            Buffer.BlockCopy(yBytes, 0, result, xBytes.Length, yBytes.Length);
            return result;
        }

        public static Vec2<TInt, TFrac> FromBytes(byte[] bytes)
        {
            int size = (Fixed<TInt, TFrac>.IntBitsConst + 7) / 8;
            if (bytes.Length < 2 * size)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {2 * size} éléments.");
            // Extraction manuelle des deux segments
            var xBytes = new byte[size];
            var yBytes = new byte[size];
            Array.Copy(bytes, 0, xBytes, 0, size);
            Array.Copy(bytes, size, yBytes, 0, size);
            var x = Fixed<TInt, TFrac>.FromBytes(xBytes);
            var y = Fixed<TInt, TFrac>.FromBytes(yBytes);
            return new Vec2<TInt, TFrac>(x, y);
        }
        #endregion

        /* ===================
         * CONVERSION/TOSTRING
         * =================== */
        #region --- CONVERSION/TOSTRING ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"({X}, {Y})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DebugString()
            => $"Vec2<{typeof(TInt).Name},{typeof(TFrac).Name}> (X={X.DebugString()}, Y={Y.DebugString()})";

        #endregion

        /* ===================
         * HELPERS INTERNES
         * =================== */
        #region --- HELPERS INTERNES --
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Fixed<TInt, TFrac> len, Vec2<TInt, TFrac> dir)
            LengthAndDir(in Vec2<TInt, TFrac> v)
        {
            var len2 = v.SqrMagnitude;
            if (len2 == Fixed<TInt, TFrac>.Zero)
                return (Fixed<TInt, TFrac>.Zero, v);
            var len = FixedMath.Sqrt(len2);           // une seule √
            return (len, v / len);                    // dir = v * (1/len)
        }
        #endregion
    }
}

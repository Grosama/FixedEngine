using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    /// <summary>
    /// Matrice 2x2 générique fixed-point unsigned (scale, transfo linéaire 2D, mais pas de rotation signée !).
    /// </summary>
    public struct UMat2x2<TUInt, TFrac>
        where TUInt : struct
        where TFrac : struct
    {
        public UFixed<TUInt, TFrac> M11, M12, M21, M22;

        /* ==========================================
         * CONSTRUCTEURS & INSTANCES PRÉDÉFINIES
         * - UMat2x2(m11, m12, m21, m22)
         * - Identity
         * - Zero
         * ========================================== */
        #region --- Constructeurs & Instances prédéfinies ---

        public UMat2x2(UFixed<TUInt, TFrac> m11, UFixed<TUInt, TFrac> m12, UFixed<TUInt, TFrac> m21, UFixed<TUInt, TFrac> m22)
        { M11 = m11; M12 = m12; M21 = m21; M22 = m22; }

        public static readonly UMat2x2<TUInt, TFrac> Identity = new UMat2x2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.One, UFixed<TUInt, TFrac>.Zero,
            UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.One);

        public static readonly UMat2x2<TUInt, TFrac> Zero = new UMat2x2<TUInt, TFrac>(
            UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero,
            UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero);

        #endregion

        /* ==========================================
         * ACCÈS & PROPRIÉTÉS
         * - this[int row, int col]
         * - this[int]   <-- accès plat, linéaire
         * - IsIdentity
         * - IsZero
         * - ByteSize
         * ========================================== */
        #region --- Accès & Propriétés ---

        public UFixed<TUInt, TFrac> this[int row, int col]
        {
            get => (row, col) switch
            {
                (0, 0) => M11,
                (0, 1) => M12,
                (1, 0) => M21,
                (1, 1) => M22,
                _ => throw new ArgumentOutOfRangeException()
            };
            set
            {
                switch ((row, col))
                {
                    case (0, 0): M11 = value; break;
                    case (0, 1): M12 = value; break;
                    case (1, 0): M21 = value; break;
                    case (1, 1): M22 = value; break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        public UFixed<TUInt, TFrac> this[int i]
        {
            get => i switch
            {
                0 => M11,
                1 => M12,
                2 => M21,
                3 => M22,
                _ => throw new ArgumentOutOfRangeException()
            };
            set
            {
                switch (i)
                {
                    case 0: M11 = value; break;
                    case 1: M12 = value; break;
                    case 2: M21 = value; break;
                    case 3: M22 = value; break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsIdentity =>
            M11 == UFixed<TUInt, TFrac>.One && M12 == UFixed<TUInt, TFrac>.Zero &&
            M21 == UFixed<TUInt, TFrac>.Zero && M22 == UFixed<TUInt, TFrac>.One;

        public bool IsZero =>
            M11 == UFixed<TUInt, TFrac>.Zero && M12 == UFixed<TUInt, TFrac>.Zero &&
            M21 == UFixed<TUInt, TFrac>.Zero && M22 == UFixed<TUInt, TFrac>.Zero;

        public static readonly int ByteSize = UFixed<TUInt, TFrac>.ByteSize * 4;

        #endregion

        /* ==========================================
         * OPÉRATEURS ARITHMÉTIQUES
         * - operator +(UMat2x2, UMat2x2)
         * - operator -(UMat2x2, UMat2x2)
         * - operator *(UMat2x2, UVec2)
         * - operator *(UMat2x2, UMat2x2)
         * - operator *(UMat2x2, UFixed)
         * - operator *(UFixed, UMat2x2)
         * - operator /(UMat2x2, UFixed)
         * ========================================== */
        #region --- Opérateurs Arithmétiques ---

        public static UMat2x2<TUInt, TFrac> operator +(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                a.M11 + b.M11, a.M12 + b.M12,
                a.M21 + b.M21, a.M22 + b.M22
            );

        public static UMat2x2<TUInt, TFrac> operator -(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                a.M11 - b.M11, a.M12 - b.M12,
                a.M21 - b.M21, a.M22 - b.M22
            );

        public static UVec2<TUInt, TFrac> operator *(UMat2x2<TUInt, TFrac> m, UVec2<TUInt, TFrac> v) =>
            new UVec2<TUInt, TFrac>(
                m.M11 * v.X + m.M12 * v.Y,
                m.M21 * v.X + m.M22 * v.Y
            );

        public static UMat2x2<TUInt, TFrac> operator *(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                a.M11 * b.M11 + a.M12 * b.M21, a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21, a.M21 * b.M12 + a.M22 * b.M22
            );

        public static UMat2x2<TUInt, TFrac> operator *(UMat2x2<TUInt, TFrac> m, UFixed<TUInt, TFrac> s) =>
            new UMat2x2<TUInt, TFrac>(
                m.M11 * s, m.M12 * s,
                m.M21 * s, m.M22 * s
            );

        public static UMat2x2<TUInt, TFrac> operator *(UFixed<TUInt, TFrac> s, UMat2x2<TUInt, TFrac> m) =>
            new UMat2x2<TUInt, TFrac>(
                s * m.M11, s * m.M12,
                s * m.M21, s * m.M22
            );

        public static UMat2x2<TUInt, TFrac> operator /(UMat2x2<TUInt, TFrac> m, UFixed<TUInt, TFrac> s) =>
            new UMat2x2<TUInt, TFrac>(
                m.M11 / s, m.M12 / s,
                m.M21 / s, m.M22 / s
            );

        #endregion

        /* ==========================================
         * FONCTIONS MATHÉMATIQUES
         * - Determinant
         * - Inverse()
         * - TryInverse(out UMat2x2)
         * - Transpose()
         * - Scale(UFixed)
         * ========================================== */
        #region --- Fonctions Mathématiques ---

        /// <summary>
        /// Calcul du déterminant (unsigned : wrap si overflow).
        /// </summary>
        public UFixed<TUInt, TFrac> Determinant => M11 * M22 - M12 * M21;

        /// <summary>
        /// Inverse la matrice si possible (comportement unsigned : wrap si négatif/interdit)
        /// </summary>
        public UMat2x2<TUInt, TFrac> Inverse()
        {
            var det = Determinant;
            if (det == UFixed<TUInt, TFrac>.Zero)
                throw new InvalidOperationException("Matrix not invertible (det = 0).");
            var invDet = UFixed<TUInt, TFrac>.One / det;
            // Pas de signed, donc pas de -M12/-M21
            // Si tu veux autoriser wrap/overflow, laisse tel quel :
            return new UMat2x2<TUInt, TFrac>(M22 * invDet, M12 * invDet, M21 * invDet, M11 * invDet);
        }

        /// <summary>
        /// Version "safe" : false si non-inversible, sinon retourne l'inverse (wrap unsigned)
        /// </summary>
        public bool TryInverse(out UMat2x2<TUInt, TFrac> inv)
        {
            var det = Determinant;
            if (det == UFixed<TUInt, TFrac>.Zero)
            {
                inv = Identity;
                return false;
            }
            var invDet = UFixed<TUInt, TFrac>.One / det;
            inv = new UMat2x2<TUInt, TFrac>(M22 * invDet, M12 * invDet, M21 * invDet, M11 * invDet);
            return true;
        }

        public UMat2x2<TUInt, TFrac> Transpose()
            => new UMat2x2<TUInt, TFrac>(M11, M21, M12, M22);

        public UMat2x2<TUInt, TFrac> Scale(UFixed<TUInt, TFrac> s)
            => new UMat2x2<TUInt, TFrac>(M11 * s, M12 * s, M21 * s, M22 * s);

        #endregion

        /* ==========================================
         * CRÉATION & HELPERS STATIQUES
         * - FromRotation(angle)       (⚠️ voir remarque ci-dessous)
         * - FromScale(sx, sy)
         * - FromArray(UFixed[])
         * - FromColumns(UVec2, UVec2)
         * - FromRows(UVec2, UVec2)
         * ========================================== */
        #region --- Création & Helpers ---

        /// <summary>
        /// Crée une matrice de rotation (⚠️ En unsigned, attention au wrap : peu de sens sauf cas ultra spécifiques).
        /// </summary>
        public static UMat2x2<TUInt, TFrac> FromRotation(UFixed<TUInt, TFrac> angle)
        {
            var cos = UFixed<TUInt, TFrac>.Cos(angle);
            var sin = UFixed<TUInt, TFrac>.Sin(angle);
            // ⚠️ Ici, pas de signed, donc pas de -sin possible, on laisse wrap/overflow.
            return new UMat2x2<TUInt, TFrac>(
                cos, sin,  // M11, M12
                sin, cos   // M21, M22
            );
        }

        public static UMat2x2<TUInt, TFrac> FromScale(UFixed<TUInt, TFrac> sx, UFixed<TUInt, TFrac> sy)
            => new UMat2x2<TUInt, TFrac>(sx, UFixed<TUInt, TFrac>.Zero, UFixed<TUInt, TFrac>.Zero, sy);

        public static UMat2x2<TUInt, TFrac> FromArray(UFixed<TUInt, TFrac>[] arr)
        {
            if (arr == null || arr.Length < 4)
                throw new ArgumentException("Tableau trop court pour FromArray");
            return new UMat2x2<TUInt, TFrac>(arr[0], arr[1], arr[2], arr[3]);
        }

        public static UMat2x2<TUInt, TFrac> FromColumns(UVec2<TUInt, TFrac> col1, UVec2<TUInt, TFrac> col2)
            => new UMat2x2<TUInt, TFrac>(col1.X, col2.X, col1.Y, col2.Y);

        public static UMat2x2<TUInt, TFrac> FromRows(UVec2<TUInt, TFrac> row1, UVec2<TUInt, TFrac> row2)
            => new UMat2x2<TUInt, TFrac>(row1.X, row1.Y, row2.X, row2.Y);

        #endregion

        /* ==========================================
         * HELPERS MUTATIFS
         * - SetRow(index, v0, v1)
         * - SetCol(index, v0, v1)
         * - Clamp(min, max)
         * - NormalizeColumns()
         * ========================================== */
        #region --- Helpers Mutatifs ---

        public void SetRow(int index, UFixed<TUInt, TFrac> v0, UFixed<TUInt, TFrac> v1)
        {
            switch (index)
            {
                case 0: M11 = v0; M12 = v1; break;
                case 1: M21 = v0; M22 = v1; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void SetCol(int index, UFixed<TUInt, TFrac> v0, UFixed<TUInt, TFrac> v1)
        {
            switch (index)
            {
                case 0: M11 = v0; M21 = v1; break;
                case 1: M12 = v0; M22 = v1; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void Clamp(UFixed<TUInt, TFrac> min, UFixed<TUInt, TFrac> max)
        {
            M11 = UFixed<TUInt, TFrac>.Clamp(M11, min, max);
            M12 = UFixed<TUInt, TFrac>.Clamp(M12, min, max);
            M21 = UFixed<TUInt, TFrac>.Clamp(M21, min, max);
            M22 = UFixed<TUInt, TFrac>.Clamp(M22, min, max);
        }

        /// <summary>
        /// Normalise chaque colonne (si possible, unsigned : attention au wrap)
        /// </summary>
        public void NormalizeColumns()
        {
            var c0 = new UVec2<TUInt, TFrac>(M11, M21).Normalized;
            var c1 = new UVec2<TUInt, TFrac>(M12, M22).Normalized;
            M11 = c0.X; M21 = c0.Y;
            M12 = c1.X; M22 = c1.Y;
        }

        #endregion

        /* ==========================================
         * AUTRES HELPERS
         * - SwapRows()
         * - SwapCols()
         * - ReverseRows()
         * - ReverseCols()
         * - Min(a, b)
         * - Max(a, b)
         * - ElementWiseMultiply(a, b)
         * - Apply(Func<UFixed, UFixed>)
         * - All(predicate)
         * - Any(predicate)
         * - Sum()
         * - Average()
         * - GetRow(index)
         * - GetCol(index)
         * ========================================== */
        #region --- Autres Helpers ---

        public void SwapRows()
        {
            (M11, M21) = (M21, M11);
            (M12, M22) = (M22, M12);
        }

        public void SwapCols()
        {
            (M11, M12) = (M12, M11);
            (M21, M22) = (M22, M21);
        }

        public void ReverseRows() => SwapRows();
        public void ReverseCols() => SwapCols();

        // Pas de Negate/NagateMut sur unsigned

        public static UMat2x2<TUInt, TFrac> Min(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Min(a.M11, b.M11),
                UFixed<TUInt, TFrac>.Min(a.M12, b.M12),
                UFixed<TUInt, TFrac>.Min(a.M21, b.M21),
                UFixed<TUInt, TFrac>.Min(a.M22, b.M22));

        public static UMat2x2<TUInt, TFrac> Max(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Max(a.M11, b.M11),
                UFixed<TUInt, TFrac>.Max(a.M12, b.M12),
                UFixed<TUInt, TFrac>.Max(a.M21, b.M21),
                UFixed<TUInt, TFrac>.Max(a.M22, b.M22));

        public static UMat2x2<TUInt, TFrac> ElementWiseMultiply(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            new UMat2x2<TUInt, TFrac>(
                a.M11 * b.M11, a.M12 * b.M12,
                a.M21 * b.M21, a.M22 * b.M22);

        public UMat2x2<TUInt, TFrac> Apply(Func<UFixed<TUInt, TFrac>, UFixed<TUInt, TFrac>> op) =>
            new UMat2x2<TUInt, TFrac>(op(M11), op(M12), op(M21), op(M22));

        public bool All(Func<UFixed<TUInt, TFrac>, bool> pred) =>
            pred(M11) && pred(M12) && pred(M21) && pred(M22);

        public bool Any(Func<UFixed<TUInt, TFrac>, bool> pred) =>
            pred(M11) || pred(M12) || pred(M21) || pred(M22);

        public UFixed<TUInt, TFrac> Sum() => M11 + M12 + M21 + M22;

        public UFixed<TUInt, TFrac> Average() => (M11 + M12 + M21 + M22) / (UFixed<TUInt, TFrac>)4;

        public UVec2<TUInt, TFrac> GetRow(int i)
            => i switch
            {
                0 => new UVec2<TUInt, TFrac>(M11, M12),
                1 => new UVec2<TUInt, TFrac>(M21, M22),
                _ => throw new ArgumentOutOfRangeException()
            };

        public UVec2<TUInt, TFrac> GetCol(int i)
            => i switch
            {
                0 => new UVec2<TUInt, TFrac>(M11, M21),
                1 => new UVec2<TUInt, TFrac>(M12, M22),
                _ => throw new ArgumentOutOfRangeException()
            };

        #endregion

        /* ==========================================
         * COMPARAISONS & ÉGALITÉ
         * - operator ==, !=
         * - Equals(object)
         * - GetHashCode()
         * - ApproxEquals(other, epsilon)
         * - TryEquals(other, epsilon)
         * ========================================== */
        #region --- Comparaisons & Égalité ---

        public static bool operator ==(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) =>
            a.M11 == b.M11 && a.M12 == b.M12 && a.M21 == b.M21 && a.M22 == b.M22;

        public static bool operator !=(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b) => !(a == b);

        public override bool Equals(object obj) =>
            obj is UMat2x2<TUInt, TFrac> other && this == other;

        public override int GetHashCode() =>
            M11.GetHashCode() ^ (M12.GetHashCode() << 1) ^ (M21.GetHashCode() << 2) ^ (M22.GetHashCode() << 3);

        public bool ApproxEquals(UMat2x2<TUInt, TFrac> other, UFixed<TUInt, TFrac> epsilon)
        {

            return
                UFixed<TUInt, TFrac>.Delta(M11, other.M11) <= epsilon &&
                UFixed<TUInt, TFrac>.Delta(M12, other.M12) <= epsilon &&
                UFixed<TUInt, TFrac>.Delta(M21, other.M21) <= epsilon &&
                UFixed<TUInt, TFrac>.Delta(M22, other.M22) <= epsilon;
        }

        public static bool TryEquals(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b, UFixed<TUInt, TFrac> epsilon) =>
            UFixed<TUInt, TFrac>.Delta(a.M11, b.M11) <= epsilon &&
            UFixed<TUInt, TFrac>.Delta(a.M12, b.M12) <= epsilon &&
            UFixed<TUInt, TFrac>.Delta(a.M21, b.M21) <= epsilon &&
            UFixed<TUInt, TFrac>.Delta(a.M22, b.M22) <= epsilon;

        #endregion

        /* ==========================================
         * TRANSFORMATIONS & INTERPOLATIONS
         * - Transform(UVec2[])
         * - TransformDirection(UVec2)
         * - TransformRect(URect)
         * - Lerp(a, b, t)
         * - SlerpRotation(angleA, angleB, t)
         * - EaseIn(a, b, t)
         * - EaseOut(a, b, t)
         * - EaseInOut(a, b, t)
         * ========================================== */
        #region --- Transformations & Interpolations ---

        // Transforme un tableau de vecteurs
        public UVec2<TUInt, TFrac>[] Transform(UVec2<TUInt, TFrac>[] points)
        {
            var arr = new UVec2<TUInt, TFrac>[points.Length];
            for (int i = 0; i < points.Length; i++)
                arr[i] = this * points[i];
            return arr;
        }

        // Transforme la direction (équivalent à la multiplication mat × vec)
        public UVec2<TUInt, TFrac> TransformDirection(UVec2<TUInt, TFrac> dir)
            => this * dir;

        // Transforme un rectangle (applique la matrice à chaque coin)
        public URect<TUInt, TFrac> TransformRect(URect<TUInt, TFrac> rect)
        {
            var bl = this * rect.BottomLeft;
            var br = this * rect.BottomRight;
            var tl = this * rect.TopLeft;
            var tr = this * rect.TopRight;
            var min = new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.X, br.X), UFixed<TUInt, TFrac>.Min(tl.X, tr.X)),
                UFixed<TUInt, TFrac>.Min(UFixed<TUInt, TFrac>.Min(bl.Y, br.Y), UFixed<TUInt, TFrac>.Min(tl.Y, tr.Y)));
            var max = new UVec2<TUInt, TFrac>(
                UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.X, br.X), UFixed<TUInt, TFrac>.Max(tl.X, tr.X)),
                UFixed<TUInt, TFrac>.Max(UFixed<TUInt, TFrac>.Max(bl.Y, br.Y), UFixed<TUInt, TFrac>.Max(tl.Y, tr.Y)));
            return new URect<TUInt, TFrac>(min, max);
        }

        // Lerp linéaire entre deux matrices
        public static UMat2x2<TUInt, TFrac> Lerp(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
            => new UMat2x2<TUInt, TFrac>(
                FixedMath.Lerp(a.M11, b.M11, t),
                FixedMath.Lerp(a.M12, b.M12, t),
                FixedMath.Lerp(a.M21, b.M21, t),
                FixedMath.Lerp(a.M22, b.M22, t));

        // SlerpRotation (pour matrices de rotation : ici, sur unsigned, c’est du Lerp sur angle, wrap safe)
        public static UMat2x2<TUInt, TFrac> SlerpRotation(
            UFixed<TUInt, TFrac> angleA, UFixed<TUInt, TFrac> angleB, UFixed<TUInt, TFrac> t)
        {
            var angle = FixedMath.Lerp(angleA, angleB, t);
            return UMat2x2<TUInt, TFrac>.FromRotation(angle);
        }

        // EaseIn/EaseOut/EaseInOut sont juste des variations de Lerp, sur t pré-transformé
        public static UMat2x2<TUInt, TFrac> EaseIn(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
        {
            var t2 = t * t;
            return Lerp(a, b, t2);
        }

        public static UMat2x2<TUInt, TFrac> EaseOut(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
        {
            var one = UFixed<TUInt, TFrac>.One;
            var t2 = one - (one - t) * (one - t);
            return Lerp(a, b, t2);
        }

        public static UMat2x2<TUInt, TFrac> EaseInOut(UMat2x2<TUInt, TFrac> a, UMat2x2<TUInt, TFrac> b, UFixed<TUInt, TFrac> t)
        {
            // EaseInOut “standard” : t^2 * (3 - 2t)
            var three = UFixed<TUInt, TFrac>.One + UFixed<TUInt, TFrac>.One + UFixed<TUInt, TFrac>.One;
            var t2 = t * t * (three - t * (UFixed<TUInt, TFrac>.One + UFixed<TUInt, TFrac>.One));
            return Lerp(a, b, t2);
        }

        #endregion

        /* ==========================================
         * DÉCOMPOSITION & TESTS STRUCTURELS
         * - Decompose(out scaleX, out scaleY, out rotation)
         * - DeterminantSign()
         * - IsRotation(epsilon)
         * - IsScale(epsilon)
         * - IsReflection()
         * ========================================== */
        #region --- Décomposition & Tests structurels ---

        /// <summary>
        /// Décomposition : extrait scaleX, scaleY, rotation (approx), branchless.
        /// </summary>
        public void Decompose(out UFixed<TUInt, TFrac> scaleX, out UFixed<TUInt, TFrac> scaleY, out UFixed<TUInt, TFrac> rotation)
        {
            // Hypothèse : colonne 0 = [M11; M21], colonne 1 = [M12; M22]
            var col0 = new UVec2<TUInt, TFrac>(M11, M21);
            var col1 = new UVec2<TUInt, TFrac>(M12, M22);

            scaleX = col0.Magnitude;
            scaleY = col1.Magnitude;

            // rotation = angle (col0) par rapport à l’axe X
            rotation = (UFixed<TUInt, TFrac>)FixedMath.Atan2(col0.Y, col0.X);
        }

        /// <summary>
        /// Signe du déterminant (toujours positif ou zero en unsigned, donc 1 ou 0).
        /// </summary>
        public int DeterminantSign()
            => Determinant == UFixed<TUInt, TFrac>.Zero ? 0 : 1;

        /// <summary>
        /// Vrai si la matrice est "presque" une rotation pure (epsilon).
        /// </summary>
        public bool IsRotation(UFixed<TUInt, TFrac> epsilon)
        {
            // Test : scale ≈ 1, ortho, det ≈ 1
            var col0 = new UVec2<TUInt, TFrac>(M11, M21);
            var col1 = new UVec2<TUInt, TFrac>(M12, M22);
            return
                UFixed<TUInt, TFrac>.Delta(col0.SqrMagnitude, UFixed<TUInt, TFrac>.One) <= epsilon &&
                UFixed<TUInt, TFrac>.Delta(col1.SqrMagnitude, UFixed<TUInt, TFrac>.One) <= epsilon &&
                UFixed<TUInt, TFrac>.Delta(Determinant, UFixed<TUInt, TFrac>.One) <= epsilon;
        }

        /// <summary>
        /// Vrai si la matrice est "presque" une échelle (epsilon).
        /// </summary>
        public bool IsScale(UFixed<TUInt, TFrac> epsilon)
        {
            // Test : off-diag ≈ 0
            return M12 <= epsilon && M21 <= epsilon;
        }

        /// <summary>
        /// Vrai si la matrice représente une réflexion (symétrie). En unsigned, c’est : det == 0
        /// </summary>
        public bool IsReflection()
            => Determinant == UFixed<TUInt, TFrac>.Zero;

        #endregion

        /* ==========================================
         * MÉTHODES DE MODIFICATION
         * - With(m11, m12, m21, m22)
         * - SetIdentity()
         * - SetZero()
         * - ToArray()
         * ========================================== */
        #region --- Méthodes de modification ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UMat2x2<TUInt, TFrac> With(
            UFixed<TUInt, TFrac> m11,
            UFixed<TUInt, TFrac> m12,
            UFixed<TUInt, TFrac> m21,
            UFixed<TUInt, TFrac> m22)
            => new UMat2x2<TUInt, TFrac>(m11, m12, m21, m22);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIdentity()
        {
            M11 = UFixed<TUInt, TFrac>.One; M12 = UFixed<TUInt, TFrac>.Zero;
            M21 = UFixed<TUInt, TFrac>.Zero; M22 = UFixed<TUInt, TFrac>.One;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetZero()
        {
            M11 = UFixed<TUInt, TFrac>.Zero; M12 = UFixed<TUInt, TFrac>.Zero;
            M21 = UFixed<TUInt, TFrac>.Zero; M22 = UFixed<TUInt, TFrac>.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac>[] ToArray()
            => new[] { M11, M12, M21, M22 };

        #endregion

        /* ==========================================
         * SÉRIALISATION OCTETS
         * - ToBytes()
         * - FromBytes(byte[])
         * ========================================== */
        #region --- Sérialisation Octets ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            var b11 = M11.ToBytes();
            var b12 = M12.ToBytes();
            var b21 = M21.ToBytes();
            var b22 = M22.ToBytes();
            byte[] result = new byte[b11.Length + b12.Length + b21.Length + b22.Length];
            Buffer.BlockCopy(b11, 0, result, 0, b11.Length);
            Buffer.BlockCopy(b12, 0, result, b11.Length, b12.Length);
            Buffer.BlockCopy(b21, 0, result, b11.Length + b12.Length, b21.Length);
            Buffer.BlockCopy(b22, 0, result, b11.Length + b12.Length + b21.Length, b22.Length);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UMat2x2<TUInt, TFrac> FromBytes(byte[] bytes)
        {
            int size = (UFixed<TUInt, TFrac>.IntBitsConst + 7) / 8;
            if (bytes.Length < 4 * size)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {4 * size} éléments.");

            // Extraction manuelle des 4 segments
            var m11Bytes = new byte[size];
            var m12Bytes = new byte[size];
            var m21Bytes = new byte[size];
            var m22Bytes = new byte[size];

            Array.Copy(bytes, 0 * size, m11Bytes, 0, size);
            Array.Copy(bytes, 1 * size, m12Bytes, 0, size);
            Array.Copy(bytes, 2 * size, m21Bytes, 0, size);
            Array.Copy(bytes, 3 * size, m22Bytes, 0, size);

            var m11 = UFixed<TUInt, TFrac>.FromBytes(m11Bytes);
            var m12 = UFixed<TUInt, TFrac>.FromBytes(m12Bytes);
            var m21 = UFixed<TUInt, TFrac>.FromBytes(m21Bytes);
            var m22 = UFixed<TUInt, TFrac>.FromBytes(m22Bytes);

            return new UMat2x2<TUInt, TFrac>(m11, m12, m21, m22);
        }
        #endregion

        /* ==========================================
         * DEBUG & TOSTRING
         * - ToString()
         * - DebugString()
         * ========================================== */
        #region --- Debug & ToString ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => $"UMat2x2({M11}, {M12}, {M21}, {M22})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DebugString()
            => $"UMat2x2[\n  {M11}, {M12}\n  {M21}, {M22}\n]";

        #endregion
    }
}

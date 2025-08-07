using System;

namespace FixedEngine.Math
{
    /// <summary>
    /// Matrice 2x2 générique fixed-point (rotation, scale, transfo linéaire 2D).
    /// </summary>
    public struct Mat2x2<TInt, TFrac>
        where TInt : struct
        where TFrac : struct
    {
        public Fixed<TInt, TFrac> M11, M12, M21, M22;

        /* ==========================================
         * CONSTRUCTEURS & INSTANCES PRÉDÉFINIES
         * - Mat2x2(m11, m12, m21, m22)
         * - Identity
         * - Zero
         * ========================================== */
        #region --- Constructeurs & Instances prédéfinies ---

        public Mat2x2(Fixed<TInt, TFrac> m11, Fixed<TInt, TFrac> m12, Fixed<TInt, TFrac> m21, Fixed<TInt, TFrac> m22)
        { M11 = m11; M12 = m12; M21 = m21; M22 = m22; }

        public static readonly Mat2x2<TInt, TFrac> Identity = new Mat2x2<TInt, TFrac>(
            Fixed<TInt, TFrac>.One, Fixed<TInt, TFrac>.Zero,
            Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.One);

        public static readonly Mat2x2<TInt, TFrac> Zero = new Mat2x2<TInt, TFrac>(
            Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.Zero,
            Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.Zero);

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

        public Fixed<TInt, TFrac> this[int row, int col]
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

        public Fixed<TInt, TFrac> this[int i]
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
            M11 == Fixed<TInt, TFrac>.One && M12 == Fixed<TInt, TFrac>.Zero &&
            M21 == Fixed<TInt, TFrac>.Zero && M22 == Fixed<TInt, TFrac>.One;

        public bool IsZero =>
            M11 == Fixed<TInt, TFrac>.Zero && M12 == Fixed<TInt, TFrac>.Zero &&
            M21 == Fixed<TInt, TFrac>.Zero && M22 == Fixed<TInt, TFrac>.Zero;

        public static readonly int ByteSize = Fixed<TInt, TFrac>.ByteSize * 4;

        #endregion

        /* ==========================================
         * OPÉRATEURS ARITHMÉTIQUES
         * - operator +(Mat2x2, Mat2x2)
         * - operator -(Mat2x2, Mat2x2)
         * - operator *(Mat2x2, Vec2)
         * - operator *(Mat2x2, Mat2x2)
         * - operator *(Mat2x2, Fixed)
         * - operator *(Fixed, Mat2x2)
         * - operator /(Mat2x2, Fixed)
         * ========================================== */
        #region --- Opérateurs Arithmétiques ---
        // Addition Mat2x2 + Mat2x2
        public static Mat2x2<TInt, TFrac> operator +(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                a.M11 + b.M11, a.M12 + b.M12,
                a.M21 + b.M21, a.M22 + b.M22
            );

        // Soustraction Mat2x2 - Mat2x2
        public static Mat2x2<TInt, TFrac> operator -(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                a.M11 - b.M11, a.M12 - b.M12,
                a.M21 - b.M21, a.M22 - b.M22
            );

        public static Vec2<TInt, TFrac> operator *(Mat2x2<TInt, TFrac> m, Vec2<TInt, TFrac> v) =>
            new Vec2<TInt, TFrac>(m.M11 * v.X + m.M12 * v.Y, m.M21 * v.X + m.M22 * v.Y);

        public static Mat2x2<TInt, TFrac> operator *(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                a.M11 * b.M11 + a.M12 * b.M21, a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21, a.M21 * b.M12 + a.M22 * b.M22
            );

        // Multiplication Mat2x2 * scalaire (à droite)
        public static Mat2x2<TInt, TFrac> operator *(Mat2x2<TInt, TFrac> m, Fixed<TInt, TFrac> s) =>
            new Mat2x2<TInt, TFrac>(
                m.M11 * s, m.M12 * s,
                m.M21 * s, m.M22 * s
            );

        // Multiplication scalaire * Mat2x2 (à gauche)
        public static Mat2x2<TInt, TFrac> operator *(Fixed<TInt, TFrac> s, Mat2x2<TInt, TFrac> m) =>
            new Mat2x2<TInt, TFrac>(
                s * m.M11, s * m.M12,
                s * m.M21, s * m.M22
            );

        // Division Mat2x2 / scalaire
        public static Mat2x2<TInt, TFrac> operator /(Mat2x2<TInt, TFrac> m, Fixed<TInt, TFrac> s) =>
            new Mat2x2<TInt, TFrac>(
                m.M11 / s, m.M12 / s,
                m.M21 / s, m.M22 / s
            );

        #endregion

        /* ==========================================
         * FONCTIONS MATHÉMATIQUES
         * - Determinant
         * - Inverse()
         * - TryInverse(out Mat2x2)
         * - Transpose()
         * - Scale(Fixed)
         * - Abs()
         * - Sign()
         * ========================================== */
        #region --- Fonctions Mathématiques ---

        public Fixed<TInt, TFrac> Determinant => M11 * M22 - M12 * M21;

        public Mat2x2<TInt, TFrac> Inverse()
        {
            var det = Determinant;
            if (det == Fixed<TInt, TFrac>.Zero)
                throw new InvalidOperationException("Matrix not invertible.");
            var invDet = Fixed<TInt, TFrac>.One / det;
            return new Mat2x2<TInt, TFrac>(M22 * invDet, -M12 * invDet, -M21 * invDet, M11 * invDet);
        }
        public bool TryInverse(out Mat2x2<TInt, TFrac> inv)
        {
            var det = Determinant;
            if (det == Fixed<TInt, TFrac>.Zero)
            {
                inv = Identity;
                return false;
            }
            var invDet = Fixed<TInt, TFrac>.One / det;
            inv = new Mat2x2<TInt, TFrac>(
                M22 * invDet, -M12 * invDet,
                -M21 * invDet, M11 * invDet
            );
            return true;
        }

        public Mat2x2<TInt, TFrac> Transpose()
             => new Mat2x2<TInt, TFrac>(M11, M21, M12, M22);

        public Mat2x2<TInt, TFrac> Scale(Fixed<TInt, TFrac> s)
            => new Mat2x2<TInt, TFrac>(M11 * s, M12 * s, M21 * s, M22 * s);

        // Retourne une matrice avec la valeur absolue de chaque élément
        public Mat2x2<TInt, TFrac> Abs()
            => new Mat2x2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Abs(M11),
                Fixed<TInt, TFrac>.Abs(M12),
                Fixed<TInt, TFrac>.Abs(M21),
                Fixed<TInt, TFrac>.Abs(M22));

        // Retourne une matrice où chaque élément vaut -1, 0 ou +1 selon le signe
        public Mat2x2<TInt, TFrac> Sign()
            => new Mat2x2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Sign(M11),
                Fixed<TInt, TFrac>.Sign(M12),
                Fixed<TInt, TFrac>.Sign(M21),
                Fixed<TInt, TFrac>.Sign(M22));

        #endregion

        /* ==========================================
         * CRÉATION & HELPERS STATIQUES
         * - FromRotation(angle)
         * - FromScale(sx, sy)
         * - FromArray(Fixed[])
         * - FromColumns(Vec2, Vec2)
         * - FromRows(Vec2, Vec2)
         * ========================================== */
        #region --- Création & Helpers ---

        public static Mat2x2<TInt, TFrac> FromRotation(Fixed<TInt, TFrac> angle)
        {
            var cos = Fixed<TInt, TFrac>.Cos(angle);
            var sin = Fixed<TInt, TFrac>.Sin(angle);
            return new Mat2x2<TInt, TFrac>(
                cos, -sin,
                sin, cos
            );
        }
        public static Mat2x2<TInt, TFrac> FromScale(Fixed<TInt, TFrac> sx, Fixed<TInt, TFrac> sy)
            => new Mat2x2<TInt, TFrac>(sx, Fixed<TInt, TFrac>.Zero, Fixed<TInt, TFrac>.Zero, sy);

        public static Mat2x2<TInt, TFrac> FromArray(Fixed<TInt, TFrac>[] arr)
        {
            if (arr == null || arr.Length < 4)
                throw new ArgumentException("Tableau trop court pour FromArray");
            return new Mat2x2<TInt, TFrac>(arr[0], arr[1], arr[2], arr[3]);
        }

        public static Mat2x2<TInt, TFrac> FromColumns(Vec2<TInt, TFrac> col1, Vec2<TInt, TFrac> col2)
             => new Mat2x2<TInt, TFrac>(col1.X, col2.X, col1.Y, col2.Y);

        public static Mat2x2<TInt, TFrac> FromRows(Vec2<TInt, TFrac> row1, Vec2<TInt, TFrac> row2)
            => new Mat2x2<TInt, TFrac>(row1.X, row1.Y, row2.X, row2.Y);

        #endregion

        /* ==========================================
         * HELPERS MUTATIFS
         * - SetRow(index, v0, v1)
         * - SetCol(index, v0, v1)
         * - Clamp(min, max)
         * - NormalizeColumns()
         * - AbsMut()
         * - SignMut()
         * ========================================== */
        #region --- Helpers Mutatifs ---
        public void SetRow(int index, Fixed<TInt, TFrac> v0, Fixed<TInt, TFrac> v1)
        {
            switch (index)
            {
                case 0: M11 = v0; M12 = v1; break;
                case 1: M21 = v0; M22 = v1; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        public void SetCol(int index, Fixed<TInt, TFrac> v0, Fixed<TInt, TFrac> v1)
        {
            switch (index)
            {
                case 0: M11 = v0; M21 = v1; break;
                case 1: M12 = v0; M22 = v1; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void Clamp(Fixed<TInt, TFrac> min, Fixed<TInt, TFrac> max)
        {
            M11 = Fixed<TInt, TFrac>.Clamp(M11, min, max);
            M12 = Fixed<TInt, TFrac>.Clamp(M12, min, max);
            M21 = Fixed<TInt, TFrac>.Clamp(M21, min, max);
            M22 = Fixed<TInt, TFrac>.Clamp(M22, min, max);
        }

        public void NormalizeColumns()
        {
            var c0 = new Vec2<TInt, TFrac>(M11, M21).Normalized;
            var c1 = new Vec2<TInt, TFrac>(M12, M22).Normalized;
            M11 = c0.X; M21 = c0.Y;
            M12 = c1.X; M22 = c1.Y;
        }

        // Modifie la matrice courante : chaque élément devient sa valeur absolue (in-place)
        public void AbsMut()
        {
            M11 = Fixed<TInt, TFrac>.Abs(M11);
            M12 = Fixed<TInt, TFrac>.Abs(M12);
            M21 = Fixed<TInt, TFrac>.Abs(M21);
            M22 = Fixed<TInt, TFrac>.Abs(M22);
        }

        // Modifie la matrice courante : chaque élément devient son signe (in-place)
        public void SignMut()
        {
            M11 = Fixed<TInt, TFrac>.Sign(M11);
            M12 = Fixed<TInt, TFrac>.Sign(M12);
            M21 = Fixed<TInt, TFrac>.Sign(M21);
            M22 = Fixed<TInt, TFrac>.Sign(M22);
        }

        #endregion


        /* ==========================================
         * AUTRES HELPERS
         * - SwapRows()
         * - SwapCols()
         * - ReverseRows()
         * - ReverseCols()
         * - Negate()
         * - NegateMut()
         * - Min(a, b)
         * - Max(a, b)
         * - ElementWiseMultiply(a, b)
         * - Apply(Func<Fixed, Fixed>)
         * - All(predicate)
         * - Any(predicate)
         * - Sum()
         * - Average()
         * - GetRow(index)
         * - GetCol(index)
         * ========================================== */
        #region --- Autres Helpers ---

        // Échange les deux lignes
        public void SwapRows()
        {
            (M11, M21) = (M21, M11);
            (M12, M22) = (M22, M12);
        }

        // Échange les deux colonnes
        public void SwapCols()
        {
            (M11, M12) = (M12, M11);
            (M21, M22) = (M22, M21);
        }

        // Inverse l'ordre des lignes (ligne 0 <-> ligne 1)
        public void ReverseRows()
        {
            (M11, M21) = (M21, M11);
            (M12, M22) = (M22, M12);
        }

        // Inverse l'ordre des colonnes (colonne 0 <-> colonne 1)
        public void ReverseCols()
        {
            (M11, M12) = (M12, M11);
            (M21, M22) = (M22, M21);
        }

        // Retourne la matrice négative
        public Mat2x2<TInt, TFrac> Negate() =>
            new Mat2x2<TInt, TFrac>(-M11, -M12, -M21, -M22);

        // Négatif in-place
        public void NegateMut()
        {
            M11 = -M11; M12 = -M12; M21 = -M21; M22 = -M22;
        }

        // Min élément par élément
        public static Mat2x2<TInt, TFrac> Min(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Min(a.M11, b.M11),
                Fixed<TInt, TFrac>.Min(a.M12, b.M12),
                Fixed<TInt, TFrac>.Min(a.M21, b.M21),
                Fixed<TInt, TFrac>.Min(a.M22, b.M22));

        // Max élément par élément
        public static Mat2x2<TInt, TFrac> Max(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Max(a.M11, b.M11),
                Fixed<TInt, TFrac>.Max(a.M12, b.M12),
                Fixed<TInt, TFrac>.Max(a.M21, b.M21),
                Fixed<TInt, TFrac>.Max(a.M22, b.M22));

        // Produit Hadamard (élément par élément)
        public static Mat2x2<TInt, TFrac> ElementWiseMultiply(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            new Mat2x2<TInt, TFrac>(
                a.M11 * b.M11, a.M12 * b.M12,
                a.M21 * b.M21, a.M22 * b.M22);

        // Applique une fonction à chaque élément (immut)
        public Mat2x2<TInt, TFrac> Apply(Func<Fixed<TInt, TFrac>, Fixed<TInt, TFrac>> op) =>
            new Mat2x2<TInt, TFrac>(op(M11), op(M12), op(M21), op(M22));

        // Renvoie true si tous les éléments valident le prédicat
        public bool All(Func<Fixed<TInt, TFrac>, bool> pred) =>
            pred(M11) && pred(M12) && pred(M21) && pred(M22);

        // Renvoie true si au moins un élément valide le prédicat
        public bool Any(Func<Fixed<TInt, TFrac>, bool> pred) =>
            pred(M11) || pred(M12) || pred(M21) || pred(M22);

        // Somme des éléments
        public Fixed<TInt, TFrac> Sum() => M11 + M12 + M21 + M22;

        // Moyenne des éléments
        public Fixed<TInt, TFrac> Average() => (M11 + M12 + M21 + M22) / (Fixed<TInt, TFrac>)4;

        // Accès ligne (Vec2)
        public Vec2<TInt, TFrac> GetRow(int i)
        {
            return i switch
            {
                0 => new Vec2<TInt, TFrac>(M11, M12),
                1 => new Vec2<TInt, TFrac>(M21, M22),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Accès colonne (Vec2)
        public Vec2<TInt, TFrac> GetCol(int i)
        {
            return i switch
            {
                0 => new Vec2<TInt, TFrac>(M11, M21),
                1 => new Vec2<TInt, TFrac>(M12, M22),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

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
        public static bool operator ==(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) =>
            a.M11 == b.M11 && a.M12 == b.M12 && a.M21 == b.M21 && a.M22 == b.M22;
        public static bool operator !=(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b) => !(a == b);
        public override bool Equals(object obj)
            => obj is Mat2x2<TInt, TFrac> m && this == m;
        public override int GetHashCode()
            => M11.GetHashCode() ^ M12.GetHashCode() ^ M21.GetHashCode() ^ M22.GetHashCode();

        public bool ApproxEquals(Mat2x2<TInt, TFrac> other, Fixed<TInt, TFrac> epsilon)
        {
            return
                Fixed<TInt, TFrac>.Abs(M11 - other.M11) < epsilon &&
                Fixed<TInt, TFrac>.Abs(M12 - other.M12) < epsilon &&
                Fixed<TInt, TFrac>.Abs(M21 - other.M21) < epsilon &&
                Fixed<TInt, TFrac>.Abs(M22 - other.M22) < epsilon;
        }

        public bool TryEquals(Mat2x2<TInt, TFrac> other, Fixed<TInt, TFrac> epsilon)
        {
            return
                Fixed<TInt, TFrac>.Abs(M11 - other.M11) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(M12 - other.M12) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(M21 - other.M21) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(M22 - other.M22) <= epsilon;
        }

        #endregion

        /* ==========================================
         * TRANSFORMATIONS & INTERPOLATIONS
         * - Transform(Vec2[])
         * - TransformDirection(Vec2)
         * - TransformRect(Rect)
         * - Lerp(a, b, t)
         * - SlerpRotation(angleA, angleB, t)
         * - EaseIn(a, b, t)
         * - EaseOut(a, b, t)
         * - EaseInOut(a, b, t)
         * ========================================== */
        #region --- Transformations & Interpolation ---

        public void Transform(Vec2<TInt, TFrac>[] points)
        {
            for (int i = 0; i < points.Length; ++i)
                points[i] = this * points[i];
        }

        public Vec2<TInt, TFrac> TransformDirection(Vec2<TInt, TFrac> v) => this * v;

        public Rect<TInt, TFrac> TransformRect(Rect<TInt, TFrac> rect)
        {
            var p1 = this * rect.Min;
            var p2 = this * new Vec2<TInt, TFrac>(rect.Max.X, rect.Min.Y);
            var p3 = this * new Vec2<TInt, TFrac>(rect.Min.X, rect.Max.Y);
            var p4 = this * rect.Max;
            // Calcule le min/max des points transformés
            var xs = new[] { p1.X, p2.X, p3.X, p4.X };
            var ys = new[] { p1.Y, p2.Y, p3.Y, p4.Y };
            var minX = xs[0]; var maxX = xs[0];
            var minY = ys[0]; var maxY = ys[0];
            for (int i = 1; i < 4; i++)
            {
                if (xs[i] < minX) minX = xs[i];
                if (xs[i] > maxX) maxX = xs[i];
                if (ys[i] < minY) minY = ys[i];
                if (ys[i] > maxY) maxY = ys[i];
            }
            return new Rect<TInt, TFrac>(new Vec2<TInt, TFrac>(minX, minY), new Vec2<TInt, TFrac>(maxX, maxY));
        }

        public static Mat2x2<TInt, TFrac> Lerp(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b, Fixed<TInt, TFrac> t)
        {
            return new Mat2x2<TInt, TFrac>(
                Fixed<TInt, TFrac>.Lerp(a.M11, b.M11, t),
                Fixed<TInt, TFrac>.Lerp(a.M12, b.M12, t),
                Fixed<TInt, TFrac>.Lerp(a.M21, b.M21, t),
                Fixed<TInt, TFrac>.Lerp(a.M22, b.M22, t)
            );
        }

        public static Mat2x2<TInt, TFrac> SlerpRotation(Fixed<TInt, TFrac> angleA, Fixed<TInt, TFrac> angleB, Fixed<TInt, TFrac> t)
        {
            var lerpAngle = angleA + (angleB - angleA) * t;
            return Mat2x2<TInt, TFrac>.FromRotation(lerpAngle);
        }

        public static Mat2x2<TInt, TFrac> EaseIn(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b, Fixed<TInt, TFrac> t)
        {
            var t2 = t * t;
            return Lerp(a, b, t2);
        }

        public static Mat2x2<TInt, TFrac> EaseOut(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b, Fixed<TInt, TFrac> t)
        {
            var one = Fixed<TInt, TFrac>.One;
            var t2 = one - ((one - t) * (one - t));
            return Lerp(a, b, t2);
        }

        public static Mat2x2<TInt, TFrac> EaseInOut(Mat2x2<TInt, TFrac> a, Mat2x2<TInt, TFrac> b, Fixed<TInt, TFrac> t)
        {
            var one = Fixed<TInt, TFrac>.One;
            var two = Fixed<TInt, TFrac>.One + Fixed<TInt, TFrac>.One;
            var ease = t < Fixed<TInt, TFrac>.Half
                ? (two * t * t)
                : (one - two * (one - t) * (one - t));
            return Lerp(a, b, ease);
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
        #region --- Decomposition et test de structure ---
        public void Decompose(out Fixed<TInt, TFrac> scaleX, out Fixed<TInt, TFrac> scaleY, out Fixed<TInt, TFrac> rotation)
        {
            // Les colonnes de la matrice
            var col0 = new Vec2<TInt, TFrac>(M11, M21);
            var col1 = new Vec2<TInt, TFrac>(M12, M22);
            scaleX = col0.Magnitude;
            scaleY = col1.Magnitude;

            // Angle : atan2(M21, M11) (col0 = [cos, sin] * scaleX)
            rotation = (Fixed<TInt, TFrac>)FixedMath.Atan2(col0.Y, col0.X);
        }

        public int DeterminantSign() => Determinant > Fixed<TInt, TFrac>.Zero ? 1 :
                       Determinant < Fixed<TInt, TFrac>.Zero ? -1 : 0;

        public bool IsRotation(Fixed<TInt, TFrac> epsilon)
        {
            var c0 = new Vec2<TInt, TFrac>(M11, M21);
            var c1 = new Vec2<TInt, TFrac>(M12, M22);
            var dot = Vec2<TInt, TFrac>.Dot(c0, c1);
            var len0 = c0.SqrMagnitude;
            var len1 = c1.SqrMagnitude;
            var det = Determinant;
            return
                Fixed<TInt, TFrac>.Abs(dot) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(len0 - Fixed<TInt, TFrac>.One) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(len1 - Fixed<TInt, TFrac>.One) <= epsilon &&
                (Fixed<TInt, TFrac>.Abs(Fixed<TInt, TFrac>.Abs(det) - Fixed<TInt, TFrac>.One) <= epsilon);
        }

        public bool IsScale(Fixed<TInt, TFrac> epsilon)
        {
            return
                Fixed<TInt, TFrac>.Abs(M12) <= epsilon &&
                Fixed<TInt, TFrac>.Abs(M21) <= epsilon;
        }

        public bool IsReflection() => Determinant < Fixed<TInt, TFrac>.Zero;

        #endregion

        /* ==========================================
         * MÉTHODES DE MODIFICATION
         * - With(m11, m12, m21, m22)
         * - SetIdentity()
         * - SetZero()
         * - ToArray()
         * ========================================== */
        #region --- Methodes de modification ---

        public Mat2x2<TInt, TFrac> With(
            Fixed<TInt, TFrac> m11, Fixed<TInt, TFrac> m12, Fixed<TInt, TFrac> m21, Fixed<TInt, TFrac> m22)
            => new Mat2x2<TInt, TFrac>(m11, m12, m21, m22);
        public void SetIdentity()
        {
            M11 = Fixed<TInt, TFrac>.One; M12 = Fixed<TInt, TFrac>.Zero;
            M21 = Fixed<TInt, TFrac>.Zero; M22 = Fixed<TInt, TFrac>.One;
        }

        public void SetZero()
        {
            M11 = M12 = M21 = M22 = Fixed<TInt, TFrac>.Zero;
        }

        public Fixed<TInt, TFrac>[] ToArray()
            => new Fixed<TInt, TFrac>[] { M11, M12, M21, M22 };
        #endregion

        /* ==========================================
         * SÉRIALISATION OCTETS
         * - ToBytes()
         * - FromBytes(byte[])
         * ========================================== */
        #region --- Serialisation Octets---
        public byte[] ToBytes()
        {
            var m11Bytes = M11.ToBytes();
            var m12Bytes = M12.ToBytes();
            var m21Bytes = M21.ToBytes();
            var m22Bytes = M22.ToBytes();
            int size = m11Bytes.Length; // même taille pour chaque Fixed

            byte[] result = new byte[size * 4];
            Buffer.BlockCopy(m11Bytes, 0, result, 0 * size, size);
            Buffer.BlockCopy(m12Bytes, 0, result, 1 * size, size);
            Buffer.BlockCopy(m21Bytes, 0, result, 2 * size, size);
            Buffer.BlockCopy(m22Bytes, 0, result, 3 * size, size);
            return result;
        }

        public static Mat2x2<TInt, TFrac> FromBytes(byte[] bytes)
        {
            int size = (Fixed<TInt, TFrac>.IntBitsConst + 7) / 8;
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
            var m11 = Fixed<TInt, TFrac>.FromBytes(m11Bytes);
            var m12 = Fixed<TInt, TFrac>.FromBytes(m12Bytes);
            var m21 = Fixed<TInt, TFrac>.FromBytes(m21Bytes);
            var m22 = Fixed<TInt, TFrac>.FromBytes(m22Bytes);
            return new Mat2x2<TInt, TFrac>(m11, m12, m21, m22);
        }
        #endregion

        /* ==========================================
         * DEBUG & TOSTRING
         * - ToString()
         * - DebugString()
         * ========================================== */
        #region --- Debug & ToString ---
        public override string ToString() => $"[{M11}, {M12} | {M21}, {M22}]";
        public string DebugString() => $"[{M11}, {M12}]\n[{M21}, {M22}]";

        #endregion

    }
}

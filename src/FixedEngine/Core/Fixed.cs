using FixedEngine.Math;
using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Core
{

    public readonly struct Fixed<TInt, TFrac>
        where TInt : struct // Doit être IntN<TBits> ou UIntN<TBits>
        where TFrac : struct // Tag bits fractionnaires (B0-B32)
    {

        public static readonly int FracBitsConst = BitsOf<TFrac>.Value;
        public static readonly int IntBitsConst = BitsOf<TInt>.Value;
        // échelle pré-calculée, repliée par le JIT/AOT
        private static readonly int ScaleConst = 1 << FracBitsConst;

        private static readonly int MinConst = Mask.SIGNED_MIN[IntBitsConst];
        private static readonly int MaxConst = Mask.SIGNED_MAX[IntBitsConst];

        public static readonly Fixed<TInt, TFrac> MinValue = new Fixed<TInt, TFrac>(IntN<TInt>.MinValue);
        public static readonly Fixed<TInt, TFrac> MaxValue = new Fixed<TInt, TFrac>(IntN<TInt>.MaxValue);

        public static readonly Fixed<TInt, TFrac> Epsilon = FromRaw(1);
        public static readonly int ByteSize = sizeof(int); // Q8.8, Q16.16, etc.

        private readonly IntN<TInt> _raw;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed(int raw) => _raw = new IntN<TInt>(raw);            // wrap automatique

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed(IntN<TInt> integer)
        {
            // Wrap réalisé une seule fois par le ctor de IntN
            _raw = new IntN<TInt>(integer.Raw << FracBitsConst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed(float value)
        {
            // utilisation de l’échelle pré-calculée
            _raw = new IntN<TInt>((int)System.Math.Round(value * ScaleConst));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed(double value)
        {
            // idem pour le double
            _raw = new IntN<TInt>((int)System.Math.Round(value * ScaleConst));
        }

        public int Raw => _raw.Raw;
        public static Fixed<TInt, TFrac> FromRaw(int raw) => new Fixed<TInt, TFrac>(raw);

        public static float ToFloat(Fixed<TInt, TFrac> value)
        {
            return (float)value.Raw / (1 << FracBitsConst);
        }

        public float ToFloat()
        {
            return (float)Raw / (1 << FracBitsConst);
        }

        /*==================================
         * --- CONVERSION EXPLICITES ---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES ---

        // float <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(Fixed<TInt, TFrac> x) => x._raw.Raw / (float)(1 << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(float x) => new Fixed<TInt, TFrac>(x);

        // double <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(Fixed<TInt, TFrac> x) => x._raw.Raw / (double)(1 << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(double x) => new Fixed<TInt, TFrac>(x);

        // int <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Fixed<TInt, TFrac> x) => x._raw.Raw >> FracBitsConst;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(int x) => new Fixed<TInt, TFrac>(x << FracBitsConst);

        // IntN <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(IntN<TInt> x) => new Fixed<TInt, TFrac>((int)x << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TInt>(Fixed<TInt, TFrac> x) => new IntN<TInt>(x._raw.Raw >> FracBitsConst);

        // UIntN <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(UIntN<TInt> x) => new Fixed<TInt, TFrac>((int)(uint)x << FracBitsConst);
        public static explicit operator UIntN<TInt>(Fixed<TInt, TFrac> x)
        {
            //int bits = BitsOf<TInt>.Value;
            uint mask = IntBitsConst == 32 ? 0xFFFFFFFFu : (1u << IntBitsConst) - 1u;
            uint rawMasked = (uint)x.Raw & mask;   // wrap hardware sur n bits
            uint value = rawMasked >> FracBitsConst;
            return new UIntN<TInt>(value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(uint x) => new Fixed<TInt, TFrac>((int)x << FracBitsConst);

        // Fixed <-> Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TOtherFrac> ConvertFrac<TOtherFrac>(Fixed<TInt, TFrac> x)
            where TOtherFrac : struct
        {
            int shift = BitsOf<TFrac>.Value - BitsOf<TOtherFrac>.Value;
            if (shift == 0) return new Fixed<TInt, TOtherFrac>(x.Raw);
            else if (shift > 0) return new Fixed<TInt, TOtherFrac>(x.Raw >> shift);
            else return new Fixed<TInt, TOtherFrac>(x.Raw << -shift);
        }

        // UFixed <-> Fixed
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TInt, TFrac>(Fixed<TInt, TFrac> x)
            => new UFixed<TInt, TFrac>((uint)x.Raw);*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Fixed<TInt, TFrac>(UFixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>((int)x.Raw);


        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMETIQUES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator +(
                Fixed<TInt, TFrac> a,
                Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>(a.Raw + b.Raw);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator +(Fixed<TInt, TFrac> x)
            => x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator -(
                Fixed<TInt, TFrac> a,
                Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>(a.Raw - b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator -(Fixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>(-x.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator *(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {
            long prod = a.Raw * (long)b.Raw; // multiplie sur 64 bits
            int result = (int)(prod >> FracBitsConst); // shift, puis wrap via le ctor
            return new Fixed<TInt, TFrac>(result); // wrap via IntN<TInt>
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator /(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {
            if (b._raw.Raw == 0)
                throw new DivideByZeroException($"Division par zéro dans Fixed<{typeof(TInt).Name}, {typeof(TFrac).Name}>");
            return new Fixed<TInt, TFrac>((int)(((long)a._raw.Raw << FracBitsConst) / b._raw.Raw));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator %(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {
            if (b.Raw == 0)
                throw new DivideByZeroException($"Modulo par zéro dans Fixed<{typeof(TInt).Name}, {typeof(TFrac).Name}>");
            int rawMod = a.Raw % b.Raw;
            return new Fixed<TInt, TFrac>(rawMod); // wrap final ici
        }

        // Optionnel, incrémentation/décrémentation de la plus petite valeur possible
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator ++(Fixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>(x._raw.Raw + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator --(Fixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>(x._raw.Raw - 1);

        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES POUR ARITHMETIQUE ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Add(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a + b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Sub(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a - b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Mul(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a * b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Div(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Mod(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a % b;

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> MulPow2(Fixed<TInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour MulPow2");
            if (n == 0)
                return a;
            unchecked
            {
                int v = a.Raw << n;
                // Pour le signed : wrap signé dans le constructeur classique
                return new Fixed<TInt, TFrac>(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> DivPow2(Fixed<TInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour DivPow2");
            if (n == 0)
                return a;
            int v = a.Raw >> n;
            return new Fixed<TInt, TFrac>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> ModPow2(Fixed<TInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n > limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit}] pour ModPow2");
            if (n == 0)
                return Zero;
            int v = a.Raw & (int)Mask.MASKS[n];
            return new Fixed<TInt, TFrac>(v);
        }
        #endregion

        /*==================================
         * --- OPERATION BITWISE ---
         * operator &
         * operator |
         * operator ^
         * operator ~
         * operator <<
         * operator >>
         ==================================*/
        #region --- OPERATION BITWISE ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator &(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>(a.Raw & b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator |(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>(a.Raw | b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator ^(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>(a.Raw ^ b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator ~(Fixed<TInt, TFrac> a)
            => new Fixed<TInt, TFrac>(~a.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator <<(Fixed<TInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour <<");

            if (n == 0)
                return a;

            unchecked
            {
                int v = a.Raw << n;
                return new Fixed<TInt, TFrac>(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> operator >>(Fixed<TInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour >>");

            if (n == 0)
                return a;

            int v = a.Raw >> n;
            return new Fixed<TInt, TFrac>(v);
        }

        #endregion

        /*==================================
         * --- METHODE STATIQUE BITWISE (alias) ---
         * And
         * Or
         * Xor
         * Not
         * Nand
         * Nor
         * Xnor
         * Shl
         * Shr
         ==================================*/
        #region --- METHODE STATIQUE BITWISE (alias) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> And(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a & b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Or(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a | b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Xor(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a ^ b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Not(Fixed<TInt, TFrac> a) => ~a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Nand(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => ~(a & b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Nor(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => ~(a | b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Xnor(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => ~(a ^ b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Shl(Fixed<TInt, TFrac> a, int n) => a << n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Shr(Fixed<TInt, TFrac> a, int n) => a >> n;

        #endregion

        /*==================================
         * --- COMPARAISONS ---
         * operator ==
         * operator !=
         * operator <
         * operator <=
         * operator >
         * operator >=
         * Equals(object obj)
         * GetHashCode()
         * Eq
         * Neq
         * Lt
         * Lte
         * Gt
         * Gte
         * IsZero
         * IsNeg
         * IsPos
         ==================================*/
        #region --- COMPARAISONS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw == b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw != b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw < b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw <= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw > b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw >= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is Fixed<TInt, TFrac> other && Raw == other.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => typeof(TInt).GetHashCode() ^ typeof(TFrac).GetHashCode() ^ Raw;

        // Alias statiques pour code générique

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Eq(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw == b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Neq(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw != b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lt(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw < b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lte(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw <= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gt(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw > b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gte(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b) => a.Raw >= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(Fixed<TInt, TFrac> a) => a.Raw == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNeg(Fixed<TInt, TFrac> a) => a.Raw < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPos(Fixed<TInt, TFrac> a) => a.Raw > 0;

        #endregion

        /*==================================
         * --- OPERATIONS UTILITAIRES ---
         * Min
         * Max
         * Avg
         * Sign
         * Abs
         * Neg
         * CopySign
         ==================================*/
        #region --- OPERATIONS UTILITAIRES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Min(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a.Raw < b.Raw ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Max(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => a.Raw > b.Raw ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Avg(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
            => new Fixed<TInt, TFrac>((a.Raw + b.Raw) / 2);

        public static Fixed<TInt, TFrac> Sign(Fixed<TInt, TFrac> x)
        {
            if (x.Raw > 0) return new Fixed<TInt, TFrac>(1 << FracBitsConst);  // +1 en Q8.8
            if (x.Raw < 0) return new Fixed<TInt, TFrac>(-1 << FracBitsConst); // -1 en Q8.8
            return new Fixed<TInt, TFrac>(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Abs(Fixed<TInt, TFrac> a)
            => new Fixed<TInt, TFrac>(a.Raw < 0 ? -a.Raw : a.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Neg(Fixed<TInt, TFrac> a)
            => new Fixed<TInt, TFrac>(-a.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> CopySign(Fixed<TInt, TFrac> value, Fixed<TInt, TFrac> sign)
            => sign.Raw < 0 ? Neg(Abs(value)) : Abs(value);

        #endregion

        /*==================================
         * --- FLOOR, CEIL, ROUND ---
         * Floor
         * Ceil
         * Round
         ==================================*/
        #region --- FLOOR, CEIL, ROUND ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Floor(Fixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>(x.Raw & ~((1 << FracBitsConst) - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Ceil(Fixed<TInt, TFrac> x)
        {
            int mask = (1 << FracBitsConst) - 1;
            int raw = x.Raw;
            if ((raw & mask) == 0)
                return new Fixed<TInt, TFrac>(raw);
            int fracAdd = raw >= 0 ? (1 << FracBitsConst) - (raw & mask) : 0;
            return new Fixed<TInt, TFrac>(raw + fracAdd & ~mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Round(Fixed<TInt, TFrac> x)
        {
            int half = 1 << FracBitsConst - 1;
            int raw = x.Raw;
            int rounded = raw >= 0
                ? raw + half & ~((1 << FracBitsConst) - 1)
                : raw - half & ~((1 << FracBitsConst) - 1);
            return new Fixed<TInt, TFrac>(rounded);
        }

        #endregion

        /*==================================
         * --- SATURATION ---
         * AddSat
         * SubSat
         * MulSat
         * Clamp
         * Clamp01
         * ClampWithOffset
         ==================================*/
        #region --- SATURATION ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> AddSat(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {
            long sum = (long)a.Raw + b.Raw;        // addition pleine largeur (évite le wrap int32)

            // clamp branch‑less : deux CMOV générés par RyuJIT (aucun saut)
            sum = sum > MaxConst ? MaxConst : sum;
            sum = sum < MinConst ? MinConst : sum;

            return new Fixed<TInt, TFrac>((int)sum);  // ctor wrap déjà géré par IntN<TInt>
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> SubSat(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {          
            long diff = (long)a.Raw - b.Raw;
            diff = diff > MaxConst ? MaxConst : diff;   // min(diff, MaxConst)
            diff = diff < MinConst ? MinConst : diff;   // max(diff, MinConst)

            return new Fixed<TInt, TFrac>((int)diff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> MulSat(Fixed<TInt, TFrac> a, Fixed<TInt, TFrac> b)
        {

            long prod = (long)a.Raw * b.Raw >> FracBitsConst;

            prod = prod > MaxConst ? MaxConst : prod;   // min(prod, MaxConst)
            prod = prod < MinConst ? MinConst : prod;   // max(prod, MinConst)

            return new Fixed<TInt, TFrac>((int)prod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Clamp(
            Fixed<TInt, TFrac> val,
            Fixed<TInt, TFrac> min,
            Fixed<TInt, TFrac> max)
        {
            int v = val.Raw;

            // Deux CMOV : max(v, min) puis min(..., max) — aucune branche
            v = v < min.Raw ? min.Raw : v;
            v = v > max.Raw ? max.Raw : v;

            return new Fixed<TInt, TFrac>(v);   // valeur déjà dans la plage, pas de re‑masque
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Clamp01(Fixed<TInt, TFrac> val)
        {
            int v = val.Raw;               // signed: peut être < 0
            int one = 1 << FracBitsConst;    // représentation de 1.0 en Q‑format

            // max(v, 0) puis min(..., 1)   — RyuJIT ⇒ deux CMOV, zéro saut
            v = v < 0 ? 0 : v;
            v = v > one ? one : v;

            return new Fixed<TInt, TFrac>(v);   // ctor : valeur déjà correcte
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> ClampWithOffset(
            Fixed<TInt, TFrac> val,
            Fixed<TInt, TFrac> min,
            Fixed<TInt, TFrac> max,
            int offsetMin,
            int offsetMax)
        {

            int vLo = min.Raw + offsetMin;
            vLo = vLo < MinConst ? MinConst : vLo > MaxConst ? MaxConst : vLo;

            int vHi = max.Raw + offsetMax;
            vHi = vHi < MinConst ? MinConst : vHi > MaxConst ? MaxConst : vHi;

         
            if (vLo > vHi)
            {
                int tmp = vLo;
                vLo = vHi;
                vHi = tmp;
            }


            int v = val.Raw;
            v = v < vLo ? vLo : v;   // max(v, vLo)
            v = v > vHi ? vHi : v;   // min(v, vHi)

            return new Fixed<TInt, TFrac>(v);      // valeur déjà dans la plage
        }

        #endregion

        /*==================================
         * --- FONCTIONS TRIGONOMETRIQUES ---
         * Sin
         * Cos
         * Tan
         * Asin
         * Acos
         * Atan
         * Atan2
         ==================================*/
        #region --- FONCTIONS TRIGONOMÉTRIQUES ---

        public static Fixed<TInt, TFrac> Sin(Fixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Sin(angle));
        public static Fixed<TInt, TFrac> Sin(IntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Sin(angle));
        public static Fixed<TInt, TFrac> Sin(UIntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Sin(angle));
        public static Fixed<TInt, TFrac> Sin(UFixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Sin(angle));


        public static Fixed<TInt, TFrac> Cos(Fixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Cos(angle));
        public static Fixed<TInt, TFrac> Cos(IntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Cos(angle));
        public static Fixed<TInt, TFrac> Cos(UIntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Cos(angle));
        public static Fixed<TInt, TFrac> Cos(UFixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Cos(angle));


        public static Fixed<TInt, TFrac> Tan(Fixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Tan(angle));
        public static Fixed<TInt, TFrac> Tan(IntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Tan(angle));
        public static Fixed<TInt, TFrac> Tan(UIntN<TInt> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Tan(angle));
        public static Fixed<TInt, TFrac> Tan(UFixed<TInt, TFrac> angle)
            => new Fixed<TInt, TFrac>(FixedMath.Tan(angle));



        // Fonctions inverses (retournent aussi un Fixed)
        public static Fixed<TInt, TFrac> Asin(Fixed<TInt, TFrac> val)
            => new Fixed<TInt, TFrac>(FixedMath.Asin(val));



        public static Fixed<TInt, TFrac> Acos(Fixed<TInt, TFrac> val)
            => new Fixed<TInt, TFrac>(FixedMath.Acos(val));


        public static Fixed<TInt, TFrac> Atan(Fixed<TInt, TFrac> val)
            => new Fixed<TInt, TFrac>(FixedMath.Atan(val));


        public static Fixed<TInt, TFrac> Atan2(Fixed<TInt, TFrac> y, Fixed<TInt, TFrac> x)
            => new Fixed<TInt, TFrac>(FixedMath.Atan2(y, x));

        #endregion

        /*==================================
         * --- MANIPULATION BITS ET ROTATIONS ---
         * Reverse
         * PopCount
         * Parity
         * LeadingZeros
         * TrailingZeros
         * Rol (rotate left)
         * Ror (rotate right)
         * Bsr (bit scan reverse)
         * Bsf (bit scan forward)
         ==================================*/
        #region --- MANIPULATION BITS ET ROTATIONS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Reverse(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            uint r = 0;
            for (int i = 0; i < IntBitsConst; i++)
            {
                r <<= 1;
                r |= v & 1;
                v >>= 1;
            }
            return new Fixed<TInt, TFrac>((int)r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            int count = 0;
            for (int i = 0; i < IntBitsConst; i++)
            {
                count += (int)(v & 1);
                v >>= 1;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Parity(Fixed<TInt, TFrac> a)
        {
            return (PopCount(a) & 1) != 0; // true = impair, false = pair
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeros(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            int count = 0;
            for (int i = IntBitsConst - 1; i >= 0; i--)
            {
                if ((v & 1u << i) == 0)
                    count++;
                else
                    break;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeros(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            int count = 0;
            for (int i = 0; i < IntBitsConst; i++)
            {
                if ((v & 1u << i) == 0)
                    count++;
                else
                    break;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Rol(Fixed<TInt, TFrac> a, int n)
        {
            int width = IntBitsConst;                         // 8, 16, 24, 32…
            uint mask = width == 32 ? 0xFFFF_FFFFu            // masque largeur réelle
                                     : (1u << width) - 1;

            n = (n % width + width) % width;                // wrap même pour n négatif
            uint v = (uint)a.Raw & mask;                    // *** on masque AVANT de shifter ***

            uint res = (v << n | v >> width - n) & mask;

            // wrap signé sur width bits
            int signed = (int)(res << 32 - width >> 32 - width);

            return new Fixed<TInt, TFrac>(signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Ror(Fixed<TInt, TFrac> a, int n)
        {
            int width = IntBitsConst;
            uint mask = width == 32 ? 0xFFFF_FFFFu : (1u << width) - 1;

            n = (n % width + width) % width;
            uint v = (uint)a.Raw & mask;                    // masque avant rotation

            uint res = (v >> n | v << width - n) & mask;
            int signed = (int)(res << 32 - width >> 32 - width);

            return new Fixed<TInt, TFrac>(signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsr(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            for (int i = IntBitsConst - 1; i >= 0; i--)
                if ((v & 1u << i) != 0)
                    return i;
            return -1; // Aucun bit à 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsf(Fixed<TInt, TFrac> a)
        {
            uint v = (uint)a.Raw;
            for (int i = 0; i < IntBitsConst; i++)
                if ((v & 1u << i) != 0)
                    return i;
            return -1; // Aucun bit à 1
        }

        #endregion

        /*==================================
         * --- CONSTANTES ---
         * Zero
         * Half
         * AllOnes
         * Msb
         * Lsb
         * Bit
         * Fraction
         ==================================*/
        #region --- CONSTANTES ---

        /// <summary>
        /// Valeur zéro (0.0)
        /// </summary>
        public static Fixed<TInt, TFrac> Zero => new Fixed<TInt, TFrac>(0);

        /// <summary>
        /// Valeur entière 1 (1.0)
        /// </summary>
        public static Fixed<TInt, TFrac> One => new Fixed<TInt, TFrac>(1 << FracBitsConst);

        /// <summary>
        /// Valeur moitié (0.5)
        /// </summary>
        public static Fixed<TInt, TFrac> Half => new Fixed<TInt, TFrac>(1 << FracBitsConst - 1);

        /// <summary>
        /// Tous les bits à 1 (utile pour du debug ou du masking, -1 en signed)
        /// </summary>
        public static Fixed<TInt, TFrac> AllOnes => new Fixed<TInt, TFrac>(unchecked((int)Mask.MASKS[IntBitsConst]));

        /// <summary>
        /// Bit de signe (utile pour détecter rapidement les négatifs)
        /// </summary>
        public static Fixed<TInt, TFrac> Msb => new Fixed<TInt, TFrac>(unchecked((int)Mask.SIGN_BITS[IntBitsConst]));

        /// <summary>
        /// LSB du format fixed-point (bit 0, valeur la plus petite positive non nulle)
        /// </summary>
        public static Fixed<TInt, TFrac> Lsb => new Fixed<TInt, TFrac>(1);

        /// <summary>
        /// Renvoie la valeur de 2^n, exprimée en fixed (utile pour tables ou packing)
        /// </summary>
        public static Fixed<TInt, TFrac> Bit(int n)
        {
            if (n < 0 || n >= IntBitsConst)
                throw new ArgumentOutOfRangeException(nameof(n), "n doit être dans [0, 31]");
            return new Fixed<TInt, TFrac>(1 << n);
        }

        public static Fixed<TInt, TFrac> Fraction(IntN<TInt> numer, IntN<TInt> denom)
        {
            if (denom.Raw == 0)
                throw new DivideByZeroException();

            int fracBits = BitsOf<TFrac>.Value;
            // On fait le cast en long pour éviter tout overflow lors du shift (numer peut être négatif)
            int result = (int)(((long)numer.Raw << fracBits) / denom.Raw);
            return new Fixed<TInt, TFrac>(result);
        }

        #endregion

        /*==================================
         * --- ACCES OCTETS ---
         * Byte (static)
         * ToBytes
         * FromBytes
         * GetByte
         * SetByte
         * ReplaceByte
         ==================================*/
        #region --- ACCES OCTETS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Byte(Fixed<TInt, TFrac> a, int n)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"n doit être entre 0 et {byteCount - 1} pour Fixed<{typeof(TInt).Name}>");
            return (byte)(a.Raw >> n * 8 & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            int byteCount = (IntBitsConst + 7) / 8;
            byte[] bytes = new byte[byteCount];
            uint v = (uint)Raw;
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)(v & 0xFF);
                v >>= 8;
            }
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> FromBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            int byteCount = (IntBitsConst + 7) / 8;
            // EXIGE exactement byteCount octets (ex : 2 pour Q8.8)
            if (bytes.Length != byteCount)
                throw new ArgumentException(
                    $"Le tableau d'octets doit contenir exactement {byteCount} éléments pour Fixed<{typeof(TInt).Name}>");

            uint v = 0;
            for (int i = 0; i < byteCount; i++)
                v |= (uint)bytes[i] << 8 * i;

            return new Fixed<TInt, TFrac>((int)v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int index)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (index < 0 || index >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"index doit être entre 0 et {byteCount - 1} pour Fixed<{typeof(TInt).Name}>");
            return (byte)(Raw >> index * 8 & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> SetByte(int n, byte b)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour Fixed<{typeof(TInt).Name}>");
            int mask = ~(0xFF << n * 8);
            int v = Raw & mask | b << n * 8;
            return new Fixed<TInt, TFrac>(v);
        }

        /// <summary>
        /// Remplace l’octet d’index n par celui de la même position dans source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed<TInt, TFrac> ReplaceByte(int n, Fixed<TInt, TFrac> source)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"Octet n doit être dans [0,{byteCount - 1}] pour Fixed<{typeof(TInt).Name}>");

            // On prend TOUJOURS l’octet 0 du source (LSB) …
            byte b = source.GetByte(0);

            // … et on le dépose dans l’octet n de la cible
            return SetByte(n, b);
        }

        public Fixed<TInt, TFrac> ReplaceByte(int n, byte value) => SetByte(n, value);

        #endregion

        /*==================================
         * --- CONVERSION EN CHAÎNE (STRING) ---
         * ToString
         * DebugString
         * ToBinaryString
         * ToHexString
         ==================================*/
        #region --- CONVERSION EN CHAÎNE (STRING) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return _raw.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DebugString()
        {
            int bits = IntBitsConst;
            uint uval = (uint)_raw.Raw & Mask.MASKS[bits];
            string bin = Convert.ToString(uval, 2).PadLeft(bits, '0');
            string hex = uval.ToString("X" + (bits + 3) / 4);
            return $"Fixed<{typeof(TInt).Name}, {typeof(TFrac).Name}>({_raw.Raw}) [bin={bin} hex={hex}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToBinaryString()
        {
            int bits = IntBitsConst; // par exemple 16 pour B16
            uint v = (uint)Raw & (1u << bits) - 1; // mask sur la vraie largeur
            return Convert.ToString(v, 2).PadLeft(bits, '0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToHexString(bool withPrefix = false)
        {
            int bits = IntBitsConst;
            int byteCount = (bits + 7) / 8;
            uint mask = (1u << bits) - 1;
            uint v = (uint)Raw & mask;
            string hex = v.ToString($"X{byteCount * 2}");
            return withPrefix ? $"0x{hex}" : hex;
        }

        #endregion

        /*==================================
         * --- PARSING ---
         * Parse
         * TryParse
         * ParseHex
         * TryParseHex
         * ParseBinary
         * TryParseBinary
         * ToJson
         * FromJson
         * ToJsonWithMeta
         * FromJsonWithMeta
         ==================================*/
        #region --- PARSING (exhaustif, JSON, HEX, BINAIRE) ---

        /// <summary>
        /// Tente de parser une chaîne décimale en Fixed&lt;TInt, TFrac&gt;.
        /// </summary>
        public static bool TryParse(string s, out Fixed<TInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return TryParseHex(s, out result);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return TryParseBinary(s, out result);

            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fval))
            {
                Console.WriteLine($"DEBUG: s='{s}' -> fval={fval}");
                if ((s.Contains("e") || s.Contains("E")) && fval == 0f)
                    return false; // Surtout pas de throw dans TryParse !
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                {
                    Console.WriteLine("DEBUG: Infinity/NaN branch hit for s=" + s);
                    return false; // Surtout pas de throw dans TryParse !
                }
                Console.WriteLine("DEBUG: Parse returns RAW conversion for s=" + s);
                result = new Fixed<TInt, TFrac>(fval);
                return true;
            }
            if (int.TryParse(s, out var ival)) { result = new Fixed<TInt, TFrac>(ival); return true; }
            return false;
        }


        /// <summary>
        /// Parse une chaîne décimale en Fixed&lt;TInt, TFrac&gt;. Throw si invalide.
        /// </summary>
        public static Fixed<TInt, TFrac> Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour Parse.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return ParseHex(s);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return ParseBinary(s);

            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fval))
            {
                Console.WriteLine($"DEBUG: s='{s}' -> fval={fval}");
                if ((s.Contains("e") || s.Contains("E")) && fval == 0f)
                    throw new OverflowException($"Sous-flux float détecté pour '{s}' (Fixed<{typeof(TInt).Name},{typeof(TFrac).Name}>).");
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                {
                    Console.WriteLine("DEBUG: Infinity/NaN branch hit for s=" + s);
                    throw new FormatException($"Impossible de parser '{s}' comme Fixed<{typeof(TInt).Name},{typeof(TFrac).Name}> (valeur float invalide)");
                }
                Console.WriteLine("DEBUG: Parse returns RAW conversion for s=" + s);
                return new Fixed<TInt, TFrac>(fval);
            }
            if (int.TryParse(s, out var ival)) return new Fixed<TInt, TFrac>(ival);
            throw new FormatException($"Impossible de parser '{s}' comme Fixed<{typeof(TInt).Name},{typeof(TFrac).Name}>.");
        }


        /// <summary>
        /// Tente de parser une chaîne hexadécimale ("0x...." ou "....") en Fixed&lt;TInt, TFrac&gt;.
        /// </summary>
        public static bool TryParseHex(string s, out Fixed<TInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            {
                result = new Fixed<TInt, TFrac>(v);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parse une chaîne hexadécimale ("0x...." ou "....") en Fixed&lt;TInt, TFrac&gt;.
        /// </summary>
        public static Fixed<TInt, TFrac> ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseHex.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Préfixe binaire non valide pour hexadécimal.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe hex.");
            return new Fixed<TInt, TFrac>(int.Parse(s, System.Globalization.NumberStyles.HexNumber));
        }

        /// <summary>
        /// Tente de parser une chaîne binaire ("0b...." ou "....") en Fixed&lt;TInt, TFrac&gt;.
        /// </summary>
        public static bool TryParseBinary(string s, out Fixed<TInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            try
            {
                int v = Convert.ToInt32(s, 2);
                result = new Fixed<TInt, TFrac>(v);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Parse une chaîne binaire ("0b...." ou "....") en Fixed&lt;TInt, TFrac&gt;.
        /// </summary>
        public static Fixed<TInt, TFrac> ParseBinary(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseBinary.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe binaire.");
            return new Fixed<TInt, TFrac>(Convert.ToInt32(s, 2));
        }

        /// <summary>
        /// Sérialise en JSON natif (raw int value).
        /// </summary>
        public string ToJson() => Raw.ToString();

        /// <summary>
        /// Désérialise depuis un champ JSON (attendu int, string, ou hex/binaire).
        /// </summary>
        public static Fixed<TInt, TFrac> FromJson(string s)
        {
            // Cas raw direct (interop ToJson)
            if (int.TryParse(s, out var raw))
                return new Fixed<TInt, TFrac>(raw);

            // Sinon, parcours classique
            if (TryParse(s, out var v)) return v;
            if (TryParseHex(s, out v)) return v;
            if (TryParseBinary(s, out v)) return v;
            throw new FormatException($"Impossible de parser '{s}' comme Fixed<{typeof(TInt).Name},{typeof(TFrac).Name}> (décimal, hex, ou binaire)");
        }

        #endregion

        /*==================================
         * --- SERIALISATION META ---
         * ToJsonWithMeta
         * FromJsonWithMeta
         ==================================*/
        #region --- SERIALISATION META ---
        public string ToJsonWithMeta()
        {
            return $"{{\"intBits\":{BitsOf<TInt>.Value},\"fracBits\":{BitsOf<TFrac>.Value},\"raw\":{Raw}}}";
        }

        public static Fixed<TA, TB> FromJsonWithMeta<TA, TB>(string json)
            where TA : struct
            where TB : struct
        {
            if (json == null)
                throw new FormatException("JSON meta cannot be null.");

            // 1. Recherche rapide des trois champs attendus
            //    Format attendu : {"intBits":XX,"fracBits":YY,"raw":ZZ}
            int intBitsPos = json.IndexOf("\"intBits\":", StringComparison.Ordinal);
            int fracBitsPos = json.IndexOf("\"fracBits\":", StringComparison.Ordinal);
            int rawPos = json.IndexOf("\"raw\":", StringComparison.Ordinal);
            if (intBitsPos < 0 || fracBitsPos < 0 || rawPos < 0)
                throw new FormatException("JSON meta invalide : champs 'intBits', 'fracBits' ou 'raw' manquants.");

            // 2. Extraction des valeurs (sans créer de sous-chaînes)
            int intBits = ParseIntAfterColon(json, intBitsPos + 10);   // après "intBits":
            int fracBits = ParseIntAfterColon(json, fracBitsPos + 11);  // après "fracBits":
            int raw = ParseIntAfterColon(json, rawPos + 6);        // après "raw":

            // 3. Validation des bits
            if (intBits != BitsOf<TA>.Value)
                throw new FormatException(
                    $"Meta-intBits ({intBits}) ≠ type générique {typeof(TA).Name} ({BitsOf<TA>.Value})");
            if (fracBits != BitsOf<TB>.Value)
                throw new FormatException(
                    $"Meta-fracBits ({fracBits}) ≠ type générique {typeof(TB).Name} ({BitsOf<TB>.Value})");

            return Fixed<TA, TB>.FromRaw(raw);
        }

        /* ---------- Helper int (bits/raw) ---------- */
        private static int ParseIntAfterColon(string s, int start)
        {
            int i = start;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;

            int sign = 1;
            if (i < s.Length && s[i] == '-')
            {
                sign = -1; i++;
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }
            else if (i < s.Length && s[i] == '+')
            {
                i++;
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            if (i >= s.Length || !char.IsDigit(s[i]))
                throw new FormatException("Aucun chiffre trouvé après le champ.");

            int value = 0;
            while (i < s.Length && char.IsDigit(s[i]))
            {
                int d = s[i] - '0';
                if (value > (int.MaxValue - d) / 10)
                    throw new FormatException("Valeur numérique trop grande.");
                value = value * 10 + d;
                i++;
            }
            return value * sign;
        }
        #endregion


    }


}
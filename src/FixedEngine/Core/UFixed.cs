using FixedEngine.Math;
using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Core
{

    public readonly struct UFixed<TUInt, TFrac>
        where TUInt : struct // Doit être UIntN<TBits>
        where TFrac : struct // Tag bits fractionnaires (B0-B32)
    {

        public static readonly int FracBitsConst = BitsOf<TFrac>.Value;
        public static readonly int IntBitsConst = BitsOf<TUInt>.Value;
        private static readonly uint ScaleConst = 1u << FracBitsConst;
        private static readonly uint MaxConst = Mask.UNSIGNED_MAX[IntBitsConst];

        public static readonly UFixed<TUInt, TFrac> MinValue = new UFixed<TUInt, TFrac>(UIntN<TUInt>.MinValue);
        public static readonly UFixed<TUInt, TFrac> MaxValue = new UFixed<TUInt, TFrac>(UIntN<TUInt>.MaxValue);

        public static readonly UFixed<TUInt, TFrac> Epsilon = FromRaw(1u);
        public static readonly int ByteSize = sizeof(uint); // Q8.8, Q16.16, etc.


        private readonly UIntN<TUInt> _raw;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed(uint raw) => _raw = new UIntN<TUInt>(raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed(UIntN<TUInt> integer)
        {
            _raw = new UIntN<TUInt>(integer.Raw << FracBitsConst); // wrap garanti par le constructeur
        }

        public UFixed(float value)
        {
            uint raw = (uint)System.Math.Round(value * ScaleConst);
            _raw = new UIntN<TUInt>(raw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed(double value)
        {
            double scaled = System.Math.Round(value * ScaleConst);
            uint raw = (uint)scaled;
            _raw = new UIntN<TUInt>(raw);
        }

        public uint Raw => _raw.Raw;
        public static UFixed<TUInt, TFrac> FromRaw(uint raw) => new UFixed<TUInt, TFrac>(raw);
        public static float ToFloat(UFixed<TUInt, TFrac> value)
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

        // float <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(UFixed<TUInt, TFrac> x) => x._raw.Raw / (float)(1 << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(float x) => new UFixed<TUInt, TFrac>(x);

        // double <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(UFixed<TUInt, TFrac> x) => x._raw.Raw / (double)(1 << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(double x) => new UFixed<TUInt, TFrac>(x);

        // uint <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(UFixed<TUInt, TFrac> x) => x._raw.Raw >> FracBitsConst;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(uint x) => new UFixed<TUInt, TFrac>(x << FracBitsConst);

        // UIntN <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(UIntN<TUInt> x) => new UFixed<TUInt, TFrac>((uint)x << FracBitsConst);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TUInt>(UFixed<TUInt, TFrac> x) => new UIntN<TUInt>(x._raw.Raw >> FracBitsConst);

        // int <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(int x) => new UFixed<TUInt, TFrac>((uint)(x << FracBitsConst));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(UFixed<TUInt, TFrac> x) => (int)(x._raw >> FracBitsConst);

        // UFixed <-> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TOtherFrac> ConvertFrac<TOtherFrac>(UFixed<TUInt, TFrac> x)
            where TOtherFrac : struct
        {
            int shift = BitsOf<TFrac>.Value - BitsOf<TOtherFrac>.Value;
            if (shift == 0) return new UFixed<TUInt, TOtherFrac>(x.Raw);
            else if (shift > 0) return new UFixed<TUInt, TOtherFrac>(x.Raw >> shift);
            else return new UFixed<TUInt, TOtherFrac>(x.Raw << -shift);
        }

        // Fixed -> UFixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UFixed<TUInt, TFrac>(Fixed<TUInt, TFrac> x)
            => new UFixed<TUInt, TFrac>((uint)x.Raw);



        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMETIQUES ---

        private static uint Wrap(uint value) => value & Mask.MASKS[BitsOf<TUInt>.Value];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator +(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap(a._raw.Raw + b._raw.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator -(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap(a._raw.Raw - b._raw.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator *(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap((uint)((ulong)a._raw * (ulong)b._raw >> FracBitsConst)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator /(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
        {
            if (b._raw.Raw == 0)
                throw new DivideByZeroException($"Division par zéro dans UFixed<{typeof(TUInt).Name}, {typeof(TFrac).Name}>");
            return new UFixed<TUInt, TFrac>(Wrap((uint)(((ulong)a._raw.Raw << FracBitsConst) / b._raw.Raw)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator %(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
        {
            if (b._raw.Raw == 0)
                throw new DivideByZeroException($"Modulo par zéro dans UFixed<{typeof(TUInt).Name}, {typeof(TFrac).Name}>");
            return new UFixed<TUInt, TFrac>(Wrap(a._raw.Raw % b._raw.Raw));
        }

        // Incrémentation/décrémentation (plus petite unité fixed)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator ++(UFixed<TUInt, TFrac> x)
            => new UFixed<TUInt, TFrac>(Wrap(x._raw.Raw + 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator --(UFixed<TUInt, TFrac> x)
            => new UFixed<TUInt, TFrac>(Wrap(x._raw.Raw - 1));

        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES POUR ARITHMETIQUE ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Add(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a + b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Sub(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a - b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Mul(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a * b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Div(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Mod(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a % b;

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> MulPow2(UFixed<TUInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour MulPow2");

            if (n == 0)
                return a;

            unchecked
            {
                uint v = a._raw.Raw << n;
                if (IntBitsConst == 32)
                    return new UFixed<TUInt, TFrac>(v);

                return new UFixed<TUInt, TFrac>(v & Mask.MASKS[IntBitsConst]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> DivPow2(UFixed<TUInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour DivPow2");

            if (n == 0)
                return a;

            uint v = a._raw.Raw >> n;

            return IntBitsConst == 32
                ? new UFixed<TUInt, TFrac>(v)
                : new UFixed<TUInt, TFrac>(v & Mask.MASKS[IntBitsConst]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> ModPow2(UFixed<TUInt, TFrac> a, int n)
        {
            uint limit = (uint)IntBitsConst;
            if ((uint)n > limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit}] pour ModPow2");

            if (n == 0)
                return Zero;

            uint v = a._raw.Raw & Mask.MASKS[n];
            return new UFixed<TUInt, TFrac>(v); // wrap implicite dans le constructeur
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
        public static UFixed<TUInt, TFrac> operator &(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap(a.Raw & b.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator |(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap(a.Raw | b.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator ^(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>(Wrap(a.Raw ^ b.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> operator ~(UFixed<TUInt, TFrac> a)
            => new UFixed<TUInt, TFrac>(Wrap(~a.Raw));

        public static UFixed<TUInt, TFrac> operator <<(UFixed<TUInt, TFrac> x, int shift)
        {
            if (shift < 0 || shift >= BitsOf<TUInt>.Value)
                throw new ArgumentOutOfRangeException(nameof(shift), $"Shift must be 0..{BitsOf<TUInt>.Value - 1}");
            return new UFixed<TUInt, TFrac>(x.Raw << shift);
        }

        public static UFixed<TUInt, TFrac> operator >>(UFixed<TUInt, TFrac> x, int shift)
        {
            if (shift < 0 || shift >= BitsOf<TUInt>.Value)
                throw new ArgumentOutOfRangeException(nameof(shift), $"Shift must be 0..{BitsOf<TUInt>.Value - 1}");
            return new UFixed<TUInt, TFrac>(x.Raw >> shift);
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
        public static UFixed<TUInt, TFrac> And(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a & b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Or(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a | b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Xor(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a ^ b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Not(UFixed<TUInt, TFrac> a) => ~a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Nand(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => ~(a & b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Nor(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => ~(a | b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Xnor(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => ~(a ^ b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Shl(UFixed<TUInt, TFrac> a, int n) => a << n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Shr(UFixed<TUInt, TFrac> a, int n) => a >> n;

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
         ==================================*/
        #region --- COMPARAISONS ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw == b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw != b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw < b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw <= b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw > b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a._raw >= b._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is UFixed<TUInt, TFrac> other && _raw == other._raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => (int)_raw;

        // Alias statiques pour code générique

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Eq(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a == b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Neq(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a != b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lt(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a < b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lte(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a <= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gt(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a > b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gte(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b) => a >= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZeroStatic(UFixed<TUInt, TFrac> a) => a._raw == UIntN<TUInt>.Zero;

        // Attention ici on ne peut pas inline. C’est déjà inliné par le JIT (car c’est une expression simple)
        public bool IsZero => _raw == UIntN<TUInt>.Zero;

        #endregion

        /*==================================
         * --- OPERATIONS UTILITAIRES ---
         * Min
         * Max
         * Avg
         * IsPowerOfTwo
         ==================================*/
        #region --- OPERATIONS UTILITAIRES ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Min(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => a < b ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Max(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => a > b ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Avg(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => new UFixed<TUInt, TFrac>((a._raw.Raw + b._raw.Raw) / 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(UFixed<TUInt, TFrac> x)
            => UIntN<TUInt>.IsPowerOfTwo(x._raw);

        #endregion

        /*==================================
         * --- FLOOR, CEIL, ROUND ---
         * Floor
         * Ceil
         * Round
         ==================================*/
        #region --- FLOOR, CEIL, ROUND ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Floor(UFixed<TUInt, TFrac> x)
            => new UFixed<TUInt, TFrac>(x.Raw & ~((1u << FracBitsConst) - 1u));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Ceil(UFixed<TUInt, TFrac> x)
        {
            uint mask = (1u << FracBitsConst) - 1u;
            uint raw = x.Raw;
            if ((raw & mask) == 0u)
                return new UFixed<TUInt, TFrac>(raw);
            uint fracAdd = (1u << FracBitsConst) - (raw & mask);
            return new UFixed<TUInt, TFrac>(raw + fracAdd & ~mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Round(UFixed<TUInt, TFrac> x)
        {
            uint half = 1u << FracBitsConst - 1;
            uint rounded = x.Raw + half & ~((1u << FracBitsConst) - 1u);
            return new UFixed<TUInt, TFrac>(rounded);
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
        public static UFixed<TUInt, TFrac> AddSat(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
        {
            uint sum = a.Raw + b.Raw;                 // addition pleine largeur (32 bits)
                                                      // branch‑less : si sum > MaxConst on force MaxConst, sinon on garde sum
            sum = sum > MaxConst ? MaxConst : sum;  // RyuJIT ⇒ CMP + CMOV, zéro saut
            return new UFixed<TUInt, TFrac>(sum);     // ctor wrap, déjà masqué
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> SubSat(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
        {
            uint diff = a.Raw - b.Raw;               // soustraction wrap 32 bits
            diff = a.Raw < b.Raw ? 0u : diff;      // si underflow → 0  (CMP+CMOV, zéro saut)
            return new UFixed<TUInt, TFrac>(diff);   // ctor = valeur déjà masquée
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> MulSat(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
        {
            /* 1. Produit 64 bits pour ne jamais wrap, puis décalage
                  de normalisation (>> FracBitsConst). */
            ulong prod64 = (ulong)a.Raw * b.Raw;
            uint res = (uint)(prod64 >> FracBitsConst);

            /* 2. Clamp branch‑less : CMP + CMOV émis par RyuJIT */
            res = res > MaxConst ? MaxConst : res;

            /* 3. Ctor wrap (déjà masqué) */
            return new UFixed<TUInt, TFrac>(res);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Clamp(
            UFixed<TUInt, TFrac> val,
            UFixed<TUInt, TFrac> min,
            UFixed<TUInt, TFrac> max)
        {
            uint v = val.Raw;

            // Deux ternaires : RyuJIT génère CMP + CMOV, pas de branche
            v = v < min.Raw ? min.Raw : v;   // max(v, min)
            v = v > max.Raw ? max.Raw : v;   // min(v, max)

            return new UFixed<TUInt, TFrac>(v);   // valeur déjà dans 0..MaxConst
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Clamp01(UFixed<TUInt, TFrac> val)
        {
            uint v = val.Raw;                     // unsigned ⇒ jamais < 0
            uint one = 1u << FracBitsConst;         // 1.0 en Q‑format

            // min(v, 1) – RyuJIT ⇒ CMP + CMOV, zéro branche
            v = v > one ? one : v;

            return new UFixed<TUInt, TFrac>(v);     // déjà masqué
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> ClampWithOffset(
            UFixed<TUInt, TFrac> val,
            UFixed<TUInt, TFrac> min,
            UFixed<TUInt, TFrac> max,
            uint offsetMin,
            uint offsetMax)
        {

            uint vLo = min.Raw + offsetMin & MaxConst;
            uint vHi = max.Raw + offsetMax & MaxConst;
            uint v = val.Raw & MaxConst;

            v = v < vLo ? vLo : v;   // max(v, vLo)
            v = v > vHi ? vHi : v;   // min(v, vHi)

            return new UFixed<TUInt, TFrac>(v);
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
        #region --- FONCTIONS TRIGONOMETRIQUES ---

        public static Fixed<TUInt, TFrac> Sin(UFixed<TUInt, TFrac> angle)
            => new Fixed<TUInt, TFrac>(FixedMath.Sin(angle));
        public static Fixed<TUInt, TFrac> Cos(UFixed<TUInt, TFrac> angle)
            => new Fixed<TUInt, TFrac>(FixedMath.Cos(angle));
        public static Fixed<TUInt, TFrac> Tan(UFixed<TUInt, TFrac> angle)
            => new Fixed<TUInt, TFrac>(FixedMath.Tan(angle));

        // Inverses
        public static Fixed<TUInt, TFrac> Asin(UFixed<TUInt, TFrac> val)
            => new Fixed<TUInt, TFrac>(FixedMath.Asin(val));
        public static UFixed<TUInt, TFrac> Acos(UFixed<TUInt, TFrac> val)
            => new UFixed<TUInt, TFrac>(FixedMath.Acos(val));
        public static UFixed<TUInt, TFrac> Atan(UFixed<TUInt, TFrac> val)
            => new UFixed<TUInt, TFrac>(FixedMath.Atan(val));
        public static UFixed<TUInt, TFrac> Atan2(UFixed<TUInt, TFrac> y, UFixed<TUInt, TFrac> x)
            => new UFixed<TUInt, TFrac>(FixedMath.Atan2(y, x));

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
        public static UFixed<TUInt, TFrac> Reverse(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
            uint r = 0;
            for (int i = 0; i < IntBitsConst; i++)
            {
                r <<= 1;
                r |= v & 1;
                v >>= 1;
            }
            return new UFixed<TUInt, TFrac>(r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
            int count = 0;
            for (int i = 0; i < IntBitsConst; i++)
            {
                count += (int)(v & 1);
                v >>= 1;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Parity(UFixed<TUInt, TFrac> a)
        {
            return (PopCount(a) & 1) != 0; // true = impair, false = pair
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeros(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
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
        public static int TrailingZeros(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
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
        public static UFixed<TUInt, TFrac> Rol(UFixed<TUInt, TFrac> a, int n)
        {
            n = n % IntBitsConst;
            uint v = a.Raw;
            uint res = (v << n | v >> IntBitsConst - n) & Mask.MASKS[IntBitsConst];
            return new UFixed<TUInt, TFrac>(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Ror(UFixed<TUInt, TFrac> a, int n)
        {
            n = n % IntBitsConst;
            uint v = a.Raw;
            uint res = (v >> n | v << IntBitsConst - n) & Mask.MASKS[IntBitsConst];
            return new UFixed<TUInt, TFrac>(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsr(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
            for (int i = IntBitsConst - 1; i >= 0; i--)
                if ((v & 1u << i) != 0)
                    return i;
            return -1; // Aucun bit à 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsf(UFixed<TUInt, TFrac> a)
        {
            uint v = a.Raw;
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
        public static UFixed<TUInt, TFrac> Zero => new UFixed<TUInt, TFrac>(UIntN<TUInt>.Zero);

        /// <summary>
        /// Valeur entière 1 (1.0)
        /// </summary>
        public static UFixed<TUInt, TFrac> One => new UFixed<TUInt, TFrac>(1u << FracBitsConst);

        /// <summary>
        /// Valeur moitié (0.5)
        /// </summary>
        public static UFixed<TUInt, TFrac> Half => new UFixed<TUInt, TFrac>(1u << FracBitsConst - 1);

        /// <summary>
        /// Tous les bits à 1 (utile pour debug/masking, max unsigned)
        /// </summary>
        public static UFixed<TUInt, TFrac> AllOnes => new UFixed<TUInt, TFrac>(Mask.MASKS[IntBitsConst]);

        /// <summary>
        /// Bit le plus significatif (utile pour le debug)
        /// </summary>
        public static UFixed<TUInt, TFrac> Msb => new UFixed<TUInt, TFrac>(Mask.SIGN_BITS[IntBitsConst]);

        /// <summary>
        /// Renvoie le bit de poids faible (Least Significant Bit), c’est-à-dire 0x0001u pour un type 16 bits.
        /// </summary>
        public static UFixed<TUInt, TFrac> Lsb => new UFixed<TUInt, TFrac>(1u);

        /// <summary>
        /// Renvoie la valeur de 2^n, exprimée en fixed (utile pour tables/packing)
        /// </summary>
        public static UFixed<TUInt, TFrac> Bit(int n)
        {
            if (n < 0 || n >= IntBitsConst)
                throw new ArgumentOutOfRangeException(nameof(n), "n doit être dans [0, IntBitsConst-1]");
            return new UFixed<TUInt, TFrac>(1u << n);
        }

        public static UFixed<TUInt, TFrac> Fraction(IntN<TUInt> numer, IntN<TUInt> denom)
        {
            if (denom.Raw == 0)
                throw new DivideByZeroException();
            int fracBits = BitsOf<TFrac>.Value;
            uint raw = (uint)(((long)numer.Raw << fracBits) / denom.Raw);
            return new UFixed<TUInt, TFrac>(raw);
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
        public static byte Byte(UFixed<TUInt, TFrac> a, int n)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"n doit être entre 0 et {byteCount - 1} pour UFixed<{typeof(TUInt).Name}>");
            return (byte)(a.Raw >> n * 8 & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            int byteCount = (IntBitsConst + 7) / 8;
            byte[] bytes = new byte[byteCount];
            uint v = Raw;
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)(v & 0xFF);
                v >>= 8;
            }
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> FromBytes(byte[] bytes)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (bytes.Length < byteCount)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {byteCount} éléments pour UFixed<{typeof(TUInt).Name}>");
            uint v = 0;
            for (int i = 0; i < byteCount; i++)
                v |= (uint)bytes[i] << 8 * i;
            return new UFixed<TUInt, TFrac>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int index)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (index < 0 || index >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"index doit être entre 0 et {byteCount - 1} pour UFixed<{typeof(TUInt).Name}>");
            return (byte)(Raw >> index * 8 & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> SetByte(int n, byte b)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour UFixed<{typeof(TUInt).Name}>");
            uint mask = ~(0xFFu << n * 8);
            uint v = Raw & mask | (uint)b << n * 8;
            return new UFixed<TUInt, TFrac>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UFixed<TUInt, TFrac> ReplaceByte(int n, UFixed<TUInt, TFrac> source)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour UFixed<{typeof(TUInt).Name}>");
            byte b = source.GetByte(n);
            return SetByte(n, b);
        }

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
            return Raw.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DebugString()
        {
            int bits = IntBitsConst;
            uint uval = Raw & Mask.UNSIGNED_MAX[bits];
            string bin = Convert.ToString(uval, 2).PadLeft(bits, '0');
            string hex = uval.ToString("X" + (bits + 3) / 4);
            return $"UFixed<{typeof(TUInt).Name}, {typeof(TFrac).Name}>({Raw}) [bin={bin} hex={hex}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToBinaryString()
        {
            uint v = Raw;
            return Convert.ToString(v, 2).PadLeft(IntBitsConst, '0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToHexString(bool withPrefix = false)
        {
            int byteCount = (IntBitsConst + 7) / 8;
            uint v = Raw;
            string hex = v.ToString("X" + byteCount * 2); // "X4" pour 16 bits
            return withPrefix ? "0x" + hex : hex;
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
        /// Tente de parser une chaîne décimale en UFixed&lt;TUInt, TFrac&gt;.
        /// </summary>
        public static bool TryParse(string s, out UFixed<TUInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return TryParseHex(s, out result);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return TryParseBinary(s, out result);

            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fval))
            {
                if ((s.Contains("e") || s.Contains("E")) && fval == 0f)
                    return false;
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                    return false;
                result = new UFixed<TUInt, TFrac>(fval); // Bit-faithful : wrap natif
                return true;
            }
            if (int.TryParse(s, out var ival)) { result = new UFixed<TUInt, TFrac>(ival); return true; }
            if (uint.TryParse(s, out var uval)) { result = new UFixed<TUInt, TFrac>(uval); return true; }
            return false;
        }

        /// <summary>
        /// Parse une chaîne décimale en UFixed&lt;TUInt, TFrac&gt;. Throw si invalide.
        /// </summary>
        public static UFixed<TUInt, TFrac> Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour Parse.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return ParseHex(s);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return ParseBinary(s);

            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fval))
            {
                if ((s.Contains("e") || s.Contains("E")) && fval == 0f)
                    throw new OverflowException($"Sous-flux float détecté pour '{s}' (UFixed<{typeof(TUInt).Name},{typeof(TFrac).Name}>).");
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                    throw new FormatException($"Impossible de parser '{s}' comme UFixed<{typeof(TUInt).Name},{typeof(TFrac).Name}> (valeur float invalide)");
                return new UFixed<TUInt, TFrac>(fval); // Bit-faithful : wrap natif
            }
            if (int.TryParse(s, out var ival)) return new UFixed<TUInt, TFrac>(ival);
            if (uint.TryParse(s, out var uval)) return new UFixed<TUInt, TFrac>(uval);
            throw new FormatException($"Impossible de parser '{s}' comme UFixed<{typeof(TUInt).Name},{typeof(TFrac).Name}>.");
        }

        /// <summary>
        /// Tente de parser une chaîne hexadécimale ("0x...." ou "....") en UFixed&lt;TUInt, TFrac&gt;.
        /// </summary>
        public static bool TryParseHex(string s, out UFixed<TUInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            {
                result = new UFixed<TUInt, TFrac>(v);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parse une chaîne hexadécimale ("0x...." ou "....") en UFixed&lt;TUInt, TFrac&gt;.
        /// </summary>
        public static UFixed<TUInt, TFrac> ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseHex.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Préfixe binaire non valide pour hexadécimal.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe hex.");
            return new UFixed<TUInt, TFrac>(uint.Parse(s, System.Globalization.NumberStyles.HexNumber));
        }

        /// <summary>
        /// Tente de parser une chaîne binaire ("0b...." ou "....") en UFixed&lt;TUInt, TFrac&gt;.
        /// </summary>
        public static bool TryParseBinary(string s, out UFixed<TUInt, TFrac> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            try
            {
                uint v = Convert.ToUInt32(s, 2);
                result = new UFixed<TUInt, TFrac>(v);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Parse une chaîne binaire ("0b...." ou "....") en UFixed&lt;TUInt, TFrac&gt;.
        /// </summary>
        public static UFixed<TUInt, TFrac> ParseBinary(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseBinary.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe binaire.");
            return new UFixed<TUInt, TFrac>(Convert.ToUInt32(s, 2));
        }

        /// <summary>
        /// Sérialise en JSON natif (raw uint value).
        /// </summary>
        public string ToJson() => Raw.ToString();

        /// <summary>
        /// Désérialise depuis un champ JSON (attendu uint, string, ou hex/binaire).
        /// </summary>
        public static UFixed<TUInt, TFrac> FromJson(string s)
        {
            // Cas raw direct (interop ToJson)
            if (uint.TryParse(s, out var raw))
                return new UFixed<TUInt, TFrac>(raw);

            // Sinon, parcours classique
            if (TryParse(s, out var v)) return v;
            if (TryParseHex(s, out v)) return v;
            if (TryParseBinary(s, out v)) return v;
            throw new FormatException($"Impossible de parser '{s}' comme UFixed<{typeof(TUInt).Name},{typeof(TFrac).Name}> (décimal, hex, ou binaire)");
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
            return $"{{\"uintBits\":{BitsOf<TUInt>.Value},\"fracBits\":{BitsOf<TFrac>.Value},\"raw\":{Raw}}}";
        }

        public static UFixed<TA, TB> FromJsonWithMeta<TA, TB>(string json)
            where TA : struct
            where TB : struct
        {
            if (json == null)
                throw new FormatException("JSON meta cannot be null.");


            int uintBitsPos = json.IndexOf("\"uintBits\":", StringComparison.Ordinal);
            int fracBitsPos = json.IndexOf("\"fracBits\":", StringComparison.Ordinal);
            int rawPos = json.IndexOf("\"raw\":", StringComparison.Ordinal);
            if (uintBitsPos < 0 || fracBitsPos < 0 || rawPos < 0)
                throw new FormatException("JSON meta invalide : champs 'uintBits', 'fracBits' ou 'raw' manquants.");

            int uintBits = ParseIntAfterColon(json, uintBitsPos + 11);
            int fracBits = ParseIntAfterColon(json, fracBitsPos + 11);
            uint raw = ParseUIntAfterColon(json, rawPos + 6);

            // 3. Validation des bits
            if (uintBits != BitsOf<TA>.Value)
                throw new FormatException(
                    $"Meta-uintBits ({uintBits}) ≠ type générique {typeof(TA).Name} ({BitsOf<TA>.Value})");
            if (fracBits != BitsOf<TB>.Value)
                throw new FormatException(
                    $"Meta-fracBits ({fracBits}) ≠ type générique {typeof(TB).Name} ({BitsOf<TB>.Value})");

            return UFixed<TA, TB>.FromRaw(raw);
        }

        /* ---------- Helper int (bits) ---------- */
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

        /* ---------- Helper uint (raw) ---------- */
        private static uint ParseUIntAfterColon(string s, int start)
        {
            int i = start;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;

            if (i < s.Length && s[i] == '-')
                throw new FormatException("Valeur négative non autorisée pour UFixed raw.");
            if (i < s.Length && s[i] == '+')
            {
                i++;
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            if (i >= s.Length || !char.IsDigit(s[i]))
                throw new FormatException("Aucun chiffre trouvé après le champ.");

            uint value = 0u;
            while (i < s.Length && char.IsDigit(s[i]))
            {
                uint d = (uint)(s[i] - '0');
                if (value > (uint.MaxValue - d) / 10u)
                    throw new FormatException("Valeur numérique trop grande.");
                value = value * 10u + d;
                i++;
            }
            return value;
        }
        #endregion

        /// <summary>
        /// Retourne la différence absolue entre deux UFixed, branchless, sans wrap.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Delta(UFixed<TUInt, TFrac> a, UFixed<TUInt, TFrac> b)
            => a > b ? a - b : b - a;


    }
}

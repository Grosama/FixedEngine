using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{

    public readonly struct UIntN<TBits>
        where TBits : struct
    {

        private readonly uint _value;
        public static readonly int BitsConst = BitsOf<TBits>.Value;
        public static readonly uint MaskConst = Mask.MASKS[BitsConst];
        private static readonly uint MaxConst = Mask.UNSIGNED_MAX[BitsConst];

        private static readonly UIntN<TBits> ZeroVal = new UIntN<TBits>(0u, true);
        private static readonly UIntN<TBits> MaxVal = new UIntN<TBits>(MaskConst, true);

        public static readonly uint MinValue = 0u;
        public static readonly uint MaxValue = (1u << BitsConst) - 1u;

        public static readonly uint Epsilon = 1u; // Le plus petit incrément possible pour ce type (entier non signé)


        public uint Raw => _value;
        public static UIntN<TBits> FromRaw(uint raw) => new UIntN<TBits>(raw, true);

        // ctor « public » sécurisé : toujours masque
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UIntN(uint value)
        {
#if DEBUG
            if ((uint)BitsConst - 1u >= 32u) // test branch-free
                throw new ArgumentOutOfRangeException(nameof(BitsConst));
#endif
            _value = (BitsConst == 32) ? value : value & MaskConst;
        }

        public UIntN(int value) : this((uint)value) { }

        // ctor interne sans second masquage
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UIntN(uint raw, bool _) => _value = raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Wrap(uint v) =>
            (BitsConst == 32) ? v : v & MaskConst;


        public float ToFloat()
        {
            return (float)this.Raw;
        }

        public static float ToFloat(UIntN<TBits> value)
        {
            return (float)value.Raw;
        }
        /*==================================
         * --- CONVERSION EXPLICITES ---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSION EXPLICITES ---

        // uint <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(UIntN<TBits> x) => x._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(uint value) => new UIntN<TBits>(value);

        // int <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(int value) => new UIntN<TBits>((uint)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(UIntN<TBits> x) => (int)x._value;

        // float <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(UIntN<TBits> x) => (float)x._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(float x) => new UIntN<TBits>((uint)x);

        // double <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(UIntN<TBits> x) => (double)x._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(double x)
        {
            // Cas spéciaux IEEE
            if (double.IsNaN(x) || double.IsInfinity(x))
                return ZeroVal;                           // NaN / ±Inf → 0

            // Tronque la partie fractionnaire
            double t = System.Math.Truncate(x);

            uint raw;
            if (t > uint.MaxValue)
            {
                // SATURATION HAUT : 0xFFFF_FFFF
                raw = uint.MaxValue;
            }
            else if (t < int.MinValue)                   // <<  -2 147 483 648
            {
                // SATURATION BAS : cast direct nous donnerait 0 après wrap,
                // on force l'équivalent matériel (-2^31 → 0x80000000)
                raw = unchecked((uint)int.MinValue);
            }
            else
            {
                // Zone sûre : on peut caster directement
                raw = unchecked((uint)(int)t);
            }

            // Mask N bits (Wrap = &((1u<<Bits)-1))
            return new UIntN<TBits>(Wrap(raw));
        }


        // IntN <-> UIntN
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(IntN<TBits> x) => new UIntN<TBits>((uint)(int)x);*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TBits>(UIntN<TBits> x) => new IntN<TBits>((int)x._value);

        // UIntN <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBitsTo> ConvertTo<TBitsTo>(UIntN<TBits> x)
            where TBitsTo : struct
            => new UIntN<TBitsTo>(x._value);

        // Fixed <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TBits, TFrac> ToFixed<TFrac>(UIntN<TBits> x)
            where TFrac : struct
            => new Fixed<TBits, TFrac>((int)x._value << BitsOf<TFrac>.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> FromFixed<TFrac>(Fixed<TBits, TFrac> x)
            where TFrac : struct
            => new UIntN<TBits>((uint)(x.Raw >> BitsOf<TFrac>.Value));

        // UFixed <-> UIntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TBits, TFrac> ToUFixed<TFrac>(UIntN<TBits> x)
            where TFrac : struct
            => new UFixed<TBits, TFrac>(x._value << BitsOf<TFrac>.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> FromUFixed<TFrac>(UFixed<TBits, TFrac> x)
            where TFrac : struct
            => new UIntN<TBits>(x.Raw >> BitsOf<TFrac>.Value);
        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMETIQUES ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator +(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(Wrap(a._value + b._value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator -(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(Wrap(a._value - b._value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator *(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(Wrap(a._value * b._value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator /(UIntN<TBits> a, UIntN<TBits> b)
        {
            if (b._value == 0)
                throw new DivideByZeroException($"Division par zéro dans UIntN<{typeof(TBits).Name}>");
            return new UIntN<TBits>(a._value / b._value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator %(UIntN<TBits> a, UIntN<TBits> b)
        {
            if (b._value == 0)
                throw new DivideByZeroException($"Modulo par zéro dans UIntN<{typeof(TBits).Name}>");
            return new UIntN<TBits>(a._value % b._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator ++(UIntN<TBits> x) => new UIntN<TBits>(Wrap(x._value + 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator --(UIntN<TBits> x) => new UIntN<TBits>(Wrap(x._value - 1));
        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES POUR ARITHMETIQUE ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Add(UIntN<TBits> a, UIntN<TBits> b) => new UIntN<TBits>(a._value + b._value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Sub(UIntN<TBits> a, UIntN<TBits> b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Mul(UIntN<TBits> a, UIntN<TBits> b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Div(UIntN<TBits> a, UIntN<TBits> b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Mod(UIntN<TBits> a, UIntN<TBits> b) => a % b;
        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> MulPow2(UIntN<TBits> a, int n)
        {

            if ((uint)n >= (BitsConst == 32 ? 32u : (uint)BitsConst))
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0, {(BitsConst == 32 ? 31 : BitsConst - 1)}] pour MulPow2");

            if (n == 0)
                return a;

            unchecked
            {
                uint v = a._value << n;
                if (BitsConst == 32)
                    return new UIntN<TBits>(v);

                return new UIntN<TBits>(v & MaskConst);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> DivPow2(UIntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour DivPow2");

            if (n == 0)                // fast-path
                return a;

            uint v = a._value >> n;

            // BitsConst constant => la branche morte disparaît
            return (BitsConst == 32)
                ? new UIntN<TBits>(v, true)   // constructeur interne, aucun masque
                : new UIntN<TBits>(v);        // un seul masque (dans le ctor)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> ModPow2(UIntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n > limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit}] pour ModPow2");

            if (n == 0)                       // a % 1 == 0
                return UIntN<TBits>.Zero;

            uint v = a._value & Mask.MASKS[n];

            return new UIntN<TBits>(v);
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
        #region --- OPERATEUR BITWISE ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator &(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(a._value & b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator |(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(a._value | b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator ^(UIntN<TBits> a, UIntN<TBits> b)
            => new UIntN<TBits>(a._value ^ b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator ~(UIntN<TBits> a)
            => new UIntN<TBits>(~a._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator <<(UIntN<TBits> a, int n)
            => new UIntN<TBits>(a._value << n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> operator >>(UIntN<TBits> a, int n)
            => new UIntN<TBits>(a._value >> n);

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
        public static UIntN<TBits> And(UIntN<TBits> a, UIntN<TBits> b) => a & b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Or(UIntN<TBits> a, UIntN<TBits> b) => a | b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Xor(UIntN<TBits> a, UIntN<TBits> b) => a ^ b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Not(UIntN<TBits> a) => ~a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Nand(UIntN<TBits> a, UIntN<TBits> b) => ~(a & b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Nor(UIntN<TBits> a, UIntN<TBits> b) => ~(a | b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Xnor(UIntN<TBits> a, UIntN<TBits> b) => ~(a ^ b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Shl(UIntN<TBits> a, int n) => a << n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Shr(UIntN<TBits> a, int n) => a >> n;

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
        public static bool operator ==(UIntN<TBits> a, UIntN<TBits> b) => a._value == b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UIntN<TBits> a, UIntN<TBits> b) => a._value != b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UIntN<TBits> a, UIntN<TBits> b) => a._value < b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UIntN<TBits> a, UIntN<TBits> b) => a._value <= b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UIntN<TBits> a, UIntN<TBits> b) => a._value > b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UIntN<TBits> a, UIntN<TBits> b) => a._value >= b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is UIntN<TBits> other && this._value == other._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => typeof(TBits).GetHashCode() ^ (int)_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Eq(UIntN<TBits> a, UIntN<TBits> b) => a._value == b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Neq(UIntN<TBits> a, UIntN<TBits> b) => a._value != b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lt(UIntN<TBits> a, UIntN<TBits> b) => a._value < b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lte(UIntN<TBits> a, UIntN<TBits> b) => a._value <= b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gt(UIntN<TBits> a, UIntN<TBits> b) => a._value > b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gte(UIntN<TBits> a, UIntN<TBits> b) => a._value >= b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(UIntN<TBits> a) => a._value == 0;

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
        public static UIntN<TBits> Min(UIntN<TBits> a, UIntN<TBits> b)
            => a._value < b._value ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Max(UIntN<TBits> a, UIntN<TBits> b)
            => a._value > b._value ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Avg(UIntN<TBits> a, UIntN<TBits> b)
        {
            uint sum = (a._value + b._value) & Mask.MASKS[BitsConst];
            return new UIntN<TBits>(sum / 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(UIntN<TBits> a)
        {
            // 0 n'est pas une puissance de deux, test rapide bitwise
            return a._value != 0 && (a._value & (a._value - 1)) == 0;
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
        public static UIntN<TBits> AddSat(UIntN<TBits> a, UIntN<TBits> b)
        {
            uint sum = a._value + b._value;          // addition pleine largeur
                                                     // Saturation : si sum dépasse MaxConst on force MaxConst.
                                                     // RyuJIT émet   CMP / CMOV  => branch‑less.
            uint res = (sum <= MaxConst) ? sum : MaxConst;
            return new UIntN<TBits>(res, true);      // ctor interne = pas de re‑AND
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> SubSat(UIntN<TBits> a, UIntN<TBits> b)
        {
            uint diff = a._value - b._value;          // wrap mod 2^32
                                                      // Si a < b, on force 0, sinon on garde diff (CMOV branch‑less).
            diff = (a._value >= b._value) ? diff : 0u;
            return new UIntN<TBits>(diff, true);      // ctor interne : pas de re‑masquage
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> MulSat(UIntN<TBits> a, UIntN<TBits> b)
        {
            // Fast‑path : si BitsConst < 32 (cas SNES : 8‑16‑24)
            if (MaskConst != 0xFFFFFFFFu)
            {
                uint prod = a._value * b._value;            // 32‑bit MUL, une seule µop
                                                            // clamp branch‑less : CMOV émis par RyuJIT
                uint res = (prod <= MaskConst) ? prod : MaskConst;
                return new UIntN<TBits>(res, true);         // ctor interne, pas de re‑AND
            }

            // Fallback 32 bits plein registre : on garde le test 64‑bit
            ulong prod64 = (ulong)a._value * b._value;
            if (prod64 > MaskConst) prod64 = MaskConst;
            return new UIntN<TBits>((uint)prod64, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Clamp(UIntN<TBits> val, UIntN<TBits> min, UIntN<TBits> max)
        {
            uint v = val._value;
            v = (v < min._value) ? min._value : v;
            v = (v > max._value) ? max._value : v;
            return new UIntN<TBits>(v, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Clamp01(UIntN<TBits> val)
        {
            uint v = val._value;
            v = (v > 1u) ? 1u : v;        // RyuJIT → CMP + CMOV, zéro branch mis‑pred
            return new UIntN<TBits>(v, true);   // ctor interne = pas de AND final
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> ClampWithOffset(
            UIntN<TBits> val, UIntN<TBits> min, UIntN<TBits> max,
            int offsetMin, int offsetMax)
        {
            int v = (int)val._value;

            // borne basse ajustée puis clampée 0..MaskConst
            int vLo = (int)min._value + offsetMin;
            vLo = vLo < 0 ? 0 : (vLo > (int)MaskConst ? (int)MaskConst : vLo);

            // borne haute ajustée puis clampée
            int vHi = (int)max._value + offsetMax;
            vHi = vHi < 0 ? 0 : (vHi > (int)MaskConst ? (int)MaskConst : vHi);

            // Sélection branch‑less : si v < min  → vLo
            //                         si v > max  → vHi
            //                         sinon       → v
            int below = v < (int)min._value ? 1 : 0;
            int above = v > (int)max._value ? 1 : 0;

            int selLo = below * vLo;           // soit vLo, soit 0
            int selHi = above * vHi;           // soit vHi, soit 0
            int stay = (1 - below - above) * v; // soit v, soit 0

            int result = selLo + selHi + stay;  // une seule valeur non‑nulle

            return new UIntN<TBits>((uint)result, true);   // ctor interne : pas de re‑masque
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

        public static IntN<TBits> Sin(UIntN<TBits> angle)
            => new IntN<TBits>(FixedMath.Sin(angle));
        public static IntN<TBits> Cos(UIntN<TBits> angle)
            => new IntN<TBits>(FixedMath.Cos(angle));
        public static IntN<TBits> Tan(UIntN<TBits> angle)
            => new IntN<TBits>(FixedMath.Tan(angle));

        //Inverse
        public static IntN<TBits> Asin(UIntN<TBits> val)
            => new IntN<TBits>(FixedMath.Asin(val));
        public static UIntN<TBits> Acos(UIntN<TBits> val)
            => new UIntN<TBits>(FixedMath.Acos(val));
        public static UIntN<TBits> Atan(UIntN<TBits> val)
            => new UIntN<TBits>(FixedMath.Atan(val));
        public static UIntN<TBits> Atan2(UIntN<TBits> y, UIntN<TBits> x)
            => new UIntN<TBits>(FixedMath.Atan2(y, x));

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
        public static UIntN<TBits> Reverse(UIntN<TBits> a)
        {
            uint v = a._value;
            uint r = 0;
            for (int i = 0; i < BitsConst; i++)
            {
                r <<= 1;
                r |= (v & 1);
                v >>= 1;
            }
            return new UIntN<TBits>(r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(UIntN<TBits> a)
        {
            uint v = a._value;
            int count = 0;
            for (int i = 0; i < BitsConst; i++)
            {
                count += (int)(v & 1);
                v >>= 1;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Parity(UIntN<TBits> a)
        {
            return (PopCount(a) & 1) != 0; // true = impair, false = pair
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeros(UIntN<TBits> a)
        {
            uint v = a._value;
            int count = 0;
            for (int i = BitsConst - 1; i >= 0; i--)
            {
                if ((v & (1u << i)) == 0)
                    count++;
                else
                    break;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeros(UIntN<TBits> a)
        {
            uint v = a._value;
            int count = 0;
            for (int i = 0; i < BitsConst; i++)
            {
                if ((v & (1u << i)) == 0)
                    count++;
                else
                    break;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Rol(UIntN<TBits> a, int n)
        {
            n %= BitsConst;
            uint v = a._value;
            uint res = ((v << n) | (v >> (BitsConst - n))) & Mask.MASKS[BitsConst];
            return new UIntN<TBits>(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Ror(UIntN<TBits> a, int n)
        {

            n %= BitsConst;
            uint v = a._value;
            uint res = ((v >> n) | (v << (BitsConst - n))) & Mask.MASKS[BitsConst];
            return new UIntN<TBits>(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsr(UIntN<TBits> a)
        {
            uint v = a._value;
            for (int i = BitsConst - 1; i >= 0; i--)
                if ((v & (1u << i)) != 0)
                    return i;
            return -1; // Aucun bit à 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsf(UIntN<TBits> a)
        {
            uint v = a._value;
            for (int i = 0; i < BitsConst; i++)
                if ((v & (1u << i)) != 0)
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
         ==================================*/
        #region --- CONSTANTES ---

        public static UIntN<TBits> Zero => new UIntN<TBits>(0);

        public static UIntN<TBits> Half
        {
            get
            {
                uint max = Mask.UNSIGNED_MAX[BitsConst];
                return new UIntN<TBits>(max / 2);
            }
        }

        public static UIntN<TBits> One => new UIntN<TBits>(1);

        public static UIntN<TBits> AllOnes => new UIntN<TBits>(Mask.MASKS[BitsConst]);

        public static UIntN<TBits> Msb
        {
            get
            {
                //int bits = BitsConst;
                if (BitsConst == 0)
                    return new UIntN<TBits>(0);
                return new UIntN<TBits>(1u << (BitsConst - 1));
            }
        }

        public static UIntN<TBits> Lsb => new UIntN<TBits>(1u);

        public static UIntN<TBits> Bit(int n)
        {
            if (n < 0 || n >= BitsConst)
                throw new ArgumentOutOfRangeException(nameof(n), $"n doit être dans [0, BitsConst-1]");
            return new UIntN<TBits>(1u << n);
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
        public static byte Byte(UIntN<TBits> a, int n)
        {
            int byteCount = (UIntN<TBits>.BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"n doit être entre 0 et {byteCount - 1} pour UIntN<{typeof(TBits).Name}>");
            return (byte)((a._value >> (n * 8)) & Mask.MASKS[8]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            int byteCount = (BitsConst + 7) / 8;
            byte[] bytes = new byte[byteCount];
            uint v = _value;
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)(v & 0xFF);
                v >>= 8;
            }
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> FromBytes(byte[] bytes)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (bytes.Length < byteCount)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {byteCount} éléments pour UIntN<{typeof(TBits).Name}>");

            uint v = 0;
            for (int i = 0; i < byteCount; i++)
                v |= (uint)bytes[i] << (8 * i);

            return new UIntN<TBits>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int index)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (index < 0 || index >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"index hors limites : [0, {byteCount - 1}] pour UIntN<{typeof(TBits).Name}>");
            return (byte)((_value >> (index * 8)) & Mask.MASKS[8]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UIntN<TBits> SetByte(int n, byte b)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour UIntN<{typeof(TBits).Name}>");
            uint mask = ~(Mask.MASKS[8] << (n * 8));
            uint v = (_value & mask) | ((uint)b << (n * 8));
            return new UIntN<TBits>(v);
        }

        /// <summary>
        /// Remplace l’octet d’index n par celui de la même position dans source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UIntN<TBits> ReplaceByte(int n, UIntN<TBits> source)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour UIntN<{typeof(TBits).Name}>");
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
            return _value.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DebugString()
        {
            // Affiche la valeur unsigned, le nom du tag, et la représentation binaire/hex pour le debug
            int bits = BitsOf<TBits>.Value;
            uint uval = _value & Mask.MASKS[bits];
            string bin = Convert.ToString(uval, 2).PadLeft(bits, '0');
            int nibbles = (bits + 3) / 4;
            string hex = uval.ToString("X" + nibbles);
            return $"UIntN<{typeof(TBits).Name}>({_value}) [bin={bin} hex={hex}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToBinaryString()
        {
            int bits = BitsConst;
            uint v = _value & Mask.MASKS[bits];
            return Convert.ToString(v, 2).PadLeft(bits, '0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToHexString(bool withPrefix = false)
        {
            int bits = BitsConst;
            int nibbles = (bits + 3) / 4; // nombre de caractères hex nécessaires
            uint v = _value & Mask.MASKS[bits];
            var hex = v.ToString("X").PadLeft(nibbles, '0');
            return withPrefix ? "0x" + hex : hex;
        }
        #endregion

        /*==================================
         * --- PARSING ---
         * Parse
         * TryParse
         * ParseHex
         * ParseBinary
         * ParseJson
         * ToJson
         * FromJson
         * ToJsonWithMeta
         ==================================*/
        #region --- PARSING (exhaustif, JSON, HEX, BINAIRE) ---

        /// <summary>
        /// Tente de parser une chaîne décimale en UIntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParse(string s, out UIntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return TryParseHex(s, out result);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return TryParseBinary(s, out result);

            if (int.TryParse(s, out var ival)) { result = new UIntN<TBits>((uint)ival); return true; }
            if (uint.TryParse(s, out var uval)) { result = new UIntN<TBits>(uval); return true; }
            return false;
        }

        /// <summary>
        /// Parse une chaîne décimale en UIntN&lt;TBits&gt;. Throw si invalide.
        /// </summary>
        public static UIntN<TBits> Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour Parse.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ParseHex(s);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                return ParseBinary(s);

            // --- Acceptation du wrapping pour valeurs hors-plage ou négatives
            if (int.TryParse(s, out var ival))
                return new UIntN<TBits>((uint)ival); // wrape comme C rétro
            if (uint.TryParse(s, out var uval))
                return new UIntN<TBits>(uval);
            throw new FormatException($"Impossible de parser '{s}' comme UIntN<{typeof(TBits).Name}>.");
        }

        /// <summary>
        /// Tente de parser une chaîne hexadécimale ("0x...." ou "....") en UIntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParseHex(string s, out UIntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return false; // <--- Patch clé !
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false; // protège contre "0x" seul
            if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            {
                result = new UIntN<TBits>(v);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parse une chaîne hexadécimale ("0x...." ou "....") en UIntN&lt;TBits&gt;.
        /// </summary>
        public static UIntN<TBits> ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseHex.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Préfixe binaire non valide pour hexadécimal.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe hex.");
            return new UIntN<TBits>(uint.Parse(s, System.Globalization.NumberStyles.HexNumber));
        }

        /// <summary>
        /// Tente de parser une chaîne binaire ("0b...." ou "....") en UIntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParseBinary(string s, out UIntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false; // ← Cette ligne est cruciale !
            try
            {
                uint v = Convert.ToUInt32(s, 2);
                result = new UIntN<TBits>(v);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Parse une chaîne binaire ("0b...." ou "....") en UIntN&lt;TBits&gt;.
        /// </summary>
        public static UIntN<TBits> ParseBinary(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseBinary.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe binaire.");
            return new UIntN<TBits>(Convert.ToUInt32(s, 2));
        }

        /// <summary>
        /// Sérialise en JSON natif (uint value).
        /// </summary>
        public string ToJson() => _value.ToString();

        /// <summary>
        /// Désérialise depuis un champ JSON (attendu uint, string, ou hex/binaire).
        /// </summary>
        public static UIntN<TBits> FromJson(string s)
        {
            if (TryParse(s, out var v)) return v;
            if (TryParseHex(s, out v)) return v;
            if (TryParseBinary(s, out v)) return v;
            // Si rien n'a fonctionné, on lève explicitement :
            throw new FormatException($"Impossible de parser '{s}' comme UIntN<{typeof(TBits).Name}> (décimal, hex, ou binaire)");
        }

        #endregion

        /*==================================
         * --- SERIALISATION META ---
         * ToJsonWithMeta
         * FromJsonWithMeta
         ==================================*/
        #region --- SERIALISATION META (exhaustif, multi-N, erreurs) ---
        public string ToJsonWithMeta() 
            => $"{{ \"bits\": {BitsOf<TBits>.Value}, \"raw\": {Raw} }}";

        public static UIntN<T> FromJsonWithMeta<T>(string json) where T : struct
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                int bits = Convert.ToInt32(obj["bits"]);
                if (bits != BitsOf<T>.Value)
                    throw new Exception($"Meta-bits ({bits}) ne correspond pas au type générique {typeof(T).Name} ({BitsOf<T>.Value})");
                uint raw = Convert.ToUInt32(obj["raw"]);
                return UIntN<T>.FromRaw(raw);
            }
            catch (Exception ex)
            {
                throw new FormatException("Erreur lors du parsing JSON meta pour UIntN.", ex);
            }
        }


        #endregion

        public static UIntN<TBits> Lerp<TInt, TFrac>(
            UIntN<TBits> a, UIntN<TBits> b, Fixed<TInt, TFrac> t)
            where TInt : struct
            where TFrac : struct
        {
            uint diff = b.Raw - a.Raw;
            uint lerpRaw = a.Raw + (uint)(((ulong)diff * (ulong)t.Raw) >> Fixed<TInt, TFrac>.FracBitsConst);
            return new UIntN<TBits>(lerpRaw);
        }


    }
}
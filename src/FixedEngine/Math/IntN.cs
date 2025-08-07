using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    /// <summary>
    /// Représente un entier signé sur N bits (wrap hardware, sign-extend), paramétré par tag générique.
    /// </summary>
    public readonly struct IntN<TBits>
        where TBits : struct
    {
        private readonly int _value;
        public static readonly int BitsConst = BitsOf<TBits>.Value;
        public static readonly int ShiftConst = 32 - BitsConst;            
        public static readonly uint MaskConst = Mask.MASKS[BitsConst];
        public static readonly int SignBitConst = 1 << (BitsConst - 1);

        private static readonly int MinConst = Mask.SIGNED_MIN[BitsConst];
        private static readonly int MaxConst = Mask.SIGNED_MAX[BitsConst];

        public static readonly int MinValue = -(1 << (BitsConst - 1));
        public static readonly int MaxValue = (1 << (BitsConst - 1)) - 1;

        public static readonly int Epsilon = 1; // Le plus petit incrément possible pour ce type (entier)

        public int Raw => _value;
        public static IntN<TBits> FromRaw(int raw) => new IntN<TBits>(raw, true);

#if DEBUG
        static IntN()
        {
            if ((uint)BitsConst - 1u >= 32u)
                throw new ArgumentOutOfRangeException(nameof(BitsConst));
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntN(int value)
        {
            _value = (BitsConst == 32)
                   ? value                                    // chemin direct
                   : (value << ShiftConst) >> ShiftConst;     // mask + sign-extend en 2 shifts
        }

        // ctor interne pour “bypass”
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IntN(int raw, bool _) => _value = raw;

        public static float ToFloat(IntN<TBits> value)
        {
            return (float)value.Raw;
        }

        public float ToFloat()
        {
            return (float)this.Raw;
        }

        /*==================================
         * --- CONVERSION EXPLICITES---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES ---
        // int <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(IntN<TBits> x) => x._value; // IntN<TBits> → int (valeur wrap/sign-extendée)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TBits>(int value) => new IntN<TBits>(value); // int → IntN<TBits> (avec wrap)

        // uint <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TBits>(uint value) => new IntN<TBits>((int)value); // uint → IntN<TBits> (wrap)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(IntN<TBits> x) => (uint)(x._value & MaskConst); // IntN<TBits> → uint (attention à la conversion négatifs !)

        // float <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(IntN<TBits> x) => (float)x._value; // IntN<TBits> → float
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TBits>(float x) => new IntN<TBits>((int)x); // float → IntN<TBits> (troncature)

        // double <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(IntN<TBits> x) => (double)x._value; // IntN<TBits> → double
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator IntN<TBits>(double x) => new IntN<TBits>((int)x); // double → IntN<TBits> (troncature)

        // UIntN <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UIntN<TBits>(IntN<TBits> x) => new UIntN<TBits>((uint)x._value);

        // IntN <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBitsTo> ConvertTo<TBitsTo>(IntN<TBits> x)
            where TBitsTo : struct
            => new IntN<TBitsTo>(x._value);

        // Fixed <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TBits, TFrac> ToFixed<TFrac>(IntN<TBits> x)
            where TFrac : struct
            => new Fixed<TBits, TFrac>((int)x << BitsOf<TFrac>.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> FromFixed<TFrac>(Fixed<TBits, TFrac> x)
            where TFrac : struct
            => new IntN<TBits>(x.Raw >> BitsOf<TFrac>.Value);


        // UFixed <-> IntN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TBits, TFrac> ToUFixed<TFrac>(IntN<TBits> x)
            where TFrac : struct
            => new UFixed<TBits, TFrac>((uint)((int)x << BitsOf<TFrac>.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> FromUFixed<TFrac>(UFixed<TBits, TFrac> x)
            where TFrac : struct
            => new IntN<TBits>((int)(x.Raw >> BitsOf<TFrac>.Value));
        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMETIQUES ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator +(IntN<TBits> a, IntN<TBits> b) => new IntN<TBits>(a._value + b._value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator -(IntN<TBits> a, IntN<TBits> b) => new IntN<TBits>(a._value - b._value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator *(IntN<TBits> a, IntN<TBits> b) => new IntN<TBits>(a._value * b._value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator /(IntN<TBits> a, IntN<TBits> b)
        {
            if (b._value == 0)
                throw new DivideByZeroException($"Division par zéro dans IntN<{typeof(TBits).Name}>");
            return new IntN<TBits>(a._value / b._value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator %(IntN<TBits> a, IntN<TBits> b)
        {
            if (b._value == 0)
                throw new DivideByZeroException($"Modulo par zéro dans IntN<{typeof(TBits).Name}>");
            return new IntN<TBits>(a._value % b._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator ++(IntN<TBits> x) => new IntN<TBits>(x._value + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator --(IntN<TBits> x) => new IntN<TBits>(x._value - 1);
        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES POUR ARITHMETIQUE ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Add(IntN<TBits> a, IntN<TBits> b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Sub(IntN<TBits> a, IntN<TBits> b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Mul(IntN<TBits> a, IntN<TBits> b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Div(IntN<TBits> a, IntN<TBits> b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Mod(IntN<TBits> a, IntN<TBits> b) => a % b;
        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> MulPow2(IntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n >= limit)  // unique garde-fou, attrape n<0 et n>=limit
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour MulPow2");

            if (n == 0)                      // fast-path
                return a;

            unchecked                       // no overflow checks in /checked builds
            {
                int v = a._value << n;

                return (BitsConst == 32)
                     ? new IntN<TBits>(v, true)      // ctor « raw » : aucun masque
                     : new IntN<TBits>(v);           // wrap signé dans le ctor
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> DivPow2(IntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour DivPow2");

            if (n == 0)
                return a;

            // >> arithmétique conserve le signe
            int v = a._value >> n;

            // BitsConst constant -> branche morte éliminée au JIT
            return (BitsConst == 32)
                 ? new IntN<TBits>(v, true)
                 : new IntN<TBits>(v);              // wrap corrige la réplication de signe
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> ModPow2(IntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n > limit)                    // ici on autorise n == BitsConst
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit}] pour ModPow2");

            if (n == 0)                             // x mod 1 == 0
                return IntN<TBits>.Zero;

            uint mask = Mask.MASKS[n];              // compile-time table
            int v = a._value & (int)mask;           // tronquer, pas besoin d’autre AND

            return new IntN<TBits>(v);    
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
        public static IntN<TBits> operator &(IntN<TBits> a, IntN<TBits> b)
            => new IntN<TBits>(a._value & b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator |(IntN<TBits> a, IntN<TBits> b)
            => new IntN<TBits>(a._value | b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator ^(IntN<TBits> a, IntN<TBits> b)
            => new IntN<TBits>(a._value ^ b._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator ~(IntN<TBits> a)
            => new IntN<TBits>(~a._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator <<(IntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour <<");

            if (n == 0)
                return a;

            unchecked
            {
                int v = a._value << n;
                return (BitsConst == 32)
                    ? new IntN<TBits>(v, true)
                    : new IntN<TBits>(v);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> operator >>(IntN<TBits> a, int n)
        {
            uint limit = (BitsConst == 32) ? 32u : (uint)BitsConst;
            if ((uint)n >= limit)
                throw new ArgumentOutOfRangeException(nameof(n),
                    $"n doit être dans [0,{limit - 1}] pour >>");

            if (n == 0)
                return a;

            int shifted = a._value >> n;
            return new IntN<TBits>(shifted);
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
         * ShrLogical
         ==================================*/
        #region --- METHODE STATIQUE BITWISE (alias) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> And(IntN<TBits> a, IntN<TBits> b) => a & b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Or(IntN<TBits> a, IntN<TBits> b) => a | b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Xor(IntN<TBits> a, IntN<TBits> b) => a ^ b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Not(IntN<TBits> a) => ~a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Nand(IntN<TBits> a, IntN<TBits> b) => ~(a & b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Nor(IntN<TBits> a, IntN<TBits> b) => ~(a | b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Xnor(IntN<TBits> a, IntN<TBits> b) => ~(a ^ b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Shl(IntN<TBits> a, int n) => a << n;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Shr(IntN<TBits> a, int n) => a >> n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> ShlLogical(IntN<TBits> x, int n) => Shl(x, n);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> ShrLogical(IntN<TBits> x, int n)
        {
            if ((uint)n >= BitsConst)                         // 0‥7 pour B8
                throw new ArgumentOutOfRangeException(
                    nameof(n), $"Shift must be 0..{BitsConst - 1}");

            // 1) isole les 8 bits significatifs
            uint raw = (uint)x._value & ((1u << BitsConst) - 1);

            // 2) décale logiquement
            uint shifted = raw >> n;

            // 3) constructeur : wrap + sign-extend éventuel
            return new IntN<TBits>((int)shifted);
        }
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
        public static bool operator ==(IntN<TBits> a, IntN<TBits> b) => a._value == b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(IntN<TBits> a, IntN<TBits> b) => a._value != b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(IntN<TBits> a, IntN<TBits> b) => a._value < b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(IntN<TBits> a, IntN<TBits> b) => a._value <= b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(IntN<TBits> a, IntN<TBits> b) => a._value > b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(IntN<TBits> a, IntN<TBits> b) => a._value >= b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is IntN<TBits> other && this._value == other._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => typeof(TBits).GetHashCode() ^ _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Eq(IntN<TBits> a, IntN<TBits> b) => a._value == b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Neq(IntN<TBits> a, IntN<TBits> b) => a._value != b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lt(IntN<TBits> a, IntN<TBits> b) => a._value < b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lte(IntN<TBits> a, IntN<TBits> b) => a._value <= b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gt(IntN<TBits> a, IntN<TBits> b) => a._value > b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Gte(IntN<TBits> a, IntN<TBits> b) => a._value >= b._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(IntN<TBits> a) => a._value == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNeg(IntN<TBits> a)
        {
            int bits = BitsConst;
            return (a._value & (int)Mask.SIGN_BITS[bits]) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPos(IntN<TBits> a)
        {
            int bits = BitsConst;
            return a._value != 0 && (a._value & (int)Mask.SIGN_BITS[bits]) == 0;
        }
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
        public static IntN<TBits> Min(IntN<TBits> a, IntN<TBits> b)
            => a._value < b._value ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Max(IntN<TBits> a, IntN<TBits> b)
            => a._value > b._value ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Avg(IntN<TBits> a, IntN<TBits> b)
            => new IntN<TBits>((a._value + b._value) / 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(IntN<TBits> a)
            => a._value == 0 ? 0 : (a._value < 0 ? -1 : 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Abs(IntN<TBits> a)
            => new IntN<TBits>(a._value < 0 ? -a._value : a._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Neg(IntN<TBits> a)
            => new IntN<TBits>(-a._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> CopySign(IntN<TBits> value, IntN<TBits> sign)
            => (sign._value < 0) ? Neg(Abs(value)) : Abs(value);
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
        public static IntN<TBits> AddSat(IntN<TBits> a, IntN<TBits> b)
        {
            int sum = a._value + b._value;

            // Clamp branch‑less : deux CMOV générés par RyuJIT
            sum = (sum > MaxConst) ? MaxConst : sum;
            sum = (sum < MinConst) ? MinConst : sum;

            return new IntN<TBits>(sum, true);  // ctor interne : pas de re‑masque
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> SubSat(IntN<TBits> a, IntN<TBits> b)
        {
            int diff = a._value - b._value;

            // Clamp branch‑less : deux CMOV générés par RyuJIT
            diff = (diff > MaxConst) ? MaxConst : diff;
            diff = (diff < MinConst) ? MinConst : diff;

            return new IntN<TBits>(diff, true);   // ctor interne : pas de re‑masque
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> MulSat(IntN<TBits> a, IntN<TBits> b)
        {
            long prod64 = (long)a._value * b._value;

            prod64 = prod64 > MaxConst ? MaxConst : prod64;
            prod64 = prod64 < MinConst ? MinConst : prod64;

            return new IntN<TBits>((int)prod64, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Clamp(IntN<TBits> val, IntN<TBits> min, IntN<TBits> max)
        {
            int v = val._value;

            // deux ternaires → RyuJIT émet CMP+CMOV, zéro saut conditionnel
            v = (v < min._value) ? min._value : v;
            v = (v > max._value) ? max._value : v;

            return new IntN<TBits>(v, true);   // ctor interne : pas de masque redondant
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Clamp01(IntN<TBits> val)
        {
            int v = val._value;

            // Deux CMOV émis par RyuJIT, zéro saut
            v = (v < 0) ? 0 : v;   // max(v,0)
            v = (v > 1) ? 1 : v;   // min(v,1)

            return new IntN<TBits>(v, true);   // ctor interne = pas de masque final
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> ClampWithOffset(
            IntN<TBits> val, IntN<TBits> min, IntN<TBits> max,
            int offsetMin, int offsetMax)
        {
            int v = val._value;

            /* ---------- bornes décalées puis clampées ---------- */
            int vLo = min._value + offsetMin;
            vLo = vLo < MinConst ? MinConst : (vLo > MaxConst ? MaxConst : vLo);

            int vHi = max._value + offsetMax;
            vHi = vHi < MinConst ? MinConst : (vHi > MaxConst ? MaxConst : vHi);

            /* ---------- remet l’ordre si inversé ---------- */
            if (vLo > vHi)
            {
                int tmp = vLo;
                vLo = vHi;
                vHi = tmp;
            }

            /* ---------- sélection branch‑less ---------- */
            int below = v < vLo ? 1 : 0;
            int above = v > vHi ? 1 : 0;

            int result = below * vLo          // v < minVal  → vLo
                       + above * vHi          // v > maxVal  → vHi
                       + (1 - below - above) * v; // sinon     → v

            return new IntN<TBits>(result, true);   // ctor interne : pas de re‑masque
        }
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
        public static IntN<TBits> Reverse(IntN<TBits> a)
        {
            uint v = (uint)a._value;
            int bits = BitsConst;
            uint r = 0;
            for (int i = 0; i < bits; i++)
            {
                r <<= 1;
                r |= (v & 1);
                v >>= 1;
            }
            int signed;
            if (bits < 32)
                signed = (int)((r << (32 - bits)) >> (32 - bits));
            else
                signed = (int)r;
            return new IntN<TBits>(signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(IntN<TBits> a)
        {
            uint v = (uint)a._value;
            int count = 0;
            for (int i = 0; i < BitsConst; i++)
            {
                count += (int)(v & 1);
                v >>= 1;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Parity(IntN<TBits> a)
        {
            return (PopCount(a) & 1) != 0; // true = impair, false = pair
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeros(IntN<TBits> a)
        {
            uint v = (uint)a._value;
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
        public static int TrailingZeros(IntN<TBits> a)
        {
            uint v = (uint)a._value;
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
        public static IntN<TBits> Rol(IntN<TBits> a, int n)
        {
            int bits = BitsOf<TBits>.Value;
            uint v = (uint)a._value & Mask.MASKS[bits];
            n = ((n % bits) + bits) % bits; // wrap safe même pour n négatif
            uint result = ((v << n) | (v >> (bits - n))) & Mask.MASKS[bits];

            // --- FORCE signed sur N bits
            int signed;
            if (bits < 32)
                signed = (int)((result << (32 - bits)) >> (32 - bits));
            else
                signed = (int)result;

            return new IntN<TBits>(signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Ror(IntN<TBits> a, int n)
        {
            int bits = BitsOf<TBits>.Value;
            uint v = (uint)a._value & Mask.MASKS[bits];
            n = ((n % bits) + bits) % bits; // wrap safe (support n négatif)
            uint result = ((v >> n) | (v << (bits - n))) & Mask.MASKS[bits];

            // --- FORCE signed sur N bits
            int signed;
            if (bits < 32)
                signed = (int)((result << (32 - bits)) >> (32 - bits));
            else
                signed = (int)result;

            return new IntN<TBits>(signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsr(IntN<TBits> a)
        {
            uint v = (uint)a._value;
            for (int i = BitsConst - 1; i >= 0; i--)
                if ((v & (1u << i)) != 0)
                    return i;
            return -1; // Aucun bit à 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bsf(IntN<TBits> a)
        {
            uint v = (uint)a._value;
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
         * Bit
         ==================================*/
        #region --- CONSTANTES ---

        public static IntN<TBits> Zero => new IntN<TBits>(0);
        public static IntN<TBits> Half
        {
            get
            {
                int bits = BitsOf<TBits>.Value;
                return new IntN<TBits>(1 << (bits - 1));
            }
        }

        public static IntN<TBits> One => new IntN<TBits>(1);
        public static IntN<TBits> AllOnes => new IntN<TBits>((int)MaskConst);
        public static IntN<TBits> Msb => new IntN<TBits>((int)Mask.SIGN_BITS[BitsConst]);
        public static IntN<TBits> Lsb => new IntN<TBits>(1);
        public static IntN<TBits> Bit(int n)
        {
            if (n < 0 || n >= BitsConst)
                throw new ArgumentOutOfRangeException(nameof(n), "n doit être dans [0, BitsConst-1]");
            return new IntN<TBits>(1 << n);
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
        public static byte Byte(IntN<TBits> a, int n)
        {
            int byteCount = (IntN<TBits>.BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"n doit être entre 0 et {byteCount - 1} pour IntN<{typeof(TBits).Name}>");
            return (byte)((a._value >> (n * 8)) & (int)Mask.MASKS[8]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            int byteCount = (BitsConst + 7) / 8;
            byte[] bytes = new byte[byteCount];
            uint v = (uint)_value;
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)(v & 0xFF);
                v >>= 8;
            }
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> FromBytes(byte[] bytes)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (bytes.Length < byteCount)
                throw new ArgumentException($"Le tableau d'octets doit contenir au moins {byteCount} éléments pour IntN<{typeof(TBits).Name}>");

            uint v = 0;
            for (int i = 0; i < byteCount; i++)
                v |= (uint)bytes[i] << (8 * i);

            // On passe par le constructeur int pour appliquer le wrap/sign-extend si besoin.
            return new IntN<TBits>((int)v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int index)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (index < 0 || index >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"index hors limites : [0, {byteCount - 1}] pour IntN<{typeof(TBits).Name}>");
            return (byte)((_value >> (index * 8)) & (int)Mask.MASKS[8]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntN<TBits> SetByte(int n, byte b)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour IntN<{typeof(TBits).Name}>");
            int mask = ~((int)Mask.MASKS[8] << (n * 8));
            int v = (_value & mask) | (b << (n * 8));
            return new IntN<TBits>(v);
        }

        /// <summary>
        /// Remplace l’octet d’index n par celui de la même position dans source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntN<TBits> ReplaceByte(int n, IntN<TBits> source)
        {
            int byteCount = (BitsConst + 7) / 8;
            if (n < 0 || n >= byteCount)
                throw new ArgumentOutOfRangeException(nameof(n), $"Octet n doit être dans [0,{byteCount - 1}] pour IntN<{typeof(TBits).Name}>");
            byte b = source.GetByte(n); // Si source est plus petit, cela jettera déjà une exception, ce qui est cohérent.
            return SetByte(n, b);
        }

        public IntN<TBits> ReplaceByte(int n, byte b)
        {
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
            // Affiche la valeur signée, le nom du tag, et la représentation binaire/hex pour le debug
            int bits = BitsOf<TBits>.Value;
            uint uval = (uint)_value & Mask.MASKS[bits];
            string bin = Convert.ToString(uval, 2).PadLeft(bits, '0');
            string hex = uval.ToString("X" + ((bits + 3) / 4));
            return $"IntN<{typeof(TBits).Name}>({_value}) [bin={bin} hex={hex}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToBinaryString()
        {
            // On n’affiche que la partie utile (BitsConst) et on pad à gauche avec des zéros si besoin
            int bits = BitsConst;
            uint v = (uint)_value & Mask.MASKS[bits];
            return Convert.ToString(v, 2).PadLeft(bits, '0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToHexString(bool withPrefix = false)
        {
            int bits = BitsConst;                  // largeur réelle
            int nibbles = (bits + 3) / 4;        // 1 nibble = 4 bits
            uint v = (uint)_value & Mask.MASKS[bits];
            string hex = v.ToString("X").PadLeft(nibbles, '0');
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
         * FromJsonWithMeta
         ==================================*/
        #region --- PARSING (exhaustif, JSON, HEX, BINAIRE) ---

        /// <summary>
        /// Tente de parser une chaîne décimale en IntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParse(string s, out IntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return TryParseHex(s, out result);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return TryParseBinary(s, out result);

            if (int.TryParse(s, out var ival)) { result = new IntN<TBits>(ival); return true; }
            if (uint.TryParse(s, out var uval)) { result = new IntN<TBits>((int)uval); return true; }
            return false;
        }

        /// <summary>
        /// Parse une chaîne décimale en IntN&lt;TBits&gt;. Throw si invalide.
        /// </summary>
        public static IntN<TBits> Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour Parse.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ParseHex(s);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                return ParseBinary(s);

            // Acceptation du wrapping pour valeurs hors-plage
            if (int.TryParse(s, out var ival))
                return new IntN<TBits>(ival);
            if (uint.TryParse(s, out var uval))
                return new IntN<TBits>((int)uval);
            throw new FormatException($"Impossible de parser '{s}' comme IntN<{typeof(TBits).Name}>.");
        }

        /// <summary>
        /// Tente de parser une chaîne hexadécimale ("0x...." ou "....") en IntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParseHex(string s, out IntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return false; // Patch clé !
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            {
                result = new IntN<TBits>(v);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parse une chaîne hexadécimale ("0x...." ou "....") en IntN&lt;TBits&gt;.
        /// </summary>
        public static IntN<TBits> ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseHex.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Préfixe binaire non valide pour hexadécimal.");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe hex.");
            return new IntN<TBits>(int.Parse(s, System.Globalization.NumberStyles.HexNumber));
        }

        /// <summary>
        /// Tente de parser une chaîne binaire ("0b...." ou "....") en IntN&lt;TBits&gt;.
        /// </summary>
        public static bool TryParseBinary(string s, out IntN<TBits> result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) return false;
            try
            {
                int v = Convert.ToInt32(s, 2);
                result = new IntN<TBits>(v);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Parse une chaîne binaire ("0b...." ou "....") en IntN&lt;TBits&gt;.
        /// </summary>
        public static IntN<TBits> ParseBinary(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide ou null pour ParseBinary.");
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (string.IsNullOrWhiteSpace(s)) throw new FormatException("String vide après préfixe binaire.");
            return new IntN<TBits>(Convert.ToInt32(s, 2));
        }

        /// <summary>
        /// Sérialise en JSON natif (int value).
        /// </summary>
        public string ToJson() => _value.ToString();

        /// <summary>
        /// Désérialise depuis un champ JSON (attendu int, string, ou hex/binaire).
        /// </summary>
        public static IntN<TBits> FromJson(string s)
        {
            if (TryParse(s, out var v)) return v;
            if (TryParseHex(s, out v)) return v;
            if (TryParseBinary(s, out v)) return v;
            throw new FormatException($"Impossible de parser '{s}' comme IntN<{typeof(TBits).Name}> (décimal, hex, ou binaire)");
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

        public static IntN<T> FromJsonWithMeta<T>(string json) where T : struct
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                int bits = Convert.ToInt32(obj["bits"]);
                if (bits != BitsOf<T>.Value)
                    throw new Exception($"Meta-bits ({bits}) ne correspond pas au type générique {typeof(T).Name} ({BitsOf<T>.Value})");
                int raw = Convert.ToInt32(obj["raw"]);
                return IntN<T>.FromRaw(raw);
            }
            catch (Exception ex)
            {
                throw new FormatException("Erreur lors du parsing JSON meta pour IntN.", ex);
            }
        }

        #endregion


        public static IntN<TBits> Lerp<TInt, TFrac>(
            IntN<TBits> a, IntN<TBits> b, Fixed<TInt, TFrac> t)
            where TInt : struct
            where TFrac : struct
        {
            int diff = b.Raw - a.Raw;
            int lerpRaw = a.Raw + (int)(((long)diff * t.Raw) >> Fixed<TInt, TFrac>.FracBitsConst);
            return new IntN<TBits>(lerpRaw);
        }
    }
}

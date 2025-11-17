using FixedEngine.Core;
using FixedEngine.Math.Consts;
using System.Runtime.CompilerServices;


namespace FixedEngine.Math
{
    public static partial class FixedMath
    {
        // ==========================
        // --- EXPONENTIELLE ---
        // ==========================
        #region --- EXPONENTIELLE ---
        //----- UIntN -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Exp2u<TBits>(IntN<TBits> x)
        where TBits : struct
        {
            int X = x.Raw;
            if (X < 0)
                return new UIntN<TBits>(0); // rétro : clamp à zéro si négatif

            int bits = UIntN<TBits>.BitsConst;
            uint val = (uint)(1 << X); // On passe tout en uint
            val &= Mask.MASKS[bits];
            return new UIntN<TBits>(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Expu<TBits>(IntN<TBits> x)
            where TBits : struct
        {
            int bits = BitsOf<TBits>.Value;
            // log2(e) au format Q selon TBits
            int log2e_q = LogConsts.LOG2E_Q[bits];

            // Multiplie x par log2(e) en Q-format, puis ramène à la même échelle
            int scaled = (x.Raw * log2e_q) >> bits;

            return Exp2u<TBits>(new IntN<TBits>(scaled));
        }

        //----- IntN -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Exp2<TBits>(IntN<TBits> x)
        where TBits : struct
        {
            int X = x.Raw;
            int bits = IntN<TBits>.BitsConst;

            // Si X < 0, retourne 0 (comportement hardware “clamp”, ou - si tu préfères “signed pow”, retourne ±1)
            if (X < 0)
                return new IntN<TBits>(0);

            int val = 1 << X;
            // Mask sur bits
            val &= (int)Mask.MASKS[bits];
            return new IntN<TBits>(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Exp<TBits>(IntN<TBits> x)
        where TBits : struct
        {
            int bits = BitsOf<TBits>.Value;
            // log2(e) au format Q selon TBits
            int log2e_q = LogConsts.LOG2E_Q[bits];
            int scaled = (x.Raw * log2e_q) >> bits;
            return Exp2<TBits>(new IntN<TBits>(scaled));
        }


        //----- UFixed -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Exp2u<TUInt, TFrac, TInt>(Fixed<TInt, TFrac> x)
            where TUInt : struct
            where TFrac : struct
            where TInt : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            int X = x.Raw;
            int intPart = X >> fracBits;
            int fracPart = X & ((1 << fracBits) - 1);

            uint baseVal = (uint)(1 << fracBits); // 1.0 en Q-format
            if (intPart >= 0)
                baseVal <<= intPart;
            else
                baseVal >>= -intPart;

            // 2^f ≈ 1 + f*ln2
            uint ln2_q = (uint)LogConsts.LN2_Q[fracBits];
            uint twoPowFrac = baseVal + (uint)(((long)baseVal * fracPart * ln2_q) >> (2 * fracBits));

            return new UFixed<TUInt, TFrac>(twoPowFrac);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Exp<TUInt, TFrac, TInt>(Fixed<TInt, TFrac> x)
            where TUInt : struct
            where TFrac : struct
            where TInt : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            int log2e_q = LogConsts.LOG2E_Q[fracBits];
            int scaled = (int)(((long)x.Raw * log2e_q) >> fracBits);
            return Exp2u<TUInt, TFrac, TInt>(new Fixed<TInt, TFrac>(scaled));
        }

        //----- Fixed -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Exp2<TInt, TFrac>(Fixed<TInt, TFrac> x)
        where TInt : struct
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            int X = x.Raw;
            int intPart = X >> fracBits;
            int fracPart = X & ((1 << fracBits) - 1);

            // 2^intPart = shift
            int baseVal = 1 << fracBits; // 1.0 en Q-format
            if (intPart >= 0)
                baseVal <<= intPart;
            else
                baseVal >>= -intPart;

            // 2^f ≈ 1 + f*ln2
            int ln2_q = LogConsts.LN2_Q[fracBits];
            int twoPowFrac = baseVal + (int)(((long)baseVal * fracPart * ln2_q) >> (2 * fracBits));

            return new Fixed<TInt, TFrac>(twoPowFrac);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Exp<TInt, TFrac>(Fixed<TInt, TFrac> x)
        where TInt : struct
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            int log2e_q = LogConsts.LOG2E_Q[fracBits];
            int scaled = (int)(((long)x.Raw * log2e_q) >> fracBits);
            return Exp2<TInt, TFrac>(new Fixed<TInt, TFrac>(scaled));
        }
        #endregion

        // ==========================
        // --- LOG ---
        // ==========================
        #region --- LOG ---

        //----- UFixed -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Log2UFixed<TUInt, TFrac, TInt>(UFixed<TUInt, TFrac> x)
        where TUInt : struct
        where TFrac : struct
        where TInt : struct
        {
            uint value = x.Raw;
            if (value == 0)
                return Fixed<TInt, TFrac>.Zero; // clamp rétro (ou -Max si tu veux une valeur spéciale)

            // Trouver la position du bit le plus significatif
            int msb = 0;
            uint v = value;
            while (v > 1)
            {
                v >>= 1;
                msb++;
            }

            // Approximer la fraction : f = (value << bits) / (1 << msb)
            int fracBits = BitsOf<TFrac>.Value;
            int shift = msb - fracBits;
            int frac = 0;
            if (shift >= 0)
                frac = (int)((value >> shift) & ((1 << fracBits) - 1));
            else
                frac = (int)((value << -shift) & ((1 << fracBits) - 1));

            // log2(x) = msb + fraction en Q-format
            int log2 = (msb << fracBits) | frac;
            return new Fixed<TInt, TFrac>(log2);
        }

        //----- Fixed -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Log2Fixed<TInt, TFrac>(Fixed<TInt, TFrac> x)
        where TInt : struct
        where TFrac : struct
        {
            int value = x.Raw;
            if (value <= 0)
                return Fixed<TInt, TFrac>.Zero; // ou retourne -Max

            int msb = 0;
            int v = value;
            while (v > 1)
            {
                v >>= 1;
                msb++;
            }

            int fracBits = BitsOf<TFrac>.Value;
            int shift = msb - fracBits;
            int frac = 0;
            if (shift >= 0)
                frac = ((value >> shift) & ((1 << fracBits) - 1));
            else
                frac = ((value << -shift) & ((1 << fracBits) - 1));

            int log2 = (msb << fracBits) | frac;
            return new Fixed<TInt, TFrac>(log2);
        }


        #endregion


    } 
}

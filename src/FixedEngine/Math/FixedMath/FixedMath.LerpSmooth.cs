using FixedEngine.Core;
using System.Runtime.CompilerServices;


namespace FixedEngine.Math
{
    public static partial class FixedMath
    {
        // ==========================
        // --- LERP ---
        // ==========================
        #region --- LERP ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Lerp<TBits>(
        UIntN<TBits> a,
        UIntN<TBits> b,
        UIntN<TBits> t)
        where TBits : struct
        {
            // t doit être dans [0, MaxConst] (ex: 0..255 pour 8 bits)
            uint one = Mask.UNSIGNED_MAX[UIntN<TBits>.BitsConst]; // 0xFF, 0xFFFF, etc.
            uint invT = one - t.Raw;
            ulong la = (ulong)a.Raw * invT;
            ulong lb = (ulong)b.Raw * t.Raw;
            uint result = (uint)((la + lb) / one);
            return new UIntN<TBits>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Lerp<TBits>(
        IntN<TBits> a,
        IntN<TBits> b,
        UIntN<TBits> t)
        where TBits : struct
        {
            // t ∈ [0, Max] (unsigned N bits)
            uint one = Mask.UNSIGNED_MAX[IntN<TBits>.BitsConst];
            uint invT = one - t.Raw;
            long la = (long)a.Raw * invT;
            long lb = (long)b.Raw * t.Raw;
            int result = (int)((la + lb) / one);
            return new IntN<TBits>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Lerp<TUInt, TFrac>(
            UFixed<TUInt, TFrac> a,
            UFixed<TUInt, TFrac> b,
            UFixed<TUInt, TFrac> t)
            where TUInt : struct
            where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            uint one = 1u << fracBits;
            uint invT = one - t.Raw;
            ulong la = (ulong)a.Raw * invT;
            ulong lb = (ulong)b.Raw * t.Raw;
            uint result = (uint)((la + lb) >> fracBits);
            return new UFixed<TUInt, TFrac>(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Lerp<TInt, TFrac>(
        Fixed<TInt, TFrac> a,
        Fixed<TInt, TFrac> b,
        UFixed<TInt, TFrac> t)
        where TInt : struct
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            int one = 1 << fracBits;
            int invT = one - (int)t.Raw;
            long la = (long)a.Raw * invT;
            long lb = (long)b.Raw * t.Raw;
            int result = (int)((la + lb) >> fracBits);
            return new Fixed<TInt, TFrac>(result);
        }
        #endregion

        // ==========================
        // --- SMOOTHSTEP ---
        // ==========================
        #region --- SMOOTHSTEP ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> SmoothStep<TBits>(UIntN<TBits> t)
        where TBits : struct
        {
            uint max = Mask.UNSIGNED_MAX[UIntN<TBits>.BitsConst]; // 0xFF, 0xFFFF, etc.
            ulong T = t.Raw;
            ulong t2 = (T * T) / max;
            ulong t3 = (t2 * T) / max;
            ulong result = (3 * t2 > 2 * t3) ? (3 * t2 - 2 * t3) : 0;
            if (result > max) result = max; // clamp
            return new UIntN<TBits>((uint)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> SmoothStep<TBits>(IntN<TBits> t)
        where TBits : struct
        {
            uint max = Mask.UNSIGNED_MAX[IntN<TBits>.BitsConst];
            int v = t.Raw;
            bool neg = v < 0;
            uint T = (uint)(neg ? -v : v); // abs pour sécurité
            ulong t2 = (T * T) / max;
            ulong t3 = (t2 * T) / max;
            ulong result = (3 * t2 > 2 * t3) ? (3 * t2 - 2 * t3) : 0;
            if (result > max) result = max;
            int signed = (int)result;
            return new IntN<TBits>(neg ? -signed : signed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> SmoothStep<TUInt, TFrac>(UFixed<TUInt, TFrac> t)
        where TUInt : struct
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            ulong T = t.Raw;
            ulong one = 1u << fracBits;

            // Q-format: résultat = 3*t^2 - 2*t^3
            // t^2 : (T * T) >> bits
            // t^3 : ((T * T) >> bits) * T >> bits
            ulong t2 = (T * T) >> fracBits;
            ulong t3 = (t2 * T) >> fracBits;
            ulong result = (3 * t2 - 2 * t3);

            // Clamp : s’assure qu’on reste dans [0, one]
            if (result > one) result = one;
            return new UFixed<TUInt, TFrac>((uint)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> SmoothStep<TInt, TFrac>(Fixed<TInt, TFrac> t)
        where TInt : struct
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            long T = t.Raw;
            long one = 1L << fracBits;

            long t2 = (T * T) >> fracBits;
            long t3 = (t2 * T) >> fracBits;
            long result = 3 * t2 - 2 * t3;

            // Clamp : assure la sortie dans [0, one] (optionnel)
            if (result < 0) result = 0;
            if (result > one) result = one;

            return new Fixed<TInt, TFrac>((int)result);
        }


        #endregion
    } 
}

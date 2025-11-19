using FixedEngine.Core;
using FixedEngine.LUT;
using FixedEngine.Math.Consts;
using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    public static partial class FixedMath
    {
        // ==========================
        // --- ASIN/ACOS LUT Retro ---
        // ==========================
        #region --- ASIN/ACOS Retro ---

        // --- ASIN ---
        #region --- ASIN LUT Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AsinLutCore(int valQ16, int bits)
        {
            const int LUT_BITS = 12;
            const int LUT_MASK = (1 << LUT_BITS) - 1;   // 4095
            var lut = AsinLUT4096.LUT;                  // θ en Q16 (radians)

            // 1) Clamp domaine
            if (valQ16 <= -65536) return -TrigConsts.PI_2_Q[16];
            if (valQ16 >= 65536) return TrigConsts.PI_2_Q[16];

            // 2) Index + fraction (mapping [-1..+1] → [0..4095]) avec ARRONDI (pas troncature)
            long num = ((long)(valQ16 + 65536) * LUT_MASK) << 16; // Q16.16
            long den = 131072;                                    // 2^17
            long idxFracQ16 = (num + (den >> 1)) / den;           // arrondi au plus proche

            int idx = (int)(idxFracQ16 >> 16);     // 0..4094
            int tQ16 = (int)(idxFracQ16 & 0xFFFF);  // 0..65535

            // 3) Mode rétro : B2..B6 → nearest neighbor
            if (bits <= 6)
            {
                if (tQ16 >= 32768 && idx < LUT_MASK) idx++; // nearest (pas floor)
                return lut[idx];
            }

            // 4) Routage "tail" (forte courbure proche de 75..90°)
            int ax = (valQ16 < 0) ? -valQ16 : valQ16;  // |x| en Q16
            int x0Tail = AsinLUT_Tail2048.X0_Q;            // seuil sin(75°) en Q16.16
            if (ax >= x0Tail)
            {
                int yTail = AsinTailEval_Q16(ax);          // asin(|x|) via LUT tail (Q16)
                return (valQ16 < 0) ? -yTail : yTail;      // impaire
            }

            // 5) Interpolation Catmull-Rom sur la LUT 4096 + clamp monotone local
            int p0 = lut[(idx == 0) ? 0 : idx - 1];
            int p1 = lut[idx];
            int p2 = lut[(idx < LUT_MASK) ? idx + 1 : LUT_MASK];
            int p3 = lut[(idx < LUT_MASK - 1) ? idx + 2 : LUT_MASK];

            int yMid = FixedMath.CatmullRom(p0, p1, p2, p3, tQ16);

            // 🔒 clamp anti-overshoot : y ∈ [min(p1,p2) .. max(p1,p2)]
            int lo = (p1 < p2) ? p1 : p2;
            int hi = (p1 > p2) ? p1 : p2;
            if (yMid < lo) yMid = lo;
            if (yMid > hi) yMid = hi;

            // 6) Correction centrale : asin(x) = atan(x / sqrt(1 - x^2))
            // Dans AsinLutCore(int valQ16, int bits), bloc 6) Correction centrale
            {
                const int ONE = 65536;

                // ✅ Fix: eviter l'overflow Q32 → uint quand x == 0
                if (ax == 0)
                {
                    return 0; // asin(0) = 0 exactement
                }

                // Ancien code:
                // long x2 = (long)ax * (long)ax;            // Q32 : x^2
                // long rest = (long)ONE * (long)ONE - x2;   // Q32 : 1 - x^2
                // if (rest > 0) { uint denomQ16 = IntegerSqrt((uint)rest, 32); ... }

                // Optionnel: calcul sûr en 64 bits non signé (robuste pour tout ax)
                ulong one2 = (ulong)ONE * (ulong)ONE;        // 2^32
                ulong x2 = (ulong)ax * (ulong)ax;         // <= 2^32
                ulong rest64 = one2 - x2;                    // ∈ [0..2^32]
                if (rest64 > 0)
                {
                    uint denomQ16 = (rest64 == one2) ? (uint)ONE   // sqrt(1.0) = 1.0 Q16
                                                     : IntegerSqrt((uint)rest64, 32);

                    uint ratioQ16 = (uint)(((ulong)ax << 16) / denomQ16);
                    uint one = 1u << 16;
                    int yAtan = (ratioQ16 <= one)
                              ? AtanLutCore(ratioQ16, one, 31)
                              : (TrigConsts.PI_2_Q[16] - AtanLutCore((uint)(((ulong)one * (ulong)one) / ratioQ16), one, 31));

                    if (valQ16 < 0) yAtan = -yAtan;
                    return yAtan; // on remplace yMid par la valeur atan
                }
                else
                {
                    // cas limite |x|=1 (déjà géré en amont normalement)
                    return (valQ16 < 0) ? -TrigConsts.PI_2_Q[16] : TrigConsts.PI_2_Q[16];
                }
            }

            //return yMid; // Q16 (radians)
        }


        // ===== ASIN TAIL EVAL =====
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AsinTailEval_Q16(int xQ16)
        {
            const int QF = 16;
            int X0 = AsinLUT_Tail2048.X0_Q;  // sin(75°) Q16.16
            int X1 = AsinLUT_Tail2048.X1_Q;  // 1.0 Q16.16
            int N = AsinLUT_Tail2048.N;     // 2048
            int PAD = AsinLUT_Tail2048.PAD;   // 1
            var V = AsinLUT_Tail2048.LUT;   // θ en Q16

            if (xQ16 <= X0) return V[PAD + 0];
            if (xQ16 >= X1) return V[PAD + (N - 1)];


            int range = X1 - X0;
            long num = ((long)xQ16 - X0) << QF;
            int uQ16 = (int)((num + (range >> 1)) / range);


            long posQ16 = (long)uQ16 * (N - 1);
            int idx = (int)(posQ16 >> QF);      // 0..N-2
            int tQ16 = (int)(posQ16 & 0xFFFF);   // 0..65535

            int baseIdx = PAD + idx;
            int p0 = V[baseIdx - 1];
            int p1 = V[baseIdx + 0];
            int p2 = V[baseIdx + 1];
            int p3 = V[baseIdx + 2];
            return CatmullRom(p0, p1, p2, p3, tQ16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Q16_16AngleToBn(int q16_16, int bits)
        {
            int max = (1 << (bits - 1)) - 1;         // 127 en B8
            long num = (long)q16_16 * max;
            long den = TrigConsts.PI_2_Q[16];         
            int result = (int)((num + (den >> 1)) / den);  // arrondi au plus proche

            // bornes [-max..+max]
            if (result < -max) result = -max;
            if (result > max) result = max;
            return result;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Q16_16AngleToBn_Atan2(int q16_16, int bits, bool signed)
        {

            int twoPi = TrigConsts.PI2_Q[16];
            int pi = TrigConsts.PI_Q[16];

            int a = q16_16;
            // Normalise vers [0 .. 2π)
            if (a < 0) a += twoPi;

            uint umax = (uint)((1 << bits) - 1);
            long num = (long)a * umax + (pi);
            int u = (int)(num / (2L * pi));


            if (u < 0) u = 0;
            if ((uint)u > umax) u = (int)umax;

            if (!signed) return u;

            int half = 1 << (bits - 1);
            return (u >= half) ? (u - (1 << bits)) : u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Q16_16AcosToBn(int q16_16, int bits, bool signed)
        {
            if (signed)
            {
                // Map [0..π] -> [0..2^(bits-1)] puis convertis 128 -> -128 en signé B8.
                int halfTurn = 1 << (bits - 1);                 // 128 en B8
                long mapped = ((long)q16_16 * halfTurn + (TrigConsts.PI_Q[16] >> 1)) / TrigConsts.PI_Q[16];
                int angle = (int)mapped;                         // 0..128

                if (angle == halfTurn) angle = -halfTurn;        // 128 -> -128 (two’s complement wrap)
                return angle;                                    // 0, 1..127, -128
            }
            else
            {

                // unsigned: [0..π] → [0..halfTurn], avec densité ~ (2^bits−1) sur π (comme asin)
                int halfTurn = 1 << (bits - 1);                 // ex: 128 en B8
                int twoMaxPlusOne = (halfTurn << 1) - 1;        // = 2^bits - 1

                long scaled = ((long)q16_16 * twoMaxPlusOne + (TrigConsts.PI_Q[16] >> 1))
                              / TrigConsts.PI_Q[16];            // 0..(2^bits-1), arrondi

                int result = (int)((scaled + 1) >> 1);          // /2 avec arrondi au plus proche
                if (result < 0) result = 0;
                if (result > halfTurn) result = halfTurn;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Asin_Raw(uint uRaw, int bits)
        {
            uint maxRawU = (1u << bits) - 1u;
            long maxRaw = (long)maxRawU;
            int maxTick = (1 << (bits - 1)) - 1;

            if (uRaw == 0u) return -maxTick;
            if (uRaw == maxRawU) return +maxTick;

            long n = ((long)uRaw * 2 - maxRaw) * 65536L;
            long h = maxRaw >> 1;
            int valQ16_round = (int)((n >= 0 ? n + h : n - h) / maxRaw);
            const int THRESH_882_Q16 = 65515;
            int ax = valQ16_round >= 0 ? valQ16_round : -valQ16_round;
            int valQ16 = (ax >= THRESH_882_Q16) ? (int)(n / maxRaw) : valQ16_round;

            if (bits <= 16)
            {
                int a = valQ16 >= 0 ? valQ16 : -valQ16;
                int eps = 65536 - a;
                int halfStepQ16 = (int)(65536L / maxRaw);
                int boosted = eps + halfStepQ16;
                if (boosted > 65536) boosted = 65536;
                int a2 = 65536 - boosted;
                if (a2 < 0) a2 = 0;
                valQ16 = valQ16 >= 0 ? a2 : -a2;
            }

            int acosQ16 = AcosLutCore(valQ16, bits);
            int asinQ16 = TrigConsts.PI_2_Q[16] - acosQ16;
            return Q16_16AngleToBn(asinQ16, bits);
        }
        #endregion

        #region --- ASIN (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Asin<TBits>(UIntN<TBits> v) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"Asin<UIntN> : B2..B31 requis (bits={bits}).");

            return Asin_Raw(v.Raw, bits);
        }
        #endregion

        #region --- ASIN (IntN) --- 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Asin<TBits>(IntN<TBits> v) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException();

            int raw = (v.Raw << (32 - bits)) >> (32 - bits);
            uint uRaw = (uint)(raw + (1 << (bits - 1)));

            // Appel direct à la version privée qui prend uint
            return Asin_Raw(uRaw, bits);
        }
        #endregion

        #region --- ASIN (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Asin<TUInt, TFrac>(UFixed<TUInt, TFrac> v)
            where TUInt : struct
            where TFrac : struct
        {
            int F = UFixed<TUInt, TFrac>.FracBitsConst;   // ex: 8 pour Q0.8
            int Abits = IntN<TUInt>.BitsConst;            // résolution ANGLE (Bn) — PAS F
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Asin UFixed : angle B2..B31 requis (bits={Abits}).");
            if (F < 1 || F > 31)
                throw new NotSupportedException($"Asin UFixed : F (bits fractionnaires) doit être dans [1..31] (F={F}).");

            // map [0..(2^F−1)] -> [-65536..+65536] en Q16, en TRUNC (cohérent avec UIntN et Acos)
            uint maxRaw = (1u << F) - 1u;
            long num = ((long)v.Raw * 2L - (long)maxRaw) * 65536L;
            int valQ16 = (int)(num / (long)maxRaw);

            // asin = π/2 − acos (core Q16)
            int acosQ16 = AcosLutCore(valQ16, Abits);
            int asinQ16 = TrigConsts.PI_2_Q[16] - acosQ16;

            // angle signé [-π/2..+π/2] -> Bn signé
            return Q16_16AngleToBn(asinQ16, Abits);
        }
        #endregion

        #region --- ASIN (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Asin<TInt, TFrac>(Fixed<TInt, TFrac> v)
            where TInt : struct
            where TFrac : struct
        {
            int F = Fixed<TInt, TFrac>.FracBitsConst;   // ex: 8 pour Q8.8
            int Abits = IntN<TInt>.BitsConst;           // résolution ANGLE (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Asin Fixed : angle B2..B31 requis (bits={Abits}).");
            if (F < 0 || F > 31)
                throw new NotSupportedException($"Asin Fixed : F (bits fractionnaires) doit être dans [0..31] (F={F}).");

            // QF -> Q16 (bit-faithful en signé), avec shifts sûrs
            int valQ16 = (F == 16) ? v.Raw
                       : (F > 16) ? (v.Raw >> (F - 16))
                                  : (v.Raw << (16 - F));

            // asin = pi/2 - acos (core LUT en Q16)
            int acosQ16 = AcosLutCore(valQ16, Abits);
            int asinQ16 = TrigConsts.PI_2_Q[16] - acosQ16;

            // angle signé [-pi/2..+pi/2] -> Bn signé
            return Q16_16AngleToBn(asinQ16, Abits);
        }
        #endregion

        // --- ACOS ---
        #region --- ACOS LUT Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AcosLutCore(int valQ16, int bits)
        {
            // Clamp & bit-faithful/interp exactement comme Asin
            int asin = AsinLutCore(valQ16, bits);                // asin(x) en Q16
            return TrigConsts.PI_2_Q[16] - asin;                     // acos(x) = π/2 - asin(x)
        }
        #endregion

        #region --- ACOS (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Acos<TBits>(UIntN<TBits> v) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"Acos<UIntN> : B2..B31 requis (bits={bits}).");
            uint maxRaw = (1u << bits) - 1;
            int valQ16 = (int)((((long)v.Raw * 2 - maxRaw) * 65536) / maxRaw);


            return Q16_16AcosToBn(AcosLutCore(valQ16, bits), bits, false);
        }
        #endregion

        #region --- ACOS (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Acos<TBits>(IntN<TBits> v) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"Acos<IntN> : B2..B31 requis (bits={bits}).");

            int raw = (v.Raw << (32 - bits)) >> (32 - bits);
            int valQ16 = (bits == 17) ? raw
                        : (bits > 17) ? (raw >> (bits - 17))
                                      : (raw << (17 - bits));

            int q16 = AcosLutCore(valQ16, bits);
            return Q16_16AcosToBn(q16, bits, signed: false);
        }
        #endregion

        #region --- ACOS (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Acos<TUInt, TFrac>(UFixed<TUInt, TFrac> v)
            where TUInt : struct where TFrac : struct
        {
            int F = BitsOf<TFrac>.Value;                 // bits fractionnaires (amplitude)
            int Abits = IntN<TUInt>.BitsConst;           // résolution ANGLE (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Acos<UFixed> : angle B2..B31 requis (bits={Abits}).");

            // map [0 .. (1<<F)-1] -> [-65536 .. +65536] en Q16
            uint maxRaw = (uint)((1 << F) - 1);
            int valQ16 = (int)((((long)v.Raw * 2 - maxRaw) * 65536) / maxRaw);

            int q16 = AcosLutCore(valQ16, Abits);    // angle en Q16, plage [0..π]
            return Q16_16AcosToBn(q16, Abits, signed: false);
        }
        #endregion

        #region --- ACOS (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Acos<TInt, TFrac>(Fixed<TInt, TFrac> v)
            where TInt : struct where TFrac : struct
        {
            int F = BitsOf<TFrac>.Value;                 // bits fractionnaires (amplitude)
            int Abits = IntN<TInt>.BitsConst;            // résolution ANGLE (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Acos<Fixed> : angle B2..B31 requis (bits={Abits}).");

            // map QF -> Q16
            int valQ16 = (F == 16) ? v.Raw
                       : (F > 16) ? (v.Raw >> (F - 16))
                                  : (v.Raw << (16 - F));

            int q16 = AcosLutCore(valQ16, Abits);    // angle en Q16, plage [0..π]
            return Q16_16AcosToBn(q16, Abits, signed: false);
        }
        #endregion

        #endregion

    } 
}

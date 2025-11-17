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
        // --- ATAN/ATAN2 Retro ---
        // ==========================
        #region --- ATAN/ATAN2 Retro ---

        //----- ATAN -----
        #region --- ATANCORE ---
        // rawOrAbs ∈ [0..scaleMax] ; retourne atan(x) en Q16.
        // scaleMax = "1.0" dans l’échelle du type appelant (ex: 1<<FracBits).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AtanLutCore(uint rawOrAbs, uint scaleMax, int bits)
        {
            const int LUT_BITS = 12;                   // 4096
            const int LUT_MASK = (1 << LUT_BITS) - 1;  // 0xFFF
            var lut = AtanLUT4096.LUT;

            if (rawOrAbs > scaleMax) rawOrAbs = scaleMax;

            // idx = floor(raw/scaleMax * 4095), tQ16 = fraction
            ulong prod = (ulong)rawOrAbs * LUT_MASK;
            int idx = (int)(prod / scaleMax);                      // 0..4095
            uint rem = (uint)(prod - (ulong)idx * scaleMax);
            int tQ16 = (int)(((ulong)rem << 16) / scaleMax);        // 0..65535

            if (bits <= 14)
            {
                if (tQ16 >= 32768 && idx < LUT_MASK) idx++;           // nearest
                return lut[idx];
            }

            int p0 = lut[(idx == 0) ? 0 : idx - 1];
            int p1 = lut[idx];
            int p2 = lut[(idx < LUT_MASK) ? idx + 1 : LUT_MASK];
            int p3 = lut[(idx < LUT_MASK - 1) ? idx + 2 : LUT_MASK];

            int y = FixedMath.CatmullRom(p0, p1, p2, p3, tQ16);
            // clamp anti-overshoot pour garder la monotonie locale
            int lo = p1 < p2 ? p1 : p2, hi = p1 > p2 ? p1 : p2;
            if (y < lo) y = lo; if (y > hi) y = hi;
            return y; // Q16
        }
        #endregion

        #region --- ATAN (UIntN) --
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan<TBits>(UIntN<TBits> x) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"Atan UIntN : B2..B31 requis (bits={bits}).");

            uint u1 = (uint)((1 << bits) - 1);
            int q16 = AtanLutCore(x.Raw, u1, bits);        // atan in Q16 (0..1)

            return Q16_16AngleToBn(q16, bits, signed: true);           // [-π/2..+π/2] → Bn signé
        }
        #endregion

        #region --- ATAN (IntN) --
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan<TBits>(IntN<TBits> x) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"Atan IntN : B2..B31 requis (bits={bits}).");

            int raw = x.Raw;
            if (raw == 0) return 0;

            // signe + |raw|
            int sign = raw >> 31;                                 // -1 si négatif, 0 sinon
            uint absRaw = (uint)((raw ^ sign) - sign);            // |raw|

            // échelle "1.0" pour IntN<Bn> : 2^(bits-1)-1 (ex: 127 en B8)
            uint scaleMax = (uint)((1 << (bits - 1)) - 1);

            // |x| ≤ 1 → LUT direct ; |x| > 1 → π/2 - atan(1/x), tout dans la même échelle
            int q16 = (absRaw <= scaleMax)
                ? AtanLutCore(absRaw, scaleMax, bits)
                : (TrigConsts.PI_2_Q[16]
                   - AtanLutCore((uint)(((ulong)scaleMax * scaleMax) / absRaw),
                                            scaleMax, bits));

            if (sign != 0) q16 = -q16;                            // atan(-x) = -atan(x)

            // fenêtre [-π/2 .. +π/2] → Bn signé [-max..+max] (même mapper que ASIN)
            return Q16_16AngleToBn(q16, bits, signed: true);
        }

        #endregion

        #region --- ATAN (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan<TInt, TFrac>(UFixed<TInt, TFrac> x)
            where TInt : struct where TFrac : struct
        {
            int F = UFixed<TInt, TFrac>.FracBitsConst;
            int Abits = IntN<TInt>.BitsConst;
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Atan UFixed : angle B2..B31 requis (bits={Abits}).");

            uint raw = x.Raw;                                       // unsigned
            if (raw == 0) return 0;

            uint one = (uint)(1 << F);
            int q16 = (raw <= one)
                ? AtanLutCore(raw, one, Abits)           // 0..1
                : (TrigConsts.PI_2_Q[16]
                   - AtanLutCore((uint)(((ulong)one * one) / raw), one, Abits));

            return Q16_16AngleToBn(q16, Abits, signed: true);        // sortie: angle principal signé
        }

        #endregion

        #region --- ATAN (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan<TInt, TFrac>(Fixed<TInt, TFrac> x)
            where TInt : struct where TFrac : struct
        {
            int F = Fixed<TInt, TFrac>.FracBitsConst;               // ex: 8 pour Qx.8
            int Abits = IntN<TInt>.BitsConst;                       // bits d’angle cible (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Atan Fixed : angle B2..B31 requis (bits={Abits}).");

            int raw = x.Raw;                                        // signé
            if (raw == 0) return 0;

            int sign = raw >> 31;
            uint absRaw = (uint)((raw ^ sign) - sign);              // |raw|

            uint one = (uint)(1 << F);                              // 1.0 en raw (QF)
            int q16 = (absRaw <= one)
                ? AtanLutCore(absRaw, one, Abits)        // |x| ≤ 1
                : (TrigConsts.PI_2_Q[16]                            // |x| > 1: π/2 - atan(1/x)
                   - AtanLutCore((uint)(((ulong)one * one) / absRaw), one, Abits));

            if (sign != 0) q16 = -q16;                              // impaire

            return Q16_16AngleToBn(q16, Abits, signed: true);        // fenêtre [-π/2..+π/2]
        }
        #endregion

        //----- ATAN2 -----
        #region --- ATAN2CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Atan2Core(int y, int x, uint scaleMax, int bits)
        {
            if (x == 0 && y == 0) return 0;

            int pi = TrigConsts.PI_Q[16];
            int pio2 = TrigConsts.PI_2_Q[16];

            int absY = System.Math.Abs(y), absX = System.Math.Abs(x);

            int baseAngle = (absX >= absY)
                ? AtanRatioQ1(absY, absX, scaleMax, bits)          // atan(|y|/|x|)
                : pio2 - AtanRatioQ1(absX, absY, scaleMax, bits);  // π/2 - atan(|x|/|y|)

            if (x >= 0)
                return (y >= 0) ? baseAngle : -baseAngle;
            else
                return (y >= 0) ? pi - baseAngle : baseAngle - pi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AtanRatioQ1(int num, int den, uint scaleMax, int bits)
        {
            // ratioRaw ∈ [0..scaleMax] = round( num/den * scaleMax )
            // Pas de float, arrondi au plus proche
            uint ratioRaw = (uint)(((long)num * scaleMax + (den >> 1)) / den);
            if (ratioRaw > scaleMax) ratioRaw = scaleMax; // sécurité
            return AtanLutCore(ratioRaw, scaleMax, bits); // Q16
        }
        #endregion

        #region --- ATAN2 (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan2<TBits>(UIntN<TBits> y, UIntN<TBits> x)
            where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"FixedMath.Atan2 LUT n'est défini que pour B2…B31 en unsigned (bits={bits}).");

            uint scaleMax = (uint)((1 << bits) - 1);         // 255 en B8
            int q16 = Atan2Core((int)y.Raw, (int)x.Raw, scaleMax, bits);
            return Q16_16AngleToBn_Atan2(q16, bits, false);  // [0..2π) → [0..2^n-1]
        }
        #endregion

        #region --- ATAN2 (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan2<TBits>(IntN<TBits> y, IntN<TBits> x)
            where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"FixedMath.Atan2 LUT n'est défini que pour B2…B31 en signed (bits={bits}).");

            uint scaleMax = (uint)((1 << (bits - 1)) - 1);   // 127 en B8
            int q16 = Atan2Core(y.Raw, x.Raw, scaleMax, bits);
            return Q16_16AngleToBn_Atan2(q16, bits, true);   // [-π..π] → [-max..+max]
        }
        #endregion

        #region --- ATAN2 (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan2<TUInt, TFrac>(UFixed<TUInt, TFrac> y, UFixed<TUInt, TFrac> x)
            where TUInt : struct
            where TFrac : struct
        {
            int F = UFixed<TUInt, TFrac>.FracBitsConst;   // échelle des valeurs (QF)
            int Abits = IntN<TUInt>.BitsConst;            // résolution ANGLE (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Atan2<UFixed> : angle B2..B31 requis (bits={Abits}).");

            uint scaleMax = (uint)(1 << F);               // 1.0 en QF
            int q16 = Atan2Core((int)y.Raw, (int)x.Raw, scaleMax, Abits);
            return Q16_16AngleToBn_Atan2(q16, Abits, signed: false); // [0..2π) pour unsigned
        }
        #endregion

        #region --- ATAN2 (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Atan2<TInt, TFrac>(Fixed<TInt, TFrac> y, Fixed<TInt, TFrac> x)
            where TInt : struct
            where TFrac : struct
        {
            int F = Fixed<TInt, TFrac>.FracBitsConst;     // échelle des valeurs (QF)
            int Abits = IntN<TInt>.BitsConst;             // résolution ANGLE (Bn)
            if (Abits < 2 || Abits > 31)
                throw new NotSupportedException($"Atan2<Fixed> : angle B2..B31 requis (bits={Abits}).");

            uint scaleMax = (uint)(1 << F);               // 1.0 en QF
            int q16 = Atan2Core(y.Raw, x.Raw, scaleMax, Abits);
            return Q16_16AngleToBn_Atan2(q16, Abits, signed: true);  // [-π..+π] pour signed
        }
        #endregion

        #endregion
    } 
}

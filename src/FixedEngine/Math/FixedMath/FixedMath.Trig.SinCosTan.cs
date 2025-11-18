using FixedEngine.Core;
using FixedEngine.LUT;
using System;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    public static partial class FixedMath
    {

        #region --- CATMULL-ROM ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CatmullRom(int y0, int y1, int y2, int y3, int tQ16)
        {
            // Catmull-Rom cubic interpolation (Q16.16)
            int t = tQ16;
            int t2 = (int)(((long)t * t) >> 16);
            int t3 = (int)(((long)t2 * t) >> 16);

            int a = ((-y0 + 3 * y1 - 3 * y2 + y3) >> 1);
            int b = (2 * y0 - 5 * y1 + 4 * y2 - y3) >> 1;
            int c = (y2 - y0) >> 1;
            int d = y1;

            long result = (a * (long)t3 + b * (long)t2 + c * (long)t + ((long)d << 16)) >> 16;
            return (int)result;
        }
        #endregion

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        //----- SIN -----
        #region --- SINCORE LUT Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SinCore(uint uraw, int bits)
        {
            const int lutBits = 12;
            var lut = SinLUT4096.LUT;

            int phaseBits = bits - 2;
            uint phaseMask = (phaseBits == 0) ? 0u : (1u << phaseBits) - 1u;
            uint phase = uraw & phaseMask;

            int quadrant = (int)(uraw >> (bits - 2)) & 0b11;
            int sign = ((quadrant & 0b10) == 0) ? 1 : -1;

            const int lutMask = (1 << lutBits) - 1;
            int fracBits = phaseBits - lutBits;

            // ----- CALCUL INDEX & FRACTION -----
            int idx, tQ16;

            if (fracBits <= 0)
            {
                // B2..B14: LUT direct
                idx = (int)(phase << (-fracBits));
                tQ16 = 0;
            }
            else if (bits == 15)
            {
                // B15: mapping haute précision
                int phaseMax = (1 << phaseBits) - 1;
                long idxQ16 = ((long)phase << 16) * lutMask / phaseMax;
                idx = (int)(idxQ16 >> 16);
                tQ16 = (int)(idxQ16 & 0xFFFF);
            }
            else
            {
                // B16+: mapping standard
                idx = (int)(phase >> fracBits);
                uint tMask = (1u << fracBits) - 1u;
                int tFrac = (int)(phase & tMask);
                int shift = 16 - fracBits;
                tQ16 = (shift >= 0) ? (tFrac << shift) : (tFrac >> (-shift));
            }

            // Clamp & symétrie
            idx = Max(0, Min(lutMask, idx));
            int lutIdx = ((quadrant & 1) == 0) ? idx : lutMask - idx;
            lutIdx = Max(0, Min(lutMask, lutIdx));

            // Voisinage
            int p0 = Max(0, lutIdx - 1);
            int p1 = lutIdx;
            int p2 = Min(lutMask, lutIdx + 1);
            int p3 = Min(lutMask, lutIdx + 2);

            // Interpolation
            int q16_16 = (fracBits <= 0)
                ? sign * lut[lutIdx]
                : sign * FixedMath.CatmullRom(lut[p0], lut[p1], lut[p2], lut[p3], tQ16);

            return Q16_16ToBn(q16_16, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Q16_16ToBn(int q16_16, int bits)
        {
            int max = (1 << (bits - 1)) - 1;
            int min = -(1 << (bits - 1));


            long tmp = (long)q16_16 * (long)max + (1L << 15);
            int val = (int)(tmp >> 16);

            if (val < min) val = min;
            if (val > max) val = max;
            return val;

        }
        #endregion

        #region --- SIN (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sin<TBits>(UIntN<TBits> angle)
            where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException(
                    $"FixedMath.Sin LUT n'est pas défini pour Bn={bits} en unsigned (min = B2).");


            return SinCore(angle.Raw, UIntN<TBits>.BitsConst);
        }
        #endregion

        #region --- SIN (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sin<TBits>(IntN<TBits> angle)
            where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            int raw = angle.Raw;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException(
                    $"FixedMath.Sin LUT n'est pas défini pour Bn={bits} en signed (seulement B2 à B31 supportés).");
            uint uraw = (uint)raw & ((1u << bits) - 1);
            return SinCore(uraw, bits);
        }
        #endregion

        #region --- SIN (UFixed) ---
        public static int Sin<TUInt, TFrac>(UFixed<TUInt, TFrac> angle)
            where TUInt : struct where TFrac : struct
        {
            int bits = UIntN<TUInt>.BitsConst;
            if (bits < 2 || bits > 31) 
                throw new NotSupportedException(
                    $"FixedMath.Sin(Fixed) : Bn={bits} non supporté (B2..B31).");

            return SinCore(angle.Raw, UIntN<TUInt>.BitsConst);
        }
        #endregion

        #region --- SIN (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sin<TInt, TFrac>(Fixed<TInt, TFrac> angle)
            where TInt : struct where TFrac : struct
        {
            int bits = IntN<TInt>.BitsConst;
            int raw = angle.Raw; 

            if (bits < 2 || bits > 31)
                throw new NotSupportedException(
                    $"FixedMath.Sin(Fixed) : Bn={bits} non supporté (B2..B31).");

            uint uraw = (uint)raw & ((1u << bits) - 1);
            return SinCore(uraw, bits);
        }
        #endregion

        //----- COS -----
        #region --- COSCORE Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetQuarterTurn(int bits)
            => (bits < 2) ? 0u : (1u << (bits - 2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CosCore(uint uraw, int bits)
        {
            uint mask = (bits == 32) ? 0xFFFF_FFFFu : ((1u << bits) - 1);
            uint quarter = GetQuarterTurn(bits);
            return SinCore((uraw + quarter) & mask, bits);
        }
        #endregion

        #region --- COS (UintN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cos<TBits>(UIntN<TBits> angle)
            where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException($"FixedMath.Cos LUT n'est pas défini pour Bn={bits} en unsigned (min = B2).");

            return CosCore(angle.Raw, bits);
        }

        #endregion

        #region --- COS (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cos<TBits>(IntN<TBits> angle)
            where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            int raw = angle.Raw;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"FixedMath.Cos LUT n'est pas défini pour Bn={bits} en signed (B2..B31).");

            uint uraw = (uint)raw & ((1u << bits) - 1); // wrap signed → unsigned
            return CosCore(uraw, bits);
        }
        #endregion

        #region --- COS (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cos<TUInt, TFrac>(UFixed<TUInt, TFrac> angle)
            where TUInt : struct where TFrac : struct
            => Cos((UIntN<TUInt>)angle);

        #endregion

        #region --- COS (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cos<TInt, TFrac>(Fixed<TInt, TFrac> angle)
            where TInt : struct where TFrac : struct
            => Cos((IntN<TInt>)angle);
        #endregion

        //----- TAN -----
        #region --- TANCORE LUT Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TanCore(uint raw, int bits)
        {
            const int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = TanLUT4096.LUT;

            int phaseBits = bits - 2;
            uint phaseMask = (phaseBits >= 32) ? 0xFFFFFFFFu : ((1u << phaseBits) - 1);

            // ----- Gestion rétro-faithful pour B2 (bits=2, phaseBits=0) -----
            if (phaseMask == 0)
            {
                // 4 positions : 0°, 90°, 180°, 270°
                int quadrant = (int)(raw >> 0) & 0b11;
                if (quadrant == 1)
                    return int.MaxValue; // tan(90°)
                if (quadrant == 3)
                    return int.MinValue; // tan(270°)
                return 0;               // tan(0°), tan(180°)
            }

            // ----- PATCH SPÉCIAL B14 -----
            if (bits == 14)
            {
                int quadrant = (int)(raw >> 12) & 0b11;
                uint phase = raw & 0x3FFF;
                int lutIdx = (int)(phase >> 2);

                // Symétrie quadrant
                if ((quadrant & 1) == 1)
                    lutIdx = lutMask - lutIdx;

                // Détection asymptote APRÈS symétrie
                if (lutIdx == lutMask && (quadrant & 1) == 1)
                    return quadrant == 1 ? int.MaxValue : int.MinValue;

                int val = lut[lutIdx];
                return (quadrant == 1 || quadrant == 3) ? -val : val;
            }

            // ----- LUT direct rétro-faithful pour B3..B13, B12 inclus -----
            if (phaseBits <= 12)
            {
                int quadrant = (int)(raw >> phaseBits) & 0b11;
                uint phase = raw & phaseMask;
                int lutIdx = (int)((long)phase * lutMask / phaseMask);
                if ((quadrant & 1) == 1) lutIdx = lutMask - lutIdx;
                if (phase == phaseMask && (quadrant & 1) == 1)
                    return quadrant == 1 ? int.MaxValue : int.MinValue;
                int val = lut[lutIdx];
                return (quadrant == 1 || quadrant == 3) ? -val : val;
            }

            // ----- Interpolation Catmull-Rom pour B15+ -----
            {
                int quadrant = (int)(raw >> phaseBits) & 0b11;
                uint phase = raw & phaseMask;
                long idxQ16 = ((long)phase << 16) * lutMask / phaseMask;
                int idx = (int)(idxQ16 >> 16);
                int tQ16 = (int)(idxQ16 & 0xFFFF);

                int baseIdx = (quadrant & 1) == 0 ? idx : lutMask - idx;
                int p0 = Max(0, baseIdx - 1);
                int p1 = baseIdx;
                int p2 = Min(lutMask, baseIdx + 1);
                int p3 = Min(lutMask, baseIdx + 2);

                long v0 = lut[p0], v1 = lut[p1], v2 = lut[p2], v3 = lut[p3];
                long t = tQ16, t2 = (t * t) >> 16, t3 = (t2 * t) >> 16;
                long a0 = -v0 + 3 * v1 - 3 * v2 + v3;
                long a1 = 2 * v0 - 5 * v1 + 4 * v2 - v3;
                long a2 = -v0 + v2;
                long a3 = 2 * v1;

                long acc = (a0 * t3) + (a1 * t2) + (a2 * t) + (a3 << 16);
                long val64 = acc >> 16;

                if (quadrant == 1 || quadrant == 2) val64 = -val64;
                if (idx == lutMask && (quadrant & 1) == 1)
                    return quadrant == 1 ? int.MaxValue : int.MinValue;
                return (int)Max(int.MinValue, Min(int.MaxValue, val64));
            }
        }
        #endregion

        #region --- TAN (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TBits>(UIntN<TBits> angle)
            where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException(
                    $"FixedMath.Tan n'est pas défini pour Bn={bits} (min: B2).");

            return Q16_16ToBn(TanCore(angle.Raw, bits), bits);
        }
        #endregion

        #region --- TAN (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TBits>(IntN<TBits> angle)
            where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException(
                    $"FixedMath.Tan n'est pas défini pour Bn={bits} (min: B2).");

            uint uraw = (uint)angle.Raw & ((1u << bits) - 1);
            return Q16_16ToBn(TanCore(uraw, bits), bits);
        }
        #endregion

        #region --- TAN (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TUInt, TFrac>(UFixed<TUInt, TFrac> angle)
            where TUInt : struct where TFrac : struct
            => Tan((UIntN<TUInt>)angle);
        #endregion

        #region --- TAN (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TInt, TFrac>(Fixed<TInt, TFrac> angle)
            where TInt : struct where TFrac : struct
            => Tan((IntN<TInt>)angle);
        #endregion

        #endregion

        // ==========================
        // --- DEBUG / RAW OUTPUT (sin/cos/tan) ---
        // ==========================
        #region --- DEBUG / RAW OUTPUT (sin/cos/tan) ---

        // ----- SIN -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SinRawCore(uint uraw, int bits)
        {
            const int lutBits = 12;
            var lut = SinLUT4096.LUT;

            int phaseBits = bits - 2;
            int fracBits = phaseBits - lutBits;

            int phaseMax = (1 << phaseBits) - 1;
            int phase = (int)(uraw & (uint)phaseMax);

            int quadrant = (int)(uraw >> (bits - 2)) & 0b11;
            int sign = ((quadrant & 0b10) == 0) ? 1 : -1;

            int lutMask = (1 << lutBits) - 1;
            int lutSize = lutMask + 1;

            bool isRetro = bits <= 14;
            uint denom = (phaseBits >= 31) ? 0x8000_0000u : (1u << phaseBits);
            int step = Max(1, lutSize / (int)denom);
            int idx_retro = phase * step;

            int idx_interp = 0, tQ16 = 0;
            if (!isRetro)
            {
                long idxQ16 = ((long)phase << 16) * (lutSize - 1) / phaseMax;
                idx_interp = (int)(idxQ16 >> 16);
                if (idx_interp < 0) idx_interp = 0;
                else if (idx_interp > lutMask) idx_interp = lutMask;
                tQ16 = (int)(idxQ16 & 0xFFFF);
            }

            int idx = isRetro ? idx_retro : idx_interp;
            int lutIdx = ((quadrant & 1) == 0) ? idx : lutMask - idx;
            if (lutIdx < 0) lutIdx = 0;
            else if (lutIdx > lutMask) lutIdx = lutMask;

            int p0 = Max(0, lutIdx - 1);
            int p1 = lutIdx;
            int p2 = Min(lutMask, lutIdx + 1);
            int p3 = Min(lutMask, lutIdx + 2);

            int q16_16 = (fracBits <= 0)
                ? sign * lut[lutIdx]
                : sign * FixedMath.CatmullRom(lut[p0], lut[p1], lut[p2], lut[p3], tQ16);

            return q16_16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRaw<TBits>(UIntN<TBits> angle) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return SinRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRaw<TBits>(IntN<TBits> angle) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            return SinRawCore((uint)angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRaw<TBits, TFrac>(UFixed<TBits, TFrac> angle)
            where TBits : struct
            where TFrac : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return SinRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRaw<TBits, TFrac>(Fixed<TBits, TFrac> angle) 
            where TBits : struct 
            where TFrac : struct
        {
            int bits = IntN<TBits>.BitsConst;
            return SinRawCore((uint)angle.Raw, bits);
        }




        // ----- COS -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CosRawCore(uint uraw, int bits)
        {
            uint mask = (bits == 32) ? 0xFFFF_FFFFu : (1u << bits) - 1;
            uint quarter = GetQuarterTurn(bits); // Quart de tour (π/2) en raw
            return SinRawCore((uraw + quarter) & mask, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CosRaw<TBits>(UIntN<TBits> angle) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return CosRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CosRaw<TBits>(IntN<TBits> angle) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            return CosRawCore((uint)angle.Raw, bits); // wrap signé → unsigned
        }


        //----- TAN -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TanRawCore(uint raw, int bits)
            => TanCore(raw, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TanRaw<TBits>(UIntN<TBits> angle)
        where TBits : struct
            => TanRawCore(angle.Raw, UIntN<TBits>.BitsConst);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TanRaw<TBits>(IntN<TBits> angle)
        where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"TanRaw<IntN> : B2..B31 requis (bits={bits}).");

            // wrap signé → unsigned sur N bits (évite toute pollution de quadrant)
            uint uraw = (uint)angle.Raw & ((1u << bits) - 1);

            return TanRawCore(uraw, bits);
        }

        #endregion


        // ==========================
        // --- Math Helpers ---
        // ==========================
        #region --- Math Helpers ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Max(int a, int b) => (a > b) ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Min(int a, int b) => (a < b) ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Max(uint a, uint b) => (a > b) ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Min(uint a, uint b) => (a < b) ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Max(long a, long b) => (a > b) ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Min(long a, long b) => (a < b) ? a : b;

        #endregion
    }
}


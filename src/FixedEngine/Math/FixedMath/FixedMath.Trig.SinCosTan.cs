using FixedEngine.Core;
using FixedEngine.LUT;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CatmullRomQ31(int y0, int y1, int y2, int y3, int tQ16)
        {

            long t = tQ16;
            long t2 = (t * t) >> 16;
            long t3 = (t2 * t) >> 16;

            long a = -y0 + 3 * (long)y1 - 3 * (long)y2 + y3;
            long b = 2 * (long)y0 - 5 * (long)y1 + 4 * (long)y2 - y3;
            long c = -y0 + y2;
            long d = 2 * (long)y1;

            long result = ((a * t3) >> 16) + ((b * t2) >> 16) + ((c * t) >> 16) + d;
            result >>= 1; 

            if (result > int.MaxValue) result = int.MaxValue;
            if (result < int.MinValue) result = int.MinValue;

            return (int)result;
        }
        #endregion

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        //----- SIN -----
        #region --- SINCORE LUT Retro ---

        #region --- SINCORE LUT HighBits ( > B16) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinCore_HQ(uint uraw, int bits)
        {
            const int LUT_SIZE = 4096;
            var sinLUT = SinLUT4096.LUT;         // Sin en Q1.31
            var angleLUT = SinAngleLUT4096Q32.LUT;  // Angles en Q0.32

            // --- Quadrant + phase ---
            int phaseBits = bits - 2;
            if (phaseBits < 0) phaseBits = 0;

            uint phaseMask = (phaseBits >= 32) ? 0xFFFFFFFFu : ((1u << phaseBits) - 1u);
            uint phase = uraw & phaseMask;
            int quadrant = (int)(uraw >> phaseBits) & 0b11;

            int sign = ((quadrant & 2) == 0) ? 1 : -1;

            // --- Normalisation phase ---
            // phase : bits de phase dans le quadrant
            uint phaseQ32;
            if (phaseBits == 0)
            {
                phaseQ32 = 0;
            }
            else if (phaseBits >= 32)
            {
                phaseQ32 = phase >> (phaseBits - 32);
            }
            else // phaseBits < 32
            {
                phaseQ32 = phase << (32 - phaseBits);
            }

            // symétrie dans le quadrant : 0..1 → 1..0
            if ((quadrant & 1) != 0)
                phaseQ32 = 0xFFFF_FFFFu - phaseQ32;

            // --- Mapping phase→index ---
            int idx = BinarySearchAngleQ32(angleLUT, phaseQ32);
            if (idx < 0) idx = 0;
            if (idx >= LUT_SIZE - 1) idx = LUT_SIZE - 2;

            uint angle0 = angleLUT[idx];
            uint angle1 = angleLUT[idx + 1];
            uint deltaAngle = angle1 - angle0;

            int tQ16;
            if (deltaAngle == 0)
            {
                tQ16 = 0;
            }
            else
            {
                // phaseQ32 ∈ [angle0, angle1] garanti par la recherche binaire
                uint diff = phaseQ32 - angle0;

                // Q0.32 → fraction Q16.16 : (diff / deltaAngle) en Q16
                ulong num = ((ulong)diff << 16);
                tQ16 = (int)(num / deltaAngle);
                if (tQ16 < 0) tQ16 = 0;
                else if (tQ16 > 65536) tQ16 = 65536;
            }

            // --- Interpolation LINÉAIRE ---
            int y1 = sinLUT[idx];
            int y2 = sinLUT[idx + 1];
            long dy = (long)(y2 - y1);

            long interp = y1 + ((dy * tQ16) >> 16);
            int resultQ31 = (int)interp;

            if (sign < 0) resultQ31 = -resultQ31;

            // --- Conversion Q1.31 → Bn ---
            return Q31ToBn(resultQ31, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearchAngleQ32(uint[] angleLUT, uint phaseQ32)
        {
            int left = 0;
            int right = angleLUT.Length - 1;

            while (left < right)
            {
                int mid = (left + right + 1) >> 1;
                if (angleLUT[mid] <= phaseQ32)
                    left = mid;
                else
                    right = mid - 1;
            }

            return left;
        }

        #endregion

        #region --- SINCORE LUT LowBits (<= B16) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinCore_LQ(uint uraw, int bits)
        {
            // LUT 4096 en Q1.31 (même que HQ)
            var sinLUT = SinLUT4096.LUT;

            // LUT d’angles en Q16.16, mais stockée en ushort
            var angleLUT = SinAngleLUT4096Q16.LUT;   // valeurs en [0..65535]

            const int LUT_SIZE = 4096;

            // ----- Quadrant + phase -----
            int phaseBits = bits - 2;
            if (phaseBits < 0) phaseBits = 0;
            if (phaseBits > 16) phaseBits = 16; // LQ = max 16 bits de phase

            uint phaseMask = (phaseBits == 0) ? 0 : ((1u << phaseBits) - 1u);
            uint phase = uraw & phaseMask;

            int quadrant = (int)(uraw >> phaseBits) & 0b11;
            int sign = ((quadrant & 2) == 0) ? 1 : -1;

            // ----- Normalisation Q16.16 -----
            // phaseQ16 : 0..65535
            uint phaseQ16;
            if (phaseBits == 0)
                phaseQ16 = 0;
            else if (phaseBits == 16)
                phaseQ16 = phase;
            else
                phaseQ16 = phase << (16 - phaseBits);

            // symétrie
            if ((quadrant & 1) != 0)
                phaseQ16 = 65535u - phaseQ16;

            // ----- Recherche angle → index -----
            int idx = BinarySearchAngleQ16(angleLUT, phaseQ16);
            if (idx < 0) idx = 0;
            if (idx >= LUT_SIZE - 1) idx = LUT_SIZE - 2;

            uint a0 = angleLUT[idx];
            uint a1 = angleLUT[idx + 1];
            uint delta = a1 - a0;

            // ----- Interpolation fraction Q16 -----
            int tQ16;
            if (delta == 0)
            {
                tQ16 = 0;
            }
            else
            {
                uint diff = phaseQ16 - a0;

                ulong num = ((ulong)diff << 16);
                tQ16 = (int)(num / delta);

                if (tQ16 < 0) tQ16 = 0;
                else if (tQ16 > 65535) tQ16 = 65535;
            }

            // ----- Interpolation sin (Q1.31 → Q1.31) -----
            int y1 = sinLUT[idx];
            int y2 = sinLUT[idx + 1];
            long dy = (long)(y2 - y1);

            long interp = y1 + ((dy * tQ16) >> 16);
            int resultQ31 = (int)interp;

            if (sign < 0)
                resultQ31 = -resultQ31;

            // ----- Conversion finale vers Bn -----
            return Q31ToBn(resultQ31, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearchAngleQ16(ushort[] angleLUT, uint phaseQ16)
        {
            int left = 0;
            int right = angleLUT.Length - 1;

            while (left < right)
            {
                int mid = (left + right + 1) >> 1;
                if (angleLUT[mid] <= phaseQ16)
                    left = mid;
                else
                    right = mid - 1;
            }

            return left;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Q31ToBn(int q31, int bits)
        {
            int max = (1 << (bits - 1)) - 1;
            int min = -(1 << (bits - 1));

            long tmp = (long)q31 * max;
            if (bits <= 16)
            {
                // arrondi vers le haut (vers zero pour négatif)
                tmp += (1L << 30);   // toujours positif
            }
            else
            {
                // arrondi symétrique haute précision
                tmp += (tmp >= 0 ? (1L << 30) : -(1L << 30));
            }
            int val = (int)(tmp >> 31);

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



            return bits <= 16
                ? SinCore_LQ(angle.Raw, UIntN<TBits>.BitsConst)
                : SinCore_HQ(angle.Raw, UIntN<TBits>.BitsConst);

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

            return bits <= 16
                ? SinCore_LQ(uraw, bits)
                : SinCore_HQ(uraw, bits);
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

            return bits <= 16
                ? SinCore_LQ(angle.Raw, UIntN<TUInt>.BitsConst)
                : SinCore_HQ(angle.Raw, UIntN<TUInt>.BitsConst);
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


            return bits <= 16
                ? SinCore_LQ(uraw, bits)
                : SinCore_HQ(uraw, bits);
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

            return bits <= 16
                ? SinCore_LQ((uraw + quarter) & mask, bits)
                : SinCore_HQ((uraw + quarter) & mask, bits);
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
            where TUInt : struct 
            where TFrac : struct
        {

            int bits = UIntN<TUInt>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException($"FixedMath.Cos LUT n'est pas défini pour Bn={bits} en unsigned (min = B2).");

            return CosCore(angle.Raw, bits);
        }


        #endregion

        #region --- COS (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cos<TInt, TFrac>(Fixed<TInt, TFrac> angle)
            where TInt : struct 
            where TFrac : struct
        {

            int bits = IntN<TInt>.BitsConst;
            int raw = angle.Raw;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"FixedMath.Cos LUT n'est pas défini pour Bn={bits} en signed (B2..B31).");

            uint uraw = (uint)raw & ((1u << bits) - 1); // wrap signed → unsigned
            return CosCore(uraw, bits);
        }

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

            return Q31ToBn(TanCore(angle.Raw, bits), bits);
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

            //uint uraw = (uint)angle.Raw & ((1u << bits) - 1);
            uint uraw = (uint)angle.Raw &  (MaskQ<TBits>() - 1);
            return Q31ToBn(TanCore(uraw, bits), bits);
        }
        #endregion

        #region --- TAN (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TUInt, TFrac>(UFixed<TUInt, TFrac> angle)
            where TUInt : struct 
            where TFrac : struct
        {

            int bits = UIntN<TUInt>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException(
                    $"FixedMath.Tan n'est pas défini pour Bn={bits} (min: B2).");

            return Q31ToBn(TanCore(angle.Raw, bits), bits);
        } 
        #endregion

        #region --- TAN (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tan<TInt, TFrac>(Fixed<TInt, TFrac> angle)
            where TInt : struct 
            where TFrac : struct
        {
            int bits = IntN<TInt>.BitsConst;
            if (bits < 2)
                throw new NotSupportedException(
                    $"FixedMath.Tan n'est pas défini pour Bn={bits} (min: B2).");

            //uint uraw = (uint)angle.Raw & ((1u << bits) - 1);
            uint uraw = (uint)angle.Raw & (MaskQ<TInt>() - 1);
            return Q31ToBn(TanCore(uraw, bits), bits);

        }
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
                : sign * FixedMath.CatmullRomQ31(lut[p0], lut[p1], lut[p2], lut[p3], tQ16);

            return q16_16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRawDebug<TBits>(UIntN<TBits> angle) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return SinRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRawDebug<TBits>(IntN<TBits> angle) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            return SinRawCore((uint)angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRawDebug<TBits, TFrac>(UFixed<TBits, TFrac> angle)
            where TBits : struct
            where TFrac : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return SinRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinRawDebug<TBits, TFrac>(Fixed<TBits, TFrac> angle) 
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
        public static int CosRawDebug<TBits>(UIntN<TBits> angle) where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst;
            return CosRawCore(angle.Raw, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CosRawDebug<TBits>(IntN<TBits> angle) where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            return CosRawCore((uint)angle.Raw, bits); // wrap signé → unsigned
        }


        //----- TAN -----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TanRawCore(uint raw, int bits)
            => TanCore(raw, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TanRawDebug<TBits>(UIntN<TBits> angle)
        where TBits : struct
            => TanRawCore(angle.Raw, UIntN<TBits>.BitsConst);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TanRawDebug<TBits>(IntN<TBits> angle)
        where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst;
            if (bits < 2 || bits > 31)
                throw new NotSupportedException($"TanRawDebug<IntN> : B2..B31 requis (bits={bits}).");

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


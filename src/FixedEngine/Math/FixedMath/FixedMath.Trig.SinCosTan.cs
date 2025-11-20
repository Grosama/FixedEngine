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
        static int CatmullAdaptiveQ31(int p0, int p1, int p2, int p3, int tQ16, int quadrant)
        {
            // Tension :
            // - Quadrants 0 & 3 : proche de Catmull standard (un peu adouci)
            // - Quadrants 1 & 2 : tension plus soft pour limiter les overshoots
            const int T_std_Q16 = (int)(0.90 * 65536);  // 0.90 en Q16.16
            const int T_soft_Q16 = (int)(0.55 * 65536);  // 0.55 en Q16.16

            int T = ((quadrant & 1) == 0) ? T_std_Q16 : T_soft_Q16;

            // Tangentes en Q1.31
            // m = 0.5 * (p2 - p0) * T
            // Q1.31 * Q16.16 = Q17.47 → >>16 pour revenir en Q1.31
            // et /2 → >>1, donc >>17 au total
            long d10 = (long)(p2 - p0);
            long d23 = (long)(p3 - p1);

            long m1 = (d10 * T + (1L << 16)) >> 17; // arrondi
            long m2 = (d23 * T + (1L << 16)) >> 17;

            // t, t², t³ en Q16.16
            int t = tQ16;
            int t2 = (int)(((long)t * t + (1L << 15)) >> 16);
            int t3 = (int)(((long)t2 * t + (1L << 15)) >> 16);

            // Coeffs Hermite en Q16.16
            // h1(t) =  2t³ - 3t² + 1
            // h2(t) =    t³ - 2t² + t
            // h3(t) = -2t³ + 3t²
            // h4(t) =    t³ -   t²
            long h1c = 2L * t3 - 3L * t2 + 65536L;
            long h2c = t3 - 2L * t2 + t;
            long h3c = -2L * t3 + 3L * t2;
            long h4c = t3 - t2;

            // Combinaison : tout en Q1.31 (arrondi à chaque étape)
            long h1 = (h1c * p1 + (1L << 15)) >> 16;
            long h2 = (h2c * m1 + (1L << 15)) >> 16;
            long h3 = (h3c * p2 + (1L << 15)) >> 16;
            long h4 = (h4c * m2 + (1L << 15)) >> 16;

            long res = h1 + h2 + h3 + h4;

            // Clamp “monotone” local pour éviter les overshoots
            int minLocal = p1 < p2 ? p1 : p2;
            int maxLocal = p1 > p2 ? p1 : p2;

            if (res < minLocal) res = minLocal;
            if (res > maxLocal) res = maxLocal;

            // Clamp global Q1.31 par sécurité
            if (res > int.MaxValue) return int.MaxValue;
            if (res < int.MinValue) return int.MinValue;

            return (int)res;
        }


        #endregion


        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        //----- SIN -----
        #region --- SINCORE LUT Retro ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SinCore(uint uraw, int bits)
        {
            // --- Configuration ---
            const int LUT_SIZE = 4096;
            var sinLUT = SinLUT4096.LUT;         // Valeurs sin en Q1.31
            var angleLUT = SinAngleLUT4096.LUT;  // Angles en Q16.16 (0..65536)
            var phaseMap = PhaseToIndex;         // Mapping phase→index

            // --- Extraction quadrant + phase ---
            int phaseBits = bits - 2;
            if (phaseBits < 0) phaseBits = 0;

            uint phaseMask = phaseBits >= 32 ? 0xFFFFFFFFu : ((1u << phaseBits) - 1u);
            uint phase = uraw & phaseMask;
            int quadrant = (int)(uraw >> phaseBits) & 0b11;

            // --- Signe selon quadrant ---
            int sign = (quadrant & 2) == 0 ? 1 : -1;

            // --- Normalisation phase → Q16.16 (0..65536 = 0°..90°) ---
            int phaseQ16;
            if (phaseBits == 0)
            {
                phaseQ16 = 0; // B2 : pas de phase
            }
            else if (phaseBits <= 16)
            {
                phaseQ16 = (int)(phase << (16 - phaseBits));
            }
            else
            {
                phaseQ16 = (int)(phase >> (phaseBits - 16));
            }

            // Clamp sécurité
            if (phaseQ16 < 0) phaseQ16 = 0;
            if (phaseQ16 > 65536) phaseQ16 = 65536;

            // --- CORRECTION : Appliquer symétrie AVANT mapping ---
            // Quadrants 1 et 3 : symétrie sin(π/2 - θ) = sin(π/2 - (π/2 - θ)) = sin(θ)
            // On inverse la phase : 90° - θ
            if ((quadrant & 1) != 0)
            {
                phaseQ16 = 65536 - phaseQ16;
            }

            // --- Mapping phase→index LUT via recherche binaire ---
            int idx = BinarySearchAngle(angleLUT, phaseQ16);

            // Clamp index
            if (idx < 0) idx = 0;
            if (idx >= LUT_SIZE - 1) idx = LUT_SIZE - 2;

            // --- Calcul du facteur d'interpolation t ---
            int angle0 = angleLUT[idx];
            int angle1 = angleLUT[idx + 1];
            int deltaAngle = angle1 - angle0;

            int tQ16;
            if (deltaAngle <= 0)
            {
                tQ16 = 0; // Sécurité division par zéro
            }
            else
            {
                // t = (phaseQ16 - angle0) / (angle1 - angle0)
                long num = (long)(phaseQ16 - angle0) << 16;
                tQ16 = (int)(num / deltaAngle);

                // Clamp t ∈ [0, 65536]
                if (tQ16 < 0) tQ16 = 0;
                if (tQ16 > 65536) tQ16 = 65536;
            }

            // --- Interpolation Catmull-Rom ---
            int p0 = Max(0, idx - 1);
            int p1 = idx;
            int p2 = Min(LUT_SIZE - 1, idx + 1);
            int p3 = Min(LUT_SIZE - 1, idx + 2);

            int resultQ31 = CatmullRomQ31(
                sinLUT[p0],
                sinLUT[p1],
                sinLUT[p2],
                sinLUT[p3],
                tQ16
            );

            // Appliquer le signe
            if (sign < 0) resultQ31 = -resultQ31;

            // --- Conversion Q1.31 → Bn ---
            return Q31ToBn(resultQ31, bits);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CatmullRomQ31(int p0, int p1, int p2, int p3, int tQ16)
        {

            long t = tQ16;
            long t2 = (t * t) >> 16;
            long t3 = (t2 * t) >> 16;

            // Coefficients en fixed-point (multipliés par 2 pour éviter 0.5)
            long a = -p0 + 3 * (long)p1 - 3 * (long)p2 + p3;
            long b = 2 * (long)p0 - 5 * (long)p1 + 4 * (long)p2 - p3;
            long c = -p0 + p2;
            long d = 2 * (long)p1;

            // result = (a*t³ + b*t² + c*t + d) / 2
            long result = ((a * t3) >> 16) + ((b * t2) >> 16) + ((c * t) >> 16) + d;
            result >>= 1; // Division par 2 finale

            // Clamp Q1.31
            if (result > int.MaxValue) result = int.MaxValue;
            if (result < int.MinValue) result = int.MinValue;

            return (int)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearchAngle(int[] angleLUT, int phaseQ16)
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Q31ToBn(int q31, int bits)
        {
            int max = (1 << (bits - 1)) - 1;
            int min = -(1 << (bits - 1));

            // Scale : q31 * max / MaxQ31
            long tmp = (long)q31 * max;
            tmp += (1L << 30); // Arrondi
            int val = (int)(tmp >> 31);

            // Clamp
            if (val < min) val = min;
            if (val > max) val = max;

            return val;
        }

        private static readonly ushort[] PhaseToIndex =
            BuildPhaseToIndex(SinAngleLUT4096.LUT);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort[] BuildPhaseToIndex(int[] angleQ16)
        {
            int len = angleQ16.Length;
            ushort[] map = new ushort[4096];

            int i = 0;
            for (int p = 0; p < 4096; p++)
            {
                int phaseNormQ16 = (int)(((long)p * 65536 + 2048) / 4095);

                while (i + 1 < len && angleQ16[i + 1] <= phaseNormQ16)
                    i++;

                if (i < 0) i = 0;
                if (i >= len) i = len - 1;

                map[p] = (ushort)i;
            }

            return map;
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
                : sign * FixedMath.CatmullRom(lut[p0], lut[p1], lut[p2], lut[p3], tQ16);

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


using FixedEngine.LUT;
using FixedEngine.Math;
using FixedEngine.Math.Consts;
using System;
using System.Runtime.CompilerServices;

public static class FixedMath
{

    #region --- CATMULL-ROM ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CatmullRom(int y0, int y1, int y2, int y3, int tQ16)
    {
        // Catmull-Rom cubic interpolation (Q16.16)
        // y0, y1, y2, y3 : 4 points consécutifs du LUT
        // tQ16 : fraction Q16.16 (0 = y1, 65536 = y2)

        // Convert Q16.16 fraction
        int t = tQ16;
        int t2 = (int)(((long)t * t) >> 16);
        int t3 = (int)(((long)t2 * t) >> 16);

        // Coefficients (adapté rétro, Catmull-Rom standard)
        // Tous les calculs en Q16.16
        int a = ((-y0 + 3 * y1 - 3 * y2 + y3) >> 1);
        int b = (2 * y0 - 5 * y1 + 4 * y2 - y3) >> 1;
        int c = (y2 - y0) >> 1;
        int d = y1;

        // Formule : ((a * t^3) + (b * t^2) + (c * t) + (d * 65536)) >> 16
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
    private static int SinCore(uint uraw, int bits, bool signed)
    {
        const int lutBits = 12;
        var lut = SinLUT4096.LUT;

        int phaseBits = bits - 2;
        int phaseMax = (1 << phaseBits) - 1;
        int phase = (int)(uraw & (uint)phaseMax);

        // Quadrant & signe rétro-faithful
        int quadrant = (int)(uraw >> (bits - 2)) & 0b11;
        int sign = ((quadrant & 0b10) == 0) ? 1 : -1;

        int lutMask = (1 << lutBits) - 1;
        int lutSize = lutMask + 1;

        int isRetro = (bits <= 14) ? 1 : 0;
        uint denom = (phaseBits >= 31) ? 0x8000_0000u : (1u << phaseBits); // évite 1<<32
        int step = Math.Max(1, lutSize / (int)denom);
        int idx_retro = phase * step;

        int idx_interp = 0, tQ16 = 0;
        if (isRetro == 0)
        {
            long idxQ16 = ((long)phase << 16) * (lutSize - 1) / phaseMax; // Q16.16
            idx_interp = (int)(idxQ16 >> 16);
            // clamp sans Math.Clamp (compat .NET Standard 2.0)
            if (idx_interp < 0) idx_interp = 0;
            else if (idx_interp > lutMask) idx_interp = lutMask;
            tQ16 = (int)(idxQ16 & 0xFFFF);
        }

        int idx = isRetro * idx_retro + (1 - isRetro) * idx_interp;
        int lutIdx = ((quadrant & 1) == 0) ? idx : lutMask - idx;
        if (lutIdx < 0) lutIdx = 0;
        else if (lutIdx > lutMask) lutIdx = lutMask;

        int p0 = Math.Max(0, lutIdx - 1);
        int p1 = lutIdx;
        int p2 = Math.Min(lutMask, lutIdx + 1);
        int p3 = Math.Min(lutMask, lutIdx + 2);

        int q16_16 = (isRetro == 1)
            ? sign * lut[lutIdx]
            : sign * FixedMath.CatmullRom(lut[p0], lut[p1], lut[p2], lut[p3], tQ16);

        if (bits == 8 && uraw == 64)
            Console.WriteLine($"angle=64: lutIdx={lutIdx}, lutVal={lut[lutIdx]}, sign={sign}, q16_16={q16_16}");
        return Q16_16ToBn(q16_16, bits, signed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Q16_16ToBn(int q16_16, int bits, bool signed)
    {
        if (signed)
        {
            int max = (1 << (bits - 1)) - 1;
            int min = -(1 << (bits - 1));
            int val = (q16_16 * max + (1 << 15)) >> 16;
            if (val < min) val = min;
            if (val > max) val = max;
            return val;
        }
        else
        {
            int max = (1 << bits) - 1;
            int val = (q16_16 * max + (1 << 16)) >> 17;
            int mask = (1 << bits) - 1;
            val &= mask;
            int signBit = 1 << (bits - 1);
            if ((val & signBit) != 0)
                val -= (1 << bits);
            return val;
        }
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


        return SinCore(angle.Raw, UIntN<TBits>.BitsConst, true);
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
        // PAS DE MULTIPLICATION DU SIGNE ICI !
        return SinCore(uraw, bits, true);
    }
    #endregion

    #region --- SIN (UFixed) --
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sin<TUInt, TFrac>(UFixed<TUInt, TFrac> angle)
        where TUInt : struct where TFrac : struct
        => Sin((UIntN<TUInt>)angle);
    #endregion

    #region --- SIN (Fixed) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sin<TInt, TFrac>(Fixed<TInt, TFrac> angle)
        where TInt : struct where TFrac : struct
            => Sin((IntN<TInt>)angle);
    #endregion

    //----- COS -----
    #region --- COSCORE Retro ---
    // Table readonly : quarter[bits] = 2^(bits-2)
    private static readonly uint[] QuarterTurns = BuildQuarterTurns();
    private static uint[] BuildQuarterTurns()
    {
        var q = new uint[33];
        for (int b = 0; b <= 32; b++)
            q[b] = (b < 2) ? 0u : (1u << (b - 2));
        return q;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CosCore(uint uraw, int bits, bool signed)
    {
        uint mask = (bits == 32) ? 0xFFFF_FFFFu : ((1u << bits) - 1);
        uint quarter = QuarterTurns[bits];
        return SinCore((uraw + quarter) & mask, bits, signed);
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

        return CosCore(angle.Raw, bits, true);
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
        return CosCore(uraw, bits, true);
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
            uint phase = raw & 0x3FFF;            // phase ∈ [0..16383]
            int lutIdx = (int)(phase >> 2);       // 16384 / 4 = 4096
            if ((quadrant & 1) == 1) lutIdx = lutMask - lutIdx;
            if (phase == 0x3FFF && (quadrant & 1) == 1)
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
            int p0 = Math.Max(0, baseIdx - 1);
            int p1 = baseIdx;
            int p2 = Math.Min(lutMask, baseIdx + 1);
            int p3 = Math.Min(lutMask, baseIdx + 2);

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
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, val64));
        }
    }
    #endregion

    #region --- TAN (UIntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Tan<TBits>(UIntN<TBits> angle)
        where TBits : struct
    {
        int bits = UIntN<TBits>.BitsConst;
        return Q16_16ToBn(TanCore(angle.Raw, bits), bits, true);
    }
    #endregion

    #region --- TAN (IntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Tan<TBits>(IntN<TBits> angle)
        where TBits : struct
    {
        int bits = IntN<TBits>.BitsConst;
        uint uraw = (uint)angle.Raw & ((1u << bits) - 1);  // wrap signé → unsigned
        return Q16_16ToBn(TanCore(uraw, bits), bits, true);
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

        // 2) Index + fraction (mapping [-1..+1] → [0..4095])
        long idxFracQ16 = ((long)(valQ16 + 65536) * LUT_MASK) << 16; // Q16.16
        idxFracQ16 /= 131072;                                        // / 2^17

        int idx = (int)(idxFracQ16 >> 16);     // 0..4094
        int tQ16 = (int)(idxFracQ16 & 0xFFFF);  // 0..65535

        // 3) Mode rétro: B2..B6 → nearest neighbor (écrase les horreurs B7..B14 vues au test)
        if (bits <= 6)
        {
            if (tQ16 >= 32768 && idx < LUT_MASK) idx++;   // nearest, pas floor
            return lut[idx];
        }

        // 4) Mode interpolé: B7+ → Catmull, avec routage "tail" pour la fin de courbe
        int ax = valQ16 < 0 ? -valQ16 : valQ16;          // |x|
        int x0Tail = AsinLUT_Tail2048.X0_Q;              // sin(75°) en Q16.16
        if (ax >= x0Tail)
        {
            int y = AsinTailEval_Q16(ax);                // asin(|x|) via LUT tail
            return (valQ16 < 0) ? -y : y;                // impaire
        }

        // Catmull sur la LUT 4096
        int p0 = lut[(idx == 0) ? 0 : idx - 1];
        int p1 = lut[idx];
        int p2 = lut[Math.Min(LUT_MASK, idx + 1)];
        int p3 = lut[Math.Min(LUT_MASK, idx + 2)];
        return FixedMath.CatmullRom(p0, p1, p2, p3, tQ16);
    }


    // ===== ASIN TAIL EVAL (x ∈ [sin(75°), 1.0], Q16.16) =====
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AsinTailEval_Q16(int xQ16)
    {
        const int QF = 16;
        int X0 = AsinLUT_Tail2048.X0_Q;  // sin(75°) en Q16.16
        int X1 = AsinLUT_Tail2048.X1_Q;  // 1.0 en Q16.16
        int N = AsinLUT_Tail2048.N;    // 2048
        int PAD = AsinLUT_Tail2048.PAD;  // 1
        var V = AsinLUT_Tail2048.LUT;

        // clamp sécurité
        if (xQ16 <= X0) return V[PAD + 0];
        if (xQ16 >= X1) return V[PAD + (N - 1)];

        // u = (x - X0) / (X1 - X0) en Q16
        long num = ((long)xQ16 - X0) << QF;
        int uQ16 = (int)(num / (X1 - X0));          // 0..65535

        // pos = u * (N-1)
        long posQ16 = (long)uQ16 * (N - 1);
        int idx = (int)(posQ16 >> QF);              // 0..N-2
        int tQ16 = (int)(posQ16 & 0xFFFF);          // 0..65535

        // fenêtre Catmull avec padding "physique" dans la table (2050 valeurs)
        int baseIdx = PAD + idx;
        int p0 = V[baseIdx - 1];
        int p1 = V[baseIdx + 0];
        int p2 = V[baseIdx + 1];
        int p3 = V[baseIdx + 2];

        return CatmullRom(p0, p1, p2, p3, tQ16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Q16_16AngleToBn(int q16_16, int bits, bool signed)
    {
        if (signed)
        {
            int max = (1 << (bits - 1)) - 1;         // 127 en B8
            long num = (long)q16_16 * max;
            long den = TrigConsts.PI_2_Q[16];         // π/2 en Q16
            int result = (int)((num + (den >> 1)) / den);  // arrondi au plus proche

            // bornes [-max..+max]
            if (result < -max) result = -max;
            if (result > max) result = max;
            return result;
        }
        else
        {
            int max = (1 << bits) - 1;
            long num = (long)(q16_16 + TrigConsts.PI_2_Q[16]) * max; // shift to [0..π]
            long den = TrigConsts.PI_Q[16];
            int result = (int)((num + (den >> 1)) / den);
            if (result < 0) result = 0;
            if (result > max) result = max;
            return result;
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Q16_16AngleToBn_Atan2(int q16_16, int bits, bool signed)
    {
        // q16_16 ∈ [-π .. +π], mais on veut [0 .. 2π) pour le mapping plein cercle.
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
    #endregion

    #region --- ASIN (UIntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Asin<TBits>(UIntN<TBits> v) where TBits : struct
    {
        int bits = UIntN<TBits>.BitsConst;
        if (bits < 2 || bits > 31)
            throw new NotSupportedException($"Asin<UIntN> : B2..B31 requis (bits={bits}).");

        uint maxRaw = (1u << bits) - 1;

        // map [0..max] -> [-65536..+65536] (rétro-faithful en entiers)
        int valQ16 = (int)((((long)v.Raw * 2 - maxRaw) * 65536L) / maxRaw);

        int acosQ16 = AcosLutCore(valQ16, bits);
        int asinQ16 = TrigConsts.PI_2_Q[16] - acosQ16;

        // asin sort un angle SIGNÉ ([-pi/2..+pi/2]) -> Bn signé
        return Q16_16AngleToBn(asinQ16, bits, signed: true);
    }
    #endregion

    #region --- ASIN (IntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Asin<TBits>(IntN<TBits> v) where TBits : struct
    {
        int bits = IntN<TBits>.BitsConst;
        if (bits < 2 || bits > 31)
            throw new NotSupportedException($"Asin<IntN> : B2..B31 requis (bits={bits}).");

        // 1) bit-faithful -> Q16
        int raw = (v.Raw << (32 - bits)) >> (32 - bits);
        int valQ16 = (bits == 17) ? raw
                  : (bits > 17) ? (raw >> (bits - 17))
                                : (raw << (17 - bits));

        // 2) asin(x) = pi/2 - acos(x), donc on utilise le core ACOS plus stable aux bords
        int acosQ16 = AcosLutCore(valQ16, bits);
        int asinQ16 = TrigConsts.PI_2_Q[16] - acosQ16;

        // 3) angle signé [-pi/2..+pi/2] -> Bn signé
        return Q16_16AngleToBn(asinQ16, bits, signed: true);
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
        return Q16_16AngleToBn(asinQ16, Abits, signed: true);
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
        return Q16_16AngleToBn(asinQ16, Abits, signed: true);
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

        int absY = Math.Abs(y), absX = Math.Abs(x);

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

    // ==========================
    // --- RACINE CARRÉE ---
    // ==========================
    #region --- RACINE CARRÉE ---

    // Méthode interne, standard rétro "shift & subtract"
    private static uint IntegerSqrt(uint a, int bits)
    {
        // bits = nombre de bits significatifs (ex: 8, 16, 24, 32)
        // On veut démarrer au bit pair le plus élevé < bits*2
        int start = ((bits - 1) / 2) * 2;
        uint bit = 1u << start;
        uint res = 0;
        while (bit > a) bit >>= 2;
        while (bit != 0)
        {
            if (a >= res + bit)
            {
                a -= res + bit;
                res = (res >> 1) + bit;
            }
            else
            {
                res >>= 1;
            }
            bit >>= 2;
        }
        return res;
    }

    /// <summary>
    /// Racine carrée entière (bit-faithful) pour UIntN<TBits>.
    /// Le résultat est toujours dans la plage [0, 2^N-1], aucune utilisation de float.
    /// </summary>
    #region --- RACINE CARREE (UIntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIntN<TBits> Sqrt<TBits>(UIntN<TBits> value)
        where TBits : struct
    {
        int bits = UIntN<TBits>.BitsConst; // <--- Lecture dynamique du format
        return new UIntN<TBits>(IntegerSqrt(value.Raw, bits));
    }
    #endregion

    /// <summary>
    /// Racine carrée entière (bit-faithful) pour IntN<TBits> : sqrt(abs(x))
    /// Le résultat est toujours >= 0, jamais négatif.
    /// </summary>
    #region --- RACINE CARREE (IntN) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntN<TBits> Sqrt<TBits>(IntN<TBits> value)
        where TBits : struct
    {
        int bits = IntN<TBits>.BitsConst; // Récupère la taille du format
        int v = value.Raw;
        uint abs = (uint)(v < 0 ? -v : v); // Authentique rétro : artefact possible sur MinValue
        uint r = IntegerSqrt(abs, bits);   // Passe bits dynamiquement !
        return new IntN<TBits>((int)r);
    }
    #endregion

    /// <summary>
    /// Racine carrée “bit-faithful” pour UFixed<TUInt, TFrac> (Q-format rétro).
    /// Algo rétro : sqrt(x) = IntegerSqrt(x << n). Résultat dans le même format Qm.n.
    /// </summary>
    #region --- RACINE CARREE (UFixed) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UFixed<TUInt, TFrac> Sqrt<TUInt, TFrac>(UFixed<TUInt, TFrac> value)
        where TUInt : struct
        where TFrac : struct
    {
        uint raw = value.Raw;
        int fracBits = BitsOf<TFrac>.Value;
        int bits = BitsOf<TUInt>.Value; // nombre de bits du backing integer (UInt8, UInt16, UInt32...)

        // On fait "x << n" (n = bits de fraction)
        uint shifted = raw << fracBits;

        uint sqrtRaw = IntegerSqrt(shifted, bits + fracBits); // 🟢 bits totaux utilisés !

        // Wrap sur la taille du type, compatible rétro (cf. constructeur UFixed)
        return new UFixed<TUInt, TFrac>(sqrtRaw);
    }
    #endregion

    /// <summary>
    /// Racine carrée bit-faithful pour Fixed<TInt, TFrac> (signed, Q-format).
    /// Convention rétro : sqrt(x) = sqrt(abs(x)), pas de NaN, jamais négatif.
    /// </summary>
    #region --- RACINE CARREE (Fixed) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed<TInt, TFrac> Sqrt<TInt, TFrac>(Fixed<TInt, TFrac> value)
        where TInt : struct
        where TFrac : struct
    {
        int raw = value.Raw;
        int fracBits = BitsOf<TFrac>.Value;
        int bits = BitsOf<TInt>.Value; // Nombre de bits du backing integer

        // Valeur absolue (authentique rétro)
        uint absRaw = (uint)(raw < 0 ? -raw : raw);

        uint shifted = absRaw << fracBits; // Qm.n : x << n
        uint sqrtRaw = IntegerSqrt(shifted, bits + fracBits);

        // Wrap signé rétro : jamais négatif, jamais NaN.
        return new Fixed<TInt, TFrac>((int)sqrtRaw);
    }
    #endregion

    #endregion

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

    // ==========================
    // --- HELPERS ---
    // ==========================
    #region --- HELPERS ---
    public static int ConstQ<TFrac>(float value)
    where TFrac : struct
    {
        int fracBits = BitsOf<TFrac>.Value;
        return (int)(value * (1 << fracBits));
    }
    #endregion

    // ==========================
    // --- HELPERS CONSTANTES TRIGONOMÉTRIQUES ---
    // ==========================
    #region --- HELPERS CONSTANTES TRIG (exhaustif, tous Q-format) ---

    /// <summary>π au format Q selon TFrac.</summary>
    public static int PiQ<TFrac>() where TFrac : struct
        => TrigConsts.PI_Q[BitsOf<TFrac>.Value];

    /// <summary>2π au format Q selon TFrac.</summary>
    public static int Pi2Q<TFrac>() where TFrac : struct
        => TrigConsts.PI2_Q[BitsOf<TFrac>.Value];

    /// <summary>π/2 au format Q selon TFrac.</summary>
    public static int Pi_2Q<TFrac>() where TFrac : struct
        => TrigConsts.PI_2_Q[BitsOf<TFrac>.Value];

    /// <summary>1/π au format Q selon TFrac.</summary>
    public static int InvPiQ<TFrac>() where TFrac : struct
        => TrigConsts.INV_PI_Q[BitsOf<TFrac>.Value];

    /// <summary>1/(2π) au format Q selon TFrac.</summary>
    public static int InvPi2Q<TFrac>() where TFrac : struct
        => TrigConsts.INV_PI2_Q[BitsOf<TFrac>.Value];

    /// <summary>π/180 (degrés vers radians) au format Q selon TFrac.</summary>
    public static int DegToRadQ<TFrac>() where TFrac : struct
        => TrigConsts.DEG_TO_RAD_Q[BitsOf<TFrac>.Value];

    /// <summary>180/π (radians vers degrés) au format Q selon TFrac.</summary>
    public static int RadToDegQ<TFrac>() where TFrac : struct
        => TrigConsts.RAD_TO_DEG_Q[BitsOf<TFrac>.Value];

    #endregion

    // ==========================
    // --- HELPERS CONSTANTES LOGARITHMIQUES ---
    // ==========================
    #region --- HELPERS CONSTANTES LOG (exhaustif, tous Q-format) ---

    /// <summary>ln(2) au format Q selon TFrac.</summary>
    public static int Ln2Q<TFrac>() where TFrac : struct
        => LogConsts.LN2_Q[BitsOf<TFrac>.Value];

    /// <summary>log2(e) au format Q selon TFrac.</summary>
    public static int Log2eQ<TFrac>() where TFrac : struct
        => LogConsts.LOG2E_Q[BitsOf<TFrac>.Value];

    #endregion

    // ==========================
    // --- HELPERS CONSTANTES RACINE ---
    // ==========================
    #region --- HELPERS CONSTANTES RACINE (exhaustif, tous Q-format) ---

    /// <summary>√2 (racine carrée de 2) au format Q selon TFrac.</summary>
    public static int Sqrt2Q<TFrac>() where TFrac : struct
        => SqrtConsts.SQRT2_Q[BitsOf<TFrac>.Value];

    /// <summary>√3 (racine carrée de 3) au format Q selon TFrac.</summary>
    public static int Sqrt3Q<TFrac>() where TFrac : struct
        => SqrtConsts.SQRT3_Q[BitsOf<TFrac>.Value];

    /// <summary>√5 (racine carrée de 5) au format Q selon TFrac.</summary>
    /* public static int Sqrt5Q<TFrac>() where TFrac : struct
        => SqrtConsts.SQRT5_Q[BitsOf<TFrac>.Value];*/

    //<summary>To-Do</summary>
    /*public static Fixed<TInt, TFrac> InvSqrt<TInt, TFrac>(Fixed<TInt, TFrac> x)
    where TInt : struct
    where TFrac : struct
    {
        // Implémentation Newton-Raphson *ou* LUT dynamique, mais pure bitwise, no float.
        // À documenter : usage pour normalisation/usage dynamique.
    }*/

    /// <summary>1/√2 (racine carrée inverse de 2) au format Q selon TFrac.</summary>
    public static int InvSqrt2Q<TFrac>() where TFrac : struct
        => SqrtConsts.INV_SQRT2_Q[BitsOf<TFrac>.Value];

    /// <summary>1/√3 (racine carrée inverse de 3) au format Q selon TFrac.</summary>
    public static int InvSqrt3Q<TFrac>() where TFrac : struct
        => SqrtConsts.INV_SQRT3_Q[BitsOf<TFrac>.Value];

    /// <summary>√π (racine carrée de Pi) au format Q selon TFrac.</summary>
    /* public static int SqrtPiQ<TFrac>() where TFrac : struct
        => SqrtConsts.SQRT_PI_Q[BitsOf<TFrac>.Value];*/

    #endregion

    // ==========================
    // --- HELPERS CONSTANTES PHI (NOMBRE D’OR) ---
    // ==========================
    #region --- HELPERS CONSTANTES PHI (exhaustif, tous Q-format) ---

    /// <summary>ln(φ) (nombre d’or) au format Q selon TFrac.</summary>
    public static int LnPhiQ<TFrac>() where TFrac : struct
        => PhiConsts.LN_PHI_Q[BitsOf<TFrac>.Value];

    /// <summary>log2(φ) (nombre d’or) au format Q selon TFrac.</summary>
    public static int Log2PhiQ<TFrac>() where TFrac : struct
        => PhiConsts.LOG2_PHI_Q[BitsOf<TFrac>.Value];

    /// <summary>√φ (racine carrée du nombre d’or) au format Q selon TFrac.</summary>
    public static int SqrtPhiQ<TFrac>() where TFrac : struct
        => PhiConsts.SQRT_PHI_Q[BitsOf<TFrac>.Value];

    #endregion

    // ==========================
    // --- HELPERS CONSTANTES MASK ---
    // ==========================
    #region --- HELPERS CONSTANTES MASK (exhaustif, tous formats) ---

    /// <summary>Mask binaire pour N bits.</summary>
    public static uint MaskQ<TBits>() where TBits : struct
        => Mask.MASKS[BitsOf<TBits>.Value];

    /// <summary>Bit de signe pour N bits.</summary>
    public static uint SignBitQ<TBits>() where TBits : struct
        => Mask.SIGN_BITS[BitsOf<TBits>.Value];

    /// <summary>Valeur min signée pour N bits.</summary>
    public static int SignedMinQ<TBits>() where TBits : struct
        => Mask.SIGNED_MIN[BitsOf<TBits>.Value];

    /// <summary>Valeur max signée pour N bits.</summary>
    public static int SignedMaxQ<TBits>() where TBits : struct
        => Mask.SIGNED_MAX[BitsOf<TBits>.Value];

    /// <summary>Valeur max unsigned pour N bits.</summary>
    public static uint UnsignedMaxQ<TBits>() where TBits : struct
        => Mask.UNSIGNED_MAX[BitsOf<TBits>.Value];

    #endregion

    // ==========================
    // --- HELPERS LUT ---
    // ==========================
    #region --- HELPERS LUT ---
    public static int SinLutQ16(int idx) => SinLUT4096.LUT[idx & 1023];
    public static int AtanLutQ16(int idx) => AtanLUT1024.LUT[idx & 1023];
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
        int phaseMax = (1 << phaseBits) - 1;
        int phase = (int)(uraw & (uint)phaseMax);

        int quadrant = (int)(uraw >> (bits - 2)) & 0b11;
        int sign = ((quadrant & 0b10) == 0) ? 1 : -1;

        int lutMask = (1 << lutBits) - 1;
        int lutSize = lutMask + 1;

        bool isRetro = bits <= 14;
        uint denom = (phaseBits >= 31) ? 0x8000_0000u : (1u << phaseBits);
        int step = Math.Max(1, lutSize / (int)denom);
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

        int p0 = Math.Max(0, lutIdx - 1);
        int p1 = lutIdx;
        int p2 = Math.Min(lutMask, lutIdx + 1);
        int p3 = Math.Min(lutMask, lutIdx + 2);

        int q16_16 = isRetro
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


    // ----- COS -----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CosRawCore(uint uraw, int bits)
    {
        uint mask = (bits == 32) ? 0xFFFF_FFFFu : (1u << bits) - 1;
        uint quarter = QuarterTurns[bits]; // Quart de tour (π/2) en raw
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
            uint phase = raw & 0x3FFF;            // phase ∈ [0..16383]
            int lutIdx = (int)(phase >> 2);       // 16384 / 4 = 4096
            if ((quadrant & 1) == 1) lutIdx = lutMask - lutIdx;
            if (phase == 0x3FFF && (quadrant & 1) == 1)
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
            int p0 = Math.Max(0, baseIdx - 1);
            int p1 = baseIdx;
            int p2 = Math.Min(lutMask, baseIdx + 1);
            int p3 = Math.Min(lutMask, baseIdx + 2);

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
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, val64));
        }


    }

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
}


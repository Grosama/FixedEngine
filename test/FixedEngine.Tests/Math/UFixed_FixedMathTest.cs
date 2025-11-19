using FixedEngine.Core;
using FixedEngine.LUT;
using FixedEngine.Math;
using NUnit.Framework;

namespace FixedEngine.Tests.Math
{

    [TestFixture]
    public class UFixed_FixedMathTest
    {

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        #region --- SIN LUT Retro (UFixed) ---
        [Test]
        [Category("FixedMath/UFixed")]
        public void Sin_UFixed_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;
            for (int totalBits = 2; totalBits <= 32; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(UFixed<,>).MakeGenericType(tagTotal, tagFrac);

                uint maxRawU = (1u << totalBits) - 1u;
                ulong domain = (ulong)maxRawU + 1;
                int phaseBits = totalBits - 2;

                // Mode bit-faithful seulement si Bn ≤ lutBits+2 (ici 14)
                if (totalBits > lutBits + 2)
                {
                    System.Console.WriteLine($"B{totalBits}: au-delà de la zone bit-faithful, skip (interpolation active)");
                    continue;
                }
                for (uint raw = 0; raw <= maxRawU; raw++)
                {
                    var angle = Activator.CreateInstance(angleType, raw);
                    uint uraw = raw;
                    int phase = (int)(uraw & ((1u << phaseBits) - 1u));
                    int quadrant = (int)(uraw >> (totalBits - 2)) & 0b11;
                    int sign = (quadrant < 2) ? 1 : -1;
                    int idx;
                    if (phaseBits > lutBits)
                        idx = phase >> (phaseBits - lutBits);
                    else
                        idx = phase << (lutBits - phaseBits);
                    idx &= lutMask;
                    int lutIdx = (quadrant == 0 || quadrant == 2) ? idx : lutMask - idx;
                    int expected = sign * lut[lutIdx];

                    // Appel vrai code prod
                    var miGen = typeof(FixedMath)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == "SinRawDebug"
                            && m.IsGenericMethod
                            && m.GetParameters().Length == 1
                            && m.GetParameters()[0].ParameterType.IsGenericType
                            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>));
                    if (miGen == null)
                        Assert.Fail("Méthode générique FixedMath.SinRawDebug<TUInt, TFrac>(UFixed<TUInt, TFrac>) absente !");
                    var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                    int resultInt = (int)mi.Invoke(null, new[] { angle });

                    Assert.That(resultInt, Is.EqualTo(expected),
                        $"B{totalBits}, raw={raw}, expected={expected}, got={resultInt}");
                }
                System.Console.WriteLine($"B{totalBits} : bit-faithful unsigned validé ({domain} valeurs)");
            }
        }

        [Explicit]
        [Test]
        [Category("FixedMath/UFixed")]
        public void Sin_UFixed_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(98765);

            for (int totalBits = 2; totalBits <= 31; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(UFixed<,>).MakeGenericType(tagTotal, tagFrac);
                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRawDebug"
                        && m.IsGenericMethod
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsGenericType
                        && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>));
                if (miGen == null)
                {
                    System.Console.WriteLine("Méthode générique FixedMath.SinRawDebug<TUInt, TFrac>(UFixed<TUInt, TFrac>) absente : SKIP");
                    continue;
                }
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                uint maxRawU = (1u << totalBits) - 1u;
                ulong domain = (ulong)maxRawU + 1u;
                int numSamples = (totalBits >= 28) ? 1_000_000 : (int)domain;
                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;
                for (int i = 0; i < numSamples; i++)
                {
                    uint uraw;
                    if (totalBits >= 28)
                    {
                        ulong span = (ulong)maxRawU + 1u;
                        uraw = (uint)rng.NextInt64(0, (long)span);
                    }
                    else
                    {
                        uraw = (uint)i;
                    }
                    var angle = Activator.CreateInstance(angleType, uraw);
                    int val = (int)mi.Invoke(null, new[] { angle });
                    // Map sur [0, 2π)
                    double radians = (double)uraw / ((double)maxRawU + 1.0) * 2.0 * System.Math.PI;
                    int expected = (int)System.Math.Round(System.Math.Sin(radians) * 65536);
                    int diff = System.Math.Abs(val - expected);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = (double)diff / 65536.0;
                        maxDiffDeg = radians * 180.0 / System.Math.PI;
                        double cosTheta = System.Math.Cos(radians);
                        maxDiffAngleEqDeg = (cosTheta != 0)
                            ? maxDiffValue / System.Math.Abs(cosTheta) * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
                    }
                }
                report += $"\nB{totalBits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
            }
            System.Console.WriteLine(report);
            Assert.Pass(report);
        }


        [Test]
        [Category("FixedMath/UFixed")]
        public void Sin_UFixed_B2toB31_VsMathSin()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);

            // Trouve la bonne méthode Sin générique
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Sin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>));
            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Sin<TUInt, TFrac>(UFixed<TUInt, TFrac>) introuvable.");

            for (int totalBits = 2; totalBits <= 31; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }

                var angleType = typeof(UFixed<,>).MakeGenericType(tagTotal, tagFrac);
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);

                // Domaine binaire complet sur totalBits
                uint maxRawU = (1u << totalBits) - 1u;
                int maxAmp = (1 << (totalBits - 1)) - 1;

                int numSamples = (totalBits >= 28)
                    ? 1_000_000
                    : System.Math.Min((int)(maxRawU + 1u), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"totalBits={totalBits} : numSamples <= 0, skip.");
                    continue;
                }

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint uraw;
                    if (totalBits >= 28)
                    {
                        ulong span = (ulong)maxRawU + 1UL;
                        long r = rng.NextInt64(0, (long)span);
                        uraw = (uint)r;
                    }
                    else
                    {
                        ulong span = (ulong)maxRawU;
                        uraw = (uint)((span * (ulong)i) / (ulong)(numSamples - 1));
                    }

                    // Injection brute (pas de shift !)
                    var angle = Activator.CreateInstance(angleType, uraw);

                    int val = (int)mi.Invoke(null, new[] { angle });

                    // Même mapping que Fixed (cycle complet sur tout le domaine binaire)
                    double radians = (double)uraw / ((double)maxRawU + 1.0) * 2.0 * System.Math.PI;
                    double s = System.Math.Sin(radians);
                    int expected = (int)System.Math.Round(s * maxAmp);
                    if (expected < -maxAmp) expected = -maxAmp;
                    if (expected > maxAmp) expected = maxAmp;

                    long diffLong = (long)val - (long)expected;
                    if (diffLong < 0) diffLong = -diffLong;
                    if (diffLong > maxDiffInt)
                    {
                        maxDiffInt = diffLong;
                        maxDiffNorm = (double)diffLong / maxAmp;
                        atDeg = radians * 180.0 / System.Math.PI;
                        rawAtMax = (int)uraw;
                        valAtMax = val;
                        expAtMax = expected;
                    }
                }

                report += $"\nB{totalBits}\t{maxDiffInt}\t{maxDiffNorm:0.000000}\t{atDeg:0.###}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
            }
            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion

        #region --- COS LUT Retro (UFixed) ---
        [Test]
        [Category("FixedMath/UFixed")]
        public void Cos_UFixed_B2toB31_VsMathCos()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(54321);

            // Trouve la bonne méthode Cos générique
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Cos"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>));
            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Cos<TUInt, TFrac>(UFixed<TUInt, TFrac>) introuvable.");

            for (int totalBits = 2; totalBits <= 31; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }

                var angleType = typeof(UFixed<,>).MakeGenericType(tagTotal, tagFrac);
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);

                uint maxRawU = (1u << totalBits) - 1u;
                int maxAmp = (1 << (totalBits - 1)) - 1;

                int numSamples = (totalBits >= 28)
                    ? 1_000_000
                    : System.Math.Min((int)(maxRawU + 1u), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"totalBits={totalBits} : numSamples <= 0, skip.");
                    continue;
                }

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint uraw;
                    if (totalBits >= 28)
                    {
                        ulong span = (ulong)maxRawU + 1UL;
                        long r = rng.NextInt64(0, (long)span);
                        uraw = (uint)r;
                    }
                    else
                    {
                        ulong span = (ulong)maxRawU;
                        uraw = (uint)((span * (ulong)i) / (ulong)(numSamples - 1));
                    }

                    var angle = Activator.CreateInstance(angleType, uraw);

                    int val = (int)mi.Invoke(null, new[] { angle });

                    double radians = (double)uraw / ((double)maxRawU + 1.0) * 2.0 * System.Math.PI;
                    double c = System.Math.Cos(radians);
                    int expected = (int)System.Math.Round(c * maxAmp);
                    if (expected < -maxAmp) expected = -maxAmp;
                    if (expected > maxAmp) expected = maxAmp;

                    long diffLong = (long)val - (long)expected;
                    if (diffLong < 0) diffLong = -diffLong;
                    if (diffLong > maxDiffInt)
                    {
                        maxDiffInt = diffLong;
                        maxDiffNorm = (double)diffLong / maxAmp;
                        atDeg = radians * 180.0 / System.Math.PI;
                        rawAtMax = (int)uraw;
                        valAtMax = val;
                        expAtMax = expected;
                    }
                }

                report += $"\nB{totalBits}\t{maxDiffInt}\t{maxDiffNorm:0.000000}\t{atDeg:0.###}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
            }
            Console.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion

        #endregion

        // ==========================
        // --- ASIN/ACOS LUT Retro ---
        // ==========================
        #region --- ASIN/ACOS LUT Retro ---

        // ----- ASIN -----

        #region --- ASIN LUT Retro (UFixed Q0.8) ---
        [Test]
        public void Asin_UFixed_Q0_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Core.B8"); // F = 8 (Q0.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            // Inverse approx. du mapping prod UFixed->Q16 (TRUNC):
            // valQ16 = (((raw*2 - maxRaw) * 65536) / maxRaw)
            static int ValQ16ToRaw_UFixed_Trunc(int valQ16, int F)
            {
                long maxRaw = (1L << F) - 1L;                   // 255 pour Q0.8
                long rawApprox = (valQ16 * maxRaw) / 65536L;    // TRUNC
                long raw = (rawApprox + maxRaw) / 2L;           // ((t*max + max)/2)
                if (raw < 0) raw = 0;
                if (raw > maxRaw) raw = maxRaw;
                return (int)raw;
            }

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Core.B{bits}");
                if (intTag == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var ufixedType = typeof(UFixed<,>).MakeGenericType(intTag, fracTag);
                var miAsin = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>))
                    .MakeGenericMethod(intTag, fracTag);

                // ctor(uint) pour contrôler le raw sans interprétation signée
                var ctorUInt = ufixedType.GetConstructor(new[] { typeof(uint) });
                if (ctorUInt == null) Assert.Fail($"Ctor (uint) introuvable pour {ufixedType}");

                // ticks signés: [-π/2..+π/2] → [-max..+max]
                int maxTick = (1 << (bits - 1)) - 1;
                double ticksToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxTick);

                int F = 8;
                int maxRaw = (1 << F) - 1;              // 255
                long domain = (long)maxRaw + 1;         // 256 valeurs
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                // Stride équilibré, copremier à 256: un impair suffit
                long stride = 257 % domain; if (stride <= 0) stride += domain;
                if ((stride & 1) == 0) stride++;
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                // Hotspots: ±sin(75°) en Q16 + offsets, plus bords et centre
                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0); // ~sin(75°) Q16
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();
                seen.Add(0);               // 0.0
                seen.Add(maxRaw);          // 1.0
                seen.Add(maxRaw >> 1);     // ~0.5

                foreach (var off in offsetsQ16)
                {
                    int rawPos = ValQ16ToRaw_UFixed_Trunc(xTailQ16 + off, F);
                    seen.Add(rawPos); if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1);

                    int rawNeg = ValQ16ToRaw_UFixed_Trunc(-(xTailQ16 + off), F);
                    seen.Add(rawNeg); if (rawNeg + 1 <= maxRaw) seen.Add(rawNeg + 1);
                }

                int bestTicks = 0; double bestRad = 0, bestDeg = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    object v = ctorUInt.Invoke(new object[] { (uint)raw });
                    int asinBn = (int)miAsin.Invoke(null, new[] { v });

                    // Référence: mapping TRUNC prod → asin(double) → Q16 → map signé
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);  // TRUNC comme prod
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits); // [-π/2..+π/2] signé
                    int diffTicks = System.Math.Abs(asinBn - expectedBn);

                    if (diffTicks > bestTicks)
                    {
                        bestTicks = diffTicks;
                        bestRad = diffTicks * ticksToRad;
                        bestDeg = bestRad * (180.0 / System.Math.PI);
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }

                // 2) Uniform stride sur le reste
                long idx = 0;
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)idx; idx += stride; if (idx >= domain) idx -= domain;
                    if (!seen.Add(raw)) continue;

                    object v = ctorUInt.Invoke(new object[] { (uint)raw });
                    int asinBn = (int)miAsin.Invoke(null, new[] { v });

                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits);
                    int diffTicks = System.Math.Abs(asinBn - expectedBn);

                    if (diffTicks > bestTicks)
                    {
                        bestTicks = diffTicks;
                        bestRad = diffTicks * ticksToRad;
                        bestDeg = bestRad * (180.0 / System.Math.PI);
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion

        // ----- ACOS -----

        #region --- ACOS LUT Retro (UFixed Q0.8) ---
        [Test]
        public void Acos_UFixed_Q0_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Core.B8"); // F = 8 (Q0.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            static int ValQ16ToRaw_UFixed_Trunc(int valQ16, int F)
            {
                long maxRaw = (1L << F) - 1L;
                long rawApprox = (valQ16 * maxRaw) / 65536L;   // TRUNC
                long raw = (rawApprox + maxRaw) / 2L;
                if (raw < 0) raw = 0;
                if (raw > maxRaw) raw = maxRaw;
                return (int)raw;
            }

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Core.B{bits}");
                if (intTag == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var ufixedType = typeof(UFixed<,>).MakeGenericType(intTag, fracTag);
                var miAcos = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UFixed<,>))
                    .MakeGenericMethod(intTag, fracTag);

                var ctorUInt = ufixedType.GetConstructor(new[] { typeof(uint) });
                if (ctorUInt == null) Assert.Fail($"Ctor (uint) introuvable pour {ufixedType}");

                // ticks unsigned: [0..π] → [0..2^(bits-1)]
                double ticksToRad = System.Math.PI / (1u << (bits - 1));

                int F = 8;
                int maxRaw = (1 << F) - 1;   // 255
                long domain = (long)maxRaw + 1; // 256
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                long stride = 257 % domain; if (stride <= 0) stride += domain;
                if ((stride & 1) == 0) stride++;
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();
                seen.Add(0); seen.Add(maxRaw); seen.Add(maxRaw >> 1);

                foreach (var off in offsetsQ16)
                {
                    int rawPos = ValQ16ToRaw_UFixed_Trunc(xTailQ16 + off, F);
                    seen.Add(rawPos); if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1);

                    int rawNeg = ValQ16ToRaw_UFixed_Trunc(-(xTailQ16 + off), F);
                    seen.Add(rawNeg); if (rawNeg + 1 <= maxRaw) seen.Add(rawNeg + 1);
                }

                int bestTicks = 0; double bestRad = 0, bestDeg = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    object v = ctorUInt.Invoke(new object[] { (uint)raw });
                    int acosBn = (int)miAcos.Invoke(null, new[] { v });

                    // Réf: mapping TRUNC prod → acos(double) → Q16 → map unsigned
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);  // TRUNC comme prod
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false); // [0..π] non signé
                    int diffTicks = System.Math.Abs(acosBn - expectedBn);

                    if (diffTicks > bestTicks)
                    {
                        bestTicks = diffTicks;
                        bestRad = diffTicks * ticksToRad;
                        bestDeg = bestRad * (180.0 / System.Math.PI);
                        atDeg = acosRadRef * (180.0 / System.Math.PI);
                    }
                }

                // 2) Uniform stride sur le reste
                long idx = 0;
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)idx; idx += stride; if (idx >= domain) idx -= domain;
                    if (!seen.Add(raw)) continue;

                    object v = ctorUInt.Invoke(new object[] { (uint)raw });
                    int acosBn = (int)miAcos.Invoke(null, new[] { v });

                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);
                    int diffTicks = System.Math.Abs(acosBn - expectedBn);

                    if (diffTicks > bestTicks)
                    {
                        bestTicks = diffTicks;
                        bestRad = diffTicks * ticksToRad;
                        bestDeg = bestRad * (180.0 / System.Math.PI);
                        atDeg = acosRadRef * (180.0 / System.Math.PI);
                    }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion






        #endregion

        // ==========================
        // --- ATAN/ATAN2 Retro ---
        // ==========================
        #region --- ATAN/ATAN2 Retro ---

        //----- ATAN -----
        //To-do

        //--- ATAN2 ---
        //To-Do

        #endregion

        // ==========================
        // --- RACINE CARRÉE ---
        // ==========================
        #region --- RACINE CARRÉ ---

        //To-Do

        #endregion

        // ==========================
        // --- LERP ---
        // ==========================
        #region --- LERP ---

        //To-Do

        #endregion

        // ==========================
        // --- SMOOTHSTEP ---
        // ==========================
        #region --- SMOOTHSTEP ---

        //To-Do

        #endregion

        // ==========================
        // --- EXPONENTIELLE ---
        // ==========================
        #region --- EXPONENTIELLE ---

        //To-Do

        #endregion

        // ==========================
        // --- LOG ---
        // ==========================
        #region --- LOG ---

        //To-Do

        #endregion


    }

}


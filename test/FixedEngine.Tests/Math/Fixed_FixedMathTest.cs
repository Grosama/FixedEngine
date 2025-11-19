using FixedEngine.Core;
using FixedEngine.Math;
using FixedEngine.LUT;
using NUnit.Framework;
using System.Reflection;

namespace FixedEngine.Tests.Math
{

    [TestFixture]
    public class Fixed_FixedMathTest
    {

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        #region --- SIN LUT Retro (Fixed) ---
        [Test]
        [Category("FixedMath/Fixed")]
        public void Sin_Fixed_B2toB32_BitFaithful()
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
                // Choix d'un TFrac fixe arbitraire (B16 courant, fallback B8) car TFrac n'impacte pas le résultat
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(Fixed<,>).MakeGenericType(tagTotal, tagFrac);
                int minRaw = -(1 << (totalBits - 1));
                int maxRaw = (1 << (totalBits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int phaseBits = totalBits - 2;
                // Mode bit-faithful seulement si Bn ≤ lutBits+2 (ici 14)
                if (totalBits > lutBits + 2)
                {
                    System.Console.WriteLine($"B{totalBits}: au-delà de la zone bit-faithful, skip (interpolation active)");
                    continue;
                }
                for (int raw = minRaw; raw <= maxRaw; raw++)
                {
                    var angle = Activator.CreateInstance(angleType, raw);
                    uint uraw = (uint)raw & ((1u << totalBits) - 1);
                    int phase = (int)(uraw & ((1u << phaseBits) - 1));
                    int quadrant = (int)(uraw >> (totalBits - 2)) & 0b11;
                    int sign = (quadrant < 2) ? 1 : -1;
                    int idx;
                    if (phaseBits > lutBits)
                        idx = (int)(phase >> (phaseBits - lutBits));
                    else
                        idx = (int)(phase << (lutBits - phaseBits));
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
                            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>));
                    if (miGen == null)
                        Assert.Fail("Méthode générique FixedMath.SinRawDebug<TTotal, TFrac>(Fixed<TTotal, TFrac>) absente !");
                    var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                    int resultInt = (int)mi.Invoke(null, new[] { angle });
                    Assert.That(resultInt, Is.EqualTo(expected),
                        $"B{totalBits}, raw={raw}, expected={expected}, got={resultInt}");
                }
                System.Console.WriteLine($"B{totalBits} : bit-faithful signé validé ({domain} valeurs)");
            }
        }

        [Explicit]
        [Test]
        [Category("FixedMath/Fixed")]
        public void Sin_Fixed_B2toB31_MaxDiffMeasure()
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
                // Choix d'un TFrac fixe arbitraire (B16 courant, fallback B8) car TFrac n'impacte pas le résultat
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(Fixed<,>).MakeGenericType(tagTotal, tagFrac);
                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRawDebug"
                        && m.IsGenericMethod
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsGenericType
                        && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>));
                if (miGen == null)
                {
                    System.Console.WriteLine("Méthode générique FixedMath.SinRawDebug<TTotal, TFrac>(Fixed<TTotal, TFrac>) absente : SKIP");
                    continue;
                }
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                int minRaw = -(1 << (totalBits - 1));
                int maxRaw = (1 << (totalBits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int numSamples = (totalBits >= 28) ? 1000000 : (int)domain;
                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;
                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (totalBits >= 28)
                    {
                        long lmin = minRaw;
                        long lmax = maxRaw;
                        raw = (int)(lmin + rng.NextInt64(lmax - lmin + 1));
                    }
                    else
                    {
                        raw = minRaw + i;
                    }
                    var angle = Activator.CreateInstance(angleType, raw);
                    int val = (int)mi.Invoke(null, new[] { angle });
                    // Map sur [-π, π)
                    double radians = ((double)raw / (1 << (totalBits - 1))) * System.Math.PI;
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
        [Category("FixedMath/Fixed")]
        public void Sin_Fixed_B2toB31_VsMathSin()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);
            // FixedMath.Sin<TTotal, TFrac>(Fixed<TTotal, TFrac>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Sin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>));
            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Sin<TTotal, TFrac>(Fixed<TTotal, TFrac>) introuvable.");
            for (int totalBits = 2; totalBits <= 31; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                // Choix d'un TFrac fixe arbitraire (B16 courant, fallback B8) car TFrac n'impacte pas le résultat
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(Fixed<,>).MakeGenericType(tagTotal, tagFrac);
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                int minRaw = -(1 << (totalBits - 1));
                int maxRaw = (1 << (totalBits - 1)) - 1;
                int numSamples = (totalBits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"totalBits={totalBits} : numSamples <= 0, skip.");
                    continue;
                }
                // amplitude idéale en Bn signé : [-maxAmp .. +maxAmp]
                int maxAmp = (1 << (totalBits - 1)) - 1; // totalBits max = 31 ici, donc safe
                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;
                uint maxRawU = (1u << totalBits) - 1u;
                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (totalBits >= 28)
                    {
                        long span = (long)maxRaw - (long)minRaw + 1;
                        long r = rng.NextInt64(0, span);
                        raw = (int)(minRaw + r);
                    }
                    else
                    {
                        long span = (long)maxRaw - (long)minRaw;
                        raw = (int)(minRaw + (span * i) / (numSamples - 1));
                    }
                    var angle = Activator.CreateInstance(angleType, raw);
                    int val = (int)mi.Invoke(null, new[] { angle });
                    uint uraw = (uint)raw & maxRawU;
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
                        rawAtMax = raw;
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

        #region --- COS LUT Retro (Fixed) ---
        [Test]
        [Category("FixedMath/Fixed")]
        public void Cos_Fixed_B2toB31_VsMathCos()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(54321);
            // FixedMath.Cos<TTotal, TFrac>(Fixed<TTotal, TFrac>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Cos"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>));
            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Cos<TTotal, TFrac>(Fixed<TTotal, TFrac>) introuvable.");
            for (int totalBits = 2; totalBits <= 31; totalBits++)
            {
                var tagTotal = asm.GetType($"FixedEngine.Core.B{totalBits}");
                if (tagTotal == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{totalBits} absent : SKIP");
                    continue;
                }
                // Choix d'un TFrac fixe arbitraire (B16 courant, fallback B8)
                var tagFrac = asm.GetType("FixedEngine.Core.B16") ?? asm.GetType("FixedEngine.Core.B8");
                if (tagFrac == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B16 ou B8 absent : SKIP");
                    continue;
                }
                var angleType = typeof(Fixed<,>).MakeGenericType(tagTotal, tagFrac);
                var mi = miGen.MakeGenericMethod(tagTotal, tagFrac);
                int minRaw = -(1 << (totalBits - 1));
                int maxRaw = (1 << (totalBits - 1)) - 1;
                int numSamples = (totalBits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"totalBits={totalBits} : numSamples <= 0, skip.");
                    continue;
                }
                int maxAmp = (1 << (totalBits - 1)) - 1;
                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;
                uint maxRawU = (1u << totalBits) - 1u;
                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (totalBits >= 28)
                    {
                        long span = (long)maxRaw - (long)minRaw + 1;
                        long r = rng.NextInt64(0, span);
                        raw = (int)(minRaw + r);
                    }
                    else
                    {
                        long span = (long)maxRaw - (long)minRaw;
                        raw = (int)(minRaw + (span * i) / (numSamples - 1));
                    }
                    var angle = Activator.CreateInstance(angleType, raw);
                    int val = (int)mi.Invoke(null, new[] { angle });
                    uint uraw = (uint)raw & maxRawU;
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
                        rawAtMax = raw;
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

        #region --- ASIN LUT Retro (Fixed Q8.8) ---
        [Test]
        public void Asin_Fixed_Q8_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Core.B8"); // F = 8 (Q8.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            // mapping inverse Q16 -> raw (Fixed, signé) en miroir du prod (shifts bit-faithful)
            static int ValQ16ToRaw_Fixed(int valQ16, int F)
                => (F == 16) ? valQ16
                   : (F > 16) ? (valQ16 << (F - 16))
                              : (valQ16 >> (16 - F));

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Core.B{bits}");
                if (intTag == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var fixedType = typeof(Fixed<,>).MakeGenericType(intTag, fracTag);
                var miAsin = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>))
                    .MakeGenericMethod(intTag, fracTag);

                var ctorInt = fixedType.GetConstructor(new[] { typeof(int) });
                if (ctorInt == null) Assert.Fail($"Ctor (int) introuvable pour {fixedType}");

                // ticks signés: [-π/2..+π/2] → [-max..+max]
                int maxTick = (1 << (bits - 1)) - 1;
                double ticksToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxTick);

                int F = 8;
                // Domaine réellement représentable par le backing signé B<bits>
                int oneRaw = 1 << F;                    // 256
                int signedMin = -(1 << (bits - 1));
                int signedMax = (1 << (bits - 1)) - 1;
                int minRaw = System.Math.Max(-oneRaw, signedMin);    // ex. B9: max(-256, -256) = -256
                int maxRaw = System.Math.Min(oneRaw, signedMax);    // ex. B9: min(+256, +255) = +255
                int domain = maxRaw - minRaw + 1; // B9: 512, pas 513
                int samples = domain;             // exhaustif (toujours petit en Q8.8)

                // Hotspots: ±sin(75°) (Q16) + offsets, plus bornes et centre
                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0); // ~sin 75° (Q16)
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };
                var seen = new System.Collections.Generic.HashSet<int> { minRaw, 0, maxRaw };

                foreach (var off in offsetsQ16)
                {
                    int rawPos = System.Math.Clamp(ValQ16ToRaw_Fixed(xTailQ16 + off, F), minRaw, maxRaw);
                    seen.Add(rawPos); if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1);

                    int rawNeg = System.Math.Clamp(ValQ16ToRaw_Fixed(-(xTailQ16 + off), F), minRaw, maxRaw);
                    seen.Add(rawNeg); if (rawNeg - 1 >= minRaw) seen.Add(rawNeg - 1);
                }

                int bestTicks = 0; double bestRad = 0, bestDeg = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    object v = ctorInt.Invoke(new object[] { raw });
                    int asinBn = (int)miAsin.Invoke(null, new[] { v });

                    // Réf prod: sign-extend sur 'bits' puis QF->Q16 (shifts), asin, map signé
                    int rawEff = (raw << (32 - bits)) >> (32 - bits); // sign-extend exact B<bits>
                    int valQ16 = (F == 16) ? rawEff
                              : (F > 16) ? (rawEff >> (F - 16))
                                         : (rawEff << (16 - F));
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

                // 2) Échantillonnage exhaustif sur le reste
                for (int raw = minRaw; raw <= maxRaw; raw++)
                {
                    if (!seen.Add(raw)) continue;
                    object v = ctorInt.Invoke(new object[] { raw });
                    int asinBn = (int)miAsin.Invoke(null, new[] { v });

                    int rawEff = (raw << (32 - bits)) >> (32 - bits); // sign-extend
                    int valQ16 = (F == 16) ? rawEff
                              : (F > 16) ? (rawEff >> (F - 16))
                                         : (rawEff << (16 - F));
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

        #region --- ACOS LUT Retro (Fixed Q8.8) ---
        [Test]
        public void Acos_Fixed_Q8_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Core.B8"); // F = 8 (Q8.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static int ValQ16ToRaw_Fixed(int valQ16, int F)
                => (F == 16) ? valQ16
                   : (F > 16) ? (valQ16 << (F - 16))
                              : (valQ16 >> (16 - F));

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Core.B{bits}");
                if (intTag == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var fixedType = typeof(Fixed<,>).MakeGenericType(intTag, fracTag);
                var miAcos = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Fixed<,>))
                    .MakeGenericMethod(intTag, fracTag);

                var ctorInt = fixedType.GetConstructor(new[] { typeof(int) });
                if (ctorInt == null) Assert.Fail($"Ctor (int) introuvable pour {fixedType}");

                // ticks unsigned: [0..π] → [0..2^(bits-1)]
                double ticksToRad = System.Math.PI / (1u << (bits - 1));

                int F = 8;

                // --- Domaine réellement représentable par le backing signé B<bits> ---
                int oneRaw = 1 << F;                    // 256
                int signedMin = -(1 << (bits - 1));
                int signedMax = (1 << (bits - 1)) - 1;
                int minRaw = System.Math.Max(-oneRaw, signedMin);   // ex. B9: -256
                int maxRaw = System.Math.Min(oneRaw, signedMax);   // ex. B9: +255 (exclut +256)
                int domain = maxRaw - minRaw + 1;                   // B9: 512
                int samples = domain;

                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int> { minRaw, 0, maxRaw };

                foreach (var off in offsetsQ16)
                {
                    int rawPos = System.Math.Clamp(ValQ16ToRaw_Fixed(xTailQ16 + off, F), minRaw, maxRaw);
                    seen.Add(rawPos); if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1);

                    int rawNeg = System.Math.Clamp(ValQ16ToRaw_Fixed(-(xTailQ16 + off), F), minRaw, maxRaw);
                    seen.Add(rawNeg); if (rawNeg - 1 >= minRaw) seen.Add(rawNeg - 1);
                }

                int bestTicks = 0; double bestRad = 0, bestDeg = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    object v = ctorInt.Invoke(new object[] { raw });
                    int acosBn = (int)miAcos.Invoke(null, new[] { v });

                    // Réf prod: SIGN-EXTEND sur 'bits', QF->Q16 (shifts), acos, map unsigned
                    int rawEff = (raw << (32 - bits)) >> (32 - bits); // sign-extend exact B<bits>
                    int valQ16 = (F == 16) ? rawEff
                              : (F > 16) ? (rawEff >> (F - 16))
                                         : (rawEff << (16 - F));
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

                // 2) Échantillonnage exhaustif sur le reste
                for (int raw = minRaw; raw <= maxRaw; raw++)
                {
                    if (!seen.Add(raw)) continue;
                    object v = ctorInt.Invoke(new object[] { raw });
                    int acosBn = (int)miAcos.Invoke(null, new[] { v });

                    int rawEff = (raw << (32 - bits)) >> (32 - bits); // sign-extend
                    int valQ16 = (F == 16) ? rawEff
                              : (F > 16) ? (rawEff >> (F - 16))
                                         : (rawEff << (16 - F));
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


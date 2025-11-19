using FixedEngine.Core;
using FixedEngine.LUT;
using FixedEngine.Math;
using NUnit.Framework;
using System.Reflection;

namespace FixedEngine.Tests.Math
{

    [TestFixture]
    public class IntN_FixedMathTest
    {

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        #region --- SIN LUT Retro (IntN) ---
        [Test]
        [Category("FixedMath/IntN")]
        public void Sin_IntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }
                var angleType = typeof(IntN<>).MakeGenericType(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int phaseBits = bits - 2;

                // Mode bit-faithful seulement si Bn ≤ lutBits+2 (ici 14)
                if (bits > lutBits + 2)
                {
                    System.Console.WriteLine($"B{bits}: au-delà de la zone bit-faithful, skip (interpolation active)");
                    continue;
                }

                for (int raw = minRaw; raw <= maxRaw; raw++)
                {
                    var angle = Activator.CreateInstance(angleType, raw);

                    uint uraw = (uint)raw & ((1u << bits) - 1);

                    int phase = (int)(uraw & ((1u << phaseBits) - 1));
                    int quadrant = (int)(uraw >> (bits - 2)) & 0b11;
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
                            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));
                    if (miGen == null)
                        Assert.Fail("Méthode générique FixedMath.Sin<TBits>(IntN<TBits>) absente !");
                    var mi = miGen.MakeGenericMethod(tagType);

                    int resultInt = (int)mi.Invoke(null, new[] { angle });

                    Assert.That(resultInt, Is.EqualTo(expected),
                        $"B{bits}, raw={raw}, expected={expected}, got={resultInt}");
                }
                System.Console.WriteLine($"B{bits} : bit-faithful signé validé ({domain} valeurs)");
            }
        }

        [Explicit]
        [Test]
        [Category("FixedMath/IntN")]
        public void Sin_IntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(98765);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }
                var angleType = typeof(IntN<>).MakeGenericType(tagType);

                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRawDebug"
                        && m.IsGenericMethod
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsGenericType
                        && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));
                if (miGen == null)
                {
                    System.Console.WriteLine("Méthode générique FixedMath.Sin<TBits>(IntN<TBits>) absente : SKIP");
                    continue;
                }
                var mi = miGen.MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int numSamples = (bits >= 28) ? 1000000 : (int)domain;

                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (bits >= 28)
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
                    double radians = ((double)raw / (1 << (bits - 1))) * System.Math.PI;
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
                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
            }
            System.Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Sin_IntN_B2toB31_VsMathSin()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);

            // FixedMath.Sin<TBits>(IntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Sin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Sin<TBits>(IntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;


                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                // amplitude idéale en Bn signé : [-maxAmp .. +maxAmp]
                int maxAmp = (1 << (bits - 1)) - 1; // bits max = 31 ici, donc safe

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                uint maxRawU = (1u << bits) - 1u;

                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (bits >= 28)
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

                report += $"\nB{bits}\t{maxDiffInt}\t{maxDiffNorm:0.000000}\t{atDeg:0.###}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion

        #region --- COS LUT Retro (IntN) ---

        [Test]
        [Category("FixedMath/IntN")]
        public void Cos_IntN_B2toB31_BitFaithful()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int phaseBits = bits - 2;

                if (bits > lutBits + 2)
                { Console.WriteLine($"B{bits}: skip, interpolation active"); continue; }

                uint mask = (bits == 32) ? 0xFFFF_FFFFu : (1u << bits) - 1;
                uint quarter = 1u << (bits - 2);

                for (int raw = minRaw; raw <= maxRaw; raw++)
                {
                    uint uraw = (uint)raw & mask;
                    uint rawShift = (uraw + quarter) & mask;

                    int idx = (phaseBits > lutBits)
                            ? (int)(rawShift >> (phaseBits - lutBits))
                            : (int)(rawShift << (lutBits - phaseBits));
                    idx &= lutMask;

                    int quadrant = (int)(rawShift >> (bits - 2)) & 0b11;
                    int lutIdx = (quadrant == 0 || quadrant == 2) ? idx : lutMask - idx;
                    int sign = (quadrant < 2) ? 1 : -1;
                    int expected = sign * lut[lutIdx];

                    var miGen = typeof(FixedMath).GetMethods()
                                 .First(m => m.Name == "CosRawDebug"
                                          && m.IsGenericMethod
                                          && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));
                    var mi = miGen.MakeGenericMethod(tagType);

                    var angle = Activator.CreateInstance(angleType, raw);
                    int result = (int)mi.Invoke(null, new[] { angle });

                    Assert.That(result, Is.EqualTo(expected),
                        $"B{bits}, raw={raw}, expected={expected}, got={result}");
                }
                Console.WriteLine($"B{bits} : COS signé bit-faithful validé ({domain} valeurs)");
            }
        }

        [Explicit]
        [Test]
        [Category("FixedMath/IntN")]
        public void Cos_IntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(8675309);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                           .First(m => m.Name == "Cos"
                                    && m.IsGenericMethod
                                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                           .MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    var angle = Activator.CreateInstance(angleType, raw);
                    int val = (int)mi.Invoke(null, new[] { angle });

                    double rad = ((double)raw / (1 << (bits - 1))) * System.Math.PI;
                    int expected = (int)System.Math.Round(System.Math.Cos(rad) * 65536);

                    int diff = System.Math.Abs(val - expected);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = rad * 180.0 / System.Math.PI;

                        double sinTheta = System.Math.Sin(rad);                // dérivée de cos
                        maxDiffAngleEqDeg = (sinTheta != 0)
                            ? maxDiffValue / System.Math.Abs(sinTheta) * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
                    }
                }
                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
            }
            Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Cos_IntN_B2toB31_VsMathCos()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(54321); // autre seed pour la beauté du test

            // FixedMath.Cos<TBits>(IntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Cos"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Cos<TBits>(IntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;

                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                int maxAmp = (1 << (bits - 1)) - 1;

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                uint maxRawU = (1u << bits) - 1u;

                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (bits >= 28)
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

                report += $"\nB{bits}\t{maxDiffInt}\t{maxDiffNorm:0.000000}\t{atDeg:0.###}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion

        #region --- TAN Retro (IntN) via TanRaw ---
        [Test]
        [Category("FixedMath/IntN")]
        public void Tan_IntN_B2toB31_MaxDiffMeasure_TanRaw()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;

            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(13579);

            // Récupère la méthode générique FixedMath.TanRawDebug(IntN<>)
            var miTanRawOpen = typeof(FixedMath).GetMethods()
                .Where(m => m.Name == "TanRawDebug" && m.IsGenericMethodDefinition)
                .First(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 1
                        && ps[0].ParameterType.IsGenericType
                        && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>);
                });

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var miTan = miTanRawOpen.MakeGenericMethod(tagType);

                int maxSigned = (1 << (bits - 1)) - 1;        // +127 (B8), etc.
                int minSigned = -(1 << (bits - 1));           // -128 (B8), etc.
                int spanSize = (1 << bits);                  // 2^bits
                uint mask = (bits == 32) ? 0xFFFF_FFFFu : ((1u << bits) - 1u); // pour sécurité (bits<=31 ici)

                // Échantillonnage : dense mais borné pour CI
                int samples = (bits >= 28) ? 300_000 : System.Math.Min(spanSize, 1_000_000);

                int maxDiff = 0;
                double maxDiffDeg = 0, maxDiffValue = 0, maxDiffAngleEqDeg = 0;

                int maxDiffLin = 0;
                double maxDiffLinDeg = 0, maxDiffLinValue = 0, maxDiffAngleEqDegLin = 0, maxDiffAngleEqDegLinAtDeg = 0;

                // denom = 2^bits pour convertir uraw -> ratio d’angle
                double denom = System.Math.Pow(2.0, bits);

                for (int i = 0; i < samples; i++)
                {
                    // Échantillon signé
                    int rawSigned;
                    if (bits >= 28)
                    {
                        // tirage pseudo-aléatoire uniforme sur [min..max]
                        long r = rng.NextInt64(minSigned, (long)maxSigned + 1);
                        rawSigned = (int)r;
                    }
                    else
                    {
                        // balayage uniforme sur [min..max]
                        // map i -> [-half .. +half-1]
                        int half = 1 << (bits - 1);
                        rawSigned = -half + (int)((long)(spanSize - 1) * i / System.Math.Max(1, samples - 1));
                        if (rawSigned > maxSigned) rawSigned = maxSigned;
                    }

                    // Construction IntN<Bn>
                    var angleObj = Activator.CreateInstance(angleType, rawSigned);

                    // Appel à FixedMath.TanRawDebug(IntN<Bn>)
                    int tanVal = (int)miTan.Invoke(null, new[] { angleObj });

                    // Skip si sentinelle asymptote
                    if (tanVal == int.MaxValue || tanVal == int.MinValue)
                        continue;

                    // IMPORTANT : pour comparer à Math.Tan, on convertit l’angle EXACTEMENT
                    // comme le fait TanRawDebug(IntN): wrap signé -> unsigned sur N bits.
                    uint uraw = (uint)rawSigned & mask;

                    // Angle en radians 0..2π
                    double angleRatio = uraw / denom;
                    double rad = angleRatio * (System.Math.PI * 2.0);
                    double tanRad = System.Math.Tan(rad);

                    // Ignore les zones trop proches des asymptotes
                    if (double.IsInfinity(tanRad) || System.Math.Abs(tanRad) > 1e9)
                        continue;

                    // Valeur attendue en Q16.16
                    double scaled = tanRad * 65536.0;
                    long expected = (System.Math.Abs(scaled) > int.MaxValue)
                        ? (scaled > 0 ? int.MaxValue : int.MinValue)
                        : (long)System.Math.Round(scaled);

                    int diff = (int)System.Math.Abs(tanVal - expected);

                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = rad * 180.0 / System.Math.PI;

                        // Équivalence d’erreur angulaire via d(tan)/dθ = sec^2(θ)
                        double sec2 = 1.0 + tanRad * tanRad;
                        maxDiffAngleEqDeg = (sec2 != 0) ? (maxDiffValue / sec2) * (180.0 / System.Math.PI) : double.PositiveInfinity;
                    }

                    // Zone "linéaire" utile gameplay (|tan| raisonnable)
                    if (System.Math.Abs(tanRad) < 10000 && System.Math.Abs(expected) < int.MaxValue)
                    {
                        if (diff > maxDiffLin)
                        {
                            maxDiffLin = diff;
                            maxDiffLinValue = diff / 65536.0;
                            maxDiffLinDeg = rad * 180.0 / System.Math.PI;
                        }

                        double sec2 = 1.0 + tanRad * tanRad;
                        double angleEqDeg = (sec2 != 0) ? (diff / 65536.0) / sec2 * (180.0 / System.Math.PI) : double.PositiveInfinity;
                        if (angleEqDeg > maxDiffAngleEqDegLin)
                        {
                            maxDiffAngleEqDegLin = angleEqDeg;
                            maxDiffAngleEqDegLinAtDeg = rad * 180.0 / System.Math.PI;
                        }
                    }
                }

                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}"
                        + $"\t| {maxDiffLin}\t{maxDiffLinDeg:0.###}\t{maxDiffLinValue:0.00000}"
                        + $"\t| {maxDiffAngleEqDegLin:0.00000}\t{maxDiffAngleEqDegLinAtDeg:0.00000}";

                report += $"\n  ↳ LinError={maxDiffLinValue:0.#####} ≈ {maxDiffAngleEqDegLin:0.##}° at {maxDiffAngleEqDegLinAtDeg:0.##}°"
                        + $" | TotalError={maxDiffValue:0.#####} ≈ {maxDiffAngleEqDeg:0.##}° at {maxDiffDeg:0.##}°";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Tan_IntN_B2toB31_VsMathTan_FloatError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffFloat\tAvgDiffFloat\tMaxDiffDeg\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);

            // FixedMath.Tan<TBits>(IntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Tan"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Tan<TBits>(IntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                // Amplitude idéale en Bn signé
                int maxAmp = (1 << (bits - 1)) - 1;

                double maxDiffFloat = 0.0;
                double sumDiffFloat = 0.0;
                int sampleCount = 0;
                double maxDiffDeg = 0.0;
                double atDeg = 0.0;
                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                uint maxRawU = (1u << bits) - 1u;

                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (bits >= 28)
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
                    int valQ16 = (int)mi.Invoke(null, new[] { angle });

                    uint uraw = (uint)raw & maxRawU;
                    double radians = (double)uraw / ((double)maxRawU + 1.0) * 2.0 * System.Math.PI;
                    double tanReal = System.Math.Tan(radians);

                    // Skip proche asymptote (évite infini)
                    if (System.Math.Abs(System.Math.Cos(radians)) < 1e-7)
                        continue;

                    double valFloat = valQ16 / 65536.0;
                    double diffFloat = System.Math.Abs(valFloat - tanReal);

                    sumDiffFloat += diffFloat;
                    sampleCount++;

                    double deg = radians * 180.0 / System.Math.PI;

                    if (diffFloat > maxDiffFloat)
                    {
                        maxDiffFloat = diffFloat;
                        maxDiffDeg = deg;
                        atDeg = deg;
                        rawAtMax = raw;
                        valAtMax = valQ16;
                        expAtMax = (int)(tanReal * 65536.0);
                    }
                }

                double avgDiffFloat = (sampleCount > 0) ? sumDiffFloat / sampleCount : 0.0;

                report += $"\nB{bits}\t{maxDiffFloat:0.#####}\t{avgDiffFloat:0.#####}\t{maxDiffDeg:0.###}\t{atDeg:0.###}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
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

        #region Helpers
        private static (int maxGapTicks, double midDeg)
        MeasureGapPositive_IntN<TBits>(int start, int end, int bits)
        where TBits : struct
        {
            if (start >= end) return (0, 0.0);

            int maxSigned = (1 << (bits - 1)) - 1;
            double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);

            int prevTick = FixedMath.Asin(new IntN<TBits>(start));
            int bestGapTicks = 0;
            double bestMidDeg = 0.0;

            for (int raw = start + 1; raw <= end; raw++)
            {
                int curTick = FixedMath.Asin(new IntN<TBits>(raw));
                int gapTicks = curTick - prevTick;

                if (gapTicks > bestGapTicks)
                {
                    bestGapTicks = gapTicks;
                    bestMidDeg = (prevTick + curTick) * 0.5 * tickToDeg;
                }

                prevTick = curTick;
            }

            return (bestGapTicks, bestMidDeg);
        }

        private static int Asin_IntN_ByBits(int bits, int raw)
        {
            switch (bits)
            {
                case 2: return FixedMath.Asin(new IntN<B2>(raw));
                case 3: return FixedMath.Asin(new IntN<B3>(raw));
                case 4: return FixedMath.Asin(new IntN<B4>(raw));
                case 5: return FixedMath.Asin(new IntN<B5>(raw));
                case 6: return FixedMath.Asin(new IntN<B6>(raw));
                case 7: return FixedMath.Asin(new IntN<B7>(raw));
                case 8: return FixedMath.Asin(new IntN<B8>(raw));
                case 9: return FixedMath.Asin(new IntN<B9>(raw));
                case 10: return FixedMath.Asin(new IntN<B10>(raw));
                case 11: return FixedMath.Asin(new IntN<B11>(raw));
                case 12: return FixedMath.Asin(new IntN<B12>(raw));
                case 13: return FixedMath.Asin(new IntN<B13>(raw));
                case 14: return FixedMath.Asin(new IntN<B14>(raw));
                case 15: return FixedMath.Asin(new IntN<B15>(raw));
                case 16: return FixedMath.Asin(new IntN<B16>(raw));
                case 17: return FixedMath.Asin(new IntN<B17>(raw));
                case 18: return FixedMath.Asin(new IntN<B18>(raw));
                case 19: return FixedMath.Asin(new IntN<B19>(raw));
                case 20: return FixedMath.Asin(new IntN<B20>(raw));
                case 21: return FixedMath.Asin(new IntN<B21>(raw));
                case 22: return FixedMath.Asin(new IntN<B22>(raw));
                case 23: return FixedMath.Asin(new IntN<B23>(raw));
                case 24: return FixedMath.Asin(new IntN<B24>(raw));
                case 25: return FixedMath.Asin(new IntN<B25>(raw));
                case 26: return FixedMath.Asin(new IntN<B26>(raw));
                case 27: return FixedMath.Asin(new IntN<B27>(raw));
                case 28: return FixedMath.Asin(new IntN<B28>(raw));
                case 29: return FixedMath.Asin(new IntN<B29>(raw));
                case 30: return FixedMath.Asin(new IntN<B30>(raw));
                case 31: return FixedMath.Asin(new IntN<B31>(raw));
                default: throw new ArgumentOutOfRangeException(nameof(bits));
            }
        }

        private static int BuildRawForSinDeg_IntN(int bits, double thetaDeg)
        {
            double x = System.Math.Sin(thetaDeg * System.Math.PI / 180.0);
            int maxSigned = (1 << (bits - 1)) - 1;

            double r = x * maxSigned;
            long raw = (long)System.Math.Round(r, MidpointRounding.AwayFromZero);

            if (raw < -maxSigned) raw = -maxSigned;
            if (raw > maxSigned) raw = maxSigned;

            return (int)raw;
        }

        #endregion

        #region --- ASIN LUT Retro (IntN)
        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            // helper locaux
            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            static int ValQ16ToRaw_IntN(int valQ16, int bits)
            {
                if (bits == 17) return valQ16;
                if (bits > 17) return valQ16 << (bits - 17);       // élargissement
                return valQ16 >> (17 - bits);                        // rétrécissement (shift arithmétique)
            }

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(IntN<>).MakeGenericType(tagType);
                var miAsin = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                    .MakeGenericMethod(tagType);

                int maxRaw = (1 << (bits - 1)) - 1;
                int minRaw = -maxRaw;
                long domain = (long)maxRaw - (long)minRaw + 1;      // = 2^bits - 1 (impair)


                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;


                long stride = 1_103_515_245L % domain;              // base LCG-like
                if (stride <= 0) stride += domain;
                if (stride % 2 == 0) stride++;                      // impair

                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }


                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();


                seen.Add(minRaw);
                seen.Add(0);
                seen.Add(maxRaw);


                foreach (var off in offsetsQ16)
                {
                    int rq = xTailQ16 + off;
                    int rawPos = ValQ16ToRaw_IntN(rq, bits);
                    if (rawPos < minRaw) rawPos = minRaw;
                    if (rawPos > maxRaw) rawPos = maxRaw;
                    seen.Add(rawPos);

                    int rawNeg = ValQ16ToRaw_IntN(-rq, bits);
                    if (rawNeg < minRaw) rawNeg = minRaw;
                    if (rawNeg > maxRaw) rawNeg = maxRaw;
                    seen.Add(rawNeg);
                }


                int maxTicks = (1 << (bits - 1)) - 1;
                double ticksToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxTicks);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;


                foreach (int raw in seen)
                {
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });


                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits);

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks;
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }


                long idx = 0; // index logique [0..domain-1]
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)(minRaw + idx);
                    idx += stride; if (idx >= domain) idx -= domain;

                    if (!seen.Add(raw)) { continue; } 

                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });

                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits);

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks;
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_IsMonotone_B16()
        {
            var seen = new HashSet<int>();
            int prev = int.MinValue;

            // Domaine IntN<B16> : [-32768, +32767]
            const int minRaw = -(1 << 15); // -32768
            const int maxRaw = (1 << 15) - 1; // +32767

            for (int raw = minRaw; raw <= maxRaw; raw++)
            {
                var v = new IntN<B16>(raw);
                int y = FixedMath.Asin(v);

                // Monotonicité non stricte : plateaux autorisés
                Assert.That(y, Is.GreaterThanOrEqualTo(prev),
                    $"non-monotone at raw={raw} (prev={prev}, current={y})");

                prev = y;
                seen.Add(y);
            }

            // Bornes théoriques pour Bn signé ([-π/2, +π/2] → [-32767, +32767])
            int maxTicks = (1 << (16 - 1)) - 1; // 32767

            int seenMin = seen.Min();
            int seenMax = seen.Max();

            Assert.Multiple(() =>
            {
                // 0° doit être présent (x = 0)
                Assert.That(seen.Contains(0), Is.True, "0° manquant (asin(0) != 0)");

                // +90° : atteint exactement grâce à l'ancrage dur
                Assert.That(seenMax, Is.EqualTo(maxTicks),
                    $"+90° attendu {maxTicks}, obtenu {seenMax}");

                // -90° : atteint exactement grâce à l'ancrage dur
                Assert.That(seenMin, Is.EqualTo(-maxTicks),
                    $"-90° attendu {-maxTicks}, obtenu {seenMin}");

                // Sécurité : rien ne dépasse l'intervalle théorique
                Assert.That(seenMin, Is.GreaterThanOrEqualTo(-maxTicks));
                Assert.That(seenMax, Is.LessThanOrEqualTo(maxTicks));
            });
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB28_MaxUserPerceivedAngleError_PositiveOnly()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var generic = typeof(IntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_IntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_IntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);

                int minRaw = -(1 << (bits - 1));          // ex: B8 => -128
                int maxRaw = (1 << (bits - 1)) - 1;       // ex: B8 => +127
                int domain = maxRaw - minRaw + 1;         // = 2^bits (symétrie avec UIntN, info)

                // Côté positif uniquement : [0 .. maxRaw]
                int start = 0;
                int end = maxRaw;

                // Fenêtre vers le bord positif
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contiguë au bord positif : [end - (window-1) .. end]
                var best = ((int maxGapTicks, double midDeg))
                           m.Invoke(null, new object[] { end - (window - 1), end, bits })!;

                int maxSigned = (1 << (bits - 1)) - 1;    // même que maxTick pour les angles
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}" +
                          $"\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDeg:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB28_MaxUserPerceivedAngleError_NegativeOnly()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // Même helper que pour le positif : mesure le max gap sur [start..end]
            var generic = typeof(IntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_IntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_IntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);

                int minRaw = -(1 << (bits - 1));       // ex: B8 => -128
                int maxRaw = (1 << (bits - 1)) - 1;    // ex: B8 => +127
                int domain = maxRaw - minRaw + 1;      // = 2^bits (juste informatif)

                // Negative side only : [minRaw .. 0]
                int start = minRaw;
                int end = 0;
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contigue au bord négatif (ancrage côté minRaw inclus)
                // -> [start .. start + (window - 1)]
                var best = ((int maxGapTicks, double midDeg))
                           m.Invoke(null, new object[] { start, start + (window - 1), bits })!;

                int maxSigned = (1 << (bits - 1)) - 1;
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}" +
                          $"\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDeg:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB28_MaxError_IncludingAnchors()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var generic = typeof(IntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_IntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_IntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;

                // Côté positif uniquement : [0 .. maxRaw-1]
                // (on exclut la marche d’ancrage raw==maxRaw qui force +90° exact)
                int start = 0;
                int end = maxRaw - 1;
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contiguë au bord positif, mais sans la toute dernière marche
                var best = ((int maxGapTicks, double midDeg))
                           m.Invoke(null, new object[] { end - (window - 1), end, bits })!;

                int maxSigned = (1 << (bits - 1)) - 1;
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}" +
                          $"\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDeg:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB28_MaxUserPerceivedAngleError_Gap_Fast()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var mPos = typeof(IntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_IntN),
                        BindingFlags.NonPublic | BindingFlags.Static);
            var mNeg = typeof(IntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_IntN),
                        BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(mPos, Is.Not.Null);
            Assert.That(mNeg, Is.Not.Null);

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;

                // ---- négatif : [minRaw .. 0] (comme NegativeOnly)
                int startN = minRaw;
                int endN = 0;
                int winN = System.Math.Min(endN - startN + 1, EDGE_WINDOW);
                var neg = ((int maxGapTicks, double midDeg))
                          mNeg!.MakeGenericMethod(tagType)
                              .Invoke(null, new object[] { startN, startN + (winN - 1), bits })!;

                // ---- positif : [0 .. maxRaw] (comme PositiveOnly)
                int startP = 0;
                int endP = maxRaw;
                int winP = System.Math.Min(endP - startP + 1, EDGE_WINDOW);
                var pos = ((int maxGapTicks, double midDeg))
                          mPos!.MakeGenericMethod(tagType)
                              .Invoke(null, new object[] { endP - (winP - 1), endP, bits })!;

                var best = (neg.maxGapTicks >= pos.maxGapTicks) ? neg : pos;

                int maxSigned = (1 << (bits - 1)) - 1;
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}" +
                          $"\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDeg:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB28_MaxUserPerceivedAngleError_Gap_Fast_NoAnchors()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var mPos = typeof(IntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_IntN),
                          BindingFlags.NonPublic | BindingFlags.Static);
            var mNeg = typeof(IntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_IntN),
                          BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(mPos, Is.Not.Null, "MeasureGapPositive_IntN introuvable");
            Assert.That(mNeg, Is.Not.Null, "MeasureGapPositive_IntN introuvable");

            const int EDGE_WINDOW = 16384; // même taille que PositiveOnly/NegativeOnly

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;

                // --- NÉGATIF (sans ancrage minRaw) : [minRaw+1 .. 0]
                int startN = minRaw + 1;
                int endN = 0;
                int winN = System.Math.Min(endN - startN + 1, EDGE_WINDOW);
                int aN = startN;
                int bN = startN + (winN - 1);

                // --- POSITIF (sans ancrage maxRaw) : [0 .. maxRaw-1]
                int startP = 0;
                int endP = maxRaw - 1;
                int winP = System.Math.Min(endP - startP + 1, EDGE_WINDOW);
                int bP = endP;
                int aP = endP - (winP - 1);

                var neg = ((int maxGapTicks, double midDeg))
                          mNeg!.MakeGenericMethod(tagType)
                               .Invoke(null, new object[] { aN, bN, bits })!;

                var pos = ((int maxGapTicks, double midDeg))
                          mPos!.MakeGenericMethod(tagType)
                               .Invoke(null, new object[] { aP, bP, bits })!;

                var best = (neg.maxGapTicks >= pos.maxGapTicks) ? neg : pos;

                int maxSigned = (1 << (bits - 1)) - 1;
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}" +
                          $"\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDeg:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB31_AtAngles_ErrorTable()
        {
            var angles = new[] { 30.0, 45.0, 60.0, 75.0, 89.0, 90.0 };

            foreach (var targetDeg in angles)
            {
                TestContext.Out.WriteLine($"\n=== θ = {targetDeg}° ===");
                TestContext.Out.WriteLine("Bn\tRaw\t\t x\t\tasinBn\tdeg\t\tΔdeg");

                for (int bits = 2; bits <= 31; bits++)
                {
                    int maxSigned = (1 << (bits - 1)) - 1;

                    int raw = BuildRawForSinDeg_IntN(bits, targetDeg);

                    double x = raw / (double)maxSigned;

                    int asinBn = Asin_IntN_ByBits(bits, raw);

                    double deg = asinBn * (90.0 / maxSigned);

                    double diff = deg - targetDeg;

                    TestContext.Out.WriteLine(
                        $"B{bits}\t{raw}\t{x,8:F6}\t{asinBn}\t{deg,8:F3}\t{diff,8:F3}");
                }
            }
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB31_AtAngles_ErrorTable_Neg()
        {
            var angles = new[] { -30.0, -45.0, -60.0, -75.0, -89.0, -90.0 };

            foreach (var targetDeg in angles)
            {
                TestContext.Out.WriteLine($"\n=== θ = {targetDeg}° ===");
                TestContext.Out.WriteLine("Bn\tRaw\t\t x\t\tasinBn\tdeg\t\tΔdeg");

                for (int bits = 2; bits <= 31; bits++)
                {
                    int maxSigned = (1 << (bits - 1)) - 1;

                    int raw = BuildRawForSinDeg_IntN(bits, targetDeg);

                    double x = raw / (double)maxSigned;

                    int asinBn = Asin_IntN_ByBits(bits, raw);

                    double deg = asinBn * (90.0 / maxSigned);

                    double diff = deg - targetDeg;

                    TestContext.Out.WriteLine(
                        $"B{bits}\t{raw}\t{x,8:F6}\t{asinBn}\t{deg,8:F3}\t{diff,8:F3}");
                }
            }
        }

        [Test]
        [Category("FixedMath/IntN")]
        public void Asin_IntN_B2toB31_VsMathAsin_FloatError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffFloat\tAvgDiffFloat\tRawAtMax\tValAtMax\tExpAtMax";
            var rng = new Random(12345);

            // FixedMath.Asin<TBits>(IntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Asin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Asin<TBits>(IntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;

                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((maxRaw - minRaw + 1), 1_000_000);
                if (numSamples <= 0)
                    continue;

                double maxDiffFloat = 0.0;
                double sumDiffFloat = 0.0;
                int sampleCount = 0;

                int rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    int raw;
                    if (bits >= 28)
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
                    int valQ16 = (int)mi.Invoke(null, new[] { angle });

                    // Convert raw to [-1,1] float
                    double x = (double)raw / ((double)(1 << (bits - 1)) - 1);

                    // Clamp just in case
                    x = System.Math.Max(-1.0, System.Math.Min(1.0, x));

                    double asinReal = System.Math.Asin(x);

                    double valFloat = valQ16 / 65536.0;
                    double diffFloat = System.Math.Abs(valFloat - asinReal);

                    sumDiffFloat += diffFloat;
                    sampleCount++;

                    if (diffFloat > maxDiffFloat)
                    {
                        maxDiffFloat = diffFloat;
                        rawAtMax = raw;
                        valAtMax = valQ16;
                        expAtMax = (int)(asinReal * 65536.0);
                    }
                }

                double avgDiffFloat = (sampleCount > 0) ? sumDiffFloat / sampleCount : 0.0;

                report += $"\nB{bits}\t{maxDiffFloat:0.#####}\t{avgDiffFloat:0.#####}\t{rawAtMax}\t{valAtMax}\t{expAtMax}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion

        #region --- ACOS LUT Retro (IntN)
        [Test]
        [Category("FixedMath/IntN")]
        public void Acos_IntN_B2toB31_MaxAngleError()
        {
            // Échantillonnage équi-réparti via un stride déterministe (copremier au domaine)
            // + hotspots autour de |x| ≈ sin(75°) (zone "tail") et aux bornes.
            // Référence double évaluée en Bn via Q16_16AcosToBn(..., signed:false).

            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            // Conversion valQ16 (Q16.16) -> raw IntN(bits), miroir du mapping prod (bit-faithful)
            static int ValQ16ToRaw_IntN(int valQ16, int bits)
            {
                if (bits == 17) return valQ16;
                if (bits > 17) return valQ16 << (bits - 17);   // élargissement
                return valQ16 >> (17 - bits);                   // rétrécissement (shift arithmétique)
            }

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(IntN<>).MakeGenericType(tagType);
                var miAcos = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                    .MakeGenericMethod(tagType);

                int maxRaw = (1 << (bits - 1)) - 1;
                int minRaw = -maxRaw;
                long domain = (long)maxRaw - (long)minRaw + 1;      // = 2^bits - 1 (impair)

                // Exhaustif si petit domaine, sinon plafond déterministe
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                // Densité angulaire (unsigned sur [0..π]): un “tick” ~ π / 2^(bits-1)
                double ticksToRad = System.Math.PI / (1u << (bits - 1));

                // Stride déterministe, copremier au domaine
                long stride = 1_103_515_245L % domain;
                if (stride <= 0) stride += domain;
                if (stride % 2 == 0) stride++;
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                // Hotspots: ±sin(75°) en Q16, avec offsets
                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();

                // Bornes & centre
                seen.Add(minRaw);
                seen.Add(0);
                seen.Add(maxRaw);

                // Hotspots autour de ±xTailQ16
                foreach (var off in offsetsQ16)
                {
                    int rq = xTailQ16 + off;
                    int rawPos = ValQ16ToRaw_IntN(rq, bits);
                    rawPos = System.Math.Max(minRaw, System.Math.Min(maxRaw, rawPos));
                    seen.Add(rawPos);

                    int rawNeg = ValQ16ToRaw_IntN(-rq, bits);
                    rawNeg = System.Math.Max(minRaw, System.Math.Min(maxRaw, rawNeg));
                    seen.Add(rawNeg);
                }

                int bestTicks = 0; double bestDeg = 0, bestRad = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)miAcos.Invoke(null, new[] { valObj });

                    // Référence: IntN -> Q16, acos(double), Q16, map unsigned
                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);

                    int diffTicks = System.Math.Abs(acosBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks;
                        atDeg = acosRadRef * (180.0 / System.Math.PI);
                    }
                }

                // 2) Échantillonnage équi-réparti sur le reste
                long idx = 0;
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)(minRaw + idx);
                    idx += stride; if (idx >= domain) idx -= domain;
                    if (!seen.Add(raw)) { continue; }

                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)miAcos.Invoke(null, new[] { valObj });

                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);

                    int diffTicks = System.Math.Abs(acosBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks;
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


using FixedEngine.Core;
using FixedEngine.LUT;
using FixedEngine.Math;
using NUnit.Framework;
using System.Reflection;

namespace FixedEngine.Tests.Math
{

    [TestFixture]
    public class UIntN_FixedMathTest
    {

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        #region --- SIN LUT Retro (UIntN) ---
        [Test]
        [Category("FixedMath/UIntN")]
        public void Sin_UIntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly; // B1 n'est pas forcément défini, mais on tente
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
                var angleType = typeof(UIntN<>).MakeGenericType(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1;
                ulong domain = (ulong)maxRaw + 1;

                // Pour B1: 0, 1; Pour B2: 0..3; etc.
                int phaseBits = bits - 2;

                // Mode bit-faithful possible seulement si Bn ≤ lutBits+2 (ici 14)
                if (bits > lutBits + 2)
                {
                    System.Console.WriteLine($"B{bits}: au-delà de la zone bit-faithful, skip (interpolation active)");
                    continue;
                }

                for (uint raw = 0; raw < domain; raw++)
                {
                    var angle = Activator.CreateInstance(angleType, raw);

                    // Calcul index LUT comme le ferait le hardware rétro
                    int idx;
                    if (phaseBits > lutBits)
                        idx = (int)(raw >> (phaseBits - lutBits));
                    else
                        idx = (int)(raw << (lutBits - phaseBits));
                    idx &= lutMask;

                    int quadrant = (int)(raw >> (bits - 2)) & 0b11;
                    int lutIdx = (quadrant == 0 || quadrant == 2) ? idx : lutMask - idx;
                    int sign = (quadrant < 2) ? 1 : -1;
                    int expected = sign * lut[lutIdx];

                    // Appel vrai code prod
                    var miGen = typeof(FixedMath)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == "SinRawDebug"
                            && m.IsGenericMethod
                            && m.GetParameters().Length == 1
                            && m.GetParameters()[0].ParameterType.IsGenericType
                            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));
                    if (miGen == null)
                        Assert.Fail("Méthode générique FixedMath.Sin<TBits>(UIntN<TBits>) absente !");
                    var mi = miGen.MakeGenericMethod(tagType);

                    int resultInt = (int)mi.Invoke(null, new[] { angle });

                    Assert.That(resultInt, Is.EqualTo(expected),
                        $"B{bits}, raw={raw}, expected={expected}, got={resultInt}");
                }
                System.Console.WriteLine($"B{bits} : bit-faithful validé ({domain} valeurs)");
            }
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Sin_UIntN_B2toB32_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(12345);

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }
                var angleType = typeof(UIntN<>).MakeGenericType(tagType);

                // Récupère la méthode générique Sin<TBits>(UIntN<TBits>)
                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRawDebug"
                        && m.IsGenericMethod
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsGenericType
                        && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));
                if (miGen == null)
                {
                    System.Console.WriteLine("Méthode générique FixedMath.Sin<TBits>(UIntN<TBits>) absente : SKIP");
                    continue;
                }
                var mi = miGen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1;
                int numSamples = (bits >= 28) ? 1000000 : System.Math.Min((int)maxRaw + 1, 1000000);
                if (numSamples == 0)
                {
                    System.Console.WriteLine($"bits={bits} : numSamples == 0, skip.");
                    continue;
                }

                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;

                uint rawMaxDiff = 0;
                double radiansMaxDiff = 0;
                int valMaxDiff = 0;
                int expectedMaxDiff = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        // Sampling random pour casser l'alignement
                        raw = (uint)rng.NextInt64(0, (long)maxRaw + 1);
                    }
                    else
                    {
                        // Sampling linéaire classique pour petits Bn
                        raw = (uint)(((ulong)maxRaw * (ulong)i) / (ulong)numSamples);
                    }

                    var angle = Activator.CreateInstance(angleType, raw);

                    int val = (int)mi.Invoke(null, new[] { angle });

                    double radians = ((double)raw / ((double)maxRaw + 1.0)) * 2.0 * System.Math.PI;
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

                        rawMaxDiff = raw;
                        radiansMaxDiff = radians;
                        valMaxDiff = val;
                        expectedMaxDiff = expected;
                    }
                }
                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
            }

            System.Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Sin_UIntN_B2toB32_VsMathSin()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);

            // FixedMath.Sin<TBits>(UIntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Sin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Sin<TBits>(UIntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1u;
                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                // amplitude idéale en Bn signé : [-maxAmp .. +maxAmp]
                // calculée en unsigned pour éviter les horreurs de 1 << 31
                int maxAmp = (bits == 32)
                    ? int.MaxValue
                    : (int)((1u << (bits - 1)) - 1u);

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                uint rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        // sampling random pour les gros Bn
                        raw = (uint)rng.NextInt64(0, (long)maxRaw + 1);
                    }
                    else
                    {
                        // sampling linéaire pour les petits Bn
                        raw = (uint)(((ulong)maxRaw * (ulong)i) / (ulong)numSamples);
                    }

                    // angle = UIntN<Bn>(raw)
                    var angle = Activator.CreateInstance(angleType, raw);

                    // ton SIN fixe (Bn signé)
                    int val = (int)mi.Invoke(null, new[] { angle });

                    // angle en radians sur [0..2π)
                    double radians = ((double)raw / ((double)maxRaw + 1.0)) * 2.0 * System.Math.PI;

                    // Math.Sin quantifié dans le même format Bn
                    double s = System.Math.Sin(radians);
                    int expected = (int)System.Math.Round(s * maxAmp);
                    if (expected < -maxAmp) expected = -maxAmp;
                    if (expected > maxAmp) expected = maxAmp;

                    // diff en long pour éviter tout overflow
                    long diffLong = (long)val - (long)expected;
                    if (diffLong < 0) diffLong = -diffLong;

                    if (diffLong > maxDiffInt)
                    {
                        maxDiffInt = diffLong;
                        maxDiffNorm = (double)diffLong / maxAmp; // erreur normalisée [-1..1]
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

        #region --- COS LUT Retro (UIntN) ---

        [Test]
        [Category("FixedMath/UIntN")]
        public void Cos_UIntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1;
                ulong domain = (ulong)maxRaw + 1;
                int phaseBits = bits - 2;

                // Bit-faithful seulement si lutBits+2 (14) ou moins
                if (bits > lutBits + 2)
                { Console.WriteLine($"B{bits}: skip, interpolation active"); continue; }

                uint quarter = 1u << (bits - 2);

                for (uint raw = 0; raw < domain; raw++)
                {
                    // Décalage de π/2 façon hardware
                    uint rawShift = (raw + quarter) & maxRaw;

                    // ------ Expected via LUT (même algo que SIN test) ------
                    int idx = (phaseBits > lutBits)
                            ? (int)(rawShift >> (phaseBits - lutBits))
                            : (int)(rawShift << (lutBits - phaseBits));
                    idx &= lutMask;

                    int quadrant = (int)(rawShift >> (bits - 2)) & 0b11;
                    int lutIdx = (quadrant == 0 || quadrant == 2) ? idx : lutMask - idx;
                    int sign = (quadrant < 2) ? 1 : -1;
                    int expected = sign * lut[lutIdx];
                    // -------------------------------------------------------

                    var miGen = typeof(FixedMath).GetMethods()
                                 .First(m => m.Name == "CosRawDebug"
                                          && m.IsGenericMethod
                                          && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));
                    var mi = miGen.MakeGenericMethod(tagType);

                    var angle = Activator.CreateInstance(angleType, raw);
                    int result = (int)mi.Invoke(null, new[] { angle });

                    Assert.That(result, Is.EqualTo(expected),
                        $"B{bits}, raw={raw}, expected={expected}, got={result}");
                }
                Console.WriteLine($"B{bits} : COS bit-faithful validé ({domain} valeurs)");
            }
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Cos_UIntN_B2toB32_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(424242);

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                           .First(m => m.Name == "Cos"
                                    && m.IsGenericMethod
                                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                           .MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1;
                int samples = (bits >= 28) ? 1_000_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);

                int maxDiff = 0;
                double maxDiffDeg = 0;
                double maxDiffValue = 0;
                double maxDiffAngleEqDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    uint raw = (bits >= 28)
                               ? (uint)rng.NextInt64(0, (long)maxRaw + 1)
                               : (uint)((ulong)maxRaw * (ulong)i / (ulong)samples);

                    var angle = Activator.CreateInstance(angleType, raw);
                    int val = (int)mi.Invoke(null, new[] { angle });

                    double rad = ((double)raw / ((double)maxRaw + 1.0)) * 2.0 * System.Math.PI;
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
        [Category("FixedMath/UIntN")]
        public void Cos_UIntN_B2toB32_VsMathCos()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffInt\tMaxDiffNorm\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(67890); // autre seed pour la beauté

            // FixedMath.Cos<TBits>(UIntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Cos"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Cos<TBits>(UIntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1u;
                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                int maxAmp = (bits == 32)
                    ? int.MaxValue
                    : (int)((1u << (bits - 1)) - 1u);

                long maxDiffInt = 0;
                double maxDiffNorm = 0.0;
                double atDeg = 0.0;
                uint rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        raw = (uint)rng.NextInt64(0, (long)maxRaw + 1);
                    }
                    else
                    {
                        raw = (uint)(((ulong)maxRaw * (ulong)i) / (ulong)numSamples);
                    }

                    var angle = Activator.CreateInstance(angleType, raw);

                    int val = (int)mi.Invoke(null, new[] { angle });

                    double radians = ((double)raw / ((double)maxRaw + 1.0)) * 2.0 * System.Math.PI;

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

        #region --- TAN Retro (UIntN) ---
        [Test]
        [Category("FixedMath/UIntN")]
        public void Tan_UIntN_B2toB32_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;

            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(24680);

            var miTanRawOpen = typeof(FixedMath).GetMethods()
                .Where(m => m.Name == "TanRawDebug" && m.IsGenericMethodDefinition)
                .First(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 1
                           && ps[0].ParameterType.IsGenericType
                           && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>);
                });

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var miTan = miTanRawOpen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? 0xFFFF_FFFFu : ((1u << bits) - 1u);

                // Échantillonnage : dense mais borné
                int samples = (bits >= 28) ? 300_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);

                int maxDiff = 0;
                double maxDiffDeg = 0, maxDiffValue = 0, maxDiffAngleEqDeg = 0;

                int maxDiffLin = 0;
                double maxDiffLinDeg = 0, maxDiffLinValue = 0, maxDiffAngleEqDegLin = 0, maxDiffAngleEqDegLinAtDeg = 0;

                ulong denom = (bits == 32) ? (1UL << 32) : (1UL << bits); // évite overflow

                for (int i = 0; i < samples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        // tirage pseudo-aléatoire uniforme
                        raw = (uint)rng.NextInt64(0, (long)maxRaw + 1);
                    }
                    else
                    {
                        // balayage uniforme
                        raw = (uint)((ulong)maxRaw * (ulong)i / (ulong)System.Math.Max(1, samples - 1));
                    }

                    // Construit l'angle UIntN<Bn>
                    var angleObj = Activator.CreateInstance(angleType, raw);

                    // Appel à FixedMath.TanRawDebug(UIntN<Bn>)
                    int tanVal = (int)miTan.Invoke(null, new[] { angleObj });

                    // Skip si sentinelle asymptote renvoyée par TanRawCore
                    if (tanVal == int.MaxValue || tanVal == int.MinValue)
                        continue;

                    // Angle en radians (0..2π) depuis l'angle N-bits
                    double angleRatio = (double)raw / (double)denom;
                    double rad = angleRatio * (System.Math.PI * 2.0);
                    double tanRad = System.Math.Tan(rad);

                    // Ignore les zones trop proches des asymptotes
                    if (double.IsInfinity(tanRad) || System.Math.Abs(tanRad) > 1e9)
                        continue;

                    // Expected en Q16.16 (≈ TanRawCore)
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

                        // équivalence d'erreur angulaire via d(tan)/dθ = sec^2(θ)
                        double sec2 = 1.0 + tanRad * tanRad;
                        maxDiffAngleEqDeg = (sec2 != 0) ? (maxDiffValue / sec2) * (180.0 / System.Math.PI) : double.PositiveInfinity;
                    }

                    // Zone "linéaire" (utile gameplay)
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
        [Category("FixedMath/UIntN")]
        public void Tan_UIntN_B2toB32_VsMathTan_FloatError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffFloat\tAvgDiffFloat\tMaxDiffDeg\tAtDeg\tRaw\tVal\tExp";
            var rng = new Random(12345);

            // FixedMath.Tan<TBits>(UIntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Tan"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Tan<TBits>(UIntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1u;
                int numSamples = (bits >= 28) ? 1_000_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);
                if (numSamples <= 0)
                {
                    Console.WriteLine($"bits={bits} : numSamples <= 0, skip.");
                    continue;
                }

                double maxDiffFloat = 0.0;
                double sumDiffFloat = 0.0;
                int sampleCount = 0;
                double maxDiffDeg = 0.0;
                double atDeg = 0.0;
                uint rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        raw = (uint)rng.NextInt64(0, (long)maxRaw + 1);
                    }
                    else
                    {
                        raw = (uint)((maxRaw * (ulong)i) / (ulong)(numSamples - 1));
                    }

                    var angle = Activator.CreateInstance(angleType, raw);
                    int valQ16 = (int)mi.Invoke(null, new[] { angle });

                    double radians = (double)raw / ((double)maxRaw + 1.0) * 2.0 * System.Math.PI;
                    double tanReal = System.Math.Tan(radians);

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
        MeasureGapPositive_UIntN<TBits>(int start, int end, int bits)
            where TBits : struct
        {
            if (start >= end) return (0, 0.0); // Sécurité

            int maxSigned = (1 << (bits - 1)) - 1;
            double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);

            int prevTick = FixedMath.Asin(new UIntN<TBits>((uint)start));
            int bestGapTicks = 0;
            double bestMidDeg = 0.0; // Sera initialisé au premier gap

            for (int raw = start + 1; raw <= end; raw++)
            {
                int curTick = FixedMath.Asin(new UIntN<TBits>((uint)raw));
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

        private static int Asin_UIntN_ByBits(int bits, uint raw)
        {
            switch (bits)
            {
                case 2: return FixedMath.Asin(new UIntN<B2>(raw));
                case 3: return FixedMath.Asin(new UIntN<B3>(raw));
                case 4: return FixedMath.Asin(new UIntN<B4>(raw));
                case 5: return FixedMath.Asin(new UIntN<B5>(raw));
                case 6: return FixedMath.Asin(new UIntN<B6>(raw));
                case 7: return FixedMath.Asin(new UIntN<B7>(raw));
                case 8: return FixedMath.Asin(new UIntN<B8>(raw));
                case 9: return FixedMath.Asin(new UIntN<B9>(raw));
                case 10: return FixedMath.Asin(new UIntN<B10>(raw));
                case 11: return FixedMath.Asin(new UIntN<B11>(raw));
                case 12: return FixedMath.Asin(new UIntN<B12>(raw));
                case 13: return FixedMath.Asin(new UIntN<B13>(raw));
                case 14: return FixedMath.Asin(new UIntN<B14>(raw));
                case 15: return FixedMath.Asin(new UIntN<B15>(raw));
                case 16: return FixedMath.Asin(new UIntN<B16>(raw));
                case 17: return FixedMath.Asin(new UIntN<B17>(raw));
                case 18: return FixedMath.Asin(new UIntN<B18>(raw));
                case 19: return FixedMath.Asin(new UIntN<B19>(raw));
                case 20: return FixedMath.Asin(new UIntN<B20>(raw));
                case 21: return FixedMath.Asin(new UIntN<B21>(raw));
                case 22: return FixedMath.Asin(new UIntN<B22>(raw));
                case 23: return FixedMath.Asin(new UIntN<B23>(raw));
                case 24: return FixedMath.Asin(new UIntN<B24>(raw));
                case 25: return FixedMath.Asin(new UIntN<B25>(raw));
                case 26: return FixedMath.Asin(new UIntN<B26>(raw));
                case 27: return FixedMath.Asin(new UIntN<B27>(raw));
                case 28: return FixedMath.Asin(new UIntN<B28>(raw));
                case 29: return FixedMath.Asin(new UIntN<B29>(raw));
                case 30: return FixedMath.Asin(new UIntN<B30>(raw));
                case 31: return FixedMath.Asin(new UIntN<B31>(raw));
                default: throw new ArgumentOutOfRangeException(nameof(bits));
            }
        }


        private static uint BuildRawForSinDeg(int bits, double thetaDeg)
        {

            double x = System.Math.Sin(thetaDeg * System.Math.PI / 180.0);
            uint maxU = (uint)((1 << bits) - 1);

            double u = (x + 1.0) * 0.5 * maxU;
            long r = (long)System.Math.Round(u, MidpointRounding.AwayFromZero);
            if (r < 0) r = 0;
            if (r > maxU) r = maxU;
            return (uint)r;
        } 
        #endregion

        #region --- ASIN LUT Retro (UIntN)
        [Test]
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            // Inverse approx. du mapping prod UIntN -> Q16 (valQ16 = ((raw*2 - maxRaw)*65536)/maxRaw, en trunc)
            static int ValQ16ToRaw_UIntN_Trunc(int valQ16, int bits)
            {
                long maxRaw = (1L << bits) - 1L;
                long rawApprox = (valQ16 * maxRaw) / 65536L;   // trunc
                long raw = (rawApprox + maxRaw) / 2L;          // ((t*max + max)/2)
                if (raw < 0) raw = 0;
                if (raw > maxRaw) raw = maxRaw;
                return (int)raw;
            }

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(UIntN<>).MakeGenericType(tagType);
                var miAsin = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                    .MakeGenericMethod(tagType);

                int maxRaw = (1 << bits) - 1;
                int minRaw = 0;
                long domain = (long)maxRaw - (long)minRaw + 1;          // = 2^bits
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                // Densité angulaire en sortie signée ([-π/2..+π/2] -> [-max..+max])
                int maxTicks = (1 << (bits - 1)) - 1;
                double ticksToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxTicks);

                // Stride déterministe, copremier avec 2^bits: il suffit qu'il soit impair
                long stride = 1_103_515_245L % domain; if (stride <= 0) stride += domain;
                if ((stride & 1) == 0) stride++;
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                // Hotspots: ±sin(75°) en Q16, + bords et centre
                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();
                seen.Add(minRaw);
                seen.Add(maxRaw);
                seen.Add(maxRaw >> 1); // centre (valQ16 ~ 0)

                foreach (var off in offsetsQ16)
                {
                    // +xTail
                    int rawPos = ValQ16ToRaw_UIntN_Trunc(xTailQ16 + off, bits);
                    seen.Add(rawPos);
                    if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1); // voisin pour contrer l’effet trunc

                    // -xTail
                    int rawNeg = ValQ16ToRaw_UIntN_Trunc(-(xTailQ16 + off), bits);
                    seen.Add(rawNeg);
                    if (rawNeg + 1 <= maxRaw) seen.Add(rawNeg + 1);
                }

                int bestTicks = 0; double bestDeg = 0, bestRad = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });

                    // Référence double: map UIntN -> Q16 (trunc comme prod), asin, puis map signé
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits);

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg) { bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks; atDeg = asinRadRef * 180.0 / System.Math.PI; }
                }

                // 2) Échantillonnage uniforme déterministe sur le reste
                long idx = 0;
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)(minRaw + idx);
                    idx += stride; if (idx >= domain) idx -= domain;
                    if (!seen.Add(raw)) continue;

                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });

                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits);

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg) { bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks; atDeg = asinRadRef * 180.0 / System.Math.PI; }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_IsMonotone_B16()
        {
            var seen = new HashSet<int>();
            int prev = int.MinValue;

            // B16 => domaine 0..65535
            for (uint raw = 0; raw <= 65535u; raw++)
            {
                int y = FixedMath.Asin(new UIntN<B16>(raw));

                // monotonicité non stricte (plateaux permis si quantums identiques)
                Assert.That(y, Is.GreaterThanOrEqualTo(prev), $"non-monotone at raw={raw}");
                prev = y;
                seen.Add(y);
            }

            // bornes théoriques pour ticks signés B16
            int maxTicks = (1 << (16 - 1)) - 1; // 32767

            // mesures réelles observées
            int seenMin = seen.Min();
            int seenMax = seen.Max();

            Assert.Multiple(() =>
            {
                // centre présent
                Assert.That(seen.Contains(0), Is.True, "0° manquant");

                // bornes atteintes à ±1 tick près (arrondi négatif peut remonter d'un cran)
                Assert.That(seenMax, Is.InRange(maxTicks - 0, maxTicks), "+90° trop bas");
                Assert.That(seenMin, Is.InRange(-maxTicks, -maxTicks + 1), "-90° trop haut");

                // sécurité: rien ne dépasse l'intervalle théorique
                Assert.That(seenMin, Is.GreaterThanOrEqualTo(-maxTicks));
                Assert.That(seenMax, Is.LessThanOrEqualTo(maxTicks));
            });
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB28_MaxUserPerceivedAngleError_PositiveOnly()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // Récupère la MethodInfo du helper défini ci-dessus
            var generic = typeof(UIntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_UIntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_UIntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;

                // Positive side only : [domain/2 .. maxRaw]
                int start = domain / 2;
                int end = maxRaw;
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contiguë aux bords positifs
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
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB28_MaxUserPerceivedAngleError_NegativeOnly()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // Même helper que pour le positif : il mesure le max gap sur [a..b]
            var generic = typeof(UIntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_UIntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_UIntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;

                // Negative side only : [0 .. domain/2]
                int start = 0;
                int end = domain / 2;
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contiguë au bord négatif (ancrage raw==0 inclus)
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
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB28_MaxError_IncludingAnchors()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // Récupère la MethodInfo du helper défini ci-dessus
            var generic = typeof(UIntN_FixedMathTest).GetMethod(
                nameof(MeasureGapPositive_UIntN),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive_UIntN introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;

                // Positive side only : [domain/2 .. maxRaw-1] 
                // (on exclut la marche d’ancrage raw==maxRaw qui force 90° exact)
                int start = domain / 2;
                int end = maxRaw - 1;
                int window = System.Math.Min(end - start + 1, EDGE_WINDOW);

                var m = generic!.MakeGenericMethod(tagType);

                // Fenêtre contiguë aux bords positifs, mais sans la toute dernière marche
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
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB28_MaxUserPerceivedAngleError_Gap_Fast()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var mPos = typeof(UIntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_UIntN),
                        BindingFlags.NonPublic | BindingFlags.Static);
            var mNeg = typeof(UIntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_UIntN),
                        BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(mPos, Is.Not.Null);
            Assert.That(mNeg, Is.Not.Null);

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;

                // ---- négatif : même fenêtre que NegativeOnly
                int startN = 0, endN = domain / 2;
                int winN = System.Math.Min(endN - startN + 1, EDGE_WINDOW);
                var neg = ((int maxGapTicks, double midDeg))
                          mNeg!.MakeGenericMethod(tagType)
                              .Invoke(null, new object[] { startN, startN + (winN - 1), bits })!;

                // ---- positif : même fenêtre que PositiveOnly
                int startP = domain / 2, endP = maxRaw;
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
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB28_MaxUserPerceivedAngleError_Gap_Fast_NoAnchors()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            var mPos = typeof(UIntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_UIntN),
                          BindingFlags.NonPublic | BindingFlags.Static);
            var mNeg = typeof(UIntN_FixedMathTest).GetMethod(nameof(MeasureGapPositive_UIntN),
                          BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(mPos, Is.Not.Null, "MeasureGapPositive_UIntN introuvable");
            Assert.That(mNeg, Is.Not.Null, "MeasureGapNegative introuvable");

            const int EDGE_WINDOW = 16384; // même taille que PositiveOnly/NegativeOnly

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;

                // --- NÉGATIF (sans ancrage) : [1 .. 1+(winN-1)]
                int startN = 1;
                int endN = domain / 2; // borne médiane (incluse côté helper)
                int winN = System.Math.Min(endN - startN + 1, EDGE_WINDOW);
                int aN = startN;
                int bN = startN + (winN - 1);

                // --- POSITIF (sans ancrage) : [maxRaw-1-(winP-1) .. maxRaw-1]
                int startP = domain / 2;
                int endP = maxRaw - 1; // exclut l’ancrage maxRaw
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
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB31_AtAngles_ErrorTable()
        {
            var angles = new[] { 30.0, 45.0, 60.0, 75.0, 89.0, 90.0 };

            foreach (var targetDeg in angles)
            {
                TestContext.Out.WriteLine($"\n=== θ = {targetDeg}° ===");
                TestContext.Out.WriteLine("Bn\tRaw\t\t x\t\tasinBn\tdeg\t\tΔdeg");

                for (int bits = 2; bits <= 31; bits++)
                {
                    uint maxU = (uint)((1 << bits) - 1);
                    int maxSigned = (1 << (bits - 1)) - 1;

                    uint raw = BuildRawForSinDeg(bits, targetDeg);

                    // x réellement injecté par le mapping utilisé par Asin(UIntN):
                    // x = (2*raw - max) / max
                    double x = ((2.0 * raw) - maxU) / maxU;

                    int asinBn = Asin_UIntN_ByBits(bits, raw);

                    // Bn signé -> degrés (fenêtre [-90..+90])
                    double deg = asinBn * (90.0 / maxSigned);

                    double diff = deg - targetDeg;

                    TestContext.Out.WriteLine(
                        $"B{bits}\t{raw}\t{x,8:F6}\t{asinBn}\t{deg,8:F3}\t{diff,8:F3}");
                }
            }
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB31_AtAngles_ErrorTable_neg()
        {
            var angles = new[] { -30.0, -45.0, -60.0, -75.0, -89.0, -90.0 };

            foreach (var targetDeg in angles)
            {
                TestContext.Out.WriteLine($"\n=== θ = {targetDeg}° ===");
                TestContext.Out.WriteLine("Bn\tRaw\t\t x\t\tasinBn\tdeg\t\tΔdeg");

                for (int bits = 2; bits <= 31; bits++)
                {
                    uint maxU = (uint)((1 << bits) - 1);
                    int maxSigned = (1 << (bits - 1)) - 1;

                    uint raw = BuildRawForSinDeg(bits, targetDeg);

                    // x réellement injecté par le mapping utilisé par Asin(UIntN):
                    // x = (2*raw - max) / max
                    double x = ((2.0 * raw) - maxU) / maxU;

                    int asinBn = Asin_UIntN_ByBits(bits, raw);

                    // Bn signé -> degrés (fenêtre [-90..+90])
                    double deg = asinBn * (90.0 / maxSigned);

                    double diff = deg - targetDeg;

                    TestContext.Out.WriteLine(
                        $"B{bits}\t{raw}\t{x,8:F6}\t{asinBn}\t{deg,8:F3}\t{diff,8:F3}");
                }
            }
        }

        [Test]
        [Category("FixedMath/UIntN")]
        public void Asin_UIntN_B2toB31_VsMathAsin_FloatError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxDiffFloat\tAvgDiffFloat\tRawAtMax\tValAtMax\tExpAtMax";
            var rng = new Random(12345);

            // FixedMath.Asin<TBits>(UIntN<TBits>)
            var miGen = typeof(FixedMath)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Asin"
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>));

            if (miGen == null)
                Assert.Fail("Méthode générique FixedMath.Asin<TBits>(UIntN<TBits>) introuvable.");

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null)
                {
                    Console.WriteLine($"Type FixedEngine.Core.B{bits} absent : SKIP");
                    continue;
                }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = miGen.MakeGenericMethod(tagType);

                uint maxRaw = (bits == 32) ? uint.MaxValue : (1u << bits) - 1u;
                int numSamples = (bits >= 28)
                    ? 1_000_000
                    : System.Math.Min((int)maxRaw + 1, 1_000_000);
                if (numSamples <= 0)
                    continue;

                double maxDiffFloat = 0.0;
                double sumDiffFloat = 0.0;
                int sampleCount = 0;

                uint rawAtMax = 0;
                int valAtMax = 0;
                int expAtMax = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    uint raw;
                    if (bits >= 28)
                    {
                        ulong r = (ulong)rng.NextInt64(0, (long)maxRaw + 1);
                        raw = (uint)r;
                    }
                    else
                    {
                        raw = (uint)(((ulong)maxRaw * (ulong)i) / (ulong)(numSamples - 1));
                    }

                    var angle = Activator.CreateInstance(angleType, raw);
                    int valQ16 = (int)mi.Invoke(null, new[] { angle });

                    // Convert raw to [0,1] float
                    double x = (double)raw / (double)maxRaw;

                    // Clamp par sécurité (devrait être inutile)
                    x = System.Math.Max(0.0, System.Math.Min(1.0, x));

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

        #region --- ACOS LUT Retro (UIntN)
        [Test]
        [Category("FixedMath/UIntN")]
        public void Acos_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Core.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            static int ValQ16ToRaw_UIntN_Trunc(int valQ16, int bits)
            {
                long maxRaw = (1L << bits) - 1L;
                long rawApprox = (valQ16 * maxRaw) / 65536L;   // trunc
                long raw = (rawApprox + maxRaw) / 2L;
                if (raw < 0) raw = 0;
                if (raw > maxRaw) raw = maxRaw;
                return (int)raw;
            }

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Core.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(UIntN<>).MakeGenericType(tagType);
                var miAcos = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                    .MakeGenericMethod(tagType);

                int maxRaw = (1 << bits) - 1;
                int minRaw = 0;
                long domain = (long)maxRaw - (long)minRaw + 1;      // = 2^bits
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                // Densité angulaire unsigned sur [0..π]
                double ticksToRad = System.Math.PI / (1u << (bits - 1));

                long stride = 1_103_515_245L % domain; if (stride <= 0) stride += domain;
                if ((stride & 1) == 0) stride++;
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();
                seen.Add(minRaw);
                seen.Add(maxRaw);
                seen.Add(maxRaw >> 1); // centre (~0 rad)

                foreach (var off in offsetsQ16)
                {
                    int rawPos = ValQ16ToRaw_UIntN_Trunc(xTailQ16 + off, bits);
                    seen.Add(rawPos);
                    if (rawPos + 1 <= maxRaw) seen.Add(rawPos + 1);

                    int rawNeg = ValQ16ToRaw_UIntN_Trunc(-(xTailQ16 + off), bits);
                    seen.Add(rawNeg);
                    if (rawNeg + 1 <= maxRaw) seen.Add(rawNeg + 1);
                }

                int bestTicks = 0; double bestDeg = 0, bestRad = 0, atDeg = 0;

                // 1) Hotspots
                foreach (int raw in seen)
                {
                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)miAcos.Invoke(null, new[] { valObj });

                    // Référence double: map UIntN -> Q16 (trunc), acos, puis map unsigned
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);

                    int diffTicks = System.Math.Abs(acosBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg) { bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks; atDeg = acosRadRef * 180.0 / System.Math.PI; }
                }

                // 2) Uniform stride sur le reste
                long idx = 0;
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)(minRaw + idx);
                    idx += stride; if (idx >= domain) idx -= domain;
                    if (!seen.Add(raw)) continue;

                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)miAcos.Invoke(null, new[] { valObj });

                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);

                    int diffTicks = System.Math.Abs(acosBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg) { bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks; atDeg = acosRadRef * 180.0 / System.Math.PI; }
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


using FixedEngine.LUT;
using FixedEngine.Math;
using FixedEngine.Math.Consts;
using NUnit.Framework;
using System.Reflection;

namespace FixedEngine.Tests.Math
{



    [TestFixture]
    public class FixedMathTest
    {

        // ==========================
        // --- SIN/COS/TAN LUT Retro ---
        // ==========================
        #region --- SIN/COS/TAN LUT Retro ---

        //----- SIN -----
        #region --- SIN LUT Retro (UIntN) ---
        [Test]
        public void Sin_UIntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly; // B1 n'est pas forcément défini, mais on tente
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Math.B{bits} absent : SKIP");
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
                            m.Name == "SinRaw"
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
        public void Sin_UIntN_B2toB32_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(12345);

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Math.B{bits} absent : SKIP");
                    continue;
                }
                var angleType = typeof(UIntN<>).MakeGenericType(tagType);

                // Récupère la méthode générique Sin<TBits>(UIntN<TBits>)
                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRaw"
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
        #endregion

        #region --- SIN LUT Retro (IntN) ---
        [Test]
        public void Sin_IntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Math.B{bits} absent : SKIP");
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
                            m.Name == "SinRaw"
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
        public void Sin_IntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(98765);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null)
                {
                    System.Console.WriteLine($"Type FixedEngine.Math.B{bits} absent : SKIP");
                    continue;
                }
                var angleType = typeof(IntN<>).MakeGenericType(tagType);

                var miGen = typeof(FixedMath)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "SinRaw"
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

        #endregion

        // --- COS ---
        #region --- COS LUT Retro (UIntN) ---

        [Test]
        public void Cos_UIntN_B2toB32_BitFaithful()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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
                                 .First(m => m.Name == "CosRaw"
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
        public void Cos_UIntN_B2toB32_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(424242);

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                           .First(m => m.Name == "CosRaw"
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
        #endregion

        #region --- COS LUT Retro (IntN) ---

        [Test]
        public void Cos_IntN_B2toB31_BitFaithful()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            int lutBits = 12;
            int lutMask = (1 << lutBits) - 1;
            var lut = SinLUT4096.LUT;

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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
                                 .First(m => m.Name == "CosRaw"
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
        public void Cos_IntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(8675309);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                           .First(m => m.Name == "CosRaw"
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
        #endregion

        // ----- TAN -----
        #region --- TAN Retro (UIntN) via TanRaw ---
        [Test]
        public void Tan_UIntN_B2toB32_MaxDiffMeasure_TanRaw()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;

            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(24680);

            var miTanRawOpen = typeof(FixedMath).GetMethods()
                .Where(m => m.Name == "TanRaw" && m.IsGenericMethodDefinition)
                .First(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 1
                           && ps[0].ParameterType.IsGenericType
                           && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>);
                });

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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

                    // Appel à FixedMath.TanRaw(UIntN<Bn>)
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
        #endregion

        #region --- TAN Retro (IntN) via TanRaw ---
        [Test]
        public void Tan_IntN_B2toB31_MaxDiffMeasure_TanRaw()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;

            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(13579);

            // Récupère la méthode générique FixedMath.TanRaw(IntN<>)
            var miTanRawOpen = typeof(FixedMath).GetMethods()
                .Where(m => m.Name == "TanRaw" && m.IsGenericMethodDefinition)
                .First(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 1
                        && ps[0].ParameterType.IsGenericType
                        && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>);
                });

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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

                    // Appel à FixedMath.TanRaw(IntN<Bn>)
                    int tanVal = (int)miTan.Invoke(null, new[] { angleObj });

                    // Skip si sentinelle asymptote
                    if (tanVal == int.MaxValue || tanVal == int.MinValue)
                        continue;

                    // IMPORTANT : pour comparer à Math.Tan, on convertit l’angle EXACTEMENT
                    // comme le fait TanRaw(IntN): wrap signé -> unsigned sur N bits.
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
        #endregion

        #endregion

        // ==========================
        // --- ASIN/ACOS LUT Retro ---
        // ==========================
        #region --- ASIN/ACOS LUT Retro ---

        // ----- ASIN -----
        #region --- ASIN LUT Retro (UIntN)
        [Test]
        public void Asin_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
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
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

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
        public void Asin_IsMonotone_B16()
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
        #endregion

        #region --- ASIN LUT Retro (IntN)
        [Test]
        public void Asin_IntN_B2toB31_MaxAngleError()
        {
            // On échantillonne de manière équi-répartie via un stride déterministe
            // + on injecte des hotspots près du seuil de la "tail" (sin 75°),
            // + on compare la sortie Bn (signé) à la référence double mappée via Q16_16AngleToBn.
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            // helper locaux
            static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return System.Math.Abs(a); }

            // Conversion valQ16 (Q16.16) -> raw IntN(bits), en miroir du mapping prod (bit-faithful)
            static int ValQ16ToRaw_IntN(int valQ16, int bits)
            {
                if (bits == 17) return valQ16;
                if (bits > 17) return valQ16 << (bits - 17);       // élargissement
                return valQ16 >> (17 - bits);                        // rétrécissement (shift arithmétique)
            }

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(FixedEngine.Math.IntN<>).MakeGenericType(tagType);
                var miAsin = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(FixedEngine.Math.IntN<>))
                    .MakeGenericMethod(tagType);

                int maxRaw = (1 << (bits - 1)) - 1;
                int minRaw = -maxRaw;
                long domain = (long)maxRaw - (long)minRaw + 1;      // = 2^bits - 1 (impair)

                // Nombre d’échantillons: exhaustif si domaine "petit", sinon plafond déterministe
                int samples = (domain <= 2_000_000L) ? (int)domain : 1_000_000;

                // Stride déterministe, copremier au domaine (pour éviter les cycles courts)
                long stride = 1_103_515_245L % domain;              // base LCG-like
                if (stride <= 0) stride += domain;
                if (stride % 2 == 0) stride++;                      // impair
                                                                    // Assure-toi que gcd(stride, domain) == 1, sinon ajuste légèrement
                while (Gcd(stride, domain) != 1) { stride += 2; if (stride >= domain) stride -= domain; }

                // Hotspots: ±sin(75°) en Q16, avec offsets
                int xTailQ16 = (int)System.Math.Round(System.Math.Sin(System.Math.PI * 75.0 / 180.0) * 65536.0);
                int[] offsetsQ16 = { -256, -128, -64, -32, -16, -8, -4, -1, 0, 1, 4, 8, 16, 32, 64, 128, 256 };

                var seen = new System.Collections.Generic.HashSet<int>();

                // Ajoute bornes et centre
                seen.Add(minRaw);
                seen.Add(0);
                seen.Add(maxRaw);

                // Ajoute hotspots autour de ±xTailQ16
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

                // Prépare metrologie
                int maxTicks = (1 << (bits - 1)) - 1;               // échelle signée
                double ticksToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxTicks);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;

                // 1) Évalue tous les hotspots
                foreach (int raw in seen)
                {
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });

                    // Référence: IntN -> Q16, asin(double), Q16, map signé
                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg; bestRad = diffRad; bestTicks = diffTicks;
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }

                // 2) Échantillonnage équi-réparti sur le reste du domaine
                long idx = 0; // index logique [0..domain-1]
                int remaining = System.Math.Max(0, samples - seen.Count);
                for (int i = 0; i < remaining; i++)
                {
                    int raw = (int)(minRaw + idx);
                    idx += stride; if (idx >= domain) idx -= domain;

                    if (!seen.Add(raw)) { continue; } // évite les doublons avec hotspots

                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)miAsin.Invoke(null, new[] { valObj });

                    int valQ16 = (bits == 17) ? raw
                              : (bits > 17) ? (raw >> (bits - 17))
                                            : (raw << (17 - bits));

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

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
        #endregion

        #region --- ASIN LUT Retro (UFixed Q0.8) ---
        [Test]
        public void Asin_UFixed_Q0_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Math.B8"); // F = 8 (Q0.8)
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
                var intTag = asm.GetType($"FixedEngine.Math.B{bits}");
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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true); // [-π/2..+π/2] signé
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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);
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

        #region --- ASIN LUT Retro (Fixed Q8.8) ---
        [Test]
        public void Asin_Fixed_Q8_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Math.B8"); // F = 8 (Q8.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            // mapping inverse Q16 -> raw (Fixed, signé) en miroir du prod (shifts bit-faithful)
            static int ValQ16ToRaw_Fixed(int valQ16, int F)
                => (F == 16) ? valQ16
                   : (F > 16) ? (valQ16 << (F - 16))
                              : (valQ16 >> (16 - F));

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Math.B{bits}");
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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

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
                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true);

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

        #region --- ASIN (UIntN) ----
        // Helper : appelle FixedMath.Asin pour le bon TBits
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

        // --- Helper: fabrique raw pour viser sin(thetaDeg) ---
        private static uint BuildRawForSinDeg(int bits, double thetaDeg)
        {
            // x = sin(theta) dans [-1..+1]
            double x = System.Math.Sin(thetaDeg * System.Math.PI / 180.0);
            uint maxU = (uint)((1 << bits) - 1);
            // raw ≈ round( (x+1)/2 * max )
            double u = (x + 1.0) * 0.5 * maxU;
            long r = (long)System.Math.Round(u, MidpointRounding.AwayFromZero);
            if (r < 0) r = 0;
            if (r > maxU) r = maxU;
            return (uint)r;
        }

        [Test]
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
        #endregion


        #region test
        // Helper générique: renvoie (maxGapTicks, midDegApprox) sur [start..end]
        private static (int maxGapTicks, double midDegApprox)
        MeasureGapGenericTicks<TBits>(int start, int end, int bits)
            where TBits : struct
        {
            int maxSigned = (1 << (bits - 1)) - 1;
            double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);

            // --------------- Pass 1 : coarse (stride) ---------------
            // Objectif: repérer rapidement où le gap est grand sans tout balayer
            int stride = 32; // ajuste 16/32 selon budget
            int bestGapTicks = 0;
            int bestRaw = start; // raw après lequel le gap est max
            int prevTick = FixedMath.Asin(new UIntN<TBits>(start));

            for (int raw = start + 1; raw <= end;)
            {
                int curTick;

                // On évalue quand même le voisin direct (raw-1 -> raw), car le gap max peut être local
                curTick = FixedMath.Asin(new UIntN<TBits>(raw));
                int gapTicks = System.Math.Abs(curTick - prevTick);
                if (gapTicks > bestGapTicks) { bestGapTicks = gapTicks; bestRaw = raw; }
                prevTick = curTick;

                // Sauter de 'stride' pas (sauf si on est proche de la fin)
                int next = raw + stride;
                if (next > end) break;

                int tickNext = FixedMath.Asin(new UIntN<TBits>(next));
                gapTicks = System.Math.Abs(tickNext - prevTick);
                if (gapTicks > bestGapTicks) { bestGapTicks = gapTicks; bestRaw = next; }
                prevTick = tickNext;

                raw = next + 1;
            }

            // --------------- Pass 2 : fine (contigu autour du best) ---------------
            // Balayage contigu court (±256 raws) autour de la zone gagnante
            int radius = 256;
            int from = System.Math.Max(start, bestRaw - radius);
            int to = System.Math.Min(end, bestRaw + radius);

            int maxGapTicks = 0;
            double midDegApprox = 0.0;

            int prev = FixedMath.Asin(new UIntN<TBits>(from));
            for (int raw = from + 1; raw <= to; raw++)
            {
                int cur = FixedMath.Asin(new UIntN<TBits>(raw));
                int gap = System.Math.Abs(cur - prev);
                if (gap > maxGapTicks)
                {
                    maxGapTicks = gap;
                    // approximation : milieu en degrés entre les deux sorties
                    midDegApprox = ((cur + prev) * 0.5) * tickToDeg;
                }
                prev = cur;
            }

            // garde le meilleur des deux passes
            if (bestGapTicks > maxGapTicks)
            {
                maxGapTicks = bestGapTicks;
                // mid≈ : si on n’a pas la paire précise ici, on approxime par la sortie à bestRaw
                double midTick = FixedMath.Asin(new UIntN<TBits>(bestRaw));
                midDegApprox = midTick * tickToDeg;
            }

            return (maxGapTicks, midDegApprox);
        }

        [Test]
        public void Asin_UIntN_B2toB31_MaxUserPerceivedAngleError_Gap_Fast()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // MethodInfo du helper générique
            var generic = typeof(FixedMathTest).GetMethod(
                nameof(MeasureGapGenericTicks),
                BindingFlags.NonPublic | BindingFlags.Static);
            if (generic == null) Assert.Fail("MeasureGapGenericTicks<TBits> introuvable.");

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}", throwOnError: true);
                int maxRaw = (1 << bits) - 1;
                int domain = maxRaw + 1;
                int window = System.Math.Min(domain, 4096); // 4096 suffit largement

                // Fermer générique UNE fois
                var m = generic.MakeGenericMethod(tagType);

                // On ne mesure qu’aux bords (là où le gap est max)
                // Bord négatif : [0 .. window-1]
                var left = ((int maxGapTicks, double midDegApprox))
                           m.Invoke(null, new object[] { 0, window - 1, bits })!;
                // Bord positif : [maxRaw-(window-1) .. maxRaw]
                var right = ((int maxGapTicks, double midDegApprox))
                            m.Invoke(null, new object[] { maxRaw - (window - 1), maxRaw, bits })!;

                var best = (left.maxGapTicks >= right.maxGapTicks) ? left : right;

                int maxSigned = (1 << (bits - 1)) - 1;
                double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);
                double tickToRad = (System.Math.PI / 2.0) / System.Math.Max(1, maxSigned);

                double maxGapDeg = best.maxGapTicks * tickToDeg;
                double maxErrDeg = 0.5 * maxGapDeg;             // gap/2 = erreur perçue max
                double maxErrRad = 0.5 * best.maxGapTicks * tickToRad;

                report += $"\nB{bits}\t{maxErrDeg:0.00000}\t{maxErrRad:0.00000}\t{maxGapDeg:0.00000}\t{best.maxGapTicks}\t{best.midDegApprox:0.###}";
            }

            TestContext.Out.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion

        #region test 2
        // 🔹 Helper générique placé AU NIVEAU DE LA CLASSE
        private static (int maxGapTicks, double midDeg)
        MeasureGapPositive<TBits>(int start, int end, int bits)
            where TBits : struct
        {
            int maxSigned = (1 << (bits - 1)) - 1;
            double tickToDeg = 90.0 / System.Math.Max(1, maxSigned);

            int prevTick = FixedMath.Asin(new UIntN<TBits>(start));
            int bestGapTicks = 0;
            double bestMidDeg = prevTick * tickToDeg;

            for (int raw = start + 1; raw <= end; raw++)
            {
                int curTick = FixedMath.Asin(new UIntN<TBits>(raw));
                int gapTicks = System.Math.Abs(curTick - prevTick);
                if (gapTicks > bestGapTicks)
                {
                    bestGapTicks = gapTicks;
                    bestMidDeg = ((curTick + prevTick) * 0.5) * tickToDeg;
                }
                prevTick = curTick;
            }
            return (bestGapTicks, bestMidDeg);
        }

        [Test]
        public void Asin_UIntN_B2toB31_MaxUserPerceivedAngleError_PositiveOnly()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxErrDeg\tMaxErrRad\tMaxGapDeg\tMaxGapTicks\tAtDeg≈";

            // Récupère la MethodInfo du helper défini ci-dessus
            var generic = typeof(FixedMathTest).GetMethod(
                nameof(MeasureGapPositive),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(generic, Is.Not.Null, "Helper MeasureGapPositive introuvable");

            const int EDGE_WINDOW = 16384;

            for (int bits = 2; bits <= 28; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}", throwOnError: true);
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
    
        #endregion

        // ----- ACOS -----
        #region --- ACOS LUT Retro (UIntN)
        [Test]
        public void Acos_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
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
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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

        #region --- ACOS LUT Retro (IntN)
        [Test]
        public void Acos_IntN_B2toB31_MaxAngleError()
        {
            // Échantillonnage équi-réparti via un stride déterministe (copremier au domaine)
            // + hotspots autour de |x| ≈ sin(75°) (zone "tail") et aux bornes.
            // Référence double évaluée en Bn via Q16_16AcosToBn(..., signed:false).

            var asm = typeof(FixedEngine.Math.B2).Assembly;
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
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
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

        #region --- ACOS LUT Retro (UFixed Q0.8) ---
        [Test]
        public void Acos_UFixed_Q0_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Math.B8"); // F = 8 (Q0.8)
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
                var intTag = asm.GetType($"FixedEngine.Math.B{bits}");
                if (intTag == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var ufixedType = typeof(FixedEngine.Math.UFixed<,>).MakeGenericType(intTag, fracTag);
                var miAcos = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(FixedEngine.Math.UFixed<,>))
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

        #region --- ACOS LUT Retro (Fixed Q8.8) ---
        [Test]
        public void Acos_Fixed_Q8_8_B9toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            var fracTag = asm.GetType("FixedEngine.Math.B8"); // F = 8 (Q8.8)
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";

            static int ValQ16ToRaw_Fixed(int valQ16, int F)
                => (F == 16) ? valQ16
                   : (F > 16) ? (valQ16 << (F - 16))
                              : (valQ16 >> (16 - F));

            for (int bits = 9; bits <= 31; bits++)
            {
                var intTag = asm.GetType($"FixedEngine.Math.B{bits}");
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


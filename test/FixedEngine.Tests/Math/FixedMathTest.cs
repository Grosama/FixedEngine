using FixedEngine.LUT;
using FixedEngine.Math;
using FixedEngine.Math.Consts;
using NUnit.Framework;

namespace FixedEngine.Tests.Math
{



    [TestFixture]
    public  class FixedMathTest
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
        [Explicit] // lourd : 1e6 itérations possibles
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
                .First(m => {
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
        [Explicit] // lourd : jusqu’à 3e5–1e6 itérations
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
        [Explicit]
        [Test]
        public void Asin_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";
            var rng = new Random(86420);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(UIntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                    .MakeGenericMethod(tagType);

                int minRaw = 0;
                int maxRaw = (1 << bits) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                double ticksToRad = (2.0 * System.Math.PI) / (1u << bits);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    // appel implé
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)mi.Invoke(null, new[] { valObj }); // ← retourne SIGNÉ

                    // pipeline de réf identique: UIntN→Q16 (rétro-faithful) → asin → Bn SIGNÉ
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536L) / maxRaw);
                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double asinRadRef = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRadRef * 65536.0);

                    int expectedBn = FixedMath.Q16_16AngleToBn(expectedQ16, bits, signed: true); // <<< signé

                    int diffTicks = System.Math.Abs(asinBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg;
                        bestRad = diffRad;
                        bestTicks = diffTicks;
                        atDeg = asinRadRef * (180.0 / System.Math.PI);
                    }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion

        #region --- ASIN LUT Retro (IntN)
        [Explicit]
        [Test]
        public void Asin_IntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";
            var rng = new Random(86420);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Asin"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                    .MakeGenericMethod(tagType);

                // domaine symétrique: on évite -2^(bits-1)
                int maxRaw = (1 << (bits - 1)) - 1;
                int minRaw = -maxRaw;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                double ticksToRad = (2.0 * System.Math.PI) / (1u << bits);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    // appel implé
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinBn = (int)mi.Invoke(null, new[] { valObj });

                    // référence: bit-faithful -> Q16 -> asin -> map angle vers Bn (signed:true)
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
                        bestDeg = diffDeg;
                        bestRad = diffRad;
                        bestTicks = diffTicks;
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
        #region --- ACOS LUT Retro (UIntN)
        [Explicit]
        [Test]
        public void Acos_UIntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";
            var rng = new Random(86420);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(UIntN<>).MakeGenericType(tagType);

                // Sélectionne la bonne surcharge générique UIntN<>
                var mi = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethodDefinition
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                    .MakeGenericMethod(tagType);

                int minRaw = 0;
                int maxRaw = (1 << bits) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                double ticksToRad = (2.0 * System.Math.PI) / (1u << bits);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    // --- appel implé ---
                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)mi.Invoke(null, new[] { valObj }); // sortie en Bn NON signé

                    // --- pipeline de référence identique ---
                    uint umax = (uint)maxRaw;
                    int valQ16 = (int)((((long)raw * 2 - umax) * 65536L) / umax); // [0..max] -> [-65536..+65536]

                    double x = System.Math.Clamp(valQ16 / 65536.0, -1.0, 1.0);
                    double acosRadRef = System.Math.Acos(x);
                    int expectedQ16 = (int)System.Math.Round(acosRadRef * 65536.0);

                    // mapper comme l'implé: ACOS → Bn non signé
                    int expectedBn = FixedMath.Q16_16AcosToBn(expectedQ16, bits, signed: false);

                    // erreur en ticks, et conversions angulaires propres
                    int diffTicks = System.Math.Abs(acosBn - expectedBn);
                    double diffRad = diffTicks * ticksToRad;
                    double diffDeg = diffRad * (180.0 / System.Math.PI);

                    if (diffDeg > bestDeg)
                    {
                        bestDeg = diffDeg;
                        bestRad = diffRad;
                        bestTicks = diffTicks;
                        atDeg = acosRadRef * (180.0 / System.Math.PI);
                    }
                }

                report += $"\nB{bits}\t{bestDeg:0.00000}\t{bestRad:0.00000}\t{bestTicks}\t{atDeg:0.###}";
            }

            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion

        #region --- ACOS LUT Retro (IntN)
        [Explicit]
        [Test]
        public void Acos_IntN_B2toB31_MaxAngleError()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxAngleErrDeg\tMaxAngleErrRad\tMaxAngleErrTicks\tAtDeg";
            var rng = new Random(86420);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var valType = typeof(IntN<>).MakeGenericType(tagType);
                var mi = typeof(FixedMath).GetMethods()
                    .First(m => m.Name == "Acos"
                             && m.IsGenericMethod
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                    .MakeGenericMethod(tagType);

                // domaine symétrique
                int maxRaw = (1 << (bits - 1)) - 1;
                int minRaw = -maxRaw;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                double ticksToRad = (2.0 * System.Math.PI) / (1u << bits);

                int bestTicks = 0;
                double bestDeg = 0, bestRad = 0, atDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    // appel implé
                    var valObj = Activator.CreateInstance(valType, raw);
                    int acosBn = (int)mi.Invoke(null, new[] { valObj });

                    // référence: bit-faithful -> Q16 -> acos -> map angle vers Bn (signed:false)
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
                        bestDeg = diffDeg;
                        bestRad = diffRad;
                        bestTicks = diffTicks;
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

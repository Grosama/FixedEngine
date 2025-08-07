using FixedEngine.LUT;
using FixedEngine.Math;
using FixedEngine.Math.Consts;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;

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
                            m.Name == "Sin"
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
                        m.Name == "Sin"
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
                            m.Name == "Sin"
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
                        m.Name == "Sin"
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
                                 .First(m => m.Name == "Cos"
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
                                 .First(m => m.Name == "Cos"
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
        #endregion

        // ----- TAN -----

        #region --- TAN Retro (UIntN) via SIN/COS ---
        [Test]
        public void Tan_UIntN_B2toB32_MaxDiffMeasure_SinCos()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(13579);

            for (int bits = 2; bits <= 32; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(UIntN<>).MakeGenericType(tagType);
                var miSin = typeof(FixedMath).GetMethods()
                                  .First(m => m.Name == "Sin"
                                           && m.IsGenericMethod
                                           && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(UIntN<>))
                                  .MakeGenericMethod(tagType);
                var miCos = typeof(FixedMath).GetMethods()
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

                int maxDiffLin = 0;
                double maxDiffLinDeg = 0;
                double maxDiffLinValue = 0;

                double maxDiffAngleEqDegLin = 0;
                double maxDiffAngleEqDegLinAtDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    uint raw = (bits >= 28)
                               ? (uint)rng.NextInt64(0, (long)maxRaw + 1)
                               : (uint)((ulong)maxRaw * (ulong)i / (ulong)samples);

                    // Ajustement de raw vers 14 bits
                    uint adjustedRaw = bits <= 14
                        ? (raw << (14 - bits)) & 0x3FFF
                        : (raw >> (bits - 14)) & 0x3FFF;

                    var angleObj = Activator.CreateInstance(angleType, raw);
                    int sinQ16 = (int)miSin.Invoke(null, new[] { angleObj });
                    int cosQ16 = (int)miCos.Invoke(null, new[] { angleObj });

                    // TAN via SIN/COS en fixed
                    int tanVal;
                    if (cosQ16 == 0)
                        tanVal = (sinQ16 > 0) ? int.MaxValue : int.MinValue;
                    else
                        tanVal = (int)(((long)sinQ16 << 16) / cosQ16);

                    // Angle réel ajusté vers 14 bits
                    double angleRatio = adjustedRaw / 16384.0;
                    double rad = angleRatio * (System.Math.PI * 2.0);
                    double cosRad = System.Math.Cos(rad);
                    double sinRad = System.Math.Sin(rad);
                    double tanRad = System.Math.Tan(rad);

                    // Version "attendue" : tan(x) via System.Math.Tan, comparée à tanVal fixed
                    long expected;
                    if (System.Math.Abs(cosRad) < 1e-12 || double.IsInfinity(tanRad))
                        expected = (rad % (2 * System.Math.PI) > System.Math.PI / 2 && rad % (2 * System.Math.PI) < 3 * System.Math.PI / 2)
                                   ? int.MinValue : int.MaxValue;
                    else
                    {
                        double scaled = tanRad * 65536.0;
                        if (System.Math.Abs(scaled) > int.MaxValue)
                            expected = (scaled > 0) ? int.MaxValue : int.MinValue;
                        else
                            expected = (long)System.Math.Round(scaled);
                    }

                    int diff = (int)System.Math.Abs(tanVal - expected);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = rad * 180.0 / System.Math.PI;

                        double sec2 = 1.0 + tanRad * tanRad;
                        maxDiffAngleEqDeg = (sec2 != 0)
                            ? maxDiffValue / sec2 * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
                    }

                    // ERREUR LINÉAIRE (hors saturation)
                    bool inLinearZone = System.Math.Abs(tanRad) < 10000 && System.Math.Abs(expected) < int.MaxValue;
                    if (inLinearZone)
                    {
                        if (diff > maxDiffLin)
                        {
                            maxDiffLin = diff;
                            maxDiffLinValue = diff / 65536.0;
                            maxDiffLinDeg = rad * 180.0 / System.Math.PI;
                        }
                        // Max erreur angulaire linéaire
                        double sec2 = 1.0 + tanRad * tanRad;
                        double angleEqDeg = (sec2 != 0)
                            ? (diff / 65536.0) / sec2 * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
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
            }
            Console.WriteLine(report);
            Assert.Pass(report);
        }

        #endregion


        #region --- TAN Retro (IntN) via SIN/COS ---

        [Test]
        public void Tan_IntN_B2toB31_MaxDiffMeasure_SinCos()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg"
                          + "\t| LinearZone | MaxDiffLin\tMaxDiffLinDeg\tMaxDiffLinValue"
                          + "\t| MaxDiffAngleEqDeg_Lin\tAtDeg";

            var rng = new Random(97531);

            for (int bits = 2; bits <= 31; bits++)
            {
                var tagType = asm.GetType($"FixedEngine.Math.B{bits}");
                if (tagType == null) { System.Console.WriteLine($"Type B{bits} absent : SKIP"); continue; }

                var angleType = typeof(IntN<>).MakeGenericType(tagType);
                var miSin = typeof(FixedMath).GetMethods()
                              .First(m => m.Name == "Sin"
                                       && m.IsGenericMethod
                                       && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
                              .MakeGenericMethod(tagType);
                var miCos = typeof(FixedMath).GetMethods()
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

                int maxDiffLin = 0;
                double maxDiffLinDeg = 0;
                double maxDiffLinValue = 0;

                double maxDiffAngleEqDegLin = 0;
                double maxDiffAngleEqDegLinAtDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    int inputRaw = bits <= 14
                        ? ((int)((uint)raw << (14 - bits)) & 0x3FFF)
                        : ((int)((uint)raw >> (bits - 14)) & 0x3FFF);

                    var angleObj = Activator.CreateInstance(angleType, inputRaw);
                    int sinQ16 = (int)miSin.Invoke(null, new[] { angleObj });
                    int cosQ16 = (int)miCos.Invoke(null, new[] { angleObj });

                    // TAN via SIN/COS en fixed
                    int tanVal;
                    if (cosQ16 == 0)
                        tanVal = (sinQ16 > 0) ? int.MaxValue : int.MinValue;
                    else
                        tanVal = (int)(((long)sinQ16 << 16) / cosQ16);

                    double angleRatio = inputRaw / 16384.0;
                    double rad = angleRatio * (System.Math.PI * 2.0);
                    double cosRad = System.Math.Cos(rad);
                    double tanRad = System.Math.Tan(rad);

                    long expected;
                    if (System.Math.Abs(cosRad) < 1e-12 || double.IsInfinity(tanRad))
                        expected = (rad > 0) ? int.MaxValue : int.MinValue;
                    else
                    {
                        double scaled = tanRad * 65536.0;
                        expected = (System.Math.Abs(scaled) > int.MaxValue)
                                   ? (scaled > 0 ? int.MaxValue : int.MinValue)
                                   : (long)System.Math.Round(scaled);
                    }

                    int diff = (int)System.Math.Abs(tanVal - expected);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = rad * 180.0 / System.Math.PI;

                        double sec2 = 1.0 + tanRad * tanRad;
                        maxDiffAngleEqDeg = (sec2 != 0)
                            ? maxDiffValue / sec2 * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
                    }

                    // ERREUR LINÉAIRE (hors saturation)
                    bool inLinearZone = System.Math.Abs(tanRad) < 10000 && System.Math.Abs(expected) < int.MaxValue;
                    if (inLinearZone)
                    {
                        if (diff > maxDiffLin)
                        {
                            maxDiffLin = diff;
                            maxDiffLinValue = diff / 65536.0;
                            maxDiffLinDeg = rad * 180.0 / System.Math.PI;
                        }
                        // Max erreur angulaire linéaire
                        double sec2 = 1.0 + tanRad * tanRad;
                        double angleEqDeg = (sec2 != 0)
                            ? (diff / 65536.0) / sec2 * 180.0 / System.Math.PI
                            : double.PositiveInfinity;
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
            }
            System.Console.WriteLine(report);
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
        public void Asin_UIntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
            var rng = new Random(24680);

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

                uint maxRaw = (1u << bits) - 1;
                int samples = (bits >= 28) ? 1_000_000 : System.Math.Min((int)maxRaw + 1, 1_000_000);

                int maxDiff = 0;
                double maxDiffDeg = 0, maxDiffValue = 0, maxDiffAngleEqDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    uint raw = (bits >= 28)
                               ? (uint)rng.NextInt64(0, (long)maxRaw + 1)
                               : (uint)((ulong)maxRaw * (ulong)i / (ulong)samples);

                    // ----- appel fixed -----
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinFixed = (int)mi.Invoke(null, new[] { valObj });

                    // ----- référence float (mapping rétro-faithful) -----
                    int valQ16 = (int)((((long)raw * 2 - maxRaw) * 65536) / maxRaw);
                    double x = valQ16 / 65536.0;             // [-1, +1]

                    double asinRad = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRad * 65536.0);

                    int diff = System.Math.Abs(asinFixed - expectedQ16);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = asinRad * 180.0 / System.Math.PI;

                        double cosTheta = System.Math.Cos(asinRad);        // dérivée = 1 / √(1-x²)
                        maxDiffAngleEqDeg = maxDiffValue * cosTheta * 180.0 / System.Math.PI;
                    }
                }
                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
            }
            Console.WriteLine(report);
            Assert.Pass(report);
        }
        #endregion


        // ============================================
        // --- ASIN LUT Retro (IntN)  *** patché *** ---
        // ============================================
        #region --- ASIN LUT Retro (IntN)

        [Test]
        public void Asin_IntN_B2toB31_MaxDiffMeasure()
        {
            var asm = typeof(FixedEngine.Math.B2).Assembly;
            string report = "\nBn\tMaxDiff\tMaxDiffDeg\tMaxDiffValue\tMaxDiffAngleEqDeg";
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

                int minRaw = -(1 << (bits - 1));
                int maxRaw = (1 << (bits - 1)) - 1;
                ulong domain = (ulong)maxRaw - (ulong)minRaw + 1;
                int samples = (bits >= 28) ? 1_000_000 : (int)domain;

                int maxDiff = 0;
                double maxDiffDeg = 0, maxDiffValue = 0, maxDiffAngleEqDeg = 0;

                for (int i = 0; i < samples; i++)
                {
                    int raw = (bits >= 28)
                              ? (int)(minRaw + rng.NextInt64((long)domain))
                              : minRaw + i;

                    // ----- appel fixed -----
                    var valObj = Activator.CreateInstance(valType, raw);
                    int asinFixed = (int)mi.Invoke(null, new[] { valObj });

                    // ----- mapping identique au wrapper Asin<IntN> -----
                    int valQ16 = bits == 17 ? raw
                                : bits > 17 ? raw >> (bits - 17)
                                            : raw << (17 - bits);

                    double x = valQ16 / 65536.0;            // [-1, +1]
                    double asinRad = System.Math.Asin(x);
                    int expectedQ16 = (int)System.Math.Round(asinRad * 65536.0);

                    int diff = System.Math.Abs(asinFixed - expectedQ16);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffValue = diff / 65536.0;
                        maxDiffDeg = asinRad * 180.0 / System.Math.PI;

                        double cosTheta = System.Math.Cos(asinRad);
                        maxDiffAngleEqDeg = maxDiffValue * cosTheta * 180.0 / System.Math.PI;
                    }
                }
                report += $"\nB{bits}\t{maxDiff}\t{maxDiffDeg:0.###}\t{maxDiffValue:0.00000}\t{maxDiffAngleEqDeg:0.00000}";
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

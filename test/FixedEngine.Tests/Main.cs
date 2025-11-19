using FixedEngine.Core;
using FixedEngine.Math;
using NUnit.Framework;
using System.Reflection;
using s0f16 = FixedEngine.Core.Fixed<FixedEngine.Core.B16, FixedEngine.Core.B16>;


public class CurveGenerator
{
    
    public static void GenerateAndSaveSinCurve(int samples, int rangeQ16, string filename, bool includeReference = true)
    {
        List<(float x, float y, float? yRef)> points = new List<(float x, float y, float? yRef)>();
        int step = rangeQ16 / samples;

        // Générer les points
        for (int i = 0; i <= samples; i++)
        {
            int xQ16 = i * step; // Angle en Q16.16
            IntN<B16> angle = new IntN<B16>(xQ16); // Construit l’angle signé
            float yQ16 = (float)s0f16.Sin(angle); // Appelle Sin<IntN<B16>>
            float x = xQ16 * (2.0f * MathF.PI / 65534.0f);
            float y = yQ16;
            float? yRef;
            if (includeReference)
            {
                yRef = (float)Math.Sin(x);
            }
            else
            {
                yRef = null;
            }

            points.Add((x, y, yRef));
        }

        // Écrire dans un fichier CSV
        using (var writer = new StreamWriter(filename))
        {
            writer.WriteLine(includeReference ? "x;y;y_ref" : "x;y"); // En-tête
            foreach (var (x, y, yRef) in points)
            {
                if (includeReference)
                    writer.WriteLine($"{x:F6};{y:F6};{yRef:F6}");
                else
                    writer.WriteLine($"{x:F6};{y:F6}");
            }
        }

        Console.WriteLine($"Fichier CSV généré : {filename}");
    }


    private void GenerateCurveFor(Type bitsType)
    {
        string root = @"W:\Projects\Csharp\FixedEngine\test\FixedEngine.Tests\bin\TestOutput";
        Directory.CreateDirectory(root);

        string filename = Path.Combine(root, $"sin_{bitsType.Name}.csv");

        var intNType = typeof(IntN<>).MakeGenericType(bitsType);

        int maxValue = (int)intNType.GetField("MaxValue",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);
        int minValue = (int)intNType.GetField("MinValue",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);

        int bitsConst = (int)intNType.GetField("BitsConst",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);

        const int MAX_SAMPLES = 65536;

        // ⚠️ ici : fullCount en long pour éviter l’overflow
        long fullCount = (long)maxValue - (long)minValue + 1L;
        int samples = (fullCount <= MAX_SAMPLES) ? (int)fullCount : MAX_SAMPLES;

        var sinMethodGeneric = typeof(FixedMath)
            .GetMethods()
            .Where(m =>
                m.Name == "Sin" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.IsGenericType &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IntN<>))
            .First();

        var sinMethod = sinMethodGeneric.MakeGenericMethod(bitsType);
        var ctor = intNType.GetConstructor(new[] { typeof(int) });

        using var writer = new StreamWriter(filename);
        writer.WriteLine("raw_angle;x_angle;y_raw;y_norm;y_ref;err");

        // stepRaw en double basé sur fullCount (long)
        double stepRaw = (double)(fullCount - 1) / (samples - 1);

        for (int i = 0; i < samples; i++)
        {
            long rawL = (long)minValue + (long)Math.Round(i * stepRaw);
            int raw = (int)rawL;

            object angleObj = ctor.Invoke(new object[] { raw });
            int yRaw = (int)sinMethod.Invoke(null, new[] { angleObj });

            // ⚠ ici on touche à ton angle
            // on garde TA version actuelle, qui marche pour tous les Bn
            float angleNorm = raw / (float)maxValue;
            float angleRad = angleNorm * MathF.PI;

            float yNorm = yRaw / (float)maxValue;
            float yRef = MathF.Sin(angleRad);
            float err = MathF.Abs(yNorm - yRef);

            writer.WriteLine($"{raw};{angleRad:F6};{yRaw};{yNorm:F6};{yRef:F6};{err:F6}");
        }

        Console.WriteLine($"✓ Fichier généré : {filename} ({samples} points, B{bitsConst})");
    }




    [Test, Explicit]
    public void GenerateCurves_ForAll_Bn()
    {
        var asm = typeof(B2).Assembly;

        // On récupère B2..B31 uniquement
        var allBits = asm.GetTypes()
            .Where(t =>
                t.IsValueType &&
                t.Name.Length >= 2 &&
                t.Name[0] == 'B' &&
                int.TryParse(t.Name.Substring(1), out var bits) &&
                bits >= 2 && bits <= 31)
            .OrderBy(t => int.Parse(t.Name.Substring(1)))
            .ToList();

        foreach (var bitsType in allBits)
        {
            Console.WriteLine($"--- Génération pour {bitsType.Name} ---");
            GenerateCurveFor(bitsType);
        }
    }

}


public class CurveGeneratorTests
{
    [Test, Explicit]
    public void GenerateSinCsv()
    {
        CurveGenerator.GenerateAndSaveSinCurve(
            samples: 100,
            rangeQ16: 65534,
            filename: "sine_curve.csv",
            includeReference: true
        );
    }


}


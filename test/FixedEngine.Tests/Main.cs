using FixedEngine.Core;
using FixedEngine.Math;
using NUnit.Framework;
using System.Reflection;
using s0f16 = FixedEngine.Core.Fixed<FixedEngine.Core.B16, FixedEngine.Core.B16>;


public class CurveGenerator
{
    
    private void GenerateCurveFor(Type bitsType)
    {
        static string FindProjectRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Length > 0)
                    return dir.FullName;

                dir = dir.Parent!;
            }

            throw new Exception("Impossible de trouver le projet.");
        }

        string projectRoot = FindProjectRoot();
        string root = Path.Combine(projectRoot, "TestOutput");

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



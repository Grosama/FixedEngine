using System;

namespace FixedEngine.Core
{
    // Tags pour toutes les tailles de bits SIGNÉS de 0 à 32
    public struct B0 { }
    public struct B1 { }
    public struct B2 { }
    public struct B3 { }
    public struct B4 { }
    public struct B5 { }
    public struct B6 { }
    public struct B7 { }
    public struct B8 { }
    public struct B9 { }
    public struct B10 { }
    public struct B11 { }
    public struct B12 { }
    public struct B13 { }
    public struct B14 { }
    public struct B15 { }
    public struct B16 { }
    public struct B17 { }
    public struct B18 { }
    public struct B19 { }
    public struct B20 { }
    public struct B21 { }
    public struct B22 { }
    public struct B23 { }
    public struct B24 { }
    public struct B25 { }
    public struct B26 { }
    public struct B27 { }
    public struct B28 { }
    public struct B29 { }
    public struct B30 { }
    public struct B31 { }
    public struct B32 { }

    /// <summary>
    /// Helper statique pour extraire la taille (en bits) du tag générique T.
    /// Compatible avec tous les tags signés (B0-B32) et non signés (uB0-uB32).
    /// </summary>
    public static class BitsOf<T>
        where T : struct
    {
        public static readonly int Value;

        static BitsOf()
        {
            // Signés
            if (typeof(T) == typeof(B0)) Value = 0;
            else if (typeof(T) == typeof(B1)) Value = 1;
            else if (typeof(T) == typeof(B2)) Value = 2;
            else if (typeof(T) == typeof(B3)) Value = 3;
            else if (typeof(T) == typeof(B4)) Value = 4;
            else if (typeof(T) == typeof(B5)) Value = 5;
            else if (typeof(T) == typeof(B6)) Value = 6;
            else if (typeof(T) == typeof(B7)) Value = 7;
            else if (typeof(T) == typeof(B8)) Value = 8;
            else if (typeof(T) == typeof(B9)) Value = 9;
            else if (typeof(T) == typeof(B10)) Value = 10;
            else if (typeof(T) == typeof(B11)) Value = 11;
            else if (typeof(T) == typeof(B12)) Value = 12;
            else if (typeof(T) == typeof(B13)) Value = 13;
            else if (typeof(T) == typeof(B14)) Value = 14;
            else if (typeof(T) == typeof(B15)) Value = 15;
            else if (typeof(T) == typeof(B16)) Value = 16;
            else if (typeof(T) == typeof(B17)) Value = 17;
            else if (typeof(T) == typeof(B18)) Value = 18;
            else if (typeof(T) == typeof(B19)) Value = 19;
            else if (typeof(T) == typeof(B20)) Value = 20;
            else if (typeof(T) == typeof(B21)) Value = 21;
            else if (typeof(T) == typeof(B22)) Value = 22;
            else if (typeof(T) == typeof(B23)) Value = 23;
            else if (typeof(T) == typeof(B24)) Value = 24;
            else if (typeof(T) == typeof(B25)) Value = 25;
            else if (typeof(T) == typeof(B26)) Value = 26;
            else if (typeof(T) == typeof(B27)) Value = 27;
            else if (typeof(T) == typeof(B28)) Value = 28;
            else if (typeof(T) == typeof(B29)) Value = 29;
            else if (typeof(T) == typeof(B30)) Value = 30;
            else if (typeof(T) == typeof(B31)) Value = 31;
            else if (typeof(T) == typeof(B32)) Value = 32;

            else throw new System.NotSupportedException($"BitsOf<{typeof(T).Name}> n'est pas supporté.");
        }
    }

    public static class BitsToType
    {
        public static Type FromValue(int bits)
        {
            switch (bits)
            {
                case 0: return typeof(B0);
                case 1: return typeof(B1);
                case 2: return typeof(B2);
                case 3: return typeof(B3);
                case 4: return typeof(B4);
                case 5: return typeof(B5);
                case 6: return typeof(B6);
                case 7: return typeof(B7);
                case 8: return typeof(B8);
                case 9: return typeof(B9);
                case 10: return typeof(B10);
                case 11: return typeof(B11);
                case 12: return typeof(B12);
                case 13: return typeof(B13);
                case 14: return typeof(B14);
                case 15: return typeof(B15);
                case 16: return typeof(B16);
                case 17: return typeof(B17);
                case 18: return typeof(B18);
                case 19: return typeof(B19);
                case 20: return typeof(B20);
                case 21: return typeof(B21);
                case 22: return typeof(B22);
                case 23: return typeof(B23);
                case 24: return typeof(B24);
                case 25: return typeof(B25);
                case 26: return typeof(B26);
                case 27: return typeof(B27);
                case 28: return typeof(B28);
                case 29: return typeof(B29);
                case 30: return typeof(B30);
                case 31: return typeof(B31);
                case 32: return typeof(B32);

                default:
                    throw new NotSupportedException(
                        $"BitsToType.FromValue({bits}) n'est pas supporté (seulement 0 à 32).");
            }
        }
    }
}

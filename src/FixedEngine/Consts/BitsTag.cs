namespace FixedEngine.Math
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

    // Tags pour toutes les tailles de bits NON SIGNÉS de 0 à 32
    public struct uB0 { }
    public struct uB1 { }
    public struct uB2 { }
    public struct uB3 { }
    public struct uB4 { }
    public struct uB5 { }
    public struct uB6 { }
    public struct uB7 { }
    public struct uB8 { }
    public struct uB9 { }
    public struct uB10 { }
    public struct uB11 { }
    public struct uB12 { }
    public struct uB13 { }
    public struct uB14 { }
    public struct uB15 { }
    public struct uB16 { }
    public struct uB17 { }
    public struct uB18 { }
    public struct uB19 { }
    public struct uB20 { }
    public struct uB21 { }
    public struct uB22 { }
    public struct uB23 { }
    public struct uB24 { }
    public struct uB25 { }
    public struct uB26 { }
    public struct uB27 { }
    public struct uB28 { }
    public struct uB29 { }
    public struct uB30 { }
    public struct uB31 { }
    public struct uB32 { }

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

            // Non signés
            else if (typeof(T) == typeof(uB0)) Value = 0;
            else if (typeof(T) == typeof(uB1)) Value = 1;
            else if (typeof(T) == typeof(uB2)) Value = 2;
            else if (typeof(T) == typeof(uB3)) Value = 3;
            else if (typeof(T) == typeof(uB4)) Value = 4;
            else if (typeof(T) == typeof(uB5)) Value = 5;
            else if (typeof(T) == typeof(uB6)) Value = 6;
            else if (typeof(T) == typeof(uB7)) Value = 7;
            else if (typeof(T) == typeof(uB8)) Value = 8;
            else if (typeof(T) == typeof(uB9)) Value = 9;
            else if (typeof(T) == typeof(uB10)) Value = 10;
            else if (typeof(T) == typeof(uB11)) Value = 11;
            else if (typeof(T) == typeof(uB12)) Value = 12;
            else if (typeof(T) == typeof(uB13)) Value = 13;
            else if (typeof(T) == typeof(uB14)) Value = 14;
            else if (typeof(T) == typeof(uB15)) Value = 15;
            else if (typeof(T) == typeof(uB16)) Value = 16;
            else if (typeof(T) == typeof(uB17)) Value = 17;
            else if (typeof(T) == typeof(uB18)) Value = 18;
            else if (typeof(T) == typeof(uB19)) Value = 19;
            else if (typeof(T) == typeof(uB20)) Value = 20;
            else if (typeof(T) == typeof(uB21)) Value = 21;
            else if (typeof(T) == typeof(uB22)) Value = 22;
            else if (typeof(T) == typeof(uB23)) Value = 23;
            else if (typeof(T) == typeof(uB24)) Value = 24;
            else if (typeof(T) == typeof(uB25)) Value = 25;
            else if (typeof(T) == typeof(uB26)) Value = 26;
            else if (typeof(T) == typeof(uB27)) Value = 27;
            else if (typeof(T) == typeof(uB28)) Value = 28;
            else if (typeof(T) == typeof(uB29)) Value = 29;
            else if (typeof(T) == typeof(uB30)) Value = 30;
            else if (typeof(T) == typeof(uB31)) Value = 31;
            else if (typeof(T) == typeof(uB32)) Value = 32;

            else throw new System.NotSupportedException($"BitsOf<{typeof(T).Name}> n'est pas supporté.");
        }
    }
}

// LogConsts.cs
// Tables de constantes logarithmiques en Q-format pour FixedEngine
// Générées pour Q0.1 à Q0.32, pour une portabilité et un déterminisme parfait.

namespace FixedEngine.Math.Consts
{
    /// <summary>
    /// Constantes logarithmiques prêtes à l’emploi pour tous les Q-formats (fracBits 0 à 32).
    /// ln(2) et log2(e) : accès par LogConsts.LN2_Q[fracBits] ou LogConsts.LOG2E_Q[fracBits].
    /// </summary>
    public static class LogConsts
    {
        // ln(2) * (1 << fracBits), fracBits de 0 à 32 inclus
        public static readonly int[] LN2_Q = new int[33] {
            1,
            1,
            3,
            6,
            11,
            22,
            44,
            89,
            177,
            355,
            710,
            1420,
            2839,
            5678,
            11357,
            22713,
            45426,
            90852,
            181704,
            363409,
            726817,
            1453635,
            2907270,
            5814540,
            11629080,
            23258160,
            46516320,
            93032640,
            186065279,
            372130559,
            744261118,
            1488522236,
            -1317922824,
        };

        // log2(e) * (1 << fracBits), fracBits de 0 à 32 inclus
        public static readonly int[] LOG2E_Q = new int[33] {
            1,
            3,
            6,
            12,
            23,
            46,
            92,
            185,
            369,
            739,
            1477,
            2955,
            5909,
            11819,
            23637,
            47274,
            94548,
            189097,
            378194,
            756388,
            1512775,
            3025551,
            6051102,
            12102203,
            24204406,
            48408813,
            96817625,
            193635251,
            387270501,
            774541002,
            1549082005,
            -1196803287,
            1901360723
        };

        // ln(10) * (1 << fracBits), fracBits de 0 à 32 inclus
        public static readonly int[] LN10_Q = new int[33] {
            2,
            5,
            9,
            18,
            37,
            74,
            147,
            295,
            589,
            1179,
            2358,
            4716,
            9431,
            18863,
            37726,
            75451,
            150902,
            301804,
            603609,
            1207218,
            2414435,
            4828871,
            9657742,
            19315484,
            38630967,
            77261935,
            154523870,
            309047740,
            618095479,
            1236190959,
            -1822585378,
            649796539,
            1299593079
        };

        // log2(10) * (1 << fracBits), fracBits de 0 à 32 inclus
        public static readonly int[] LOG2_10_Q = new int[33] {
            3,
            7,
            13,
            27,
            53,
            106,
            213,
            425,
            850,
            1701,
            3402,
            6803,
            13607,
            27213,
            54426,
            108853,
            217706,
            435412,
            870824,
            1741647,
            3483294,
            6966588,
            13933176,
            27866353,
            55732705,
            111465410,
            222930821,
            445861641,
            891723283,
            1783446566,
            -728074164,
            -1456148328,
            1382670639
        };
    }
}

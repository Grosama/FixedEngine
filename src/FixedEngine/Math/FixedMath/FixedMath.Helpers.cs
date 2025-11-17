using FixedEngine.LUT;
using FixedEngine.Math.Consts;

namespace FixedEngine.Math
{
    public static partial class FixedMath
    {
        // ==========================
        // --- HELPERS ---
        // ==========================
        #region --- HELPERS ---
        public static int ConstQ<TFrac>(float value)
        where TFrac : struct
        {
            int fracBits = BitsOf<TFrac>.Value;
            return (int)(value * (1 << fracBits));
        }
        #endregion

        // ==========================
        // --- HELPERS CONSTANTES TRIGONOMÉTRIQUES ---
        // ==========================
        #region --- HELPERS CONSTANTES TRIG (exhaustif, tous Q-format) ---

        /// <summary>π au format Q selon TFrac.</summary>
        public static int PiQ<TFrac>() where TFrac : struct
            => TrigConsts.PI_Q[BitsOf<TFrac>.Value];

        /// <summary>2π au format Q selon TFrac.</summary>
        public static int Pi2Q<TFrac>() where TFrac : struct
            => TrigConsts.PI2_Q[BitsOf<TFrac>.Value];

        /// <summary>π/2 au format Q selon TFrac.</summary>
        public static int Pi_2Q<TFrac>() where TFrac : struct
            => TrigConsts.PI_2_Q[BitsOf<TFrac>.Value];

        /// <summary>1/π au format Q selon TFrac.</summary>
        public static int InvPiQ<TFrac>() where TFrac : struct
            => TrigConsts.INV_PI_Q[BitsOf<TFrac>.Value];

        /// <summary>1/(2π) au format Q selon TFrac.</summary>
        public static int InvPi2Q<TFrac>() where TFrac : struct
            => TrigConsts.INV_PI2_Q[BitsOf<TFrac>.Value];

        /// <summary>π/180 (degrés vers radians) au format Q selon TFrac.</summary>
        public static int DegToRadQ<TFrac>() where TFrac : struct
            => TrigConsts.DEG_TO_RAD_Q[BitsOf<TFrac>.Value];

        /// <summary>180/π (radians vers degrés) au format Q selon TFrac.</summary>
        public static int RadToDegQ<TFrac>() where TFrac : struct
            => TrigConsts.RAD_TO_DEG_Q[BitsOf<TFrac>.Value];

        #endregion

        // ==========================
        // --- HELPERS CONSTANTES LOGARITHMIQUES ---
        // ==========================
        #region --- HELPERS CONSTANTES LOG (exhaustif, tous Q-format) ---

        /// <summary>ln(2) au format Q selon TFrac.</summary>
        public static int Ln2Q<TFrac>() where TFrac : struct
            => LogConsts.LN2_Q[BitsOf<TFrac>.Value];

        /// <summary>log2(e) au format Q selon TFrac.</summary>
        public static int Log2eQ<TFrac>() where TFrac : struct
            => LogConsts.LOG2E_Q[BitsOf<TFrac>.Value];

        #endregion

        // ==========================
        // --- HELPERS CONSTANTES RACINE ---
        // ==========================
        #region --- HELPERS CONSTANTES RACINE (exhaustif, tous Q-format) ---

        /// <summary>√2 (racine carrée de 2) au format Q selon TFrac.</summary>
        public static int Sqrt2Q<TFrac>() where TFrac : struct
            => SqrtConsts.SQRT2_Q[BitsOf<TFrac>.Value];

        /// <summary>√3 (racine carrée de 3) au format Q selon TFrac.</summary>
        public static int Sqrt3Q<TFrac>() where TFrac : struct
            => SqrtConsts.SQRT3_Q[BitsOf<TFrac>.Value];

        /// <summary>√5 (racine carrée de 5) au format Q selon TFrac.</summary>
        /* public static int Sqrt5Q<TFrac>() where TFrac : struct
            => SqrtConsts.SQRT5_Q[BitsOf<TFrac>.Value];*/

        //<summary>To-Do</summary>
        /*public static Fixed<TInt, TFrac> InvSqrt<TInt, TFrac>(Fixed<TInt, TFrac> x)
        where TInt : struct
        where TFrac : struct
        {
            // Implémentation Newton-Raphson *ou* LUT dynamique, mais pure bitwise, no float.
            // À documenter : usage pour normalisation/usage dynamique.
        }*/

        /// <summary>1/√2 (racine carrée inverse de 2) au format Q selon TFrac.</summary>
        public static int InvSqrt2Q<TFrac>() where TFrac : struct
            => SqrtConsts.INV_SQRT2_Q[BitsOf<TFrac>.Value];

        /// <summary>1/√3 (racine carrée inverse de 3) au format Q selon TFrac.</summary>
        public static int InvSqrt3Q<TFrac>() where TFrac : struct
            => SqrtConsts.INV_SQRT3_Q[BitsOf<TFrac>.Value];

        /// <summary>√π (racine carrée de Pi) au format Q selon TFrac.</summary>
        /* public static int SqrtPiQ<TFrac>() where TFrac : struct
            => SqrtConsts.SQRT_PI_Q[BitsOf<TFrac>.Value];*/

        #endregion

        // ==========================
        // --- HELPERS CONSTANTES PHI (NOMBRE D’OR) ---
        // ==========================
        #region --- HELPERS CONSTANTES PHI (exhaustif, tous Q-format) ---

        /// <summary>ln(φ) (nombre d’or) au format Q selon TFrac.</summary>
        public static int LnPhiQ<TFrac>() where TFrac : struct
            => PhiConsts.LN_PHI_Q[BitsOf<TFrac>.Value];

        /// <summary>log2(φ) (nombre d’or) au format Q selon TFrac.</summary>
        public static int Log2PhiQ<TFrac>() where TFrac : struct
            => PhiConsts.LOG2_PHI_Q[BitsOf<TFrac>.Value];

        /// <summary>√φ (racine carrée du nombre d’or) au format Q selon TFrac.</summary>
        public static int SqrtPhiQ<TFrac>() where TFrac : struct
            => PhiConsts.SQRT_PHI_Q[BitsOf<TFrac>.Value];

        #endregion

        // ==========================
        // --- HELPERS CONSTANTES MASK ---
        // ==========================
        #region --- HELPERS CONSTANTES MASK (exhaustif, tous formats) ---

        /// <summary>Mask binaire pour N bits.</summary>
        public static uint MaskQ<TBits>() where TBits : struct
            => Mask.MASKS[BitsOf<TBits>.Value];

        /// <summary>Bit de signe pour N bits.</summary>
        public static uint SignBitQ<TBits>() where TBits : struct
            => Mask.SIGN_BITS[BitsOf<TBits>.Value];

        /// <summary>Valeur min signée pour N bits.</summary>
        public static int SignedMinQ<TBits>() where TBits : struct
            => Mask.SIGNED_MIN[BitsOf<TBits>.Value];

        /// <summary>Valeur max signée pour N bits.</summary>
        public static int SignedMaxQ<TBits>() where TBits : struct
            => Mask.SIGNED_MAX[BitsOf<TBits>.Value];

        /// <summary>Valeur max unsigned pour N bits.</summary>
        public static uint UnsignedMaxQ<TBits>() where TBits : struct
            => Mask.UNSIGNED_MAX[BitsOf<TBits>.Value];

        #endregion

        // ==========================
        // --- HELPERS LUT ---
        // ==========================
        #region --- HELPERS LUT ---
        public static int SinLutQ16(int idx) => SinLUT4096.LUT[idx & 1023];
        public static int AtanLutQ16(int idx) => AtanLUT1024.LUT[idx & 1023];
        #endregion
    } 
}

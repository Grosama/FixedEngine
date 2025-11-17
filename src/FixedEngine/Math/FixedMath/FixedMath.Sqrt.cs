using FixedEngine.Core;
using System.Runtime.CompilerServices;

namespace FixedEngine.Math
{
    public static partial class FixedMath
    {
        // ==========================
        // --- RACINE CARRÉE ---
        // ==========================
        #region --- RACINE CARRÉE ---

        // Méthode interne, standard rétro "shift & subtract"
        private static uint IntegerSqrt(uint a, int bits)
        {
            // bits = nombre de bits significatifs (ex: 8, 16, 24, 32)
            // On veut démarrer au bit pair le plus élevé < bits*2
            int start = ((bits - 1) / 2) * 2;
            uint bit = 1u << start;
            uint res = 0;
            while (bit > a) bit >>= 2;
            while (bit != 0)
            {
                if (a >= res + bit)
                {
                    a -= res + bit;
                    res = (res >> 1) + bit;
                }
                else
                {
                    res >>= 1;
                }
                bit >>= 2;
            }
            return res;
        }

        /// <summary>
        /// Racine carrée entière (bit-faithful) pour UIntN<TBits>.
        /// Le résultat est toujours dans la plage [0, 2^N-1], aucune utilisation de float.
        /// </summary>
        #region --- RACINE CARREE (UIntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntN<TBits> Sqrt<TBits>(UIntN<TBits> value)
            where TBits : struct
        {
            int bits = UIntN<TBits>.BitsConst; // <--- Lecture dynamique du format
            return new UIntN<TBits>(IntegerSqrt(value.Raw, bits));
        }
        #endregion

        /// <summary>
        /// Racine carrée entière (bit-faithful) pour IntN<TBits> : sqrt(abs(x))
        /// Le résultat est toujours >= 0, jamais négatif.
        /// </summary>
        #region --- RACINE CARREE (IntN) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntN<TBits> Sqrt<TBits>(IntN<TBits> value)
            where TBits : struct
        {
            int bits = IntN<TBits>.BitsConst; // Récupère la taille du format
            int v = value.Raw;
            uint abs = (uint)(v < 0 ? -v : v); // Authentique rétro : artefact possible sur MinValue
            uint r = IntegerSqrt(abs, bits);   // Passe bits dynamiquement !
            return new IntN<TBits>((int)r);
        }
        #endregion

        /// <summary>
        /// Racine carrée “bit-faithful” pour UFixed<TUInt, TFrac> (Q-format rétro).
        /// Algo rétro : sqrt(x) = IntegerSqrt(x << n). Résultat dans le même format Qm.n.
        /// </summary>
        #region --- RACINE CARREE (UFixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UFixed<TUInt, TFrac> Sqrt<TUInt, TFrac>(UFixed<TUInt, TFrac> value)
            where TUInt : struct
            where TFrac : struct
        {
            uint raw = value.Raw;
            int fracBits = BitsOf<TFrac>.Value;
            int bits = BitsOf<TUInt>.Value; // nombre de bits du backing integer (UInt8, UInt16, UInt32...)

            // On fait "x << n" (n = bits de fraction)
            uint shifted = raw << fracBits;

            uint sqrtRaw = IntegerSqrt(shifted, bits + fracBits); // 🟢 bits totaux utilisés !

            // Wrap sur la taille du type, compatible rétro (cf. constructeur UFixed)
            return new UFixed<TUInt, TFrac>(sqrtRaw);
        }
        #endregion

        /// <summary>
        /// Racine carrée bit-faithful pour Fixed<TInt, TFrac> (signed, Q-format).
        /// Convention rétro : sqrt(x) = sqrt(abs(x)), pas de NaN, jamais négatif.
        /// </summary>
        #region --- RACINE CARREE (Fixed) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed<TInt, TFrac> Sqrt<TInt, TFrac>(Fixed<TInt, TFrac> value)
            where TInt : struct
            where TFrac : struct
        {
            int raw = value.Raw;
            int fracBits = BitsOf<TFrac>.Value;
            int bits = BitsOf<TInt>.Value; // Nombre de bits du backing integer

            // Valeur absolue (authentique rétro)
            uint absRaw = (uint)(raw < 0 ? -raw : raw);

            uint shifted = absRaw << fracBits; // Qm.n : x << n
            uint sqrtRaw = IntegerSqrt(shifted, bits + fracBits);

            // Wrap signé rétro : jamais négatif, jamais NaN.
            return new Fixed<TInt, TFrac>((int)sqrtRaw);
        }
        #endregion

        #endregion
    } 
}
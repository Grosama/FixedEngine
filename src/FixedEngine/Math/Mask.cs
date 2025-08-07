// Mask.cs : Masques binaires pré-calculés pour FixedEngine
namespace FixedEngine.Math
{
    public static class Mask
    {
        // Tableaux readonly pour performance et compatibilité AOT
        public static readonly uint[] MASKS = new uint[33]
        {
            0x00000000, 0x00000001, 0x00000003, 0x00000007,
            0x0000000F, 0x0000001F, 0x0000003F, 0x0000007F,
            0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF,
            0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF,
            0x0000FFFF, 0x0001FFFF, 0x0003FFFF, 0x0007FFFF,
            0x000FFFFF, 0x001FFFFF, 0x003FFFFF, 0x007FFFFF,
            0x00FFFFFF, 0x01FFFFFF, 0x03FFFFFF, 0x07FFFFFF,
            0x0FFFFFFF, 0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF,
            0xFFFFFFFF
        };

        public static readonly uint[] SIGN_BITS = new uint[33]
        {
            0x00000000, 0x00000001, 0x00000002, 0x00000004,
            0x00000008, 0x00000010, 0x00000020, 0x00000040,
            0x00000080, 0x00000100, 0x00000200, 0x00000400,
            0x00000800, 0x00001000, 0x00002000, 0x00004000,
            0x00008000, 0x00010000, 0x00020000, 0x00040000,
            0x00080000, 0x00100000, 0x00200000, 0x00400000,
            0x00800000, 0x01000000, 0x02000000, 0x04000000,
            0x08000000, 0x10000000, 0x20000000, 0x40000000,
            0x80000000
        };

        public static readonly int[] SIGNED_MIN = new int[33]
        {
            0,
            -1, -2, -4, -8, -16, -32, -64, -128, -256,
            -512, -1024, -2048, -4096, -8192, -16384, -32768,
            -65536, -131072, -262144, -524288, -1048576,
            -2097152, -4194304, -8388608, -16777216, -33554432,
            -67108864, -134217728, -268435456, -536870912,
            -1073741824, -2147483648
        };

        public static readonly int[] SIGNED_MAX = new int[33]
        {
            0,
            0, 1, 3, 7, 15, 31, 63, 127, 255,
            511, 1023, 2047, 4095, 8191, 16383, 32767,
            65535, 131071, 262143, 524287, 1048575,
            2097151, 4194303, 8388607, 16777215, 33554431,
            67108863, 134217727, 268435455, 536870911,
            1073741823, 2147483647
        };

        public static readonly uint[] UNSIGNED_MIN = new uint[33]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public static readonly uint[] UNSIGNED_MAX = new uint[33]
        {
            0,
            1, 3, 7, 15, 31, 63, 127, 255, 511,
            1023, 2047, 4095, 8191, 16383, 32767, 65535,
            131071, 262143, 524287, 1048575,
            2097151, 4194303, 8388607, 16777215, 33554431,
            67108863, 134217727, 268435455, 536870911,
            1073741823, 2147483647, 4294967295
        };

        // Retourne un masque d'extension de signe (comme SIGN_EXTENDS Lua)
        public static uint SignExtendMask(int bits)
        {
            switch (bits)
            {
                case 0: return 0xFFFFFFFF;
                case 1: return 0xFFFFFFFE;
                case 2: return 0xFFFFFFFC;
                case 3: return 0xFFFFFFF8;
                case 4: return 0xFFFFFFF0;
                case 5: return 0xFFFFFFE0;
                case 6: return 0xFFFFFFC0;
                case 7: return 0xFFFFFF80;
                case 8: return 0xFFFFFF00;
                case 9: return 0xFFFFFE00;
                case 10: return 0xFFFFFC00;
                case 11: return 0xFFFFF800;
                case 12: return 0xFFFFF000;
                case 13: return 0xFFFFE000;
                case 14: return 0xFFFFC000;
                case 15: return 0xFFFF8000;
                case 16: return 0xFFFF0000;
                case 17: return 0xFFFE0000;
                case 18: return 0xFFFC0000;
                case 19: return 0xFFF80000;
                case 20: return 0xFFF00000;
                case 21: return 0xFFE00000;
                case 22: return 0xFFC00000;
                case 23: return 0xFF800000;
                case 24: return 0xFF000000;
                case 25: return 0xFE000000;
                case 26: return 0xFC000000;
                case 27: return 0xF8000000;
                case 28: return 0xF0000000;
                case 29: return 0xE0000000;
                case 30: return 0xC0000000;
                case 31: return 0x80000000;
                default: return 0; // Rien à étendre sur 32 bits !
            }
        }

        // Extension du signe, style Lua
        public static int SignExtend(int value, int bits)
        {
            var signBit = SIGN_BITS[bits];
            var ext = SignExtendMask(bits);
            if (((uint)value & signBit) != 0)
                return (int)(((uint)value) | ext);
            else
                return value;
        }
    }
}

using NUnit.Framework;
using FixedEngine.Math;

namespace FixedEngine.Tests.Math
{
    [TestFixture]
    public class UFixedTests
    {
        /*==================================
         * --- CONSTRUCTOR ---
         ==================================*/
        #region --- CONSTRUCTOR (exhaustif, UFixed<B16,B8>) ---

        // ---------- uint  -> UFixed ----------
        [TestCase(0u, 0u)]
        [TestCase(1u, 1u)]
        [TestCase(0xFFFFu, 0xFFFFu)]
        [TestCase(0x1_0000u, 0u)]          // 65536 → wrap = 0
        [TestCase(0x1234_5678u, 0x5678u)]     // wrap arbitraire

        public void Constructor_FromRaw_UInt_Wrap(uint raw, uint expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // ---------- UIntN -> UFixed (<< 8 puis wrap) ----------
        [TestCase(0u, 0u)]
        [TestCase(1u, 1u << 8)]   // 1.0
        [TestCase(255u, 255u << 8)]   // 255.0
        [TestCase(256u, 0u)]         // 256 → wrap = 0, puis << 8
        [TestCase(511u, 255u << 8)]   // wrap 511 → 255

        public void Constructor_From_UIntN_Wrap(uint intVal, uint expectedRaw)
        {
            var intValN = new UIntN<B16>(intVal);
            var fx = new UFixed<B16, B8>(intValN);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // ---------- float -> UFixed ----------
        [TestCase(0f, 0u)]
        [TestCase(0.5f, 128u)]          // 0.5 × 256
        [TestCase(1f, 256u)]          // 1.0
        [TestCase(1.999f, 512u)]          // 1.999 × 256 = 511.744 → round = 512
        [TestCase(255.996f, 0xFFFFu)]       // quasi max, round up
        [TestCase(-1f, 0xFF00u)]       // -256 wrap → 0xFF00

        public void Constructor_From_Float_Exhaustive(float value, uint expectedRaw)
        {
            var fx = new UFixed<B16, B8>(value);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // ---------- double -> UFixed ----------
        [TestCase(0.0, 0u)]
        [TestCase(1.0 / 256.0, 1u)]          // plus petit quantum
        [TestCase(7.125, 0x0720u)]     // cas exact (7 + 1/8)
        [TestCase(255.999, 0u)]     // saturation wrap
        [TestCase(-0.5, 0xFF80u)]     // -128 wrap → 0xFF80

        public void Constructor_From_Double_Exhaustive(double value, uint expectedRaw)
        {
            var fx = new UFixed<B16, B8>(value);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        #endregion

        /*==================================
         * --- CONVERSION EXPLICITES ---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES (exhaustif, UFixed<B16,B8>) ---

        // --- int -> UFixed ---
        [TestCase(0, 0u)]
        [TestCase(1, 256u)]
        [TestCase(-1, 65280u)]         // -1 << 8 = 0xFF00
        [TestCase(-128, 32768u)]       // -128 << 8 = 0x8000
        [TestCase(255, 65280u)]
        [TestCase(256, 0u)]
        [TestCase(32767, 65280u)]
        [TestCase(-32768, 0u)]
        [TestCase(int.MaxValue, 65280u)] // int.MaxValue << 8 & 0xFFFF
        [TestCase(int.MinValue, 0u)]     // int.MinValue << 8 & 0xFFFF
        public void Explicit_Int_To_UFixed_Wrap(int val, uint expected)
        {
            var fx = (UFixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- uint -> UFixed ---
        [TestCase(0u, 0u)]
        [TestCase(1u, 256u)]
        [TestCase(255u, 65280u)]
        [TestCase(256u, 0u)]
        [TestCase(65535u, 65280u)]
        [TestCase(65536u, 0u)]
        [TestCase(uint.MaxValue, 65280u)]
        public void Explicit_UInt_To_UFixed_Wrap(uint val, uint expected)
        {
            var fx = (UFixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- IntN -> UFixed ---
        [TestCase(0, 0u)]
        [TestCase(42, 10752u)]
        [TestCase(-1, 65280u)]
        [TestCase(-128, 32768u)]
        [TestCase(255, 65280u)]
        public void Explicit_IntN_To_UFixed_Wrap(int raw, uint expected)
        {
            var n = new IntN<B16>(raw);
            var fx = (UFixed<B16, B8>)(uint)n.Raw;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- UIntN -> UFixed ---
        [TestCase(0u, 0u)]
        [TestCase(77u, 19712u)]
        [TestCase(255u, 65280u)]
        [TestCase(256u, 0u)]
        public void Explicit_UIntN_To_UFixed_Wrap(uint raw, uint expected)
        {
            var n = new UIntN<B16>(raw);
            var fx = (UFixed<B16, B8>)n;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- float -> UFixed (wrap, bords, NaN/Inf, arrondis) ---
        [TestCase(0.0f, 0u)]
        [TestCase(1.0f, 256u)]
        [TestCase(-1.0f, 65280u)]
        [TestCase(255.0f, 65280u)]
        [TestCase(255.5f, 65408u)]
        [TestCase(255.996f, 65535u)]
        [TestCase(255.999f, 0u)]      // 255.999*256 arrondi=65536
        [TestCase(256.0f, 0u)]
        [TestCase(-0.5f, 65408u)]
        [TestCase(32767f, 65280u)]    // 32767*256=8388352, &0xFFFF=65280
        [TestCase(float.MaxValue, 0u)]
        [TestCase(float.MinValue, 0u)]
        [TestCase(float.NaN, 0u)]
        [TestCase(float.PositiveInfinity, 0u)]
        [TestCase(float.NegativeInfinity, 0u)]
        [TestCase(-0.0f, 0u)]
        public void Explicit_Float_To_UFixed_Wrap(float val, uint expected)
        {
            var fx = (UFixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- double -> UFixed (wrap, bords, NaN/Inf, arrondis) ---
        [TestCase(0.0, 0u)]
        [TestCase(1.0, 256u)]
        [TestCase(-2.0, 65024u)]
        [TestCase(255.0, 65280u)]
        [TestCase(255.5, 65408u)]
        [TestCase(255.996, 65535u)]
        [TestCase(255.999, 0u)]
        [TestCase(256.0, 0u)]
        [TestCase(65535.0, 65280u)]
        [TestCase(-0.5, 65408u)]
        [TestCase(double.MaxValue, 0u)]
        [TestCase(double.MinValue, 0u)]
        [TestCase(double.NaN, 0u)]
        [TestCase(double.PositiveInfinity, 0u)]
        [TestCase(double.NegativeInfinity, 0u)]
        [TestCase(-0.0, 0u)]
        [TestCase(1e-10, 0u)]
        [TestCase(-1e-10, 0u)]
        [TestCase(127.9995, 32768u)]
        public void Explicit_Double_To_UFixed_Wrap(double val, uint expected)
        {
            var fx = (UFixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // --- UFixed -> int (tronqué, wrap) ---
        [TestCase(0u, 0)]
        [TestCase(256u, 1)]
        [TestCase(65280u, 255)]
        [TestCase(65408u, 255)]
        [TestCase(65535u, 255)]
        [TestCase(32768u, 128)]
        public void Explicit_UFixed_To_Int_Wrap(uint raw, int expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            int val = (int)fx;
            Assert.That(val, Is.EqualTo(expected));
        }

        // --- UFixed -> uint (tronqué, wrap) ---
        [TestCase(0u, 0u)]
        [TestCase(256u, 1u)]
        [TestCase(65280u, 255u)]
        [TestCase(65535u, 255u)]
        public void Explicit_UFixed_To_UInt_Wrap(uint raw, uint expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            uint val = (uint)fx;
            Assert.That(val, Is.EqualTo(expected));
        }

        // --- UFixed -> float (conversion fractionnaire) ---
        [TestCase(0u, 0.0f)]
        [TestCase(256u, 1.0f)]
        [TestCase(65408u, 255.5f)]
        [TestCase(65535u, 255.99609375f)] // 65535/256
        [TestCase(32768u, 128.0f)]
        [TestCase(1u, 0.00390625f)]      // 1/256
        public void Explicit_UFixed_To_Float(uint raw, float expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            float val = (float)fx;
            Assert.That(val, Is.EqualTo(expected).Within(0.0001f));
        }

        // --- UFixed -> double (conversion fractionnaire) ---
        [TestCase(0u, 0.0d)]
        [TestCase(256u, 1.0d)]
        [TestCase(65408u, 255.5d)]
        [TestCase(65535u, 255.99609375d)] // 65535/256
        [TestCase(32768u, 128.0d)]
        [TestCase(1u, 0.00390625d)]
        public void Explicit_UFixed_To_Double(uint raw, double expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            double val = (double)fx;
            Assert.That(val, Is.EqualTo(expected).Within(0.0001d));
        }

        // --- UFixed -> IntN/UIntN ---
        [TestCase(512u, 2)]
        [TestCase(0u, 0)]
        [TestCase(65408u, 255)]
        [TestCase(65535u, 255)]
        [TestCase(32768u, 128)]
        public void Explicit_UFixed_To_IntN(uint raw, int expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            var n = (IntN<B16>)(int)fx;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        [TestCase(512u, 2u)]
        [TestCase(0u, 0u)]
        [TestCase(65408u, 255u)]
        [TestCase(65535u, 255u)]
        [TestCase(32768u, 128u)]
        public void Explicit_UFixed_To_UIntN(uint raw, uint expected)
        {
            var fx = new UFixed<B16, B8>(raw);
            var n = (UIntN<B16>)(uint)fx;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        // --- Fixed <-> UFixed croisé, wrap ---
        [TestCase(0, 0u)]
        [TestCase(-1, 65535u)]
        [TestCase(-128, 65408u)]
        [TestCase(32767, 32767u)]
        [TestCase(int.MinValue, 0u)]
        public void Explicit_Fixed_To_UFixed_Wrap(int raw, uint expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            var ufx = (UFixed<B16, B8>)fx;
            Assert.That(ufx.Raw, Is.EqualTo(expected));
        }

        [TestCase(0u, 0)]
        [TestCase(65408u, -128)]
        [TestCase(65535u, -1)]
        [TestCase(32767u, 32767)]
        [TestCase(32768u, -32768)]
        public void Explicit_UFixed_To_Fixed_Wrap(uint raw, int expected)
        {
            var ufx = new UFixed<B16, B8>(raw);
            var fx = (Fixed<B16, B8>)ufx;
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMETIQUES (exhaustif, UFixed<B16,B8> ---

        // --- Addition (wrap overflow, extrêmes, neutre, symétrie) ---
        [TestCase(65535u, 1u, 0u)]           // Max + 1 = 0 (wrap)
        [TestCase(65535u, 2u, 1u)]           // Max + 2 = 1
        [TestCase(32768u, 32768u, 0u)]       // 128.0 + 128.0 = 256.0 (wrap)
        [TestCase(0u, 65535u, 65535u)]       // 0 + max = max
        [TestCase(1u, 65535u, 0u)]           // 1 + max = 0
        [TestCase(0u, 1u, 1u)]               // 0 + 1 = 1
        [TestCase(65535u, 65535u, 65534u)]   // max + max = 65534 (wrap)
        [TestCase(1u, 0u, 1u)]               // 1 + 0 = 1
        [TestCase(0u, 0u, 0u)]               // 0 + 0 = 0
        public void UFixed_Add_Exotic(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x + y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Soustraction (wrap underflow, extrêmes, neutre, symétrie) ---
        [TestCase(0u, 1u, 65535u)]           // 0 - 1 = 65535 (wrap)
        [TestCase(0u, 65535u, 1u)]           // 0 - max = 1 (wrap)
        [TestCase(65535u, 65535u, 0u)]       // max - max = 0
        [TestCase(1u, 65535u, 2u)]           // 1 - max = 2
        [TestCase(65535u, 0u, 65535u)]       // max - 0 = max
        [TestCase(1u, 0u, 1u)]               // 1 - 0 = 1
        [TestCase(0u, 0u, 0u)]               // 0 - 0 = 0
        public void UFixed_Sub_Exotic(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x - y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Multiplication (incl. overflow, neutre, symétrie, -1) ---
        [TestCase(0u, 65535u, 0u)]
        [TestCase(1u, 65535u, 255u)]         // 1 * max, puis >> 8
        [TestCase(65535u, 65535u, 65024u)]   // max * max, puis >> 8, wrap
        [TestCase(32768u, 2u, 256u)]         // 128.0 * 2 = 256.0
        [TestCase(256u, 256u, 256u)]           // 1.0 * 1.0 = 1.0
        [TestCase(65535u, 1u, 255u)]         // max * 1.0 = 255
        [TestCase(65535u, 0u, 0u)]           // max * 0 = 0
        public void UFixed_Mul_Exotic(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x * y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Division (div by zero, max, min, neutre) ---
        [TestCase(0u, 65535u, 0u)]           // 0 / max = 0
        [TestCase(65535u, 1u, 65280u)]       // max / 1 = max
        [TestCase(65535u, 65535u, 256u)]     // max / max = 1.0
        [TestCase(1u, 65535u, 0u)]           // 1 / max = 0
        [TestCase(1u, 1u, 256u)]             // 1.0 / 1.0 = 1.0
        [TestCase(0u, 1u, 0u)]               // 0 / 1 = 0
                                             //[TestCase(1u, 0u, ...)]            // Optionnel : division par 0 (undefined) → comportement à définir selon ton moteur
        public void UFixed_Div_Exotic(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x / y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Modulo (symétrie, overflow, 1, max) ---
        [TestCase(0u, 65535u, 0u)]
        [TestCase(65535u, 65535u, 0u)]
        [TestCase(65535u, 1u, 0u)]
        [TestCase(1u, 65535u, 1u)]
        [TestCase(255u, 256u, 255u)]         // 255.0 mod 1.0 = 255
        public void UFixed_Mod_Exotic(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x % y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Incrémentation ++ (wrap hardware) ---
        [Test]
        public void UFixed_Increment_Exotic()
        {
            var x = new UFixed<B16, B8>(65535u);
            x++;
            Assert.That(x.Raw, Is.EqualTo(0u));
            x++;
            Assert.That(x.Raw, Is.EqualTo(1u));
            var y = new UFixed<B16, B8>(0u);
            y++;
            Assert.That(y.Raw, Is.EqualTo(1u));
        }

        // --- Décrémentation -- (wrap hardware) ---
        [Test]
        public void UFixed_Decrement_Exotic()
        {
            var x = new UFixed<B16, B8>(0u);
            x--;
            Assert.That(x.Raw, Is.EqualTo(65535u));
            x--;
            Assert.That(x.Raw, Is.EqualTo(65534u));
            var y = new UFixed<B16, B8>(1u);
            y--;
            Assert.That(y.Raw, Is.EqualTo(0u));
        }

        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES ARITHMETIQUE (exhaustif, UFixed<B16,B8) ---

        // --- ADD ---
        [TestCase(0u, 0u, 0u)]
        [TestCase(100u, 156u, 256u)]
        [TestCase(65535u, 1u, 0u)]            // overflow wrap
        [TestCase(40000u, 30000u, 4464u)]     // 40000+30000=70000 & 0xFFFF=4464
        [TestCase(65535u, 65535u, 65534u)]    // max+max=65534
        [TestCase(0u, 65535u, 65535u)]
        [TestCase(1u, 65535u, 0u)]            // 1+max=0
        [TestCase(32768u, 32768u, 0u)]        // 128.0+128.0=256.0 wrap
        public void UFixed_Add_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Add(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- SUB ---
        [TestCase(1000u, 200u, 800u)]
        [TestCase(0u, 1u, 65535u)]            // underflow wrap
        [TestCase(0u, 65535u, 1u)]
        [TestCase(65535u, 0u, 65535u)]
        [TestCase(65535u, 65535u, 0u)]
        [TestCase(1u, 1u, 0u)]
        [TestCase(1u, 65535u, 2u)]
        public void UFixed_Sub_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Sub(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- MUL ---
        [TestCase(256u, 256u, 256u)]            // 1.0 * 1.0 = 1.0 (raw=256)
        [TestCase(65535u, 65535u, 65024u)]      // max*max, wrap
        [TestCase(32768u, 2u, 256u)]            // 128.0 * 2/256 = 1.0
        [TestCase(1u, 1u, 0u)]                  // min*min, arrondi vers 0
        [TestCase(0u, 65535u, 0u)]
        [TestCase(65535u, 1u, 255u)]
        public void UFixed_Mul_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Mul(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- DIV ---
        [TestCase(1024u, 2u, 131072u & 0xFFFF)] // (1024 << 8) / 2 = 131072, wrap
        [TestCase(65535u, 255u, (65535u << 8) / 255u & 0xFFFF)] // test calcul
        [TestCase(65535u, 1u, (65535u << 8) / 1u & 0xFFFF)] // (65535<<8)/1 = 16776960 & 0xFFFF = 65280
        [TestCase(1u, 1u, 256u)]                  // 1.0 / 1.0 = 1.0
        [TestCase(0u, 65535u, 0u)]                // 0/max = 0
        public void UFixed_Div_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Div(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [Test]
        public void UFixed_Div_Static_ByZero_Throws()
        {
            var a = new UFixed<B16, B8>(1000u);
            var b = new UFixed<B16, B8>(0u);
            Assert.Throws<DivideByZeroException>(() => { var _ = UFixed<B16, B8>.Div(a, b); });
        }

        // --- MOD ---
        [TestCase(1000u, 256u, 232u)]
        [TestCase(65535u, 256u, 255u)]
        [TestCase(100u, 1u, 0u)]
        [TestCase(255u, 256u, 255u)]
        [TestCase(1u, 65535u, 1u)]
        [TestCase(65535u, 65535u, 0u)]
        public void UFixed_Mod_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Mod(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [Test]
        public void UFixed_Mod_Static_ByZero_Throws()
        {
            var a = new UFixed<B16, B8>(42u);
            var b = new UFixed<B16, B8>(0u);
            Assert.Throws<DivideByZeroException>(() => { var _ = UFixed<B16, B8>.Mod(a, b); });
        }

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE) (exhaustif, UFixed<B16,B8) ---

        // --- Multiplication par puissance de 2 ---
        [TestCase(256u, 1, 512u)]        // 1.0 * 2 = 2.0 (256*2=512)
        [TestCase(256u, 2, 1024u)]       // 1.0 * 4 = 4.0 (256*4=1024)
        [TestCase(65535u, 1, 65534u)]    // max * 2 = 0xFFFE (wrap 16b)
        [TestCase(32768u, 3, 0u)]        // 128*8=1024, 32768*8=262144&0xFFFF=0
        [TestCase(0u, 4, 0u)]            // 0 * 16 = 0
        [TestCase(1u, 8, 256u)]          // 0.0039... * 256 = 1 (wrap up, test arrondi up)
        [TestCase(4095u, 2, 16380u)]      // Petit * 4
        public void UFixed_MulPow2_BitFaithful(uint raw, int pow, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.MulPow2(x, pow);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Division par puissance de 2 ---
        [TestCase(256u, 1, 128u)]        // 1.0 / 2 = 0.5
        [TestCase(256u, 2, 64u)]         // 1.0 / 4 = 0.25
        [TestCase(1u, 1, 0u)]            // petit / 2 = 0 (troncature)
        [TestCase(65535u, 4, 4095u)]     // max >> 4 = 0x0FFF
        [TestCase(32768u, 8, 128u)]      // 128.0 / 256 = 0.5
        [TestCase(65535u, 8, 255u)]      // max / 256 = 255 (0xFF)
        [TestCase(0u, 8, 0u)]            // 0 / 256 = 0
        public void UFixed_DivPow2_BitFaithful(uint raw, int pow, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.DivPow2(x, pow);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- Modulo puissance de 2 (mask) ---
        [TestCase(65535u, 1, 1u)]        // 65535 % 2 = 1
        [TestCase(65535u, 8, 255u)]      // 65535 % 256 = 255
        [TestCase(32768u, 8, 0u)]        // 32768 % 256 = 0
        [TestCase(300u, 2, 0u)]          // 300 % 4 = 0
        [TestCase(12345u, 4, 9u)]        
        [TestCase(0u, 16, 0u)]           // 0 % 65536 = 0
        public void UFixed_ModPow2_BitFaithful(uint raw, int pow, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.ModPow2(x, pow);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- OPERATION BITWISE ---
         * operator &
         * operator |
         * operator ^
         * operator ~
         * operator <<
         * operator >>
         ==================================*/
        #region --- OPERATION BITWISE (exhaustif, UFixed<B16,B8) ---

        // --- AND ---
        [TestCase(0x1234u, 0x00FFu, 0x0034u)]
        [TestCase(0xFFFFu, 0xAAAAu, 0xAAAAu)]
        [TestCase(0xF0F0u, 0x0F0Fu, 0x0000u)]
        [TestCase(0xFFFFu, 0xFFFFu, 0xFFFFu)] // max & max = max
        [TestCase(0x0000u, 0xFFFFu, 0x0000u)] // zero & max = 0
        [TestCase(0x8000u, 0x0001u, 0x0000u)] // extrêmes opposés
        public void UFixed_BitwiseAnd(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x & y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- OR ---
        [TestCase(0x1234u, 0x00FFu, 0x12FFu)]
        [TestCase(0xF0F0u, 0x0F0Fu, 0xFFFFu)]
        [TestCase(0x0000u, 0x5555u, 0x5555u)]
        [TestCase(0xFFFFu, 0x0000u, 0xFFFFu)]
        [TestCase(0x8000u, 0x0001u, 0x8001u)] // extrêmes
        public void UFixed_BitwiseOr(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x | y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- XOR ---
        [TestCase(0x1234u, 0xFFFFu, 0xEDCBu)]
        [TestCase(0xAAAAu, 0x5555u, 0xFFFFu)]
        [TestCase(0xF0F0u, 0x0F0Fu, 0xFFFFu)]
        [TestCase(0x0000u, 0x0000u, 0x0000u)]
        [TestCase(0xFFFFu, 0xFFFFu, 0x0000u)]
        public void UFixed_BitwiseXor(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = x ^ y;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- NOT ---
        [TestCase(0x0000u, 0xFFFFu)]
        [TestCase(0xFFFFu, 0x0000u)]
        [TestCase(0x1234u, 0xEDCBu)]
        [TestCase(0x8000u, 0x7FFFu)] // bit de signe
        [TestCase(0x0001u, 0xFFFEu)] // bit faible
        public void UFixed_BitwiseNot(uint a, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = ~x;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- SHIFT LEFT ---
        [TestCase(0x0001u, 1, 0x0002u)]
        [TestCase(0x0100u, 4, 0x1000u)]
        [TestCase(0xFFFFu, 1, 0xFFFEu)]
        [TestCase(0x8000u, 1, 0x0000u)]    // overflow sortant
        [TestCase(0x0001u, 0, 0x0001u)]    // shift 0 = identique
        [TestCase(0x0001u, 15, 0x8000u)]   // shift maximal
        public void UFixed_BitwiseShiftLeft(uint a, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = x << shift;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // --- SHIFT RIGHT ---
        [TestCase(0x8000u, 1, 0x4000u)]
        [TestCase(0x0002u, 1, 0x0001u)]
        [TestCase(0xFFFFu, 4, 0x0FFFu)]
        [TestCase(0x0001u, 0, 0x0001u)]    // shift 0 = identique
        [TestCase(0x8000u, 15, 0x0001u)]   // shift maximal
        public void UFixed_BitwiseShiftRight(uint a, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = x >> shift;
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- METHODE STATIQUE BITWISE (alias) ---
         * And
         * Or
         * Xor
         * Not
         * Nand
         * Nor
         * Xnor
         * Shl
         * Shr
         ==================================*/
        #region --- METHODE STATIQUE BITWISE (alias) (exhaustif, UFixed<B16,B8) ---

        // AND
        [TestCase(0xABCDu, 0x0F0Fu, 0x0B0Du)]
        [TestCase(0xFFFFu, 0x0000u, 0x0000u)]
        [TestCase(0xFFFFu, 0xFFFFu, 0xFFFFu)]
        public void UFixed_And_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.And(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // OR
        [TestCase(0xABCDu, 0x1234u, 0xBBFDu)]
        [TestCase(0x0000u, 0x0000u, 0x0000u)]
        [TestCase(0x0F0Fu, 0xF0F0u, 0xFFFFu)]
        public void UFixed_Or_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Or(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // XOR
        [TestCase(0xAAAAu, 0x5555u, 0xFFFFu)]
        [TestCase(0x0000u, 0x0000u, 0x0000u)]
        [TestCase(0xFFFFu, 0xFFFFu, 0x0000u)]
        public void UFixed_Xor_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Xor(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // NOT
        [TestCase(0xFFFFu, 0x0000u)]
        [TestCase(0x0000u, 0xFFFFu)]
        [TestCase(0x1234u, 0xEDCBu)]
        public void UFixed_Not_Static(uint a, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = UFixed<B16, B8>.Not(x);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // NAND
        [TestCase(0xFFFFu, 0xFFFFu, 0x0000u)]    // ~(max & max) = 0
        [TestCase(0xFF00u, 0x0F0Fu, 0xF0FFu)]    // ~(0xFF00 & 0x0F0F) = 0xF0FF
        [TestCase(0x0000u, 0x0000u, 0xFFFFu)]    // ~(0 & 0) = max
        public void UFixed_Nand_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Nand(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // NOR
        [TestCase(0x0000u, 0xFFFFu, 0x0000u)]
        [TestCase(0x0F0Fu, 0xF0F0u, 0x0000u)]
        [TestCase(0x0000u, 0x0000u, 0xFFFFu)]
        public void UFixed_Nor_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Nor(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // XNOR
        [TestCase(0xFFFFu, 0xFFFFu, 0xFFFFu)]
        [TestCase(0xAAAAu, 0x5555u, 0x0000u)]
        [TestCase(0x0000u, 0x0000u, 0xFFFFu)]
        public void UFixed_Xnor_Static(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Xnor(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Shift Left (SHL)
        [TestCase(0x0001u, 4, 0x0010u)]
        [TestCase(0x0F0Fu, 8, 0x0F00u)]
        [TestCase(0xFFFFu, 12, 0xF000u)]
        [TestCase(0xFFFFu, 0, 0xFFFFu)]
        [TestCase(0xFFFFu, 15, 0x8000u)] // shift max
        public void UFixed_Shl_Static(uint a, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = UFixed<B16, B8>.Shl(x, shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Shift Right (SHR)
        [TestCase(0x8000u, 8, 0x0080u)]
        [TestCase(0xFFFFu, 4, 0x0FFFu)]
        [TestCase(0x00FFu, 1, 0x007Fu)]
        [TestCase(0x0001u, 0, 0x0001u)]
        [TestCase(0x8000u, 15, 0x0001u)] // shift max
        public void UFixed_Shr_Static(uint a, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var res = UFixed<B16, B8>.Shr(x, shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // SHIFT LEFT (trop grand)
        [TestCase(0x0001u, -1)]
        [TestCase(0x0001u, 16)] // pour 16 bits
        [TestCase(0x0001u, 42)]
        public void UFixed_ShiftLeft_Throws_On_OutOfRange(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = x << shift; });
        }

        // SHIFT RIGHT (trop grand)
        [TestCase(0x0001u, -1)]
        [TestCase(0x0001u, 16)]
        [TestCase(0x0001u, 99)]
        public void UFixed_ShiftRight_Throws_On_OutOfRange(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = x >> shift; });
        }

        // SHL/Shr statique (idem)
        [TestCase(0x1234u, -1)]
        [TestCase(0x1234u, 16)]
        public void UFixed_Shl_Static_Throws(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = UFixed<B16, B8>.Shl(x, shift); });
        }

        [TestCase(0x1234u, -1)]
        [TestCase(0x1234u, 16)]
        public void UFixed_Shr_Static_Throws(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = UFixed<B16, B8>.Shr(x, shift); });
        }

        #endregion

        /*==================================
         * --- COMPARAISONS ---
         * operator ==
         * operator !=
         * operator <
         * operator <=
         * operator >
         * operator >=
         * Equals(object obj)
         * GetHashCode()
         * Eq
         * Neq
         * Lt
         * Lte
         * Gt
         * Gte
         * IsZero
         ==================================*/
        #region --- COMPARAISONS (exhaustif, UFixed<B16,B8>) ---

        // == / !=
        [TestCase(1000u, 1000u, true)]
        [TestCase(1000u, 999u, false)]
        [TestCase(0u, 0u, true)]
        [TestCase(65535u, 0u, false)]
        public void UFixed_Equality(uint a, uint b, bool expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            Assert.That(x == y, Is.EqualTo(expected));
            Assert.That(x.Equals(y), Is.EqualTo(expected));
        }

        [TestCase(1000u, 1000u, false)]
        [TestCase(1000u, 999u, true)]
        [TestCase(1u, 0u, true)]
        public void UFixed_Inequality(uint a, uint b, bool expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            Assert.That(x != y, Is.EqualTo(expected));
        }

        // < / <=
        [TestCase(1000u, 2000u, true, true)]
        [TestCase(2000u, 1000u, false, false)]
        [TestCase(1500u, 1500u, false, true)]
        [TestCase(0u, 65535u, true, true)]
        public void UFixed_Less_LessEq(uint a, uint b, bool expectedLt, bool expectedLte)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            Assert.That(x < y, Is.EqualTo(expectedLt));
            Assert.That(x <= y, Is.EqualTo(expectedLte));
        }

        // > / >=
        [TestCase(2000u, 1000u, true, true)]
        [TestCase(1000u, 2000u, false, false)]
        [TestCase(1500u, 1500u, false, true)]
        [TestCase(65535u, 0u, true, true)]
        public void UFixed_Greater_GreaterEq(uint a, uint b, bool expectedGt, bool expectedGte)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            Assert.That(x > y, Is.EqualTo(expectedGt));
            Assert.That(x >= y, Is.EqualTo(expectedGte));
        }

        // Equals(object) et GetHashCode
        [Test]
        public void UFixed_Equals_Object()
        {
            var a = new UFixed<B16, B8>(1234u);
            object b = new UFixed<B16, B8>(1234u);
            object c = new UFixed<B16, B8>(5678u);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.Equals(null), Is.False);
            Assert.That(a.Equals(1234u), Is.False);
        }

        [Test]
        public void UFixed_GetHashCode_MatchesRaw()
        {
            var a = new UFixed<B16, B8>(4567u);
            Assert.That(a.GetHashCode(), Is.EqualTo(4567u.GetHashCode()));
        }

        // Helpers Eq/Neq/Lt/Lte/Gt/Gte
        [TestCase(1u, 1u, true, false, false, true, false, true)]
        [TestCase(2u, 1u, false, true, false, false, true, true)]
        [TestCase(1u, 2u, false, true, true, true, false, false)]
        public void UFixed_HelperComparisons(uint a, uint b, bool eq, bool neq, bool lt, bool lte, bool gt, bool gte)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            Assert.That(UFixed<B16, B8>.Eq(x, y), Is.EqualTo(eq));
            Assert.That(UFixed<B16, B8>.Neq(x, y), Is.EqualTo(neq));
            Assert.That(UFixed<B16, B8>.Lt(x, y), Is.EqualTo(lt));
            Assert.That(UFixed<B16, B8>.Lte(x, y), Is.EqualTo(lte));
            Assert.That(UFixed<B16, B8>.Gt(x, y), Is.EqualTo(gt));
            Assert.That(UFixed<B16, B8>.Gte(x, y), Is.EqualTo(gte));
        }

        // IsZero (statique & instance)
        [TestCase(0u, true)]
        [TestCase(1u, false)]
        [TestCase(65535u, false)]
        [TestCase(256u, false)]
        [TestCase(0x8000u, false)]
        public void UFixed_IsZero_Static(uint raw, bool expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.IsZeroStatic(x), Is.EqualTo(expected));
        }

        [TestCase(0u, true)]
        [TestCase(1u, false)]
        [TestCase(65535u, false)]
        public void UFixed_IsZero(uint raw, bool expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.IsZero, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- OPERATIONS UTILITAIRES ---
         * Min
         * Max
         * Avg
         * IsPowerOfTwo
         ==================================*/
        #region --- OPERATIONS UTILITAIRES (exhaustif, UFixed<B16,B8>) ---

        // Min
        [TestCase(1234u, 5678u, 1234u)]
        [TestCase(5678u, 1234u, 1234u)]
        [TestCase(65535u, 0u, 0u)]
        [TestCase(0u, 0u, 0u)]
        [TestCase(65535u, 65535u, 65535u)]
        public void UFixed_Min(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Min(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Max
        [TestCase(1234u, 5678u, 5678u)]
        [TestCase(5678u, 1234u, 5678u)]
        [TestCase(65535u, 0u, 65535u)]
        [TestCase(0u, 0u, 0u)]
        [TestCase(65535u, 65535u, 65535u)]
        public void UFixed_Max(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Max(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Moyenne arithmétique (avg, division entière, wrap-safe)
        [TestCase(0u, 256u, 128u)]
        [TestCase(255u, 255u, 255u)]
        [TestCase(1u, 65535u, 32768u)]
        [TestCase(65535u, 65535u, 65535u)]
        [TestCase(0u, 0u, 0u)]
        [TestCase(65535u, 0u, 32767u)]   // (65535+0)/2=32767
        public void UFixed_Avg(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.Avg(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Puissance de deux brute (raw) : utile pour test de “flag”, etc.
        [TestCase(0u, false)]
        [TestCase(1u, true)]
        [TestCase(2u, true)]
        [TestCase(3u, false)]
        [TestCase(4u, true)]
        [TestCase(8u, true)]
        [TestCase(256u, true)]      // 1.0 Q8.8
        [TestCase(32768u, true)]    // 128.0 Q8.8
        [TestCase(65535u, false)]   // max Q8.8, pas power of two
        [TestCase(65536u, false)]   // overflow Q8.8 (hors 16 bits)
        [TestCase(0x8000u, true)]   // 0x8000 == 32768, puissance de 2
        public void UFixed_IsPowerOfTwo(uint raw, bool expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.IsPowerOfTwo(x), Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- SATURATION ---
         * AddSat
         * SubSat
         * MulSat
         * Clamp
         * Clamp01
         * ClampWithOffset
         ==================================*/
        #region --- SATURATION (UFixed<B16,B8>, exhaustive) ---

        // AddSat (saturation max sans wrap)
        [TestCase(65535u, 1u, 65535u)]     // Max + 1 = saturé à max
        [TestCase(32768u, 32768u, 65535u)] // 128+128=256 (wrap) mais sat à max
        [TestCase(0u, 123u, 123u)]         // 0+123=123
        [TestCase(40000u, 30000u, 65535u)] // overflow saturé
        [TestCase(70000u, 100u, 4564u)]    // 70000 wrap→4464, 4464+100 < max, pas de sat
        [TestCase(65535u, 0u, 65535u)]     // max + 0 = max
        [TestCase(0u, 65535u, 65535u)]     // 0 + max = max
        public void UFixed_AddSat(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.AddSat(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // SubSat (saturation à 0)
        [TestCase(123u, 50u, 73u)]           // 123-50=73
        [TestCase(50u, 123u, 0u)]            // underflow saturé à 0
        [TestCase(0u, 1u, 0u)]               // underflow saturé à 0
        [TestCase(0u, 65535u, 0u)]           // underflow bas
        [TestCase(65535u, 0u, 65535u)]       // pas de sat
        [TestCase(65535u, 65535u, 0u)]       // max - max = 0
        public void UFixed_SubSat(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.SubSat(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // MulSat (saturation max en overflow)
        [TestCase(256u, 256u, 256u)]           // 1.0*1.0=1.0
        [TestCase(65535u, 65535u, 65535u)]     // max*max saturé à max
        [TestCase(32768u, 2u, 256u)]           // 128.0*2/256 = 1.0
        [TestCase(1u, 1u, 0u)]                 // (1/256)^2 -> raw 0
        [TestCase(256u, 65535u, 65535u)]       // 1.0 * max -> max
        [TestCase(0u, 1234u, 0u)]              // 0 * n = 0
        public void UFixed_MulSat(uint a, uint b, uint expected)
        {
            var x = new UFixed<B16, B8>(a);
            var y = new UFixed<B16, B8>(b);
            var res = UFixed<B16, B8>.MulSat(x, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Clamp (arbitraire, min/max inclus, no swap)
        [TestCase(500u, 100u, 400u, 400u)]  // clamp à max=400
        [TestCase(500u, 100u, 600u, 500u)]  // 500 dans [100,600] => 500
        [TestCase(50u, 100u, 400u, 100u)]   // clamp à min=100
        [TestCase(100u, 100u, 400u, 100u)]  // clamp à min=100
        [TestCase(350u, 300u, 320u, 320u)]  // min>max : résultat max (pas de swap)
        public void UFixed_Clamp(uint val, uint min, uint max, uint expected)
        {
            var x = new UFixed<B16, B8>(val);
            var minX = new UFixed<B16, B8>(min);
            var maxX = new UFixed<B16, B8>(max);
            var res = UFixed<B16, B8>.Clamp(x, minX, maxX);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Clamp01 (clamp entre 0.0 et 1.0)
        [TestCase(0u, 0u)]
        [TestCase(128u, 128u)]
        [TestCase(256u, 256u)]     // 1.0
        [TestCase(300u, 256u)]     // clamp à 1.0
        [TestCase(65535u, 256u)]   // clamp à 1.0
        [TestCase(4294967295u, 256u)] // overflow, clamp à 1.0
        public void UFixed_Clamp01(uint val, uint expected)
        {
            var x = new UFixed<B16, B8>(val);
            var res = UFixed<B16, B8>.Clamp01(x);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // ClampWithOffset (bords, offset négatifs, wraps, non-swap)
        [TestCase(200u, 100u, 300u, 20u, 40u, 200u)]
        [TestCase(90u, 100u, 300u, 20u, 40u, 120u)]
        [TestCase(400u, 100u, 300u, 20u, 40u, 340u)]
        [TestCase(50u, 100u, 300u, 500u, 40u, 340u)]
        [TestCase(70000u, 100u, 300u, 20u, 100000u, 4464u)]
        [TestCase(350u, 300u, 320u, 100u, 0u, 320u)]      // vLo wrapé < vHi
        [TestCase(10u, 50u, 100u, 70000u, 70000u, 4514u)]
        [TestCase(65000u, 60000u, 65000u,
          unchecked((uint)-70000), unchecked((uint)-10), 64990u)]
        public void UFixed_ClampWithOffset(uint val, uint min, uint max, uint offsetMin, uint offsetMax, uint expected)
        {
            var x = new UFixed<B16, B8>(val);
            var minX = new UFixed<B16, B8>(min);
            var maxX = new UFixed<B16, B8>(max);
            var res = UFixed<B16, B8>.ClampWithOffset(x, minX, maxX, offsetMin, offsetMax);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- FONCTIONS TRIGONOMETRIQUES ---
         * Sin
         * Cos
         * Tan
         * Asin
         * Acos
         * Atan
         * Atan2
         ==================================*/

        /*==================================
         * --- MANIPULATION BITS ET ROTATIONS ---
         * Reverse
         * PopCount
         * Parity
         * LeadingZeros
         * TrailingZeros
         * Rol (rotate left)
         * Ror (rotate right)
         * Bsr (bit scan reverse)
         * Bsf (bit scan forward)
         ==================================*/
        #region --- MANIPULATION BITS ET ROTATIONS (UFixed<B16,B8>) ---

        // Reverse (bitwise mirror)
        [TestCase(0x0001u, 0x8000u)]
        [TestCase(0x8000u, 0x0001u)]
        [TestCase(0x00FFu, 0xFF00u)]
        [TestCase(0x1234u, 0x2C48u)]
        [TestCase(0xFFFFu, 0xFFFFu)]
        [TestCase(0x0000u, 0x0000u)]
        public void UFixed_Reverse(uint raw, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.Reverse(x);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(0x0000u)]
        [TestCase(0x0001u)]
        [TestCase(0x00FFu)]
        [TestCase(0x1234u)]
        [TestCase(0x8000u)]
        [TestCase(0xFFFFu)]
        public void UFixed_Reverse_IsInvolutive(uint raw)
        {
            var x = new UFixed<B16, B8>(raw);
            var xx = UFixed<B16, B8>.Reverse(x);
            var xxx = UFixed<B16, B8>.Reverse(xx);
            Assert.That(xxx.Raw, Is.EqualTo(x.Raw));
        }

        // PopCount (nombre de bits à 1)
        [TestCase(0x0000u, 0)]
        [TestCase(0xFFFFu, 16)]
        [TestCase(0x00FFu, 8)]
        [TestCase(0x1234u, 5)]
        [TestCase(0x8000u, 1)]
        [TestCase(0x5555u, 8)] // binaire: 01010101 01010101
        public void UFixed_PopCount(uint raw, int expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.PopCount(x), Is.EqualTo(expected));
        }

        // Parity (impair=true, pair=false)
        [TestCase(0x0000u, false)]  // 0 bit à 1
        [TestCase(0xFFFFu, false)]  // 16 bits à 1
        [TestCase(0x00FFu, false)]  // 8 bits à 1
        [TestCase(0x8001u, false)]  // 2 bits à 1
        [TestCase(0x8000u, true)]   // 1 bit à 1
        [TestCase(0x0001u, true)]
        public void UFixed_Parity(uint raw, bool expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.Parity(x), Is.EqualTo(expected));
        }

        // LeadingZeros (nb de zéros en tête)
        [TestCase(0x0000u, 16)]
        [TestCase(0x8000u, 0)]
        [TestCase(0x4000u, 1)]
        [TestCase(0x0001u, 15)]
        [TestCase(0x1234u, 3)]
        [TestCase(0x0002u, 14)]
        public void UFixed_LeadingZeros(uint raw, int expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.LeadingZeros(x), Is.EqualTo(expected));
        }

        // TrailingZeros (nb de zéros à droite)
        [TestCase(0x0000u, 16)]
        [TestCase(0x8000u, 15)]
        [TestCase(0x0001u, 0)]
        [TestCase(0x1234u, 2)]
        [TestCase(0x0010u, 4)]
        public void UFixed_TrailingZeros(uint raw, int expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.TrailingZeros(x), Is.EqualTo(expected));
        }

        // Rotate left (rol)
        [TestCase(0x1234u, 4, 0x2341u)]
        [TestCase(0x8000u, 1, 0x0001u)]
        [TestCase(0x0001u, 1, 0x0002u)]
        [TestCase(0xFFFFu, 8, 0xFFFFu)]
        [TestCase(0x0001u, 16, 0x0001u)] // rot 16 bits = identité
        [TestCase(0x0001u, 0, 0x0001u)]
        public void UFixed_Rol(uint raw, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.Rol(x, shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Rotate right (ror)
        [TestCase(0x1234u, 4, 0x4123u)]
        [TestCase(0x8000u, 1, 0x4000u)]
        [TestCase(0x0001u, 1, 0x8000u)]
        [TestCase(0xFFFFu, 8, 0xFFFFu)]
        [TestCase(0x8000u, 16, 0x8000u)] // rot 16 bits = identité
        [TestCase(0x0001u, 0, 0x0001u)]
        public void UFixed_Ror(uint raw, int shift, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = UFixed<B16, B8>.Ror(x, shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // Involutivité : Rol puis Ror du même shift donne la valeur d'origine
        [TestCase(0x1234u, 4)]
        [TestCase(0xCAFEu, 8)]
        [TestCase(0x8001u, 15)]
        [TestCase(0x0001u, 1)]
        [TestCase(0xFFFFu, 7)]
        public void UFixed_Rol_Ror_AreInverses(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            var rolled = UFixed<B16, B8>.Rol(x, shift);
            var back = UFixed<B16, B8>.Ror(rolled, shift);
            Assert.That(back.Raw, Is.EqualTo(x.Raw));
        }

        // Idem dans l'autre sens : Ror puis Rol
        [TestCase(0x1234u, 4)]
        [TestCase(0xCAFEu, 8)]
        [TestCase(0x8001u, 15)]
        [TestCase(0x0001u, 1)]
        [TestCase(0xFFFFu, 7)]
        public void UFixed_Ror_Rol_AreInverses(uint raw, int shift)
        {
            var x = new UFixed<B16, B8>(raw);
            var ror = UFixed<B16, B8>.Ror(x, shift);
            var back = UFixed<B16, B8>.Rol(ror, shift);
            Assert.That(back.Raw, Is.EqualTo(x.Raw));
        }

        // Cyclicité complète sur 16 bits (un tour complet ramène à la valeur initiale)
        [TestCase(0x1234u)]
        [TestCase(0xCAFEu)]
        [TestCase(0x8001u)]
        [TestCase(0x0001u)]
        [TestCase(0xFFFFu)]
        public void UFixed_Rol_Ror_Cycle16(uint raw)
        {
            var x = new UFixed<B16, B8>(raw);
            var rolled = UFixed<B16, B8>.Rol(x, 16);
            var ror = UFixed<B16, B8>.Ror(x, 16);
            Assert.That(rolled.Raw, Is.EqualTo(x.Raw));
            Assert.That(ror.Raw, Is.EqualTo(x.Raw));
        }

        // Shift de 0 doit être identique
        [TestCase(0x1234u)]
        [TestCase(0xCAFEu)]
        [TestCase(0x8001u)]
        [TestCase(0x0001u)]
        [TestCase(0xFFFFu)]
        public void UFixed_Rol_Ror_Shift0_IsIdentity(uint raw)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.Rol(x, 0).Raw, Is.EqualTo(x.Raw));
            Assert.That(UFixed<B16, B8>.Ror(x, 0).Raw, Is.EqualTo(x.Raw));
        }

        // BSR (Bit Scan Reverse)
        [TestCase(0x0000u, -1)]
        [TestCase(0x0001u, 0)]
        [TestCase(0x8000u, 15)]
        [TestCase(0x00F0u, 7)]
        [TestCase(0x1234u, 12)]
        public void UFixed_Bsr(uint raw, int expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.Bsr(x), Is.EqualTo(expected));
        }

        // BSF (Bit Scan Forward)
        [TestCase(0x0000u, -1)]
        [TestCase(0x0001u, 0)]
        [TestCase(0x8000u, 15)]
        [TestCase(0x00F0u, 4)]
        [TestCase(0x1234u, 2)]
        public void UFixed_Bsf(uint raw, int expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.Bsf(x), Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- CONSTANTES ---
         * Zero
         * Half
         * AllOnes
         * Msb
         * Lsb
         * Bit
         * Fraction
         ==================================*/
        #region --- CONSTANTES (UFixed<B16,B8>) ---

        [Test]
        public void UFixed_Zero_Is_0()
        {
            Assert.That(UFixed<B16, B8>.Zero.Raw, Is.EqualTo(0u));
        }

        [Test]
        public void UFixed_One_Is_256()
        {
            Assert.That(UFixed<B16, B8>.One.Raw, Is.EqualTo(256u)); // 1.0 en Q8.8
        }

        [Test]
        public void UFixed_Half_Is_128()
        {
            Assert.That(UFixed<B16, B8>.Half.Raw, Is.EqualTo(128u)); // 0.5 en Q8.8
        }

        [Test]
        public void UFixed_AllOnes_Is_FFFF()
        {
            Assert.That(UFixed<B16, B8>.AllOnes.Raw, Is.EqualTo(0xFFFFu));
        }

        [Test]
        public void UFixed_Msb_Is_8000()
        {
            Assert.That(UFixed<B16, B8>.Msb.Raw, Is.EqualTo(0x8000u));
        }

        [Test]
        public void UFixed_Lsb_Is_0001()
        {
            Assert.That(UFixed<B16, B8>.Lsb.Raw, Is.EqualTo(0x0001u));
        }

        // Bit(n)
        [TestCase(0, 0x0001u)]
        [TestCase(1, 0x0002u)]
        [TestCase(8, 0x0100u)]
        [TestCase(15, 0x8000u)]
        [TestCase(7, 0x0080u)]
        public void UFixed_Bit(int n, uint expected)
        {
            Assert.That(UFixed<B16, B8>.Bit(n).Raw, Is.EqualTo(expected));
        }

        // Fraction(n, d) — (utile pour Q8.8 : Fraction(1,2)=128, Fraction(1,4)=64)
        [TestCase(1, 2, 128u)]
        [TestCase(1, 4, 64u)]
        [TestCase(3, 4, 192u)]
        [TestCase(5, 8, 160u)]
        public void UFixed_Fraction(int numer, int denom, uint expected)
        {
            var n = new IntN<B16>(numer);
            var d = new IntN<B16>(denom);
            Assert.That(UFixed<B16, B8>.Fraction(n, d).Raw, Is.EqualTo(expected));
        }
        #endregion

        /*==================================
         * --- ACCES OCTETS ---
         * Byte (static)
         * ToBytes
         * FromBytes
         * GetByte
         * SetByte
         * ReplaceByte
         ==================================*/
        #region --- ACCES OCTETS (UFixed<B16, B8>) ---

        // Byte (static, accès à l'octet n, little endian)
        [TestCase(0x1234u, 0, 0x34)]
        [TestCase(0x1234u, 1, 0x12)]
        [TestCase(0xABCDu, 0, 0xCD)]
        [TestCase(0xABCDu, 1, 0xAB)]
        public void UFixed_Byte_Static(uint raw, int n, byte expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(UFixed<B16, B8>.Byte(x, n), Is.EqualTo(expected));
        }

        // ToBytes (little endian)
        [TestCase(0x1234u, 0x34, 0x12)]
        [TestCase(0xFF01u, 0x01, 0xFF)]
        [TestCase(0x0000u, 0x00, 0x00)]
        [TestCase(0xFFFFu, 0xFF, 0xFF)]
        public void UFixed_ToBytes(uint raw, byte expected0, byte expected1)
        {
            var x = new UFixed<B16, B8>(raw);
            var bytes = x.ToBytes();
            Assert.That(bytes.Length, Is.EqualTo(2));
            Assert.That(bytes[0], Is.EqualTo(expected0)); // LSB
            Assert.That(bytes[1], Is.EqualTo(expected1)); // MSB
        }

        // FromBytes (little endian)
        [Test]
        public void UFixed_FromBytes_LittleEndian()
        {
            var bytes = new byte[] { 0xCD, 0xAB };
            var x = UFixed<B16, B8>.FromBytes(bytes);
            Assert.That(x.Raw, Is.EqualTo(0xABCDu));
        }

        // GetByte (instance)
        [TestCase(0xCAFEu, 0, 0xFE)]
        [TestCase(0xCAFEu, 1, 0xCA)]
        [TestCase(0x0000u, 0, 0x00)]
        [TestCase(0xFFFFu, 1, 0xFF)]
        public void UFixed_GetByte(uint raw, int n, byte expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.GetByte(n), Is.EqualTo(expected));
        }

        // SetByte (instance, retourne nouvelle instance)
        [TestCase(0xCAFEu, 0, 0x12, 0xCA12u)]
        [TestCase(0xCAFEu, 1, 0x34, 0x34FEu)]
        [TestCase(0xFFFFu, 0, 0x00, 0xFF00u)]
        [TestCase(0xFFFFu, 1, 0x00, 0x00FFu)]
        public void UFixed_SetByte(uint raw, int n, byte b, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var res = x.SetByte(n, b);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        // ReplaceByte (remplace l'octet n par celui d'une autre instance)
        [TestCase(0xCAFEu, 0xBEEFu, 0, 0xCAEFu)]
        [TestCase(0xCAFEu, 0xBEEFu, 1, 0xBEFEu)]
        [TestCase(0xFFFFu, 0x0000u, 0, 0xFF00u)]
        [TestCase(0xFFFFu, 0x0000u, 1, 0x00FFu)]
        public void UFixed_ReplaceByte(uint raw, uint raw2, int n, uint expected)
        {
            var x = new UFixed<B16, B8>(raw);
            var y = new UFixed<B16, B8>(raw2);
            var res = x.ReplaceByte(n, y);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- CONVERSION EN CHAÎNE (STRING) ---
         * ToString
         * DebugString
         * ToBinaryString
         * ToHexString
         ==================================*/
        #region --- CONVERSION EN CHAÎNE (STRING) ---

        // ToString (juste la valeur brute)
        [TestCase(0u, "0")]
        [TestCase(256u, "256")]
        [TestCase(65535u, "65535")]
        [TestCase(0xCAFEu, "51966")]
        public void UFixed_ToString(uint raw, string expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.ToString(), Is.EqualTo(expected));
        }

        // DebugString (full : nom type, valeur, binaire, hex)
        [TestCase(0u, "UFixed<B16, B8>(0) [bin=0000000000000000 hex=0000]")]
        [TestCase(1u, "UFixed<B16, B8>(1) [bin=0000000000000001 hex=0001]")]
        [TestCase(255u, "UFixed<B16, B8>(255) [bin=0000000011111111 hex=00FF]")]
        [TestCase(256u, "UFixed<B16, B8>(256) [bin=0000000100000000 hex=0100]")]
        [TestCase(0xCAFEu, "UFixed<B16, B8>(51966) [bin=1100101011111110 hex=CAFE]")]
        [TestCase(0xFFFFu, "UFixed<B16, B8>(65535) [bin=1111111111111111 hex=FFFF]")]
        public void UFixed_DebugString(uint raw, string expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.DebugString(), Is.EqualTo(expected));
        }

        // ToBinaryString (toujours 16 bits, zéro-padding)
        [TestCase(0u, "0000000000000000")]
        [TestCase(1u, "0000000000000001")]
        [TestCase(0xFFu, "0000000011111111")]
        [TestCase(0x1234u, "0001001000110100")]
        [TestCase(0xFFFFu, "1111111111111111")]
        public void UFixed_ToBinaryString(uint raw, string expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.ToBinaryString(), Is.EqualTo(expected));
        }

        // ToHexString (maj, 4 digits, option prefixe)
        [TestCase(0u, false, "0000")]
        [TestCase(255u, false, "00FF")]
        [TestCase(0x1234u, false, "1234")]
        [TestCase(0xFFFFu, false, "FFFF")]
        [TestCase(0x1234u, true, "0x1234")]
        [TestCase(0xFFFFu, true, "0xFFFF")]
        public void UFixed_ToHexString(uint raw, bool withPrefix, string expected)
        {
            var x = new UFixed<B16, B8>(raw);
            Assert.That(x.ToHexString(withPrefix), Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- PARSING ---
         * Parse
         * TryParse
         * ParseHex
         * TryParseHex
         * ParseBinary
         * TryParseBinary
         * ToJson
         * FromJson
         ==================================*/
        #region --- PARSING (exhaustif, JSON, HEX, BINAIRE) ---

        // --- Décimal ---
        [TestCase("0", 0u)]
        [TestCase("255", 65280u)]
        [TestCase("42", 10752u)]
        [TestCase("260", 1024u)]
        [TestCase("-1", 65280u)]   // wrap : -1 as uint = 0xFFFFFFFF
        [TestCase("1.5", 384u)]         // 1.5 en Q8.8 = 1.5*256=384
        public void Parse_Decimal_Valid(string s, uint expected)
        {
            var val = UFixed<B16, B8>.Parse(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UFixed<B16, B8>.TryParse(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Décimal erreurs ---
        [TestCase("")]
        [TestCase("abc")]
        // [TestCase("99999999999999999")] // NON TESTÉ : wrap hardware (pas d'exception, conversion float + bitcast RAW)
        public void Parse_Decimal_Invalid(string s)
        {
            Assert.Throws(Is.TypeOf<FormatException>().Or.TypeOf<OverflowException>(), () => UFixed<B16, B8>.Parse(s));
            Assert.That(UFixed<B16, B8>.TryParse(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Decimal_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.Parse(null));
            Assert.That(UFixed<B16, B8>.TryParse(null, out var _), Is.False);
        }

        // --- Hexadécimal ---
        [TestCase("0x0", 0u)]
        [TestCase("0xFF", 255u)]
        [TestCase("FF", 255u)]
        [TestCase("ab", 171u)]
        [TestCase("0x00", 0u)]
        [TestCase("0000", 0u)]
        public void Parse_Hex_Valid(string s, uint expected)
        {
            var val = UFixed<B16, B8>.ParseHex(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UFixed<B16, B8>.TryParseHex(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("0x")]
        [TestCase("xyz")]
        public void Parse_Hex_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.ParseHex(s));
            Assert.That(UFixed<B16, B8>.TryParseHex(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Hex_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.ParseHex(null));
            Assert.That(UFixed<B16, B8>.TryParseHex(null, out var _), Is.False);
        }

        // --- Binaire ---
        [TestCase("0b0", 0u)]
        [TestCase("0b11111111", 255u)]
        [TestCase("11111111", 255u)]
        [TestCase("00000000", 0u)]
        [TestCase("10101010", 170u)]
        public void Parse_Binary_Valid(string s, uint expected)
        {
            var val = UFixed<B16, B8>.ParseBinary(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UFixed<B16, B8>.TryParseBinary(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Binaire erreurs ---
        [TestCase("")]
        [TestCase("0b")]
        [TestCase("21001100")]   // chiffre non-binaire
        public void Parse_Binary_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.ParseBinary(s));
            Assert.That(UFixed<B16, B8>.TryParseBinary(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Binary_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.ParseBinary(null));
            Assert.That(UFixed<B16, B8>.TryParseBinary(null, out var _), Is.False);
        }

        // --- Round-trip JSON natif ---
        [TestCase(0u)]
        [TestCase(127u)]
        [TestCase(65535u)]
        [TestCase(4294967295u)]
        public void ToJson_RoundTrip(uint src)
        {
            var a = new UFixed<B16, B8>(src);
            string json = a.ToJson();
            var b = UFixed<B16, B8>.FromJson(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- FromJson mixte (décimal, hex, binaire) ---
        [TestCase("127", 127u)]
        [TestCase("0x7F", 127u)]
        [TestCase("0b01111111", 127u)]
        [TestCase("255", 255u)]
        [TestCase("0xFF", 255u)]
        [TestCase("0b11111111", 255u)]
        public void FromJson_Mixte(string s, uint expected)
        {
            var x = UFixed<B16, B8>.FromJson(s);
            Assert.That(x.Raw, Is.EqualTo(expected));
        }

        // --- FromJson erreurs ---
        [TestCase("")]
        [TestCase("NaN")]
        [TestCase("0x")]
        [TestCase("0b")]
        [TestCase("GG")]
        public void FromJson_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.FromJson(s));
        }

        [Test]
        public void FromJson_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.FromJson(null));
        }

        // --- Debug: les "0b" ne passent jamais ---
        [Test]
        public void TryParse_Debug_0b()
        {
            Assert.That(UFixed<B16, B8>.TryParse("0b", out var _), Is.False, "TryParse doit retourner false pour '0b'");
            Assert.That(UFixed<B16, B8>.TryParseHex("0b", out var _), Is.False, "TryParseHex doit retourner false pour '0b'");
            Assert.That(UFixed<B16, B8>.TryParseBinary("0b", out var _), Is.False, "TryParseBinary doit retourner false pour '0b'");
        }

        #endregion

        /*==================================
 * --- SERIALISATION META ---
 * ToJsonWithMeta
 * FromJsonWithMeta
 ==================================*/
        #region --- SERIALISATION META (exhaustif, multi-Q, erreurs) ---

        // --- Q8.8 ---
        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(255u)]
        [TestCase(65280u)]   // 255 << 8
        [TestCase(65535u)]   // Max Q8.8 (0xFFFF)
        public void ToJsonWithMeta_RoundTrip_Q8_8(uint raw)
        {
            var a = new UFixed<B16, B8>(raw);
            string json = a.ToJsonWithMeta();
            var b = UFixed<B16, B8>.FromJsonWithMeta<B16, B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Q16.8 ---
        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(255u)]
        [TestCase(16776960u)] // 0xFFFF00
        [TestCase(16777215u)] // Max Q16.8 (0xFFFFFF)
        public void ToJsonWithMeta_RoundTrip_Q16_8(uint raw)
        {
            var a = new UFixed<B24, B8>(raw); // Q16.8 = 24 bits total
            string json = a.ToJsonWithMeta();
            var b = UFixed<B24, B8>.FromJsonWithMeta<B24, B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Q24.8 ---
        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(255u)]
        [TestCase(4278190080u)] // 0xFF000000
        [TestCase(4294967295u)] // Max Q24.8 (0xFFFFFFFF)
        public void ToJsonWithMeta_RoundTrip_Q24_8(uint raw)
        {
            var a = new UFixed<B32, B8>(raw); // Q24.8 = 32 bits total
            string json = a.ToJsonWithMeta();
            var b = UFixed<B32, B8>.FromJsonWithMeta<B32, B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Erreur : meta bits/fracs non concordants ---
        [Test]
        public void FromJsonWithMeta_BitsMismatch_Throws()
        {
            var a = new UFixed<B16, B8>(12345);
            string json = a.ToJsonWithMeta();
            Assert.Throws<FormatException>(() => UFixed<B12, B4>.FromJsonWithMeta<B12, B4>(json));
        }

        // --- Tests malformés, string vide, mauvais champs, etc. ---
        [TestCase("{ \"raw\": 123 }")]    // uintBits/fracs manquant
        [TestCase("{ \"uintBits\": 16 }")]    // raw manquant
        [TestCase("{ \"uintBits\": 16, \"fracBits\": 8 }")] // raw manquant
        [TestCase("{ \"uintBits\": 16, \"fracBits\": 8, \"raw\": \"oops\" }")]
        [TestCase("{ \"uintBits\": \"oops\", \"fracBits\": 8, \"raw\": 12 }")]
        [TestCase("{ \"uintBits\": 16, \"fracBits\": \"bad\", \"raw\": 12 }")]
        [TestCase("{}")]
        [TestCase("")]
        public void FromJsonWithMeta_InvalidJson_Throws(string json)
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.FromJsonWithMeta<B16, B8>(json));
        }

        // --- Test spécifique pour null ---
        [Test]
        public void FromJsonWithMeta_Null_Throws()
        {
            Assert.Throws<FormatException>(() => UFixed<B16, B8>.FromJsonWithMeta<B16, B8>(null));
        }

        #endregion


    }
}

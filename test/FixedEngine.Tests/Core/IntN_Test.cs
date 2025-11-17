using FixedEngine.Core;
using FixedEngine.Math;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FixedEngine.Tests.Core
{
    [TestFixture]
    public class IntNTests
    {

        /*==================================
         * --- CONSTRUCTOR ---
         ==================================*/
        #region --- CONSTRUCTEUR  (exhaustif, IntN<B8>) ---

        /*  0) cas neutre et identités  */
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(-1, -1)]

        /*  1) bornes strictes du domaine 8 bits signé  */
        [TestCase(127, 127)]   // +Max
        [TestCase(-128, -128)]   // -Min

        /*  2) overflows “+”  */
        [TestCase(128, -128)]   // +Max+1
        [TestCase(129, -127)]
        [TestCase(130, -126)]
        [TestCase(255, -1)]
        [TestCase(256, 0)]
        [TestCase(257, 1)]
        [TestCase(511, -1)]   // 0x1FF → 0xFF → -1

        /*  3) underflows “–”  */
        [TestCase(-129, 127)]   // -Min-1
        [TestCase(-130, 126)]
        [TestCase(-255, 1)]
        [TestCase(-256, 0)]
        [TestCase(-257, -1)]

        /*  4) extrêmes 32 bits  */
        [TestCase(int.MaxValue, -1)]   // 0x7FFF_FFFF & 0xFF = 0xFF → -1
        [TestCase(int.MinValue, 0)]   // 0x8000_0000 <<24>>24  = 0

        public void IntN_Constructor_Wraps_SignExtends(int input, int expected)
        {
            var v = new IntN<B8>(input);
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- CONVERSION EXPLICITES---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES & WRAP (exhaustif, IntN<B8>) ---

        /* ---------- int  -> IntN<B8> ---------- */
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(-1, -1)]
        [TestCase(127, 127)]
        [TestCase(-128, -128)]
        [TestCase(128, -128)]   // wrap
        [TestCase(129, -127)]
        [TestCase(255, -1)]
        [TestCase(256, 0)]
        [TestCase(511, -1)]
        [TestCase(-256, 0)]
        [TestCase(-257, -1)]
        [TestCase(int.MaxValue, -1)]
        [TestCase(int.MinValue, 0)]
        public void Explicit_Int_To_IntN_B8_Wrap(int src, int expected)
        {
            var n = (IntN<B8>)src;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        /* ---------- IntN<B8> -> int ---------- */
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(127)]
        [TestCase(-128)]
        public void Explicit_IntN_B8_To_Int_RoundTrip(int v)
        {
            int round = (int)new IntN<B8>(v);
            Assert.That(round, Is.EqualTo(v));
        }

        /* ---------- uint -> IntN<B8> ---------- */
        [TestCase(0u, 0)]
        [TestCase(127u, 127)]
        [TestCase(128u, -128)]
        [TestCase(250u, -6)]
        [TestCase(255u, -1)]
        [TestCase(256u, 0)]
        [TestCase(512u, 0)]
        [TestCase(uint.MaxValue, -1)]
        public void Explicit_UInt_To_IntN_B8_Wrap(uint src, int expected)
        {
            var n = (IntN<B8>)src;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        /* ---------- IntN<B8> -> uint ---------- */
        [TestCase(0, 0u)]
        [TestCase(1, 1u)]
        [TestCase(127, 127u)]
        [TestCase(-128, 128u)]   // -128 & 0xFF = 128
        [TestCase(-1, 255u)]   // -1   & 0xFF = 255
        public void Explicit_IntN_B8_To_UInt(int src, uint expected)
        {
            var n = new IntN<B8>(src);
            uint u = (uint)n;
            Assert.That(u, Is.EqualTo(expected));
        }

        /* ---------- float -> IntN<B8> ---------- */
        [TestCase(0.0f, 0)]
        [TestCase(127.99f, 127)]   // trunc
        [TestCase(-128.1f, -128)]
        [TestCase(128.0f, -128)]   // wrap
        [TestCase(-129.0f, 127)]
        [TestCase(255.0f, -1)]
        [TestCase(256.0f, 0)]
        [TestCase(float.NaN, 0)]
        [TestCase(float.PositiveInfinity, 0)]
        [TestCase(float.NegativeInfinity, 0)]
        public void Explicit_Float_To_IntN_B8_Wrap(float src, int expected)
        {
            var n = (IntN<B8>)src;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        /* ---------- IntN<B8> -> float ---------- */
        [TestCase(-128)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(127)]
        public void Explicit_IntN_B8_To_Float_RoundTrip(int src)
        {
            float f = (float)new IntN<B8>(src);
            Assert.That(f, Is.EqualTo(src).Within(1e-4f));
        }

        /* ---------- double -> IntN<B8> ---------- */
        [TestCase(0.0, 0)]
        [TestCase(127.8, 127)]
        [TestCase(-128.5, -128)]
        [TestCase(128.2, -128)]
        [TestCase(-129.0, 127)]
        [TestCase(255.0, -1)]
        [TestCase(256.0, 0)]
        [TestCase(double.NaN, 0)]
        [TestCase(double.PositiveInfinity, 0)]
        [TestCase(double.NegativeInfinity, 0)]
        public void Explicit_Double_To_IntN_B8_Wrap(double src, int expected)
        {
            var n = (IntN<B8>)src;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        /* ---------- IntN<B8> -> double ---------- */
        [TestCase(-128)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(127)]
        public void Explicit_IntN_B8_To_Double_RoundTrip(int src)
        {
            double d = (double)new IntN<B8>(src);
            Assert.That(d, Is.EqualTo(src).Within(1e-4));
        }

        /* ---------- UIntN<B8> -> IntN<B8> & back ---------- */
        [TestCase(0u, 0)]
        [TestCase(128u, -128)]
        [TestCase(255u, -1)]
        public void Convert_UIntN_To_IntN_And_Back(uint raw, int signed)
        {
            var u = new UIntN<B8>(raw);
            var s = (IntN<B8>)u;            // UIntN → IntN
            Assert.That(s.Raw, Is.EqualTo(signed));

            var u2 = (UIntN<B8>)s;          // IntN → UIntN
            Assert.That(u2.Raw, Is.EqualTo(raw & 0xFF));
        }

        /* ---------- IntN<B8> <-> IntN<B16> ---------- */
        [TestCase(-130, 126)]   // wrap en B16
        [TestCase(127, 127)]
        public void Convert_IntN_B8_B16_RoundTrip(int src, int asU16)
        {
            var n8 = new IntN<B8>(src);
            var n16 = IntN<B8>.ConvertTo<B16>(n8);      // up-cast sans wrap
            Assert.That(n16.Raw, Is.EqualTo(asU16));

            var n8b = IntN<B16>.ConvertTo<B8>(n16);     // down-cast wrap
            Assert.That(n8b.Raw, Is.EqualTo(n8.Raw));
        }

        #endregion


        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMÉTIQUES (exhaustif, IntN<B8>) ---

        /* ========== 1. Addition (+ & Add) ========== */
        [TestCase(0, 0, 0)]
        [TestCase(1, -1, 0)]
        [TestCase(127, 1, -128)]   // wrap haut
        [TestCase(-128, -1, 127)]    // wrap bas
        [TestCase(60, 100, -96)]    // 160 → 0xA0 → -96
        [TestCase(255, 255, -2)]     // (wrap d’entrée) -1 + -1 = -2
        public void IntN_Add_Both(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x + y).Raw, Is.EqualTo(expected));
                Assert.That(IntN<B8>.Add(x, y).Raw, Is.EqualTo(expected));
            });
        }

        /* ========== 2. Soustraction (– & Sub) ========== */
        [TestCase(50, 20, 30)]
        [TestCase(0, 1, -1)]
        [TestCase(127, -1, -128)]
        [TestCase(-128, 1, 127)]
        [TestCase(-100, 100, 56)]
        public void IntN_Sub_Both(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x - y).Raw, Is.EqualTo(expected));
                Assert.That(IntN<B8>.Sub(x, y).Raw, Is.EqualTo(expected));
            });
        }

        /* ========== 3. Multiplication (* & Mul) ========== */
        [TestCase(0, 123, 0)]
        [TestCase(2, 3, 6)]
        [TestCase(10, 13, -126)]
        [TestCase(20, 13, 4)]
        [TestCase(50, 50, -60)]
        [TestCase(127, 2, -2)]
        [TestCase(-128, 2, 0)]
        [TestCase(-1, -1, 1)]
        public void IntN_Mul_Both(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x * y).Raw, Is.EqualTo(expected));
                Assert.That(IntN<B8>.Mul(x, y).Raw, Is.EqualTo(expected));
            });
        }

        /* ========== 4. Division (/ & Div) ========== */
        [TestCase(100, 4, 25)]
        [TestCase(-128, 2, -64)]
        [TestCase(127, 2, 63)]
        [TestCase(-127, 2, -63)]
        [TestCase(-128, -1, -128)]  // overflow “hardware-like”
        public void IntN_Div_Both(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x / y).Raw, Is.EqualTo(expected));
                Assert.That(IntN<B8>.Div(x, y).Raw, Is.EqualTo(expected));
            });
        }

        [Test]
        public void IntN_Div_By_Zero_Throws()
        {
            var v = new IntN<B8>(42);
            Assert.Throws<DivideByZeroException>(() => _ = v / IntN<B8>.Zero);
            Assert.Throws<DivideByZeroException>(() => _ = IntN<B8>.Div(v, IntN<B8>.Zero));
        }

        // ---------- Modulo (% & Mod) ----------
        [TestCase(13, 5, 3)]
        [TestCase(130, 3, 0)]     // 130 wrap -> -126,  -126 % 3 = 0
        [TestCase(-128, 3, -2)]
        [TestCase(127, 2, 1)]
        [TestCase(-1, 5, -1)]
        public void IntN_Mod_Both(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x % y).Raw, Is.EqualTo(expected));
                Assert.That(IntN<B8>.Mod(x, y).Raw, Is.EqualTo(expected));
            });
        }

        [Test]
        public void IntN_Mod_By_Zero_Throws()
        {
            var v = new IntN<B8>(99);
            Assert.Throws<DivideByZeroException>(() => _ = v % IntN<B8>.Zero);
            Assert.Throws<DivideByZeroException>(() => _ = IntN<B8>.Mod(v, IntN<B8>.Zero));
        }

        /* ========== 6. Incrément / Décrément ========== */
        [TestCase(127, -128)]
        [TestCase(0, 1)]
        [TestCase(-1, 0)]
        public void IntN_Increment_Wrap(int start, int expected)
        {
            var v = new IntN<B8>(start);
            v++;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        [TestCase(-128, 127)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        public void IntN_Decrement_Wrap(int start, int expected)
        {
            var v = new IntN<B8>(start);
            v--;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        #endregion


        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES POUR ARITHMETIQUE ---

        // ADDITION – covers wrap, overflow, min/max, extremes
        [TestCase(127, 1, -128)]        // Max + 1 → wrap (min)
        [TestCase(-128, -1, 127)]       // Min + -1 → wrap (max)
        [TestCase(127, 127, -2)]        // Max + Max → overflow
        [TestCase(-128, -128, 0)]       // Min + Min → overflow
        [TestCase(127, -128, -1)]       // Max + Min
        [TestCase(200, 200, -112)]      // Extreme wrap
        [TestCase(-100, -50, 106)]      // Underflow
        [TestCase(0, 0, 0)]
        [TestCase(1, -1, 0)]
        public void Add_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var sum = IntN<B8>.Add(x, y);
            Assert.That(sum.Raw, Is.EqualTo(expectedRaw));
        }

        // SUBTRACTION
        [TestCase(127, -1, -128)]       // Max - (-1) → wrap (min)
        [TestCase(-128, 1, 127)]        // Min - 1 → wrap (max)
        [TestCase(127, 127, 0)]
        [TestCase(-128, -128, 0)]
        [TestCase(0, 255, 1)]           // 0 - (-1) → 1 (255 wraps to -1)
        [TestCase(1, 255, 2)]
        [TestCase(0, 1, -1)]
        [TestCase(-128, 127, 1)]
        [TestCase(120, 200, -80)]
        public void Sub_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var sub = IntN<B8>.Sub(x, y);
            Assert.That(sub.Raw, Is.EqualTo(expectedRaw));
        }

        // MULTIPLICATION
        [TestCase(127, 2, -2)]          // Max * 2
        [TestCase(-128, 2, 0)]          // Min * 2
        [TestCase(127, 127, 1)]         // Max * Max
        [TestCase(-128, -128, 0)]       // Min * Min
        [TestCase(127, -128, -128)]        // Max * Min
        [TestCase(13, 20, 4)]           // overflow
        [TestCase(-7, 18, -126)]
        [TestCase(-5, 20, -100)]
        [TestCase(50, 3, 150)]          // No overflow (150 in 8 bits signed)
        public void Mul_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var mul = IntN<B8>.Mul(x, y);
            // Le constructeur wrappe automatiquement, donc expectedRaw doit être la valeur 8 bits
            Assert.That(mul.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        // DIVISION (attention : division par -1 de -128 = undefined en C, ici on wrap)
        [TestCase(127, 1, 127)]
        [TestCase(127, -1, -127)]
        [TestCase(-128, -1, -128)]      // overflow possible en C (ici wrappe à -128)
        [TestCase(-128, 1, -128)]
        [TestCase(127, 127, 1)]
        [TestCase(-128, 127, -1)]
        [TestCase(0, 5, 0)]
        public void Div_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var div = IntN<B8>.Div(x, y);
            Assert.That(div.Raw, Is.EqualTo(expectedRaw));
        }

        // MODULO (même signe que le numérateur)
        [TestCase(127, 2, 1)]
        [TestCase(-128, 2, 0)]
        [TestCase(127, 127, 0)]
        [TestCase(-128, 127, -1)]
        [TestCase(15, 4, 3)]
        [TestCase(-15, 4, -3)]
        [TestCase(15, -4, 3)]
        [TestCase(-15, -4, -3)]
        public void Mod_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var mod = IntN<B8>.Mod(x, y);
            Assert.That(mod.Raw, Is.EqualTo(expectedRaw));
        }

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/

        #region --- PUISSANCE DE 2 (SHIFT SAFE)  (exhaustif, IntN<B8>) ---
        /* ───── 1. MulPow2 : val << n (wrap 8 bits) ───── */
        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(1, 7, -128)]
        [TestCase(2, 7, 0)]
        [TestCase(64, 1, -128)]
        [TestCase(64, 2, 0)]
        [TestCase(127, 1, -2)]
        [TestCase(-128, 1, 0)]
        [TestCase(-1, 3, -8)]
        public void MulPow2_IntN_8bits(int value, int n, int expectedRaw)
        {
            var x = new IntN<B8>(value);
            var res = IntN<B8>.MulPow2(x, n);
            Assert.That(res.Raw, Is.EqualTo(expectedRaw));
        }

        /* exceptions MulPow2 : n < 0 ou n ≥ 8 */
        [TestCase(1, 8)]
        [TestCase(1, -1)]
        [TestCase(1, 32)]
        public void MulPow2_IntN_8bits_OutOfRange(int value, int n)
        {
            var x = new IntN<B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => IntN<B8>.MulPow2(x, n));
        }

        /* ───── 2. DivPow2 : val >> n ───── */
        [TestCase(0, 0, 0)]
        [TestCase(64, 0, 64)]
        [TestCase(64, 1, 32)]
        [TestCase(-128, 1, -64)]
        [TestCase(127, 7, 0)]
        [TestCase(-128, 7, -1)]
        [TestCase(-1, 1, -1)]
        public void DivPow2_IntN_8bits(int value, int n, int expectedRaw)
        {
            var x = new IntN<B8>(value);
            var res = IntN<B8>.DivPow2(x, n);
            Assert.That(res.Raw, Is.EqualTo(expectedRaw));
        }

        /* exceptions DivPow2 : n < 0 ou n ≥ 8 */
        [TestCase(64, 8)]
        [TestCase(-128, 8)]
        [TestCase(5, -1)]
        [TestCase(5, 32)]
        public void DivPow2_IntN_8bits_OutOfRange(int value, int n)
        {
            var x = new IntN<B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => IntN<B8>.DivPow2(x, n));
        }

        /* ───── 3. ModPow2 : val & (2^n – 1) ───── */
        [TestCase(255, 1, 1)]     // 0b11111111 & 0b01
        [TestCase(255, 2, 3)]
        [TestCase(255, 8, -1)]
        [TestCase(127, 4, 15)]
        [TestCase(-1, 8, -1)]
        [TestCase(-128, 3, 0)]
        [TestCase(0, 8, 0)]
        [TestCase(123, 0, 0)]
        public void ModPow2_IntN_8bits(int value, int n, int expectedRaw)
        {
            var x = new IntN<B8>(value);
            var res = IntN<B8>.ModPow2(x, n);
            Assert.That(res.Raw, Is.EqualTo(expectedRaw));
        }

        /* exceptions ModPow2 : n < 0 ou n > 8 */
        [TestCase(5, -1)]
        [TestCase(5, 9)]
        [TestCase(5, 33)]
        public void ModPow2_IntN_8bits_OutOfRange(int value, int n)
        {
            var x = new IntN<B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => IntN<B8>.ModPow2(x, n));
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
        #region --- OPERATION BITWISE (exhaustif, IntN<B8>) ---

        // AND (&)
        [TestCase(0b11110000, 0b10101010, 0b10100000)]
        [TestCase(0b11111111, 0b00000000, 0)]
        [TestCase(-1, 0, 0)]               // -1 == 0xFF
        [TestCase(-1, -1, -1)]             // all ones
        public void And_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var result = x & y;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        // OR (|)
        [TestCase(0b11000011, 0b10101010, 0b11101011)]
        [TestCase(-1, 0, -1)]
        [TestCase(0, 0, 0)]
        public void Or_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var result = x | y;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        // XOR (^)
        [TestCase(0b11001100, 0b10101010, 0b01100110)]
        [TestCase(-1, 0, -1)]
        [TestCase(0, 0, 0)]
        [TestCase(-1, -1, 0)]
        public void Xor_IntN_8bits(int a, int b, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            var result = x ^ y;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        // NOT (~)
        [TestCase(0, -1)]
        [TestCase(-1, 0)]
        [TestCase(0b00001111, -16)]
        public void Not_IntN_8bits(int a, int expectedRaw)
        {
            var x = new IntN<B8>(a);
            var result = ~x;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        // SHIFT LEFT (<<)
        [TestCase(0b00000001, 1, 0b00000010)]
        [TestCase(0b01000000, 2, 0b00000000)]      // 0x40 << 2 = 0x100, wrap 8 bits = 0
        [TestCase(-1, 1, -2)]                      // 0xFF << 1 = 0x1FE, wrap = 0xFE = -2
        [TestCase(1, 7, -128)]                     // 1 << 7 = 128 → -128
        public void Shl_IntN_8bits(int value, int n, int expectedRaw)
        {
            var x = new IntN<B8>(value);
            var result = x << n;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        [TestCase(1, 8)]
        public void Shl_IntN_8bits_OutOfRange_Throws(int value, int n)
        {
            var x = new IntN<B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = x << n; });
        }

        // SHIFT RIGHT (>>) – arithmétique rétro-faithful (signe propagé)
        [TestCase(0b10000000, 1, -64)]     // -128 >> 1 = -64
        [TestCase(-1, 1, -1)]              // -1 >> 1 = -1
        [TestCase(0b01000000, 2, 16)]      // 0x40 >> 2 = 0x10 = 16
        [TestCase(-128, 7, -1)]            // -128 >> 7 = -1
        [TestCase(127, 7, 0)]              // 127 >> 7 = 0
        public void Shr_IntN_8bits(int value, int n, int expectedRaw)
        {
            var x = new IntN<B8>(value);
            var result = x >> n;
            Assert.That(result.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        [TestCase(1, 8)]
        [TestCase(-1, 8)]
        public void Shr_IntN_8bits_OutOfRange_Throws(int value, int n)
        {
            var x = new IntN<B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = x >> n; });
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
        #region --- METHODES STATIQUES BITWISE (exhaustif, IntN<B8>) ---

        /* ─────── 1. AND (alias) ─────── */
        [TestCase(0xFF, 0x00, 0)]        // 0x00
        [TestCase(0xF0, 0xAA, -96)]        // 0xA0 → -96
        [TestCase(0x0F, 0x33, 3)]        // 0x03
        [TestCase(-1, -1, -1)]
        public void And_IntN_8bits_alias(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);

            Assert.Multiple(() =>
            {
                Assert.That(IntN<B8>.And(x, y).Raw, Is.EqualTo(expected));
                Assert.That((x & y).Raw, Is.EqualTo(expected));
            });
        }

        /* ─────── 2. OR (alias) ─────── */
        [TestCase(0xC3, 0xAA, -21)]        // 0xEB → -21
        [TestCase(0x00, 0x00, 0)]
        [TestCase(0xCC, 0x33, -1)]        // 0xFF → -1
        [TestCase(-1, 0, -1)]
        public void Or_IntN_8bits_alias(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);

            Assert.Multiple(() =>
            {
                Assert.That(IntN<B8>.Or(x, y).Raw, Is.EqualTo(expected));
                Assert.That((x | y).Raw, Is.EqualTo(expected));
            });
        }

        /* ─────── 3. XOR (alias) ─────── */
        [TestCase(0xCC, 0xAA, 102)]       // 0x66
        [TestCase(0xFF, 0xFF, 0)]
        [TestCase(-1, 0, -1)]
        [TestCase(0x55, 0xAA, -1)]        // 0xFF → -1
        public void Xor_IntN_8bits_alias(int a, int b, int expected)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);

            Assert.Multiple(() =>
            {
                Assert.That(IntN<B8>.Xor(x, y).Raw, Is.EqualTo(expected));
                Assert.That((x ^ y).Raw, Is.EqualTo(expected));
            });
        }

        /* ─────── 4. NOT (alias) ─────── */
        [TestCase(0x00, -1)]              // 0xFF → -1
        [TestCase(0x0F, -16)]              // 0xF0 → -16
        [TestCase(-1, 0)]
        public void Not_IntN_8bits_alias(int value, int expected)
        {
            var x = new IntN<B8>(value);
            Assert.That(IntN<B8>.Not(x).Raw, Is.EqualTo(expected));
            Assert.That((~x).Raw, Is.EqualTo(expected));
        }

        /* ─────── 5. NAND / NOR / XNOR ─────── */
        [TestCase(0xF0, 0xAA, 95)]        // ~(A & B) = 0x5F
        [TestCase(-1, -1, 0)]
        public void Nand_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.Nand(new IntN<B8>(a), new IntN<B8>(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(0xC3, 0xAA, 20)]        // ~(A | B) = 0x14
        [TestCase(0x00, 0x00, -1)]        // ~(0x00) = 0xFF → -1
        public void Nor_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.Nor(new IntN<B8>(a), new IntN<B8>(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(0xCC, 0xAA, -103)]       // ~(A ^ B) = 0x99 → -103
        [TestCase(-1, -1, -1)]
        public void Xnor_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.Xnor(new IntN<B8>(a), new IntN<B8>(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ─────── 6. SHIFT LEFT (alias) ─────── */
        [TestCase(0x01, 0, 1)]
        [TestCase(0x01, 1, 2)]
        [TestCase(0x40, 2, 0)]
        [TestCase(-1, 3, -8)]            // 0xF8 → -8
        [TestCase(1, 7, -128)]
        public void Shl_IntN_8bits_alias(int val, int n, int expected)
        {
            var x = new IntN<B8>(val);
            Assert.That(IntN<B8>.Shl(x, n).Raw, Is.EqualTo(expected));
            Assert.That((x << n).Raw, Is.EqualTo(expected));
        }

        [TestCase(1, 8)]
        [TestCase(1, -1)]
        public void Shl_IntN_8bits_OutOfRange(int val, int n)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _ = IntN<B8>.Shl(new IntN<B8>(val), n));
        }

        /* ─────── 7. SHIFT RIGHT arithmétique (alias) ─────── */
        [TestCase(-128, 1, -64)]
        [TestCase(-1, 1, -1)]
        [TestCase(0x40, 2, 16)]
        [TestCase(127, 7, 0)]
        public void Shr_IntN_8bits_alias(int val, int n, int expected)
        {
            var x = new IntN<B8>(val);
            Assert.That(IntN<B8>.Shr(x, n).Raw, Is.EqualTo(expected));
            Assert.That((x >> n).Raw, Is.EqualTo(expected));
        }

        [TestCase(1, 8)]
        public void Shr_IntN_8bits_OutOfRange(int val, int n)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _ = IntN<B8>.Shr(new IntN<B8>(val), n));
        }

        /* ─────── SHIFT LEFT LOGICAL (alias) ─────── */
        [TestCase(0x01, 0, 0x01)]
        [TestCase(0x01, 1, 0x02)]
        [TestCase(0x40, 2, 0x00)]
        [TestCase(-1, 3, -8)]      // 0xF8
        [TestCase(1, 7, -128)]
        public void ShlLogical_IntN_8bits(int val, int n, int expected)
        {
            var x = new IntN<B8>(val);
            Assert.That(IntN<B8>.ShlLogical(x, n).Raw, Is.EqualTo(expected));
            // optionnel : parité avec Shl
            Assert.That(IntN<B8>.Shl(x, n).Raw, Is.EqualTo(expected));
        }

        /* ─────── 8. SHIFT RIGHT LOGICAL (alias) ─────── */
        [TestCase(-1, 1, 0x7F)]
        [TestCase(0x80, 1, 0x40)]
        [TestCase(0xFF, 7, 0x01)]
        public void ShrLogical_IntN_8bits(int val, int n, int expected)
        {
            var x = new IntN<B8>(val);
            Assert.That(IntN<B8>.ShrLogical(x, n).Raw, Is.EqualTo(expected));
        }

        /* --- SHIFT RIGHT LOGICAL (alias) : cas out-of-range --- */
        [TestCase(1, 8)]          // n == Bits  → doit throw
        public void ShrLogical_IntN_8bits_OutOfRange(int val, int n)
        {
            var x = new IntN<B8>(val);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _ = IntN<B8>.ShrLogical(x, n));
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
        * IsNeg
        * IsPos
        ==================================*/
        #region --- COMPARAISONS (exhaustif, IntN<B8>) ---

        // ───────────── Égalité  == / != / Eq / Neq ─────────────
        [TestCase(0, 0, true)]
        [TestCase(256, 0, true)]     // wrap identique
        [TestCase(-129, 127, true)]    // wrap identique
        [TestCase(127, -128, false)]
        [TestCase(-1, 1, false)]
        public void Equality_IntN_8bits(int a, int b, bool areEqual)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That(x == y, Is.EqualTo(areEqual));
                Assert.That(x != y, Is.EqualTo(!areEqual));
                Assert.That(IntN<B8>.Eq(x, y), Is.EqualTo(areEqual));
                Assert.That(IntN<B8>.Neq(x, y), Is.EqualTo(!areEqual));
            });
        }

        // ───────────── Ordre  < ≤ > ≥ / Lt Lte Gt Gte ─────────────
        [TestCase(-128, -1, true, true, false, false)]
        [TestCase(-1, 0, true, true, false, false)]
        [TestCase(0, 1, true, true, false, false)]
        [TestCase(1, 127, true, true, false, false)]
        [TestCase(127, -128, false, false, true, true)]
        // -129 wrap → 127, donc 127 > 0
        [TestCase(-129, 0, false, false, true, true)]
        public void Order_IntN_8bits(int a, int b,
                                      bool isLt, bool isLte,
                                      bool isGt, bool isGte)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That(x < y, Is.EqualTo(isLt));
                Assert.That(x <= y, Is.EqualTo(isLte));
                Assert.That(x > y, Is.EqualTo(isGt));
                Assert.That(x >= y, Is.EqualTo(isGte));

                Assert.That(IntN<B8>.Lt(x, y), Is.EqualTo(isLt));
                Assert.That(IntN<B8>.Lte(x, y), Is.EqualTo(isLte));
                Assert.That(IntN<B8>.Gt(x, y), Is.EqualTo(isGt));
                Assert.That(IntN<B8>.Gte(x, y), Is.EqualTo(isGte));
            });
        }

        // ───────────── IsZero / IsNeg / IsPos ─────────────
        [TestCase(0, true)]
        [TestCase(256, true)]   // wrap → 0
        [TestCase(1, false)]
        [TestCase(-1, false)]
        public void IsZero_IntN_8bits(int val, bool expected)
            => Assert.That(IntN<B8>.IsZero(new(val)), Is.EqualTo(expected));

        [TestCase(-1, true)]
        [TestCase(-128, true)]
        [TestCase(127, false)]
        [TestCase(0, false)]
        public void IsNeg_IntN_8bits(int val, bool expected)
            => Assert.That(IntN<B8>.IsNeg(new(val)), Is.EqualTo(expected));

        [TestCase(1, true)]
        [TestCase(127, true)]
        [TestCase(-128, false)]
        [TestCase(0, false)]
        public void IsPos_IntN_8bits(int val, bool expected)
            => Assert.That(IntN<B8>.IsPos(new(val)), Is.EqualTo(expected));

        // ───────────── Equals(object) ─────────────
        [Test]
        public void Equals_Object_IntN_8bits()
        {
            var a = new IntN<B8>(42);
            object same = new IntN<B8>(42);
            object other = new IntN<B8>(-42);
            object notNum = "string";

            Assert.That(a.Equals(same), Is.True);
            Assert.That(a.Equals(other), Is.False);
            Assert.That(a.Equals(null), Is.False);
            Assert.That(a.Equals(notNum), Is.False);
        }

        // ───────────── GetHashCode ─────────────
        [Test]
        public void HashCode_Consistency_IntN_8bits()
        {
            var x1 = new IntN<B8>(99);
            var x2 = new IntN<B8>(99);
            var y = new IntN<B8>(-99);

            Assert.That(x1.GetHashCode(), Is.EqualTo(x2.GetHashCode()));
            Assert.That(x1.GetHashCode(), Is.Not.EqualTo(y.GetHashCode()));
        }

        #endregion

        /*==================================
         * --- OPERATIONS UTILITAIRES ---
         * Min
         * Max
         * Avg
         * Sign
         * Abs
         * Neg
         * CopySign
         ==================================*/
        #region --- OPERATIONS UTILITAIRES (exhaustif, IntN<B8>) ---

        /* ────────── Min / Max ────────── */
        [TestCase(-128, 127, -128, 127)]
        [TestCase(127, -128, -128, 127)]   // ordre inversé
        [TestCase(42, 42, 42, 42)]   // égalité
        [TestCase(256, 0, 0, 0)]   // wrap 256→0
        [TestCase(-129, -128, -128, 127)]  // -129→127
        public void Min_Max_IntN(int a, int b, int expectedMin, int expectedMax)
        {
            var x = new IntN<B8>(a);
            var y = new IntN<B8>(b);
            Assert.That(IntN<B8>.Min(x, y).Raw, Is.EqualTo(expectedMin));
            Assert.That(IntN<B8>.Max(x, y).Raw, Is.EqualTo(expectedMax));
        }

        /* ────────── Avg (troncature vers zéro) ────────── */
        [TestCase(0, 0, 0)]
        [TestCase(10, 20, 15)]
        [TestCase(5, 6, 5)]   // 11/2 → 5
        [TestCase(-10, -20, -15)]
        [TestCase(-3, 3, 0)]
        [TestCase(250, 250, -6)]    // 250 wrap→-6, somme -12, /2 = -6 → rewrap -6
        [TestCase(127, 127, 127)]
        [TestCase(-128, -128, -128)]
        public void Avg_IntN(int a, int b, int expected)
        {
            var r1 = IntN<B8>.Avg(new(a), new(b));
            var r2 = IntN<B8>.Avg(new(b), new(a));   // commutativité
            Assert.That(r1.Raw, Is.EqualTo(expected));
            Assert.That(r2.Raw, Is.EqualTo(expected));
        }

        /* ────────── Sign / Abs / Neg ────────── */
        [TestCase(0, 0)]
        [TestCase(42, 1)]
        [TestCase(-128, -1)]
        [TestCase(-1, -1)]
        public void Sign_IntN(int v, int expected)
            => Assert.That(IntN<B8>.Sign(new(v)), Is.EqualTo(expected));

        [TestCase(0, 0)]
        [TestCase(42, 42)]
        [TestCase(-42, 42)]
        [TestCase(-128, -128)]   // cas particulier
        public void Abs_IntN(int v, int expected)
            => Assert.That(IntN<B8>.Abs(new(v)).Raw, Is.EqualTo(expected));

        [TestCase(0, 0)]
        [TestCase(42, -42)]
        [TestCase(-42, 42)]
        [TestCase(-128, -128)]   // −(−128) wrap
        public void Neg_IntN(int v, int expected)
            => Assert.That(IntN<B8>.Neg(new(v)).Raw, Is.EqualTo(expected));

        /* ────────── CopySign(value, sign) ────────── */
        [TestCase(42, 1, 42)]
        [TestCase(42, -1, -42)]
        [TestCase(-42, 1, 42)]
        [TestCase(-42, -1, -42)]
        [TestCase(-128, 1, -128)]   // abs(-128) reste -128
        public void CopySign_IntN(int value, int sign, int expected)
        {
            var res = IntN<B8>.CopySign(new(value), new(sign));
            Assert.That(res.Raw, Is.EqualTo(expected));
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
        #region --- SATURATION & CLAMP (exhaustif, IntN<B8>) ---

        /* ───────────── AddSat ───────────── */
        [TestCase(127, 1, 127)]   // overflow +
        [TestCase(-128, -1, -128)]   // overflow –
        [TestCase(100, 30, 127)]
        [TestCase(-100, -50, -128)]
        [TestCase(50, 20, 70)]
        [TestCase(0, 0, 0)]
        [TestCase(120, -10, 110)]
        [TestCase(-120, 10, -110)]
        // ajouts
        [TestCase(70, 60, 127)]   // 130 → sat
        [TestCase(-90, -60, -128)]   // -150 → sat
        [TestCase(60, -70, -10)]   // pas saturé
        [TestCase(-5, 10, 5)]
        public void AddSat_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.AddSat(new(a), new(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ───────────── SubSat ───────────── */
        [TestCase(-128, 1, -128)]
        [TestCase(127, -1, 127)]
        [TestCase(-100, 50, -128)]
        [TestCase(100, -50, 127)]
        [TestCase(50, 20, 30)]
        [TestCase(0, 0, 0)]
        [TestCase(-120, -5, -115)]
        [TestCase(120, 5, 115)]
        // ajouts
        [TestCase(127, -100, 127)]   // overflow +
        [TestCase(-128, 100, -128)]   // overflow –
        [TestCase(100, 200, 127)]   // 100-200 = -100 → sat
        [TestCase(-100, -200, -128)]   // pas saturé
        public void SubSat_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.SubSat(new(a), new(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ───────────── MulSat ───────────── */
        [TestCase(127, 2, 127)]
        [TestCase(-128, 2, -128)]
        [TestCase(127, -2, -128)]
        [TestCase(-128, -2, 127)]
        [TestCase(13, 10, 127)]
        [TestCase(-13, 10, -128)]
        [TestCase(5, 4, 20)]
        [TestCase(0, 10, 0)]
        [TestCase(1, 77, 77)]
        [TestCase(-1, 77, -77)]
        [TestCase(-1, -1, 1)]
        // ajouts
        [TestCase(64, 3, 127)]   // 192 → sat +
        [TestCase(-64, 3, -128)]   // -192 → sat –
        [TestCase(11, 11, 121)]   // pas saturé
        [TestCase(-11, -11, 121)]   // pas saturé
        [TestCase(32, 4, 127)]   // 128 → sat +
        public void MulSat_IntN_8bits(int a, int b, int expected)
        {
            var res = IntN<B8>.MulSat(new(a), new(b));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ───────────── Clamp(min,max) ───────────── */
        [TestCase(0, -10, 10, 0)]
        [TestCase(-20, -10, 10, -10)]
        [TestCase(20, -10, 10, 10)]
        [TestCase(-10, -10, 10, -10)]
        [TestCase(10, -10, 10, 10)]
        // ajouts
        [TestCase(50, 60, 100, 60)]   // sous-borne
        [TestCase(-56, -30, 30, -30)]   // wrap 200→-56  → sous-borne
        public void Clamp_IntN_8bits(int v, int min, int max, int expected)
        {
            var res = IntN<B8>.Clamp(new(v), new(min), new(max));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ───────────── Clamp01 ───────────── */
        [TestCase(-5, 0)]
        [TestCase(2, 1)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        // ajout : wrap négatif
        [TestCase(-129, 1)]   // -129→127  >1 ⇒ clamp à 1
        public void Clamp01_IntN_8bits(int v, int expected)
        {
            var res = IntN<B8>.Clamp01(new(v));
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ───────────── ClampWithOffset ───────────── */
        [TestCase(0, -10, 10, 2, 2, 0)]  // [-8,12]
        [TestCase(-20, -10, 10, 3, -3, -7)]  // [-7,7]
        [TestCase(20, -10, 10, -5, 8, 18)]  // [-15,18]
        [TestCase(5, -5, 5, 1, 2, 5)]  // [-4,7]
        [TestCase(-200, -100, -50, -100, -10, -60)]
        [TestCase(200, 50, 100, 10, 200, 60)]
        // ajouts
        [TestCase(60, 10, 50, 0, 0, 50)]  // au-dessus
        [TestCase(-60, -50, -10, 0, 0, -50)]  // au-dessous
        public void ClampWithOffset_IntN_8bits(int value, int min, int max,
                                               int offMin, int offMax, int expected)
        {
            var res = IntN<B8>.ClampWithOffset(new(value),
                                               new(min), new(max),
                                               offMin, offMax);
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
        #region --- FONCTIONS TRIGONOMÉTRIQUES (exhaustif, IntN<B8>) ---

        [TestCase(0, 0)]
        [TestCase(64, 127)]
        [TestCase(128, 0)]
        [TestCase(192, -128)]
        public void Sin_IntN_B8_Approx(int angle, int expected)
        {
            var x = new IntN<B8>(angle);
            var result = IntN<B8>.Sin(x);
            // À affiner: comparer à la valeur attendue selon LUT/approx
            Assert.That(result.Raw, Is.EqualTo(expected).Within(1)); // tolerance selon ton LUT
        }

        [TestCase(0, 127)]     // Cos(0°) = +1.0 => 127
        [TestCase(64, 0)]      // Cos(90°) = 0.0 => 0
        [TestCase(128, -128)]  // Cos(180°) = -1.0 => -128
        public void Cos_IntN_B8_Approx(int angle, int expected)
        {
            var x = new IntN<B8>(angle);
            var result = IntN<B8>.Cos(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(1));
        }

        [TestCase(0, 0)]
        [TestCase(32, 127)] // Tan(45°) = 1
        [TestCase(-32, -124)]
        public void Tan_IntN_B8_Approx(int angle, int expected)
        {
            var x = new IntN<B8>(angle);
            var result = IntN<B8>.Tan(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(3));
        }

        [TestCase(-128, -128)] 
        [TestCase(-64, -42)] 
        [TestCase(0, 0)] 
        [TestCase(64, 42)] 
        [TestCase(127, 127)] 
        public void Asin_IntN_B8_Approx(int val, int expected)
        {
            var x = new IntN<B8>(val);
            var result = IntN<B8>.Asin(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(11),
                $"asin({val}) attendu≈{expected}, obtenu={result.Raw}");
        }

        [Test]
        public void Asin_IntN_B8_RegressionTable()
        {
            // Génère et vérifie tous les attendus "by code" sur la plage [-128, +127]
            for (int raw = -128; raw <= 127; ++raw)
            {
                var x = new IntN<B8>(raw);
                int expected = IntN<B8>.Asin(x).Raw; // valeur de référence (by code)
                int actual = IntN<B8>.Asin(x).Raw; // doit toujours matcher
                Assert.That(actual, Is.EqualTo(expected),
                    $"asin({raw}) : attendu={expected}, obtenu={actual}");
            }
        }

        [TestCase(127, 0)]   // cos=+1  -> acos=0°   -> 0
        [TestCase(0, 64)]   // cos=0   -> acos=90°  -> 64
        [TestCase(-128, -128)]   // cos=-1  -> acos=180° -> -128
        public void Acos_IntN_B8_Approx(int val, int expected)
        {
            var x = new IntN<B8>(val);
            var result = IntN<B8>.Acos(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(11),
                $"acos({val}) attendu≈{expected}, obtenu={result.Raw}");
        }

        [Test]
        public void Acos_IntN_B8_RegressionTable()
        {
            // Génère et vérifie tous les attendus "by code" sur la plage [-128, +127]
            for (int raw = -128; raw <= 127; ++raw)
            {
                var x = new IntN<B8>(raw);
                int expected = IntN<B8>.Acos(x).Raw; // valeur de référence (by code)
                int actual = IntN<B8>.Acos(x).Raw;   // doit toujours matcher
                Assert.That(actual, Is.EqualTo(expected),
                    $"acos({raw}) : attendu={expected}, obtenu={actual}");
            }
        }

        [TestCase(0, 0)]    // atan(0)    = 0°    -> 0
        [TestCase(1, 0)]    // ~0         ≈ 0°    -> 0
        [TestCase(64, 38)]    // 0.5        ≈ 26.565° -> round(26.565*127/90)=38
        [TestCase(127, 64)]    // 1.0        = 45°     -> 64
        [TestCase(-64, -38)]    // -0.5       = -26.565°-> -38
        [TestCase(-127, -64)]    // -1.0       = -45°    -> -64
        public void Atan_IntN_B8_Approx(int val, int expected)
        {
            var x = new IntN<B8>(val);
            var result = IntN<B8>.Atan(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(1));
        }

        [TestCase(0, 1, 0)]   // 0°
        [TestCase(1, 0, 64)]   // +90°
        [TestCase(0, -1, -128)]   // 180° (wrap)
        [TestCase(-1, 0, -64)]   // -90°
        [TestCase(1, 1, 32)]   // 45°
        [TestCase(1, -1, 96)]   // 135°
        [TestCase(-1, -1, -96)]   // -135°
        [TestCase(-1, 1, -32)]   // -45°
        public void Atan2_IntN_B8_Approx(int y, int x, int expected)
        {
            var a = new IntN<B8>(y);
            var b = new IntN<B8>(x);
            var result = IntN<B8>.Atan2(a, b);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(1));
        }

        #endregion

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
        #region --- MANIPULATION BITS & ROTATIONS (exhaustif, IntN<B8>) ---

        /* ───────── Reverse ───────── */
        [TestCase(0x01, -128)]
        [TestCase(0xF0, 0x0F)]
        [TestCase(0xAA, 0x55)]
        [TestCase(0x00, 0x00)]
        [TestCase(0xFF, -1)]
        [TestCase(0x3C, 0x3C)]         // palindrome 0b00111100
        public void Reverse_IntN(int v, int expected)
        {
            var r = IntN<B8>.Reverse(new(v));
            Assert.That(r.Raw, Is.EqualTo(expected));
        }

        /* ───────── PopCount ───────── */
        [TestCase(0x00, 0)]
        [TestCase(0x01, 1)]
        [TestCase(0xFF, 8)]
        [TestCase(0xAA, 4)]
        [TestCase(0xF0, 4)]
        [TestCase(0x80, 1)]
        [TestCase(0x3C, 4)]
        public void PopCount_IntN(int v, int expected)
            => Assert.That(IntN<B8>.PopCount(new(v)), Is.EqualTo(expected));

        /* ───────── Parity (odd = true) ───────── */
        [TestCase(0x00, false)]
        [TestCase(0x01, true)]
        [TestCase(0xFF, false)]
        [TestCase(0xAA, false)]
        [TestCase(0xAB, true)]
        public void Parity_IntN(int v, bool expected)
            => Assert.That(IntN<B8>.Parity(new(v)), Is.EqualTo(expected));

        /* ───────── Leading / Trailing Zeros ───────── */
        [TestCase(0x00, 8, 8)]   // LZ, TZ
        [TestCase(0x01, 7, 0)]
        [TestCase(0x80, 0, 7)]
        [TestCase(0x0F, 4, 0)]
        [TestCase(0xF0, 0, 4)]
        [TestCase(0xFF, 0, 0)]
        public void Lz_Tz_IntN(int v, int lz, int tz)
        {
            var x = new IntN<B8>(v);
            Assert.That(IntN<B8>.LeadingZeros(x), Is.EqualTo(lz));
            Assert.That(IntN<B8>.TrailingZeros(x), Is.EqualTo(tz));
        }

        /* ───────── Rol (rotate-left) ───────── */
        [TestCase(0x01, 1, 0x02)]
        [TestCase(0x80, 1, 0x01)]
        [TestCase(0xF0, 4, 0x0F)]
        [TestCase(0xAA, 8, -86)]   // 0xAA = 170 → -86 en signé
        [TestCase(0x01, 9, 0x02)]   // n>7
        [TestCase(0x01, -1, -128)]   // neg = Ror
        public void Rol_IntN(int v, int n, int expected)
            => Assert.That(IntN<B8>.Rol(new(v), n).Raw, Is.EqualTo(expected));

        /* ───────── Ror (rotate-right) ───────── */
        [TestCase(0x01, 1, -128)]
        [TestCase(0x80, 1, 0x40)]
        [TestCase(0xF0, 4, 0x0F)]
        [TestCase(0xAA, 8, -86)]
        [TestCase(0x80, 9, 0x40)]
        [TestCase(0x80, -1, 0x01)]   // neg = Rol
        public void Ror_IntN(int v, int n, int expected)
            => Assert.That(IntN<B8>.Ror(new(v), n).Raw, Is.EqualTo(expected));

        /* ───────── Bsr / Bsf ───────── */
        [TestCase(0x00, -1, -1)]   // (value, msbIdx, lsbIdx)
        [TestCase(0x01, 0, 0)]
        [TestCase(0x80, 7, 7)]
        [TestCase(0x20, 5, 5)]
        [TestCase(0xF0, 7, 4)]
        public void Bsr_Bsf_IntN(int v, int msb, int lsb)
        {
            var x = new IntN<B8>(v);
            Assert.That(IntN<B8>.Bsr(x), Is.EqualTo(msb));
            Assert.That(IntN<B8>.Bsf(x), Is.EqualTo(lsb));
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
         ==================================*/
        #region --- CONSTANTES (exhaustif, IntN<B8>) ---

        [Test]
        public void CoreConstants_IntN_8bits()
        {
            Assert.Multiple(() =>
            {
                Assert.That(IntN<B8>.BitsConst, Is.EqualTo(8));
                Assert.That(IntN<B8>.ShiftConst, Is.EqualTo(24));
                Assert.That(IntN<B8>.MaskConst, Is.EqualTo(0xFFu));

                Assert.That(IntN<B8>.Zero.Raw, Is.EqualTo(0));
                Assert.That(IntN<B8>.One.Raw, Is.EqualTo(1));
                Assert.That(IntN<B8>.Half.Raw, Is.EqualTo(-128)); // 0x80
                Assert.That(IntN<B8>.AllOnes.Raw, Is.EqualTo(-1));   // 0xFF
                Assert.That(IntN<B8>.Msb.Raw, Is.EqualTo(-128)); // bit 7
            });
        }

        [Test]
        public void Const_Msb_IntN_8bits()
        {
            // MSB sur 8 bits signé, c’est -128 (0b1000_0000)
            Assert.That(IntN<B8>.Msb.Raw, Is.EqualTo(-128));
            // MSB doit être identique à Bit(7)
            Assert.That(IntN<B8>.Bit(7).Raw, Is.EqualTo(IntN<B8>.Msb.Raw));
        }

        [Test]
        public void Const_Lsb()
        {
            Assert.That(IntN<B8>.Lsb.Raw, Is.EqualTo(1));
            Assert.That(IntN<B8>.Bit(0).Raw, Is.EqualTo(IntN<B8>.Lsb.Raw));
        }

        /* ---------- Bit(n) individuel ---------- */
        [TestCase(0, 0x01)]
        [TestCase(1, 0x02)]
        [TestCase(2, 0x04)]
        [TestCase(3, 0x08)]
        [TestCase(4, 0x10)]
        [TestCase(5, 0x20)]
        [TestCase(6, 0x40)]
        [TestCase(7, -128)]          // 0x80 -> -128 signé
        public void Bit_Single_IntN_8bits(int n, int expected)
            => Assert.That(IntN<B8>.Bit(n).Raw, Is.EqualTo(expected));

        /* ---------- OR de tous les bits = AllOnes ---------- */
        [Test]
        public void Bit_AllOr_Equals_AllOnes()
        {
            int acc = 0;
            for (int i = 0; i < 8; i++)
                acc |= IntN<B8>.Bit(i).Raw & 0xFF;
            Assert.That(acc, Is.EqualTo(IntN<B8>.AllOnes.Raw & 0xFF));
        }

        /* ---------- Bit(n) out-of-range ---------- */
        [TestCase(-1)]
        [TestCase(8)]
        public void Bit_OutOfRange_Throws(int n)
            => Assert.Throws<ArgumentOutOfRangeException>(() => IntN<B8>.Bit(n));

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

        #region --- ACCÈS OCTETS (exhaustif, IntN<B8>) ---

        /* ---------- Byte(x,n) ---------- */
        [TestCase(0xFF, 0, 0xFF)]
        [TestCase(0xAB, 0, 0xAB)]
        [TestCase(0x00, 0, 0x00)]
        public void Byte_Static_IntN_8bits(int v, int n, int expected)
        {
            var x = new IntN<B8>(v);
            Assert.That(IntN<B8>.Byte(x, n), Is.EqualTo((byte)expected));
        }

        /* ---------- ToBytes ---------- */
        [TestCase(0x00, new byte[] { 0x00 })]
        [TestCase(0xFF, new byte[] { 0xFF })]
        [TestCase(0x7F, new byte[] { 0x7F })]
        [TestCase(-1, new byte[] { 0xFF })]   // valeur négative
        public void ToBytes_IntN_8bits(int v, byte[] expected)
        {
            Assert.That(new IntN<B8>(v).ToBytes(), Is.EqualTo(expected));
        }

        /* ---------- FromBytes ---------- */
        [TestCase(new byte[] { 0x00 }, 0)]
        [TestCase(new byte[] { 0xFF }, -1)]
        [TestCase(new byte[] { 0x80 }, -128)]
        public void FromBytes_IntN_8bits(byte[] bytes, int expected)
        {
            var res = IntN<B8>.FromBytes(bytes);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        /* ---------- FromBytes : tableau vide → ArgumentException ---------- */
        [Test]
        public void FromBytes_Empty_Throws()
        {
            Assert.Throws<ArgumentException>(() => IntN<B8>.FromBytes(Array.Empty<byte>()));
        }

        /* ---------- FromBytes : ≥1 octet → utilise l’octet 0 ---------- */
        [TestCase(new byte[] { 0x01, 0x02 }, 0x01)]   // 0x01
        [TestCase(new byte[] { 0xFF, 0xAA }, -1)]      // 0xFF → -1
        public void FromBytes_Uses_First_Byte(byte[] bytes, int expectedRaw)
        {
            var res = IntN<B8>.FromBytes(bytes);
            Assert.That(res.Raw, Is.EqualTo(new IntN<B8>(expectedRaw).Raw));
        }

        /* ---------- GetByte ---------- */
        [TestCase(0xAB, 0, 0xAB)]
        [TestCase(-1, 0, 0xFF)]
        [TestCase(0, 0, 0x00)]
        public void GetByte_IntN_8bits(int v, int idx, int expected)
        {
            Assert.That(new IntN<B8>(v).GetByte(idx), Is.EqualTo((byte)expected));
        }

        /* ---------- SetByte (immutabilité + résultat) ---------- */
        [TestCase(0x00, 0, 0xFF, -1)]
        [TestCase(-86, 0, 0x55, 0x55)]
        [TestCase(-1, 0, 0x00, 0)]
        public void SetByte_IntN_8bits(int v, int idx, int b, int expected)
        {
            var x = new IntN<B8>(v);
            var res = x.SetByte(idx, (byte)b);
            Assert.Multiple(() =>
            {
                Assert.That(res.Raw, Is.EqualTo(expected));
                Assert.That(x.Raw, Is.EqualTo(v));     // x inchangé
            });
        }

        /* ---------- ReplaceByte (source IntN) ---------- */
        [TestCase(-86, 0, 0x55, 85)]   // 0xAA (-86) devient 0x55 (85)
        [TestCase(0x00, 0, 0xFF, -1)]     // 0x00 devient 0xFF (-1)
        public void ReplaceByte_Source_IntN(int v, int idx, int src, int expected)
        {
            var x = new IntN<B8>(v);
            var s = new IntN<B8>(src);
            var r = x.ReplaceByte(idx, s);
            Assert.Multiple(() =>
            {
                Assert.That(r.Raw, Is.EqualTo(expected));
                Assert.That(x.Raw, Is.EqualTo(v));      // x inchangé
            });
        }

        /* ---------- ReplaceByte (source byte) ---------- */
        [TestCase(0x12, 0, 0x34, 0x34)]
        [TestCase(0x00, 0, 0xAB, -85)]   // 0xAB → -85
        public void ReplaceByte_Source_Byte(int v, int idx, int b, int expected)
        {
            var x = new IntN<B8>(v);
            var r = x.ReplaceByte(idx, (byte)b);
            Assert.Multiple(() =>
            {
                Assert.That(r.Raw, Is.EqualTo(expected));
                Assert.That(x.Raw, Is.EqualTo(v));
            });
        }

        /* ---------- Round-trip : ToBytes → FromBytes ---------- */
        [TestCase(0x00)]
        [TestCase(0x7F)]
        [TestCase(-1)]
        [TestCase(-128)]
        public void Byte_RoundTrip_IntN_8bits(int v)
        {
            var original = new IntN<B8>(v);
            var rebuilt = IntN<B8>.FromBytes(original.ToBytes());
            Assert.That(rebuilt.Raw, Is.EqualTo(original.Raw));
        }

        /* ---------- Index hors-plage : Byte/Get/Set/Replace ---------- */
        [TestCase(-1)]
        [TestCase(1)]
        public void Byte_Idx_OutOfRange_Throws(int n)
        {
            var x = new IntN<B8>(42);
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => IntN<B8>.Byte(x, n));
                Assert.Throws<ArgumentOutOfRangeException>(() => x.GetByte(n));
                Assert.Throws<ArgumentOutOfRangeException>(() => x.SetByte(n, 0xAA));
                Assert.Throws<ArgumentOutOfRangeException>(() => x.ReplaceByte(n, new IntN<B8>(1)));
                Assert.Throws<ArgumentOutOfRangeException>(() => x.ReplaceByte(n, 0x11));
            });
        }

        #endregion

        /*==================================
         * --- CONVERSION EN CHAÎNE (STRING) ---
         * ToString
         * DebugString
         * ToBinaryString
         * ToHexString
         ==================================*/
        #region --- CONVERSION EN CHAÎNE (exhaustif, IntN<B8>) ---

        /* ---------- ToString() ---------- */
        [TestCase(0, "0")]
        [TestCase(42, "42")]
        [TestCase(-1, "-1")]
        [TestCase(-128, "-128")]
        [TestCase(300, "44")]     // 300 wrap → 44
        [TestCase(-129, "127")]     // -129 wrap → 127
        public void ToString_IntN_8bits(int v, string expected)
        {
            var x = new IntN<B8>(v);
            Assert.That(x.ToString(), Is.EqualTo(expected));
        }

        /* ---------- DebugString() ---------- */
        [TestCase(42)]
        [TestCase(-1)]
        [TestCase(-128)]
        public void DebugString_IntN_8bits(int v)
        {
            var x = new IntN<B8>(v);
            var dbg = x.DebugString();

            Assert.Multiple(() =>
            {
                Assert.That(dbg, Does.Contain("IntN<B8>("));
                Assert.That(dbg, Does.Contain($"({x.ToString()})")); // valeur entre ( )
                Assert.That(dbg, Does.Contain("bin="));
                Assert.That(dbg, Does.Contain("hex="));
            });
        }

        /* ---------- ToBinaryString() : exactement 8 bits ---------- */
        [TestCase(0, "00000000")]
        [TestCase(1, "00000001")]
        [TestCase(255, "11111111")]
        [TestCase(-1, "11111111")]
        [TestCase(-128, "10000000")]
        [TestCase(127, "01111111")]
        [TestCase(170, "10101010")]   // 0xAA → -86 mais motif binaire conservé
        public void ToBinaryString_IntN_8bits(int v, string expected)
        {
            var s = new IntN<B8>(v).ToBinaryString();
            Assert.That(s, Is.EqualTo(expected));
            Assert.That(s.Length, Is.EqualTo(IntN<B8>.BitsConst));   // toujours 8
        }

        /* ---------- ToHexString() : 2 hex digits, préfixe optionnel ---------- */
        [TestCase(0, false, "00")]
        [TestCase(1, false, "01")]
        [TestCase(255, false, "FF")]
        [TestCase(-1, false, "FF")]
        [TestCase(-128, false, "80")]
        [TestCase(127, false, "7F")]
        [TestCase(0, true, "0x00")]
        [TestCase(255, true, "0xFF")]
        [TestCase(-128, true, "0x80")]
        [TestCase(300, false, "2C")]     // 300 wrap → 0x2C
        public void ToHexString_IntN_8bits(int v, bool prefix, string expected)
        {
            var s = new IntN<B8>(v).ToHexString(prefix);
            Assert.That(s, Is.EqualTo(expected));

            // Les deux chiffres hexadécimaux doivent être en majuscules
            string hexPart = prefix ? s.Substring(2) : s;
            Assert.That(hexPart, Is.EqualTo(hexPart.ToUpperInvariant()));
        }

        /* ---------- Immuabilité : ToXxx() ne doit pas changer l’objet ---------- */
        [Test]
        public void StringConversions_DoNotMutate()
        {
            var x = new IntN<B8>(170);           // 0xAA → Raw -86
            _ = x.ToString();
            _ = x.ToBinaryString();
            _ = x.ToHexString(true);
            _ = x.DebugString();

            Assert.That(x.Raw, Is.EqualTo(new IntN<B8>(170).Raw));   // reste -86
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
        [TestCase("0", 0)]
        [TestCase("127", 127)]
        [TestCase("-1", -1)]
        [TestCase("-128", -128)]
        [TestCase("255", -1)]    // wrap: 255 = 0xFF -> -1 (pour B8, signed)
        [TestCase("128", -128)] // wrap: 128 = 0x80 -> -128 (pour B8, signed)
        public void Parse_Decimal_Valid(string s, int expected)
        {
            var val = IntN<B8>.Parse(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(IntN<B8>.TryParse(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Décimal erreurs ---
        [TestCase("")]
        [TestCase("abc")]
        [TestCase("99999999999999999")]
        [TestCase("-99999999999999999")]
        public void Parse_Decimal_Invalid(string s)
        {
            Assert.Throws(Is.TypeOf<FormatException>().Or.TypeOf<OverflowException>(), () => IntN<B8>.Parse(s));
            Assert.That(IntN<B8>.TryParse(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Decimal_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => IntN<B8>.Parse(null));
            Assert.That(IntN<B8>.TryParse(null, out var _), Is.False);
        }

        // --- Hexadécimal ---
        [TestCase("0x0", 0)]
        [TestCase("0x7F", 127)]
        [TestCase("0xFF", -1)]      // 0xFF = -1 en signed 8-bit
        [TestCase("FF", -1)]
        [TestCase("80", -128)]      // 0x80 = -128 en signed 8-bit
        [TestCase("0x00", 0)]
        public void Parse_Hex_Valid(string s, int expected)
        {
            var val = IntN<B8>.ParseHex(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(IntN<B8>.TryParseHex(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("0x")]
        [TestCase("xyz")]
        public void Parse_Hex_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => IntN<B8>.ParseHex(s));
            Assert.That(IntN<B8>.TryParseHex(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Hex_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => IntN<B8>.ParseHex(null));
            Assert.That(IntN<B8>.TryParseHex(null, out var _), Is.False);
        }

        // --- Binaire ---
        [TestCase("0b0", 0)]
        [TestCase("0b01111111", 127)]
        [TestCase("0b11111111", -1)]       // 0xFF = -1 en signed 8-bit
        [TestCase("01111111", 127)]
        [TestCase("11111111", -1)]
        [TestCase("10000000", -128)]
        public void Parse_Binary_Valid(string s, int expected)
        {
            var val = IntN<B8>.ParseBinary(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(IntN<B8>.TryParseBinary(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Binaire erreurs ---
        [TestCase("")]
        [TestCase("0b")]
        [TestCase("21001100")]   // chiffre non-binaire
        public void Parse_Binary_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => IntN<B8>.ParseBinary(s));
            Assert.That(IntN<B8>.TryParseBinary(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Binary_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => IntN<B8>.ParseBinary(null));
            Assert.That(IntN<B8>.TryParseBinary(null, out var _), Is.False);
        }

        // --- Round-trip JSON natif ---
        [TestCase(0)]
        [TestCase(127)]
        [TestCase(-1)]
        [TestCase(-128)]
        public void ToJson_RoundTrip(int src)
        {
            var a = new IntN<B8>(src);
            string json = a.ToJson();
            var b = IntN<B8>.FromJson(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- FromJson mixte (décimal, hex, binaire) ---
        [TestCase("127", 127)]
        [TestCase("0x7F", 127)]
        [TestCase("0b01111111", 127)]
        [TestCase("-1", -1)]
        [TestCase("255", -1)]
        [TestCase("0xFF", -1)]
        [TestCase("0b11111111", -1)]
        public void FromJson_Mixte(string s, int expected)
        {
            var x = IntN<B8>.FromJson(s);
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
            Assert.Throws<FormatException>(() => IntN<B8>.FromJson(s));
        }

        [Test]
        public void FromJson_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => IntN<B8>.FromJson(null));
        }

        // --- Debug: les "0b" ne passent jamais ---
        [Test]
        public void TryParse_Debug_0b()
        {
            Assert.That(IntN<B8>.TryParse("0b", out var _), Is.False, "TryParse doit retourner false pour '0b'");
            Assert.That(IntN<B8>.TryParseHex("0b", out var _), Is.False, "TryParseHex doit retourner false pour '0b'");
            Assert.That(IntN<B8>.TryParseBinary("0b", out var _), Is.False, "TryParseBinary doit retourner false pour '0b'");
        }

        #endregion


        /*==================================
         * --- SERIALISATION META ---
         * ToJsonWithMeta
         * FromJsonWithMeta
         ==================================*/
        #region --- SERIALISATION META (exhaustif, multi-N, erreurs) ---

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(127)]
        [TestCase(-128)]
        public void ToJsonWithMeta_RoundTrip_B8(int raw)
        {
            var a = new IntN<B8>(raw);
            string json = a.ToJsonWithMeta();
            var b = IntN<B8>.FromJsonWithMeta<B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(32767)]
        [TestCase(-32768)]
        public void ToJsonWithMeta_RoundTrip_B16(int raw)
        {
            var a = new IntN<B16>(raw);
            string json = a.ToJsonWithMeta();
            var b = IntN<B16>.FromJsonWithMeta<B16>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Erreur : meta bits non concordants ---
        [Test]
        public void FromJsonWithMeta_BitsMismatch_Throws()
        {
            var a = new IntN<B8>(123);
            string json = a.ToJsonWithMeta();
            Assert.Throws<FormatException>(() => IntN<B16>.FromJsonWithMeta<B16>(json));
        }

        // --- Tests malformés, string vide, mauvais champs, etc. ---
        [TestCase("{ \"raw\": 123 }")]    // bits manquant
        [TestCase("{ \"bits\": 8 }")]    // raw manquant
        [TestCase("{ \"bits\": 8, \"raw\": \"oops\" }")]
        [TestCase("{ \"bits\": \"oops\", \"raw\": 12 }")]
        [TestCase("{}")]
        [TestCase("")]
        public void FromJsonWithMeta_InvalidJson_Throws(string json)
        {
            Assert.Throws<FormatException>(() => IntN<B8>.FromJsonWithMeta<B8>(json));
        }

        // --- Test spécifique pour null ---
        [Test]
        public void FromJsonWithMeta_Null_Throws()
        {
            Assert.Throws<FormatException>(() => IntN<B8>.FromJsonWithMeta<B8>(null));
        }

        #endregion

    }

}
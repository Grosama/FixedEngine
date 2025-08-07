using FixedEngine.Math;
using NUnit.Framework;
using System.Diagnostics;

namespace FixedEngine.Tests.Math
{
    [TestFixture]
    public class FixedTests
    {
        /*==================================
         * --- CONSTRUCTOR ---
         ==================================*/
        #region --- CONSTRUCTOR (exhaustif, Fixed<B16, B8>) ---

        // Construction à partir d'un entier signé (int), valeur brute
        [TestCase(0, 0)]
        [TestCase(42, 42)]
        [TestCase(-1, -1)]
        [TestCase(0x7FFF, 0x7FFF)]    // plus grand positif sur 16 bits
        [TestCase(unchecked((int)0xFFFF8000), unchecked((int)0xFFFF8000))] // plus petit négatif sur 16 bits (int.MinValue >> 0)
        public void Constructor_FromRaw_Int(int raw, int expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            Assert.That(fx.Raw, Is.EqualTo(expected));
        }

        // Construction à partir d'un IntN<T>
        [TestCase(0, 0)]
        [TestCase(3, 3 << 8)]     // 3 -> 3*256
        [TestCase(-5, -5 << 8)]   // -5 -> -1280
        public void Constructor_From_IntN(int intVal, int expectedRaw)
        {
            var n = new IntN<B16>(intVal);
            var fx = new Fixed<B16, B8>(n);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // Construction à partir d'un float (tests valeurs positives, négatives, décimales, arrondi, overflow, sous-flux)
        [TestCase(2.5f, 640)]         // 2.5 * 256 = 640
        [TestCase(-1.5f, -384)]       // -1.5 * 256 = -384
        [TestCase(0.0f, 0)]
        [TestCase(0.00390625f, 1)]    // 1/256
        [TestCase(-0.00390625f, -1)]  // -1/256
        [TestCase(255.0f, -256)]     
        [TestCase(-128.0f, -32768)]   // -128*256 = -32768
        [TestCase(32767f, -256)] // overflow sur grand float
        public void Constructor_From_Float(float val, int expectedRaw)
        {
            var fx = new Fixed<B16, B8>(val);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // Construction à partir d'un double
        [TestCase(7.125, 1824)]         // 7.125 * 256 = 1824
        [TestCase(-3.5, -896)]          // -3.5 * 256 = -896
        [TestCase(0.0, 0)]
        [TestCase(0.00390625, 1)]       // 1/256
        [TestCase(-0.00390625, -1)]     // -1/256
        [TestCase(255.0, -256)]
        [TestCase(-128.0, -32768)]
        [TestCase(32767.0, -256)]
        [TestCase(double.MaxValue, 0)]      // overflow, wrap hardware (valeur imprévisible mais souvent 0 ou saturée)
        [TestCase(double.MinValue, 0)]      // sous-flux, wrap hardware
        public void Constructor_From_Double(double val, int expectedRaw)
        {
            var fx = new Fixed<B16, B8>(val);
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        #endregion


        /*==================================
         * --- CONVERSIONS EXPLICITES ---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES (exhaustif, Fixed<B16, B8>) ---

        // --- int -> Fixed ---
        [TestCase(0, 0)]
        [TestCase(1, 256)]
        [TestCase(-1, -256)]
        [TestCase(128, -32768)]         // 128 << 8 = 32768, wrap 16 bits signed = -32768
        [TestCase(-128, -32768)]        // -128 << 8 = -32768
        [TestCase(32767, -256)]         // overflow wrap
        [TestCase(int.MinValue, 0)]     // -2^31 << 8 wrap = 0 (en pratique)
        public void Explicit_Int_To_Fixed(int val, int expectedRaw)
        {
            var fx = (Fixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- uint -> Fixed (signed wrap) ---
        [TestCase(0u, 0)]
        [TestCase(1u, 256)]
        [TestCase(255u, -256)]          // 255 << 8 = 65280, wrap = -256
        [TestCase(32768u, 0)]           // 32768 << 8 = 8388608, wrap = 0
        [TestCase(65535u, -256)]        // 65535 << 8 = 16776960, wrap = -256
        [TestCase(uint.MaxValue, -256)] // full wrap
        public void Explicit_UInt_To_Fixed_Wrap(uint val, int expectedRaw)
        {
            var fx = (Fixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- IntN -> Fixed ---
        [TestCase(0, 0)]
        [TestCase(42, 10752)]           // 42 << 8
        [TestCase(-1, -256)]
        [TestCase(127, 32512)]          // 127 << 8
        [TestCase(-128, -32768)]        // -128 << 8
        public void Explicit_IntN_To_Fixed(int raw, int expectedRaw)
        {
            var n = new IntN<B16>(raw);
            var fx = (Fixed<B16, B8>)n;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- UIntN -> Fixed ---
        [TestCase(0u, 0)]
        [TestCase(77u, 19712)]           // 77 << 8
        [TestCase(255u, -256)]           // 255 << 8 wrap signed
        [TestCase(128u, -32768)]         // 128 << 8 = 32768 wrap signed
        public void Explicit_UIntN_To_Fixed(uint raw, int expectedRaw)
        {
            var n = new UIntN<B16>(raw);
            var fx = (Fixed<B16, B8>)n;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- float -> Fixed ---
        [TestCase(0.0f, 0)]
        [TestCase(1.0f, 256)]
        [TestCase(-1.0f, -256)]
        [TestCase(255.5f, -128)]      // 255.5 * 256 = 65508, wrap = -128
        [TestCase(-127.5f, -32640)]   // -127.5 * 256 = -32640
        [TestCase(32767f, -256)]      // 32767*256 = 8388352, wrap = -256
        [TestCase(-128.0f, -32768)]
        [TestCase(float.MaxValue, 0)] // overflow float: wrap 0
        [TestCase(float.MinValue, 0)] // underflow float: wrap 0
        public void Explicit_Float_To_Fixed(float val, int expectedRaw)
        {
            var fx = (Fixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- double -> Fixed ---
        [TestCase(0.0, 0)]
        [TestCase(1.0, 256)]
        [TestCase(-2.0, -512)]
        [TestCase(255.0, -256)]        
        [TestCase(255.999, 0)]        
        [TestCase(-128.0, -32768)]
        [TestCase(32767.0, -256)]      // wrap
        [TestCase(double.MaxValue, 0)] // overflow: wrap
        [TestCase(double.MinValue, 0)] // underflow: wrap
        public void Explicit_Double_To_Fixed(double val, int expectedRaw)
        {
            var fx = (Fixed<B16, B8>)val;
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Fixed -> int (troncature, signe préservé) ---
        [TestCase(0, 0)]
        [TestCase(256, 1)]
        [TestCase(-256, -1)]
        [TestCase(32768, -128)]        // wrap
        [TestCase(-32768, -128)]       // -32768 >> 8 = -128
        public void Explicit_Fixed_To_Int(int raw, int expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            int val = (int)fx;
            Assert.That(val, Is.EqualTo(expected));
        }

        // --- Fixed -> float/double ---
        [TestCase(512, 2.0f)]
        [TestCase(0, 0.0f)]
        [TestCase(-256, -1.0f)]
        [TestCase(32512, 127.0f)]
        public void Explicit_Fixed_To_Float(int raw, float expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            float val = (float)fx;
            Assert.That(val, Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(512, 2.0d)]
        [TestCase(0, 0.0d)]
        [TestCase(-256, -1.0d)]
        [TestCase(32512, 127.0d)]
        public void Explicit_Fixed_To_Double(int raw, double expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            double val = (double)fx;
            Assert.That(val, Is.EqualTo(expected).Within(0.0001d));
        }

        // --- Fixed -> IntN/UIntN ---
        [TestCase(512, 2)]
        [TestCase(0, 0)]
        [TestCase(-256, -1)]
        [TestCase(32512, 127)]
        public void Explicit_Fixed_To_IntN(int raw, int expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            var n = (IntN<B16>)fx;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        [TestCase(512, 2u)]
        [TestCase(0, 0u)]
        [TestCase(65280, 255u)]
        [TestCase(-256, 255u)]
        public void Explicit_Fixed_To_UIntN(int raw, uint expected)
        {
            var fx = new Fixed<B16, B8>(raw);
            var n = (UIntN<B16>)fx;
            Assert.That(n.Raw, Is.EqualTo(expected));
        }

        // --- Fixed <-> Fixed : conversion de fractionnaire ---
        [Test]
        public void Fixed_ConvertFrac_BitFaithful()
        {
            var fx = new Fixed<B16, B8>(12345);

            // Q8.8 -> Q8.4 : shift right (OK)
            var fx4 = Fixed<B16, B8>.ConvertFrac<B4>(fx);
            Assert.That(fx4.Raw, Is.EqualTo(12345 >> 4));

            // Q8.8 -> Q8.16 : shift left puis tronque à 16 bits !
            var fx16 = Fixed<B16, B8>.ConvertFrac<B16>(fx);
            Assert.That(fx16.Raw, Is.EqualTo(12345 << 8 & 0xFFFF));
        }

        // --- UFixed -> Fixed (wrap signed hardware) ---
        [TestCase(0u, 0)]
        [TestCase(65408u, -128)]
        [TestCase(65535u, -1)]
        [TestCase(128u, 128)]
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
        #region --- OPERATEURS ARITHMETIQUES (exhaustif, Fixed<B16, B8>) ---

        // --- Addition ---
        [TestCase(256, 256, 512)]           // 1.0 + 1.0 = 2.0
        [TestCase(32767, 1, -32768)]        // overflow : max+1 -> min
        [TestCase(-32768, -1, 32767)]       // underflow : min-1 -> max
        [TestCase(-32768, 32767, -1)]       // min+max
        [TestCase(0, 0, 0)]                 // zero+zero
        [TestCase(32767, 32767, -2)]        // max+max
        public void Operator_Add(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a + b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Soustraction ---
        [TestCase(256, 128, 128)]           // 1.0 - 0.5 = 0.5
        [TestCase(-32768, 1, 32767)]        // min-1 -> max
        [TestCase(32767, -1, -32768)]       // max-(-1) = min
        [TestCase(0, 0, 0)]
        [TestCase(1000, 1000, 0)]           // any value - itself = 0
        [TestCase(32767, 32767, 0)]
        public void Operator_Sub(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a - b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Multiplication ---
        [TestCase(256, 256, 256)]           // 1.0 * 1.0 = 1.0
        [TestCase(256, -256, -256)]         // 1.0 * -1.0 = -1.0
        [TestCase(32767, 2, 255)]           // max * 2 (bit test)
        [TestCase(-32768, 2, -256)]            // min * 2
        [TestCase(128, 2, 1)]               // 0.5 * 2 = 1 (bit test)
        [TestCase(0, 32767, 0)]             // zero * max
        [TestCase(32767, 32767, -256)]         // max * max (Q8.8)
        [TestCase(-32768, -1, 128)]           // min * -1 = 0 (Q8.8)
        public void Operator_Mul(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a * b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Division ---
        [TestCase(256, 256, 256)]           // 1.0 / 1.0 = 1.0
        [TestCase(-256, 256, -256)]         // -1.0 / 1.0 = -1.0
        [TestCase(256, -256, -256)]         // 1.0 / -1.0 = -1.0
        [TestCase(32767, 256, 32767)]       // max / 1.0
        [TestCase(-32768, 256, -32768)]     // min / 1.0
        [TestCase(0, 32767, 0)]             // zero / max
        public void Operator_Div(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a / b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Division par zéro ---
        [Test]
        public void Operator_Div_ByZero_Throws()
        {
            var a = new Fixed<B16, B8>(256);
            var b = new Fixed<B16, B8>(0);
            Assert.Throws<DivideByZeroException>(() => { var res = a / b; });
        }

        // --- Modulo ---
        [TestCase(512, 256, 0)]             // 2.0 % 1.0 = 0
        [TestCase(384, 256, 128)]           // 1.5 % 1.0 = 0.5 (128)
        [TestCase(-384, 256, -128)]         // -1.5 % 1.0 = -0.5 (-128)
        [TestCase(32767, 256, 255)]         // max % 1.0
        [TestCase(-32768, 256, 0)]          // min % 1.0
        [TestCase(0, 256, 0)]               // zero % 1.0
        public void Operator_Mod(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a % b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Incrémentation ++ ---
        [TestCase(32767, -32768)]           // overflow max
        [TestCase(-1, 0)]                   // -1 + 1 = 0
        [TestCase(0, 1)]                    // 0 + 1 = 1
        [TestCase(-32768, -32767)]          // min + 1 = -32767
        public void Operator_Inc(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = ++a;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Décrémentation -- ---
        [TestCase(-32768, 32767)]           // underflow min
        [TestCase(1, 0)]                    // 1 - 1 = 0
        [TestCase(0, -1)]                   // 0 - 1 = -1
        [TestCase(32767, 32766)]            // max - 1 = max-1
        public void Operator_Dec(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = --a;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- METHODES STATIQUES ARITHMETIQUE (exhaustif, Fixed<B16, B8>) ---

        // --- Add ---
        [TestCase(256, 256, 512)]             // 1.0 + 1.0 = 2.0
        [TestCase(32767, 1, -32768)]          // overflow : max+1 = min
        [TestCase(-32768, -1, 32767)]         // underflow : min-1 = max
        [TestCase(-32768, 32767, -1)]         // min+max = -1
        [TestCase(0, 0, 0)]                   // zero+zero
        [TestCase(32767, 32767, -2)]          // max+max = -2
        public void Static_Add(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Add(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Sub ---
        [TestCase(256, 128, 128)]             // 1.0 - 0.5 = 0.5
        [TestCase(-32768, 1, 32767)]          // min-1 = max
        [TestCase(32767, -1, -32768)]         // max-(-1) = min
        [TestCase(0, 0, 0)]
        [TestCase(1000, 1000, 0)]             // v-v = 0
        [TestCase(32767, 32767, 0)]
        public void Static_Sub(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Sub(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Mul ---
        [TestCase(256, 256, 256)]             // 1.0 * 1.0 = 1.0
        [TestCase(256, -256, -256)]           // 1.0 * -1.0 = -1.0
        [TestCase(32767, 2, 255)]             // max * 2
        [TestCase(-32768, 2, -256)]           // min * 2 = -256
        [TestCase(-32768, -1, 128)]           // min * -1 = 128 (Q8.8)
        [TestCase(0, 32767, 0)]               // 0 * max = 0
        [TestCase(32767, 32767, -256)]        // max * max = -256
        public void Static_Mul(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Mul(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Div ---
        [TestCase(256, 256, 256)]             // 1.0 / 1.0 = 1.0
        [TestCase(-256, 256, -256)]           // -1.0 / 1.0 = -1.0
        [TestCase(256, -256, -256)]           // 1.0 / -1.0 = -1.0
        [TestCase(32767, 256, 32767)]         // max / 1.0
        [TestCase(-32768, 256, -32768)]       // min / 1.0
        [TestCase(0, 32767, 0)]               // zero / max = 0
        public void Static_Div(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Div(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- Mod ---
        [TestCase(512, 256, 0)]               // 2.0 % 1.0 = 0
        [TestCase(384, 256, 128)]             // 1.5 % 1.0 = 0.5 (128)
        [TestCase(-384, 256, -128)]           // -1.5 % 1.0 = -0.5 (-128)
        [TestCase(32767, 256, 255)]           // max % 1.0 = 255
        [TestCase(-32768, 256, 0)]            // min % 1.0 = 0
        [TestCase(0, 256, 0)]                 // zero % 1.0 = 0
        public void Static_Mod(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Mod(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE, exhaustif, Fixed<B16,B8>) ---

        // --- MulPow2 : multiplie par 2^n, wrap hardware ---
        [TestCase(1, 0, 1)]             // 1 * 2^0 = 1
        [TestCase(1, 1, 2)]             // 1 * 2 = 2
        [TestCase(1, 8, 256)]           // 1 * 256 = 256 (1.0 Q8.8)
        [TestCase(128, 1, 256)]         // 0.5 * 2 = 1.0
        [TestCase(256, 8, 0)]           // 1.0 * 256 = 0x0100 << 8 = 0x010000 & 0xFFFF = 0
        [TestCase(-256, 1, -512)]       // -1.0 * 2 = -2.0
        [TestCase(32767, 1, -2)]        // max << 1 = 0xFFFE & 0xFFFF = -2
        [TestCase(-32768, 1, 0)]        // min << 1 = 0x8000 << 1 = 0x0000 (Q8.8 wrap)
        [TestCase(32767, 8, -256)]      // max << 8 = overflow
        [TestCase(-1, 4, -16)]          // -1 << 4 = 0xFFF0 (signed)
        public void Static_MulPow2(int raw, int shift, int expectedRaw)
        {
            var fx = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.MulPow2(fx, shift);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        [Test]
        public void Static_MulPow2_ShiftTooBig_Throws()
        {
            var fx = new Fixed<B16, B8>(256);
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.MulPow2(fx, 32));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.MulPow2(fx, -1));
        }

        // --- DivPow2 : divise par 2^n, shift arithmétique (signé) ---
        [TestCase(256, 0, 256)]         // 1.0 / 1 = 1.0
        [TestCase(256, 1, 128)]         // 1.0 / 2 = 0.5
        [TestCase(512, 2, 128)]         // 2.0 / 4 = 0.5
        [TestCase(-256, 1, -128)]       // -1.0 / 2 = -0.5
        [TestCase(32767, 8, 127)]       // max / 256 = 127
        [TestCase(-32768, 8, -128)]     // min / 256 = -128
        [TestCase(1, 1, 0)]             // 1 / 2 = 0 (troncature)
        [TestCase(-1, 1, -1)]           // -1 / 2 = -1 (arithmétique signé)
        [TestCase(-32768, 15, -1)]      // min / 32768 = -1
        public void Static_DivPow2(int raw, int shift, int expectedRaw)
        {
            var fx = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.DivPow2(fx, shift);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        [Test]
        public void Static_DivPow2_ShiftTooBig_Throws()
        {
            var fx = new Fixed<B16, B8>(256);
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.DivPow2(fx, 32));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.DivPow2(fx, -1));
        }

        // --- ModPow2 : AND bitmask (hardware) ---
        [TestCase(0xFFFF, 4, 0x000F)]       // 0xFFFF & 0x000F = 0x000F
        [TestCase(0xFF00, 8, 0x00)]         // 0xFF00 & 0x00FF = 0
        [TestCase(0x1234, 12, 0x0234)]      // 0x1234 & 0x0FFF = 0x0234
        [TestCase(256, 8, 0)]               // 1.0 & 0x00FF = 0
        [TestCase(-1, 16, -1)]              // -1 & 0xFFFF = 0xFFFF = -1 (int16)
        [TestCase(0x8000, 4, 0x0000)]       // 0x8000 & 0x000F = 0
        [TestCase(-32768, 16, -32768)]      // min & 0xFFFF = 0x8000 = -32768
        [TestCase(0, 1, 0)]                 // 0 & 1 = 0
        [TestCase(12345, 2, 1)]             // 12345 & 3 = 1
        public void Static_ModPow2(int raw, int n, int expectedRaw)
        {
            var fx = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.ModPow2(fx, n);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        [Test]
        public void Static_ModPow2_ShiftTooBig_Throws()
        {
            var fx = new Fixed<B16, B8>(256);
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.ModPow2(fx, 33));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.ModPow2(fx, -1));
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
        #region --- OPERATION BITWISE (exhaustif, Fixed<B16, B8>) ---

        // --- AND ---
        [TestCase(0xFFFF, 0x00FF, 0x00FF)]   // all ones & lower 8 bits
        [TestCase(0xF0F0, 0x0F0F, 0x0000)]
        [TestCase(0x1234, 0x00FF, 0x0034)]
        [TestCase(0x0000, 0xFFFF, 0x0000)]
        [TestCase(-1, 0x8000, -32768)]       // -1 (0xFFFF) & MSB only
        public void Operator_And(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a & b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- OR ---
        [TestCase(0xF0F0, 0x0F0F, -1)]       // full mask (0xFFFF)
        [TestCase(0x1234, 0x00FF, 0x12FF)]
        [TestCase(0x0000, 0xFFFF, -1)]       // 0xFFFF = -1
        [TestCase(0x8000, 0x7FFF, -1)]       // 0x8000 | 0x7FFF = 0xFFFF = -1
        [TestCase(0x8000, 0, -32768)]
        [TestCase(0, 0, 0)]
        public void Operator_Or(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a | b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- XOR ---
        [TestCase(0xF0F0, 0x0F0F, -1)]       // 0xFFFF = -1
        [TestCase(0xFFFF, 0xFFFF, 0x0000)]
        [TestCase(0x1234, 0x00FF, 0x12CB)]
        [TestCase(0x8000, 0x7FFF, -1)]
        [TestCase(0x8000, 0, -32768)]
        public void Operator_Xor(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = a ^ b;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- NOT ---
        [TestCase(0xFFFF, 0x0000)]
        [TestCase(0x0000, -1)]
        [TestCase(0x1234, -4661)]           // ~0x1234 = 0xEDCB = -4661
        [TestCase(0x8000, 0x7FFF)]
        [TestCase(-1, 0)]
        public void Operator_Not(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = ~a;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // --- SHIFT LEFT (<<) ---
        [TestCase(0x0001, 1, 0x0002)]
        [TestCase(0x0100, 4, 0x1000)]
        [TestCase(0x7FFF, 1, -2)]           // 0xFFFE signed == -2
        [TestCase(0x8000, 1, 0x0000)]       // overflow, sign bit out
        [TestCase(-1, 1, -2)]               // 0xFFFF << 1 = 0xFFFE = -2
        [TestCase(0x0001, 15, -32768)]      // lowest bit to highest bit
        public void Operator_Shl(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = a << n;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Cas hors borne (négatif ou trop grand)
        [TestCase(1, -1)]
        [TestCase(1, 16)]
        [TestCase(1, 32)]
        public void Shl_Fixed16_OutOfRange_Throws(int value, int n)
        {
            var x = new Fixed<B16, B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = x << n; });
        }

        // --- SHIFT RIGHT (>>) ---
        [TestCase(0x8000, 1, -16384)]       // -32768 >> 1 = -16384 (arithmétique signé)
        [TestCase(0x7FFF, 1, 0x3FFF)]       // 32767 >> 1 = 16383
        [TestCase(0x0100, 4, 0x0010)]       // 256 >> 4 = 16
        [TestCase(0x0001, 1, 0x0000)]
        [TestCase(-1, 1, -1)]               // -1 >> 1 = -1 (sign extend)
        [TestCase(0x8000, 15, -1)]          // MSB down to LSB, signed fill
        public void Operator_Shr(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = a >> n;
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Cas hors borne (négatif ou trop grand)
        [TestCase(1, -1)]
        [TestCase(1, 16)]
        [TestCase(1, 32)]
        public void Shr_Fixed16_OutOfRange_Throws(int value, int n)
        {
            var x = new Fixed<B16, B8>(value);
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
        #region --- METHODE STATIQUE BITWISE (alias, exhaustif, Fixed<B16,B8>) ---

        // And
        [TestCase(0xFFFF, 0x00FF, 0x00FF)]
        [TestCase(0xF0F0, 0x0F0F, 0x0000)]
        [TestCase(-1, 0x8000, -32768)]        // 0xFFFF & 0x8000 = 0x8000 = -32768
        [TestCase(0x1234, 0x0000, 0x0000)]
        public void Static_And(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.And(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Or
        [TestCase(0x0000, 0xFFFF, -1)]     // 0xFFFF = -1
        [TestCase(0x1234, 0x00FF, 0x12FF)]
        [TestCase(0x0000, 0x0000, 0x0000)]
        [TestCase(0x8000, 0x7FFF, -1)]     // 0x8000 | 0x7FFF = 0xFFFF = -1
        public void Static_Or(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Or(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Xor
        [TestCase(0xFFFF, 0xFFFF, 0x0000)]
        [TestCase(0x1234, 0x00FF, 0x12CB)]
        [TestCase(-1, 0x8000, 0x7FFF)]     // 0xFFFF ^ 0x8000 = 0x7FFF = 32767
        [TestCase(0x0000, 0xFFFF, -1)]
        public void Static_Xor(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Xor(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Not
        [TestCase(0x0000, -1)]             // ~0 = 0xFFFF = -1
        [TestCase(0xFFFF, 0x0000)]
        [TestCase(0x1234, -4661)]          // ~0x1234 = 0xEDCB = -4661
        [TestCase(-32768, 32767)]          // ~0x8000 = 0x7FFF = 32767
        public void Static_Not(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Not(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Nand
        [TestCase(0xF0F0, 0x0F0F, -1)]         // ~(0xF0F0 & 0x0F0F) = ~0 = 0xFFFF = -1
        [TestCase(0xFFFF, 0xFFFF, 0x0000)]
        [TestCase(0x1234, 0x1234, -4661)]      // ~(0x1234) = 0xEDCB = -4661
        public void Static_Nand(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Nand(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Nor
        [TestCase(0x0000, 0x0000, -1)]         // ~(0 | 0) = ~0 = -1
        [TestCase(0xFFFF, 0x0000, 0x0000)]     // ~(0xFFFF | 0) = ~0xFFFF = 0
        [TestCase(0xF0F0, 0x0F0F, 0x0000)]     // ~(0xF0F0 | 0x0F0F) = ~0xFFFF = 0
        public void Static_Nor(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Nor(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Xnor
        [TestCase(0xFFFF, 0xFFFF, -1)]         // ~(0xFFFF ^ 0xFFFF) = ~0 = 0xFFFF = -1
        [TestCase(0x1234, 0x00FF, -4812)]      // ~(0x1234 ^ 0x00FF) = ~0x12CB = 0xED34 = -4812
        [TestCase(0x0000, 0xFFFF, 0x0000)]     // ~(0 ^ -1) = ~0xFFFF = 0x0000
        public void Static_Xnor(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Xnor(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Shl (Shift Left)
        [TestCase(0x0001, 1, 0x0002)]
        [TestCase(0x0100, 4, 0x1000)]
        [TestCase(0x7FFF, 1, -2)]
        [TestCase(0x8000, 1, 0x0000)]          // sign bit out
        [TestCase(-1, 1, -2)]                  // 0xFFFF << 1 = 0xFFFE = -2
        public void Static_Shl(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Shl(a, n);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Shr (Shift Right)
        [TestCase(0x8000, 1, -16384)]          // -32768 >> 1 = -16384
        [TestCase(0x7FFF, 1, 0x3FFF)]
        [TestCase(0x0100, 4, 0x0010)]
        [TestCase(-1, 1, -1)]                  // -1 >> 1 = -1 (sign extend)
        public void Static_Shr(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Shr(a, n);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
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
        #region --- COMPARAISONS (exhaustif, Fixed<B16, B8>) ---

        // == et !=
        [TestCase(0, 0, true)]
        [TestCase(1234, 1234, true)]
        [TestCase(-1, -1, true)]
        [TestCase(1, 0, false)]
        [TestCase(1000, -1000, false)]
        [TestCase(32767, 32767, true)]      // max == max
        [TestCase(-32768, -32768, true)]   // min == min
        [TestCase(-32768, 32767, false)]   // min != max
        [TestCase(0, -0, true)]            // +0 == -0 (rare, mais parfois utile)
        public void Operator_Equality(int rawA, int rawB, bool expected)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            Assert.That(a == b, Is.EqualTo(expected));
            Assert.That(a != b, Is.EqualTo(!expected));
        }

        // <, <=, >, >=
        [TestCase(0, 1, true, true, false, false)]
        [TestCase(0, 0, false, true, false, true)]
        [TestCase(100, 50, false, false, true, true)]
        [TestCase(-200, 0, true, true, false, false)]
        [TestCase(32767, -32768, false, false, true, true)]     // max > min
        [TestCase(-32768, 32767, true, true, false, false)]     // min < max
        [TestCase(-1, 0, true, true, false, false)]
        [TestCase(1, -1, false, false, true, true)]
        public void Operator_Comparisons(int rawA, int rawB, bool expectedLT, bool expectedLTE, bool expectedGT, bool expectedGTE)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            Assert.That(a < b, Is.EqualTo(expectedLT));
            Assert.That(a <= b, Is.EqualTo(expectedLTE));
            Assert.That(a > b, Is.EqualTo(expectedGT));
            Assert.That(a >= b, Is.EqualTo(expectedGTE));
        }

        // Equals (object) et GetHashCode
        [TestCase(1234)]
        [TestCase(-32768)]
        [TestCase(0)]
        [TestCase(32767)]
        public void ObjectEquals_And_Hash(int raw)
        {
            var a = new Fixed<B16, B8>(raw);
            var b = new Fixed<B16, B8>(raw);
            object oa = a, ob = b;
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals(oa), Is.True);
            Assert.That(a.Equals(ob), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(null), Is.False);
            Assert.That(a.Equals(1234), Is.False);
        }

        // Méthodes statiques Eq, Neq, Lt, Lte, Gt, Gte, IsZero, IsNeg, IsPos
        [TestCase(100, 100, true, false, false, true, false, true, false, false, true)]    // a==b
        [TestCase(0, 1, false, true, true, true, false, false, true, false, false)]        // a < b
        [TestCase(1, 0, false, true, false, false, true, true, false, false, true)]        // a > b
        [TestCase(0, 0, true, false, false, true, false, true, true, false, false)]        // a==b, zero
        [TestCase(-1, 0, false, true, true, true, false, false, false, true, false)]       // a < b, a négatif
        [TestCase(-32768, 0, false, true, true, true, false, false, false, true, false)]   // min < 0
        [TestCase(0, -32768, false, true, false, false, true, true, true, false, false)]   // 0 > min
        [TestCase(32767, -1, false, true, false, false, true, true, false, false, true)]   // max > -1
        public void Static_Comparisons(
            int rawA, int rawB,
            bool expectedEq, bool expectedNeq,
            bool expectedLt, bool expectedLte,
            bool expectedGt, bool expectedGte,
            bool expectedIsZero, bool expectedIsNeg, bool expectedIsPos)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            Assert.That(Fixed<B16, B8>.Eq(a, b), Is.EqualTo(expectedEq));
            Assert.That(Fixed<B16, B8>.Neq(a, b), Is.EqualTo(expectedNeq));
            Assert.That(Fixed<B16, B8>.Lt(a, b), Is.EqualTo(expectedLt));
            Assert.That(Fixed<B16, B8>.Lte(a, b), Is.EqualTo(expectedLte));
            Assert.That(Fixed<B16, B8>.Gt(a, b), Is.EqualTo(expectedGt));
            Assert.That(Fixed<B16, B8>.Gte(a, b), Is.EqualTo(expectedGte));
            Assert.That(Fixed<B16, B8>.IsZero(a), Is.EqualTo(expectedIsZero));
            Assert.That(Fixed<B16, B8>.IsNeg(a), Is.EqualTo(expectedIsNeg));
            Assert.That(Fixed<B16, B8>.IsPos(a), Is.EqualTo(expectedIsPos));
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
        #region --- OPERATIONS UTILITAIRES (exhaustif, Fixed<B16, B8>) ---

        // Min
        [TestCase(1000, 2000, 1000)]
        [TestCase(-3000, 3000, -3000)]
        [TestCase(-500, -500, -500)]
        [TestCase(32767, -32768, -32768)]   // max/min
        [TestCase(0, -1, -1)]              // +0 / -1
        public void Static_Min(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Min(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Max
        [TestCase(1000, 2000, 2000)]
        [TestCase(-3000, 3000, 3000)]
        [TestCase(-500, -500, -500)]
        [TestCase(32767, -32768, 32767)]    // max/min
        [TestCase(0, -1, 0)]               // +0 / -1
        public void Static_Max(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Max(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Avg (arithmetic mean, round to zero, wrap Q8.8)
        [TestCase(100, 300, 200)]
        [TestCase(-100, 100, 0)]
        [TestCase(32767, -32768, 0)]   // (32767 + -32768) / 2 = -0.5 => 0 in truncation
        [TestCase(0, 0, 0)]
        [TestCase(-1, 1, 0)]            // -1 + 1 = 0, /2 = 0
        public void Static_Avg(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.Avg(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Sign (returns -1, 0, +1 in Q8.8: -256, 0, 256)
        [TestCase(0, 0)]
        [TestCase(500, 256)]           // positive
        [TestCase(-500, -256)]         // negative
        [TestCase(-32768, -256)]       // min = negative
        [TestCase(32767, 256)]         // max = positive
        [TestCase(-1, -256)]           // -1 = negative
        public void Static_Sign(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Sign(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Abs
        [TestCase(500, 500)]
        [TestCase(-500, 500)]
        [TestCase(-32768, -32768)]      // abs(min) == min (hardware-faithful)
        [TestCase(0, 0)]
        public void Static_Abs(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Abs(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Neg
        [TestCase(500, -500)]
        [TestCase(-500, 500)]
        [TestCase(-32768, -32768)]      // -(-32768) == -32768 (hardware wrap)
        [TestCase(0, 0)]
        public void Static_Neg(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Neg(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // CopySign
        [TestCase(500, -1000, -500)]       // 500 with sign of -1000 = -500
        [TestCase(-500, 1000, 500)]        // -500 with sign of 1000 = 500
        [TestCase(0, -1000, 0)]            // 0 keeps 0
        [TestCase(-32768, 500, -32768)]    // min, positive → stays min (hardware-faithful)
        [TestCase(32767, -1, -32767)]      // max, negative
        [TestCase(1, 0, 1)]                // sign(0) is considered positive
        public void Static_CopySign(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.CopySign(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
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
        #region --- SATURATION (exhaustif, Fixed<B16, B8>) ---

        // AddSat
        [TestCase(30000, 30000, 32767)]        // overflow saturé max
        [TestCase(-30000, -30000, -32768)]     // underflow saturé min
        [TestCase(1000, 2000, 3000)]           // pas de saturation
        [TestCase(32766, +1, 32767)]           // max-1 +1
        [TestCase(-32767, -1, -32768)]         // min+1 -1
        [TestCase(12000, -5000, 7000)]         // signes opposés, pas de sat
        [TestCase(-12000, +5000, -7000)]
        [TestCase(32767, 1, 32767)]            // max +1 sat
        [TestCase(-32768, -1, -32768)]         // min -1 sat
        [TestCase(0, 0, 0)]                    // zéro
        public void Static_AddSat(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.AddSat(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // SubSat
        [TestCase(32767, -1, 32767)]           // overflow saturé max
        [TestCase(-32768, 1, -32768)]          // underflow saturé min
        [TestCase(10000, -30000, 32767)]       // overflow saturé max
        [TestCase(-10000, 30000, -32768)]      // underflow saturé min
        [TestCase(2000, 1000, 1000)]           // pas de saturation
        [TestCase(-32767, +1, -32768)]         // min+1 - 1  → min
        [TestCase(32766, -1, 32767)]           // max-1 - (-1) → max
        [TestCase(-15000, -2000, -13000)]      // même signe, pas de sat
        [TestCase(15000, 2000, 13000)]
        [TestCase(0, 0, 0)]
        public void Static_SubSat(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.SubSat(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // MulSat
        [TestCase(3000, 3000, 32767)]          // overflow positif saturé max
        [TestCase(-3000, 3000, -32768)]        // overflow négatif saturé min
        [TestCase(32767, 32767, 32767)]        // overflow max saturé
        [TestCase(-32768, 32767, -32768)]      // overflow min saturé
        [TestCase(256, 256, 256)]              // 1.0 * 1.0 = 1.0
        [TestCase(-256, 256, -256)]            // -1.0 * 1.0 = -1.0
        [TestCase(0, 1234, 0)]                 // 0*N
        [TestCase(1, 2000, 7)]                 // 1*2000/256 = 7 (tronc)
        [TestCase(-1, 2000, -8)]               // -1*2000/256 = -8 (tronc)
        [TestCase(-1, -1, 0)]                  // (-1)*(-1)/256 = 0 (tronc)
        [TestCase(-3000, -3000, 32767)]        // nég×nég overflow → max
        [TestCase(32767, -32768, -32768)]      // max * min → min sat
        [TestCase(-32768, -32768, 32767)]      // min * min → max sat (hardware)
        public void Static_MulSat(int rawA, int rawB, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(rawA);
            var b = new Fixed<B16, B8>(rawB);
            var result = Fixed<B16, B8>.MulSat(a, b);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Clamp (tranche entre deux bornes, min/max inclus)
        [TestCase(32767, -20000, 20000, 20000)]    // clamp max
        [TestCase(-32768, -20000, 20000, -20000)]  // clamp min
        [TestCase(15000, -20000, 20000, 15000)]    // pas de clamp
        [TestCase(-32768, -32768, 32767, -32768)]  // clamp bas extrême
        [TestCase(32767, -32768, 32767, 32767)]    // clamp haut extrême
        public void Static_Clamp(int raw, int minRaw, int maxRaw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var min = new Fixed<B16, B8>(minRaw);
            var max = new Fixed<B16, B8>(maxRaw);
            var result = Fixed<B16, B8>.Clamp(a, min, max);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Clamp01 (clamp entre 0.0 et 1.0 en Q8.8, donc [0, 256])
        [TestCase(-500, 0)]              // clamp bas
        [TestCase(500, 256)]             // dans [0,1], clamp haut
        [TestCase(70000, 256)]           // > 1.0, clamp haut (1.0 Q8.8)
        [TestCase(-300, 0)]              // < 0, clamp bas
        [TestCase(300, 256)]             // > 1.0, clamp haut
        [TestCase(64, 64)]               // 0.25 reste 0.25
        [TestCase(0, 0)]                 // 0 reste 0
        [TestCase(256, 256)]             // 1.0 reste 1.0
        public void Static_Clamp01(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Clamp01(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // ClampWithOffset (min/max + offset, test wrap, bornes inversées, bords extrêmes)
        [TestCase(20000, 10000, 15000, 1000, 2000, 17000)]      // clamp à min+offset
        [TestCase(20000, 15000, 18000, -500, 100, 18100)]       // clamp à max+offset
        [TestCase(16000, 15000, 18000, 0, 0, 16000)]            // pas de clamp
        [TestCase(10000, 15000, 12000, -1000, +2000, 14000)]    // vLo>vHi → swap
        [TestCase(-5000, -1000, 1000, -9000, -9000, -8000)]     // bornes au-delà min
        [TestCase(-32768, -32768, 32767, -1, 1, -32768)]        // min, bords offset
        [TestCase(32767, -32768, 32767, -1, 1, 32767)]          // max, bords offset
        public void Static_ClampWithOffset(int raw, int minRaw, int maxRaw, int offsetMin, int offsetMax, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var min = new Fixed<B16, B8>(minRaw);
            var max = new Fixed<B16, B8>(maxRaw);
            var result = Fixed<B16, B8>.ClampWithOffset(a, min, max, offsetMin, offsetMax);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
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
        #region --- MANIPULATION BITS ET ROTATIONS (exhaustif, Fixed<B16,B8>) ---

        // Reverse (renversement des bits sur 16 bits)
        [TestCase(0x0001, -32768)]       // 0x8000 = -32768 signed
        [TestCase(0x8000, 0x0001)]
        [TestCase(0xFFFF, -1)]           // 0xFFFF = -1 signed
        [TestCase(0x1234, 0x2C48)]
        [TestCase(0x0000, 0x0000)]       // tous bits à 0
        [TestCase(0x5555, -21846)]       // alternance binaire
        [TestCase(0x00FF, -256)]         // 0xFF00 = -256 signed
        public void Static_Reverse(int raw, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Reverse(a);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // PopCount (nombre de bits à 1)
        [TestCase(0x0000, 0)]
        [TestCase(0xFFFF, 16)]
        [TestCase(0xF0F0, 8)]
        [TestCase(0x8001, 2)]
        [TestCase(0x5555, 8)]            // alternance binaire
        [TestCase(0xAAAA, 8)]
        [TestCase(0x8000, 1)]
        [TestCase(0x00FF, 8)]
        public void Static_PopCount(int raw, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.PopCount(a);
            Assert.That(result, Is.EqualTo(expected));
        }

        // Parity (true si nombre de bits à 1 impair)
        [TestCase(0x0000, false)]
        [TestCase(0x0001, true)]
        [TestCase(0x8001, false)]        // 2 bits à 1
        [TestCase(0xFFFF, false)]       // 16 bits à 1 = pair
        [TestCase(0x7FFF, true)]        // 15 bits à 1 = impair
        [TestCase(0xAAAA, false)]       // 8 bits à 1
        [TestCase(0x5555, false)]       // 8 bits à 1
        public void Static_Parity(int raw, bool expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Parity(a);
            Assert.That(result, Is.EqualTo(expected));
        }

        // LeadingZeros (nombre de zéros en tête sur 16 bits)
        [TestCase(0x0000, 16)]
        [TestCase(0x0001, 15)]
        [TestCase(0x8000, 0)]
        [TestCase(0x00FF, 8)]
        [TestCase(0x0800, 4)]
        [TestCase(0x0008, 12)]
        public void Static_LeadingZeros(int raw, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.LeadingZeros(a);
            Assert.That(result, Is.EqualTo(expected));
        }

        // TrailingZeros (nombre de zéros de poids faible)
        [TestCase(0x0000, 16)]
        [TestCase(0x0001, 0)]
        [TestCase(0x8000, 15)]
        [TestCase(0x00F0, 4)]
        [TestCase(0x0F00, 8)]
        [TestCase(0x8008, 3)]
        public void Static_TrailingZeros(int raw, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.TrailingZeros(a);
            Assert.That(result, Is.EqualTo(expected));
        }

        // Rol (rotate left, wrap sur 16 bits)
        [TestCase(0x0001, 1, 0x0002)]
        [TestCase(-32768, 1, 0x0001)]      // 0x8000 <<1 + wrap = 0x0001
        [TestCase(0x1234, 4, 0x2341)]
        [TestCase(-1, 7, -1)]              // 0xFFFF rot 7 = 0xFFFF = -1
        [TestCase(0x8000, 15, 16384)]     // 0x8000 rotleft 15 = 0x4000 = 16384
        [TestCase(0x7FFF, 1, -2)]          // 0xFFFE signed = -2
        public void Static_Rol(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Rol(a, n);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Ror (rotate right, wrap sur 16 bits)
        [TestCase(0x0001, 1, -32768)]      // 0x8000 = -32768 signed
        [TestCase(0x8000, 1, 0x4000)]
        [TestCase(0x1234, 4, 0x4123)]
        [TestCase(-1, 9, -1)]              // 0xFFFF rot 9 = 0xFFFF = -1
        [TestCase(0x8000, 15, 0x0001)]     // rot 15
        [TestCase(0x7FFF, 1, -16385)]      // rot right
        public void Static_Ror(int raw, int n, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Ror(a, n);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // Bsr (bit scan reverse — position du bit de poids fort, -1 si aucun)
        [TestCase(0x0000, -1)]
        [TestCase(0x0001, 0)]
        [TestCase(0x8000, 15)]
        [TestCase(0x00F0, 7)]
        [TestCase(0x0800, 11)]
        public void Static_Bsr(int raw, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Bsr(a);
            Assert.That(result, Is.EqualTo(expected));
        }

        // Bsf (bit scan forward — position du bit de poids faible, -1 si aucun)
        [TestCase(0x0000, -1)]
        [TestCase(0x0001, 0)]
        [TestCase(0x8000, 15)]
        [TestCase(0x00F0, 4)]
        [TestCase(0x0F00, 8)]
        public void Static_Bsf(int raw, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = Fixed<B16, B8>.Bsf(a);
            Assert.That(result, Is.EqualTo(expected));
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
        #region --- CONSTANTES (exhaustif, Fixed<B16,B8>) ---

        [Test]
        public void Const_Zero()
        {
            Assert.That(Fixed<B16, B8>.Zero.Raw, Is.EqualTo(0));
        }

        [Test]
        public void Const_Half()
        {
            Assert.That(Fixed<B16, B8>.Half.Raw, Is.EqualTo(128)); // 0.5 en Q8.8
        }

        [Test]
        public void Const_AllOnes()
        {
            Assert.That(Fixed<B16, B8>.AllOnes.Raw, Is.EqualTo(-1)); // 0xFFFF = -1
        }

        [Test]
        public void Const_Msb()
        {
            Assert.That(Fixed<B16, B8>.Msb.Raw, Is.EqualTo(-32768)); // 0x8000 = -32768
        }

        [Test]
        public void Const_Lsb()
        {
            Assert.That(Fixed<B16, B8>.Lsb.Raw, Is.EqualTo(1)); // LSB = bit 0
        }

        // Bit(n)
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(7, 128)]
        [TestCase(8, 256)]
        [TestCase(15, -32768)]  // 0x8000 = -32768
        [TestCase(14, 16384)]  // 0x4000 = 16384
        [TestCase(5, 32)]
        public void Const_Bit(int n, int expectedRaw)
        {
            Assert.That(Fixed<B16, B8>.Bit(n).Raw, Is.EqualTo(expectedRaw));
        }

        // Bit(n) hors bornes (optionnel, selon implémentation attendue)
        [TestCase(-1)]
        [TestCase(16)]
        [TestCase(32)]
        public void Const_Bit_OutOfRange_Throws(int n)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.Bit(n));
        }

        // Fraction(n, d) — (utile pour Q8.8 : Fraction(1,2)=128, Fraction(1,4)=64)
        [TestCase(1, 2, 128)]
        [TestCase(1, 4, 64)]
        [TestCase(3, 4, 192)]
        [TestCase(5, 8, 160)]
        [TestCase(-1, 2, -128)]      // négatif
        [TestCase(-3, 4, -192)]      // négatif
        [TestCase(1, -2, -128)]      // dénominateur négatif
        [TestCase(-1, -4, 64)]       // deux négatifs = positif
        public void Fixed_Fraction(int numer, int denom, int expected)
        {
            var n = new IntN<B16>(numer);
            var d = new IntN<B16>(denom);
            Assert.That(Fixed<B16, B8>.Fraction(n, d).Raw, Is.EqualTo(expected));
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
        #region --- ACCES OCTETS (exhaustif, Fixed<B16,B8>) ---

        // Byte (static, Little Endian)
        [TestCase(0x1234, 0, 0x34)]       // LSB
        [TestCase(0x1234, 1, 0x12)]       // MSB
        [TestCase(-1, 0, 0xFF)]           // 0xFFFF, LSB = 0xFF
        [TestCase(-1, 1, 0xFF)]           // 0xFFFF, MSB = 0xFF
        [TestCase(0, 0, 0x00)]
        [TestCase(0, 1, 0x00)]
        public void Static_Byte(int raw, int index, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.That(Fixed<B16, B8>.Byte(a, index), Is.EqualTo(expected));
        }

        // Byte hors bornes (optionnel)
        [TestCase(0x1234, -1)]
        [TestCase(0x1234, 2)]
        public void Static_Byte_OutOfRange_Throws(int raw, int index)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed<B16, B8>.Byte(a, index));
        }

        // ToBytes (toujours little endian)
        [TestCase(0x1234, 0x34, 0x12)]
        [TestCase(-1, 0xFF, 0xFF)]
        [TestCase(0, 0x00, 0x00)]
        [TestCase(0x8000, 0x00, 0x80)] // -32768
        public void ToBytes(int raw, byte expected0, byte expected1)
        {
            var a = new Fixed<B16, B8>(raw);
            var bytes = a.ToBytes();
            Assert.That(bytes.Length, Is.EqualTo(2));
            Assert.That(bytes[0], Is.EqualTo(expected0)); // LSB
            Assert.That(bytes[1], Is.EqualTo(expected1)); // MSB
        }

        // FromBytes (little endian)
        [TestCase(0x34, 0x12, 0x1234)]
        [TestCase(0xFF, 0xFF, -1)]
        [TestCase(0x00, 0x00, 0)]
        [TestCase(0x00, 0x80, -32768)] // 0x8000
        public void FromBytes(byte b0, byte b1, int expectedRaw)
        {
            var fx = Fixed<B16, B8>.FromBytes(new byte[] { b0, b1 });
            Assert.That(fx.Raw, Is.EqualTo(expectedRaw));
        }

        // FromBytes : mauvaise taille (optionnel)
        [Test]
        public void FromBytes_Throws_If_Not2()
        {
            Assert.Throws<ArgumentException>(() => Fixed<B16, B8>.FromBytes(new byte[] { 0x01 }));
            Assert.Throws<ArgumentException>(() => Fixed<B16, B8>.FromBytes(new byte[] { 0x01, 0x02, 0x03 }));
        }

        // GetByte (instance)
        [TestCase(0xABCD, 0, 0xCD)]
        [TestCase(0xABCD, 1, 0xAB)]
        [TestCase(-1, 0, 0xFF)]
        [TestCase(-1, 1, 0xFF)]
        public void GetByte(int raw, int index, int expected)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.That(a.GetByte(index), Is.EqualTo(expected));
        }

        // GetByte hors bornes (optionnel)
        [TestCase(0x1234, -1)]
        [TestCase(0x1234, 2)]
        public void GetByte_OutOfRange_Throws(int raw, int index)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.GetByte(index));
        }

        // SetByte (nouvelle instance)
        [TestCase(0x1234, 0, 0xFF, 0x12FF)]
        [TestCase(0x1234, 1, 0xAB, -21708)]
        [TestCase(0x0000, 1, 0x80, -32768)] // MSB = 0x80 = -32768
        public void SetByte(int raw, int index, int value, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var result = a.SetByte(index, (byte)value);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // SetByte hors bornes (optionnel)
        [TestCase(0x1234, -1, 0xFF)]
        [TestCase(0x1234, 2, 0xFF)]
        public void SetByte_OutOfRange_Throws(int raw, int index, int value)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.SetByte(index, (byte)value));
        }

        // ReplaceByte (remplace l'octet n par celui d'une autre instance)
        [TestCase(0xCAFE, 1, 0xDE, -8450)]  // MSB = 0xDE
        [TestCase(0xCAFE, 0, 0xAD, -13651)] // LSB = 0xAD
        [TestCase(-1, 0, 0x12, -238)]       // remplace LSB de -1 par 0x12 (0xFF12 = -241)
        public void ReplaceByte(int raw, int index, int value, int expectedRaw)
        {
            var a = new Fixed<B16, B8>(raw);
            var source = new Fixed<B16, B8>(value);
            var result = a.ReplaceByte(index, source);
            Assert.That(result.Raw, Is.EqualTo(expectedRaw));
        }

        // ReplaceByte hors bornes (optionnel)
        [TestCase(0x1234, -1, 0xAD)]
        [TestCase(0x1234, 2, 0xAD)]
        public void ReplaceByte_OutOfRange_Throws(int raw, int index, int value)
        {
            var a = new Fixed<B16, B8>(raw);
            var source = new Fixed<B16, B8>(value);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.ReplaceByte(index, source));
        }

        #endregion

        /*==================================
         * --- CONVERSION EN CHAÎNE (STRING) ---
         * ToString
         * DebugString
         * ToBinaryString
         * ToHexString
         ==================================*/
        #region --- CONVERSION EN CHAÎNE (STRING, exhaustif, Fixed<B16,B8>) ---

        // ToString() (décimal signé, valeur "user")
        [TestCase(0, "0")]
        [TestCase(42, "42")]
        [TestCase(-42, "-42")]
        [TestCase(256, "256")]              // 1.0 Q8.8
        [TestCase(-32768, "-32768")]        // Min
        [TestCase(32767, "32767")]          // Max
        [TestCase(-1, "-1")]
        public void Fixed_ToString(int raw, string expected)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.That(a.ToString(), Is.EqualTo(expected));
        }

        // DebugString() — doit contenir type, valeur, bin et hex
        [TestCase(0x1234)]
        [TestCase(-1)]
        [TestCase(0x0000)]
        [TestCase(32767)]
        [TestCase(-32768)]
        public void Fixed_DebugString_ContainsInfo(int raw)
        {
            var a = new Fixed<B16, B8>(raw);
            var debug = a.DebugString();
            Assert.That(debug, Does.Contain("Fixed<B16, B8>"));
            Assert.That(debug, Does.Contain(raw.ToString()));
            Assert.That(debug, Does.Contain("bin="));
            Assert.That(debug, Does.Contain("hex="));
        }

        // ToBinaryString (toujours 16 bits, zero pad)
        [TestCase(0x0000, "0000000000000000")]
        [TestCase(0x0001, "0000000000000001")]
        [TestCase(0xFFFF, "1111111111111111")]
        [TestCase(0x1234, "0001001000110100")]
        [TestCase(-1, "1111111111111111")]
        [TestCase(-32768, "1000000000000000")]
        [TestCase(32767, "0111111111111111")]
        public void Fixed_ToBinaryString(int raw, string expectedBinary)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.That(a.ToBinaryString(), Is.EqualTo(expectedBinary));
        }

        // ToHexString (4 chiffres, uppercase, optionnel prefixe 0x)
        [TestCase(0x0000, false, "0000")]
        [TestCase(0x0001, false, "0001")]
        [TestCase(0x1234, false, "1234")]
        [TestCase(0xFFFF, false, "FFFF")]
        [TestCase(-1, false, "FFFF")]
        [TestCase(-32768, false, "8000")]
        [TestCase(32767, false, "7FFF")]
        [TestCase(0xABCD, true, "0xABCD")]
        [TestCase(-32768, true, "0x8000")]
        [TestCase(-1, true, "0xFFFF")]
        public void Fixed_ToHexString(int raw, bool withPrefix, string expectedHex)
        {
            var a = new Fixed<B16, B8>(raw);
            Assert.That(a.ToHexString(withPrefix), Is.EqualTo(expectedHex));
        }

        #endregion

        /*==================================
         * --- PARSING ---
         * Parse
         * TryParse
         * ParseHex
         * TryParseHex
         * ParseBinary
         * ParseTryParseBinary
         * ToJson
         * FromJson
         ==================================*/
        #region --- PARSING (exhaustif, JSON, HEX, BINAIRE, EDGE CASES) ---

        // --- Décimal (valeurs extrêmes, edge, wrap, floats) ---
        [TestCase("0", 0)]
        [TestCase("1", 256)]
        [TestCase("-1", -256)]
        [TestCase("127", 32512)]          // 127*256
        [TestCase("-128", -32768)]        // -128*256
        [TestCase("255", -256)]          // 255*256, wrap hardware signed = -256 si stocké en int16 !
        [TestCase("32767", -256)]         // overflow/wrap: 32767*256 = 0x7FFF00 => low 16 bits = 0xFF00 = -256
        [TestCase("-32768", 0)]           // wrap hardware: -32768*256 = 0x8000000, low 16 bits = 0
        [TestCase("65535", -256)]         // 65535*256 = 0xFFFF00, low 16 bits = 0xFF00 = -256
        [TestCase("1.5", 384)]            // 1.5*256 = 384
        [TestCase("-1.5", -384)]          // -1.5*256 = -384
        [TestCase("0.00390625", 1)]       // 1/256 = 0.00390625 (valeur la plus petite non nulle en Q8.8)
        [TestCase("-0.00390625", -1)]     // -1/256
        [TestCase("32767.996", -1)]       // max possible positif en Q8.8: (0x7FFF << 8) + 0xFF = 0x7FFFFF = -1 (int16 wrap)
        [TestCase("32768", 0)]            // overflow vers 0
        [TestCase("999999999", 0)]        // overflow float: catch as 0 (par wrap)
        [TestCase("-999999999", 0)]       // sous-flux float: catch as 0 (par wrap)
        [TestCase("3.4028235E+38", 0)]    // float.MaxValue
        [TestCase("-3.4028235E+38", 0)]   // float.MinValue
        public void Parse_Decimal_Valid(string s, int expected)
        {
            var val = Fixed<B16, B8>.Parse(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(Fixed<B16, B8>.TryParse(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Décimal erreurs (null, NaN, infini, overflow réel float, vide, etc.) ---
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("abc")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("1e309")]             // > double.MaxValue
        [TestCase("-1e309")]
        [TestCase("1e-400")]            // < double.MinValue
        // [TestCase("99999999999999999")] // NON TESTÉ : wrap hardware (pas d'exception, conversion float + bitcast RAW)
        // [TestCase("-99999999999999999")] // NON TESTÉ : wrap hardware (idem, conversion float + bitcast RAW)
        public void Parse_Decimal_Invalid(string s)
        {
            Assert.Throws(Is.TypeOf<FormatException>().Or.TypeOf<OverflowException>(), () => Fixed<B16, B8>.Parse(s));
            Assert.That(Fixed<B16, B8>.TryParse(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Decimal_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.Parse(null));
            Assert.That(Fixed<B16, B8>.TryParse(null, out var _), Is.False);
        }

        // --- Hexadécimal (valeurs extrêmes, wrap, edge, padding) ---
        [TestCase("0x0", 0)]
        [TestCase("0x1", 1)]
        [TestCase("0xFF", 255)]
        [TestCase("FF", 255)]
        [TestCase("0xFF00", -256)]
        [TestCase("FF00", -256)]
        [TestCase("0x8000", -32768)]        // signed hardware wrap
        [TestCase("8000", -32768)]
        [TestCase("0x7F00", 32512)]         // 127.0
        [TestCase("7F00", 32512)]
        [TestCase("0xFFFF", -1)]            // signed int16 wrap
        [TestCase("FFFF", -1)]
        [TestCase("0000", 0)]               // padding
        [TestCase("0001", 1)]
        public void Parse_Hex_Valid(string s, int expected)
        {
            var val = Fixed<B16, B8>.ParseHex(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(Fixed<B16, B8>.TryParseHex(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("0x")]
        [TestCase("xyz")]
        [TestCase(" ")]
        public void Parse_Hex_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.ParseHex(s));
            Assert.That(Fixed<B16, B8>.TryParseHex(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Hex_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.ParseHex(null));
            Assert.That(Fixed<B16, B8>.TryParseHex(null, out var _), Is.False);
        }

        // --- Binaire (valeurs extrêmes, wrap, signed, padding, little endian) ---
        [TestCase("0b0000000000000000", 0)]
        [TestCase("0b0000000000000001", 1)]
        [TestCase("0b1111111111111111", -1)]
        [TestCase("0b0111111100000000", 32512)]
        [TestCase("0b1111111100000000", -256)]
        [TestCase("0b1000000000000000", -32768)]
        [TestCase("0111111100000000", 32512)]
        [TestCase("1111111100000000", -256)]
        [TestCase("1000000000000000", -32768)]
        [TestCase("0000000000000001", 1)]
        public void Parse_Binary_Valid(string s, int expected)
        {
            var val = Fixed<B16, B8>.ParseBinary(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(Fixed<B16, B8>.TryParseBinary(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Binaire erreurs ---
        [TestCase("")]
        [TestCase("0b")]
        [TestCase("21001100")]
        [TestCase(" ")]
        [TestCase("11111111111111112")]   // trop long, overflow
        public void Parse_Binary_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.ParseBinary(s));
            Assert.That(Fixed<B16, B8>.TryParseBinary(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Binary_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.ParseBinary(null));
            Assert.That(Fixed<B16, B8>.TryParseBinary(null, out var _), Is.False);
        }

        // --- Round-trip JSON natif (raw signés, edge, overflows, signed/unsigned) ---
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(32512)]
        [TestCase(-256)]
        [TestCase(-32768)]
        [TestCase(65280)]
        [TestCase(65535)]
        public void ToJson_RoundTrip(int src)
        {
            var a = new Fixed<B16, B8>(src);
            string json = a.ToJson();
            var b = Fixed<B16, B8>.FromJson(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- FromJson mixte (décimal, hex, binaire, signed/unsigned/wrap, min/max) ---
        [TestCase("127", 127)]
        [TestCase("0x7F00", 32512)]
        [TestCase("0b0111111100000000", 32512)]
        [TestCase("-1", -1)]
        [TestCase("0xFF00", -256)]
        [TestCase("0b1111111100000000", -256)]
        [TestCase("-32768", -32768)]
        [TestCase("0x8000", -32768)]
        [TestCase("0b1000000000000000", -32768)]
        [TestCase("65535", -1)]
        public void FromJson_Mixte(string s, int expected)
        {
            var x = Fixed<B16, B8>.FromJson(s);
            Assert.That(x.Raw, Is.EqualTo(expected));
        }

        // --- FromJson erreurs (null, NaN, inf, garbage, float/double overflow) ---
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("0x")]
        [TestCase("0b")]
        [TestCase("GG")]
        // [TestCase("99999999999999999")] // NON TESTÉ : wrap hardware (pas d'exception, conversion float + bitcast RAW)
        // [TestCase("-99999999999999999")] // NON TESTÉ : wrap hardware (idem, conversion float + bitcast RAW)
        public void FromJson_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.FromJson(s));
        }

        [Test]
        public void FromJson_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.FromJson(null));
        }

        // --- Debug: les "0b" ne passent jamais ---
        [Test]
        public void TryParse_Debug_0b()
        {
            Assert.That(Fixed<B16, B8>.TryParse("0b", out var _), Is.False, "TryParse doit retourner false pour '0b'");
            Assert.That(Fixed<B16, B8>.TryParseHex("0b", out var _), Is.False, "TryParseHex doit retourner false pour '0b'");
            Assert.That(Fixed<B16, B8>.TryParseBinary("0b", out var _), Is.False, "TryParseBinary doit retourner false pour '0b'");
        }

        #endregion

        /*==================================
 * --- SERIALISATION META ---
 * ToJsonWithMeta
 * FromJsonWithMeta
 ==================================*/
        #region --- SERIALISATION META (exhaustif, multi-Q, erreurs) ---

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(32512)]
        [TestCase(-256)]
        [TestCase(-32768)]
        public void ToJsonWithMeta_RoundTrip_Q8_8(int raw)
        {
            var a = new Fixed<B16, B8>(raw);
            string json = a.ToJsonWithMeta();
            var b = Fixed<B16, B8>.FromJsonWithMeta<B16, B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(8388607)]
        [TestCase(-8388608)]
        public void ToJsonWithMeta_RoundTrip_Q16_8(int raw)
        {
            var a = new Fixed<B24, B8>(raw); // Q16.8 : 24 bits total, 8 bits frac
            string json = a.ToJsonWithMeta();
            var b = Fixed<B24, B8>.FromJsonWithMeta<B24, B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(2047)]
        [TestCase(-2048)]
        public void ToJsonWithMeta_RoundTrip_Q12_4(int raw)
        {
            var a = new Fixed<B16, B4>(raw);
            string json = a.ToJsonWithMeta();
            var b = Fixed<B16, B4>.FromJsonWithMeta<B16, B4>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Erreur : meta bits/fracs non concordants ---
        [Test]
        public void FromJsonWithMeta_BitsMismatch_Throws()
        {
            var a = new Fixed<B16, B8>(1234);
            string json = a.ToJsonWithMeta();
            Assert.Throws<FormatException>(() => Fixed<B12, B4>.FromJsonWithMeta<B12, B4>(json));
        }

        // --- Tests malformés, string vide, mauvais champs, etc. ---
        [TestCase("{ \"raw\": 123 }")]    // intBits/fracs manquant
        [TestCase("{ \"intBits\": 16 }")]    // raw manquant
        [TestCase("{ \"intBits\": 16, \"fracBits\": 8 }")] // raw manquant
        [TestCase("{ \"intBits\": 16, \"fracBits\": 8, \"raw\": \"oops\" }")]
        [TestCase("{ \"intBits\": \"oops\", \"fracBits\": 8, \"raw\": 12 }")]
        [TestCase("{ \"intBits\": 16, \"fracBits\": \"bad\", \"raw\": 12 }")]
        [TestCase("{}")]
        [TestCase("")]
        public void FromJsonWithMeta_InvalidJson_Throws(string json)
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.FromJsonWithMeta<B16, B8>(json));
        }

        // --- Test spécifique pour null ---
        [Test]
        public void FromJsonWithMeta_Null_Throws()
        {
            Assert.Throws<FormatException>(() => Fixed<B16, B8>.FromJsonWithMeta<B16, B8>(null));
        }

        #endregion

    }
}

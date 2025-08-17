using NUnit.Framework;
using FixedEngine.Math;

namespace FixedEngine.Tests.Math
{
    [TestFixture]
    public class UIntNTests
    {
        /*==================================
         * --- CONSTRUCTOR ---
         ==================================*/
        #region --- CONSTRUCTOR (exhaustif, UIntN<B8>) ---

        // ─────────── Int32 in‑range ───────────
        [TestCase(0, 0u)]
        [TestCase(42, 42u)]
        [TestCase(255, 255u)]
        public void Ctor_Int32_InRange(int src, uint expected)
            => Assert.That(new UIntN<B8>(src).Raw, Is.EqualTo(expected));


        // ─────────── Int32 overflow / wrap ───────────
        [TestCase(260, 4u)]      // 260 & 0xFF = 4
        [TestCase(300, 44u)]      // 300 & 0xFF = 44
        [TestCase(1024, 0u)]      // 1024 & 0xFF = 0
        [TestCase(int.MaxValue, 255u)]      // 2 147 483 647 wrap
        [TestCase(-1, 255u)]      // ‑1   → 0xFF
        [TestCase(int.MinValue, 0u)]      // ‑2 147 483 648 wrap
        public void Ctor_Int32_Wrap(int src, uint expected)
            => Assert.That(new UIntN<B8>(src).Raw, Is.EqualTo(expected));


        // ─────────── UInt32 overload (si disponible) ───────────
        [TestCase(0u, 0u)]
        [TestCase(255u, 255u)]
        [TestCase(456u, 200u)]      // 456 & 0xFF = 200
        public void Ctor_UInt32(uint src, uint expected)
        {
            var val = new UIntN<B8>(src);      // appelle ctor(uint) s’il existe, sinon convertit à int
            Assert.That(val.Raw, Is.EqualTo(expected));
        }

        // ─────────── Consistance Zero / AllOnes après wrap ───────────
        [Test]
        public void Ctor_Relations()
        {
            // 256 wrappe sur Zero
            var zero = new UIntN<B8>(256);
            Assert.That(zero.Raw, Is.EqualTo(UIntN<B8>.Zero.Raw));

            // -1 wrappe sur AllOnes
            var ff = new UIntN<B8>(-1);
            Assert.That(ff.Raw, Is.EqualTo(UIntN<B8>.AllOnes.Raw));
        }

        #endregion

        /*==================================
         * --- CONVERSION EXPLICITES ---
         * int, uint, IntN, UIntN, float, double
         * fixed, ufixed
         ==================================*/
        #region --- CONVERSIONS EXPLICITES (exhaustif, UIntN<B8>) ---

        /* ---------- int  -> UIntN<B8> ---------- */
        [TestCase(0, 0u)]
        [TestCase(42, 42u)]
        [TestCase(255, 255u)]
        [TestCase(-1, 255u)]        // wrap : -1 & 0xFF = 255
        [TestCase(256, 0u)]          // wrap
        [TestCase(511, 255u)]        // wrap
        [TestCase(-256, 0u)]          // wrap
        [TestCase(-257, 255u)]        // wrap
        [TestCase(int.MaxValue, 255u)]        //  2 147 483 647 → 0xFF
        [TestCase(int.MinValue, 0u)]          // -2 147 483 648 → 0x00
        public void Explicit_Int_To_UIntN_B8_Wrap(int val, uint expected)
        {
            var u = (UIntN<B8>)val;
            Assert.That(u.Raw, Is.EqualTo(expected));
        }

        /* ---------- uint -> UIntN<B8> ---------- */
        [TestCase(0u, 0u)]
        [TestCase(250u, 250u)]
        [TestCase(255u, 255u)]
        [TestCase(256u, 0u)]          // wrap
        [TestCase(511u, 255u)]        // wrap
        [TestCase(65535u, 255u)]        // wrap
        [TestCase(uint.MaxValue, 255u)]        // 0xFFFF_FFFF → 0xFF
        public void Explicit_UInt_To_UIntN_B8_Wrap(uint val, uint expected)
        {
            var u = (UIntN<B8>)val;
            Assert.That(u.Raw, Is.EqualTo(expected));
        }

        /* ---------- float -> UIntN<B8> ---------- */
        [TestCase(0.0f, 0u)]
        [TestCase(42.3f, 42u)]         // trunc
        [TestCase(255.99f, 255u)]        // trunc
        [TestCase(256.0f, 0u)]          // wrap
        [TestCase(-1.0f, 255u)]        // wrap
        [TestCase(-0.1f, 0u)]          // -0.1f → 0  → 0x00
        [TestCase(511.1f, 255u)]        // 511 → 0x1FF → 0xFF
        [TestCase(-257.0f, 255u)]        // wrap
        public void Explicit_Float_To_UIntN_B8_Wrap(float val, uint expected)
        {
            var u = (UIntN<B8>)val;
            Assert.That(u.Raw, Is.EqualTo(expected));
        }

        /* ---------- double -> UIntN<B8> ---------- */
        [TestCase(0.0, 0u)]
        [TestCase(42.6, 42u)]
        [TestCase(255.999, 255u)]
        [TestCase(256.0, 0u)]
        [TestCase(-1.0, 255u)]
        [TestCase(512.4, 0u)]          // 512 & 0xFF = 0
        [TestCase(double.NaN, 0u)]          // NaN → 0
        [TestCase(double.PositiveInfinity, 0u)]      // +∞ → 0
        [TestCase(double.NegativeInfinity, 0u)]      // -∞ → 0
        [TestCase(1e20, 255u)]        // clip interne → 0xFF
        [TestCase(double.MinValue, 0u)]          // clip interne → 0x00
        public void Explicit_Double_To_UIntN_B8_Wrap(double val, uint expected)
        {
            var u = (UIntN<B8>)val;
            Assert.That(u.Raw, Is.EqualTo(expected));
        }

        /* ---------- IntN<B8> -> UIntN<B8> ---------- */
        [TestCase(0, 0u)]
        [TestCase(127, 127u)]
        [TestCase(-1, 255u)] // wrap
        public void Explicit_IntN_B8_To_UIntN_B8(int raw, uint expected)
        {
            var i = new IntN<B8>(raw);
            var u = (UIntN<B8>)i;
            Assert.That(u.Raw, Is.EqualTo(expected));
        }

        /* ---------- UIntN<B8> -> Int ---------- */
        [TestCase(0u, 0)]
        [TestCase(127u, 127)]
        [TestCase(255u, 255)]
        public void Explicit_UIntN_B8_To_Int(uint raw, int expected)
        {
            var u = new UIntN<B8>(raw);
            int x = (int)u;
            Assert.That(x, Is.EqualTo(expected));
        }

        /* ---------- UIntN<B8> -> UInt ---------- */
        [TestCase(0u, 0u)]
        [TestCase(200u, 200u)]
        [TestCase(255u, 255u)]
        public void Explicit_UIntN_B8_To_UInt(uint raw, uint expected)
        {
            var u = new UIntN<B8>(raw);
            uint x = (uint)u;
            Assert.That(x, Is.EqualTo(expected));
        }

        /* ---------- UIntN<B8> -> float ---------- */
        [TestCase(0u, 0.0f)]
        [TestCase(42u, 42.0f)]
        [TestCase(255u, 255.0f)]
        public void Explicit_UIntN_B8_To_Float(uint raw, float expected)
        {
            var u = new UIntN<B8>(raw);
            float x = (float)u;
            Assert.That(x, Is.EqualTo(expected).Within(1e-4f));
        }

        /* ---------- UIntN<B8> -> double ---------- */
        [TestCase(0u, 0.0)]
        [TestCase(200u, 200.0)]
        [TestCase(255u, 255.0)]
        public void Explicit_UIntN_B8_To_Double(uint raw, double expected)
        {
            var u = new UIntN<B8>(raw);
            double x = (double)u;
            Assert.That(x, Is.EqualTo(expected).Within(1e-4));
        }

        /* ---------- UIntN<B8> -> IntN<B8> ---------- */
        [TestCase(0u, 0)]
        [TestCase(255u, -1)]    // 0xFF → signed = -1
        public void Explicit_UIntN_B8_To_IntN_B8(uint raw, int expected)
        {
            var u = new UIntN<B8>(raw);
            var i = (IntN<B8>)u;
            Assert.That(i.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- OPERATEURS ARITHMETIQUES ---
         * +, -, *, /, %, ++, --
         ==================================*/
        #region --- OPERATEURS ARITHMÉTIQUES (exhaustif, UIntN<B8>) ---

        /* ---------- Addition ---------- */
        [TestCase(0u, 0u, 0u)]
        [TestCase(1u, 1u, 2u)]
        [TestCase(127u, 128u, 255u)]
        [TestCase(128u, 128u, 0u)]       // 256 → 0
        [TestCase(200u, 56u, 0u)]       // 256 → 0
        [TestCase(250u, 10u, 4u)]       // 260 → 4
        [TestCase(255u, 0u, 255u)]
        [TestCase(255u, 1u, 0u)]       // wrap
        [TestCase(255u, 255u, 254u)]
        public void UIntN_Add_Wrap(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That((x + y).Raw, Is.EqualTo(expected));
        }

        /* ---------- Soustraction ---------- */
        [TestCase(0u, 0u, 0u)]
        [TestCase(1u, 1u, 0u)]
        [TestCase(1u, 2u, 255u)]      // underflow
        [TestCase(0u, 255u, 1u)]
        [TestCase(255u, 1u, 254u)]
        [TestCase(200u, 250u, 206u)]      // 200-250 = -50 → 206
        [TestCase(255u, 255u, 0u)]
        public void UIntN_Sub_Wrap(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That((x - y).Raw, Is.EqualTo(expected));
        }

        /* ---------- Multiplication ---------- */
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 123u, 0u)]
        [TestCase(1u, 200u, 200u)]
        [TestCase(2u, 3u, 6u)]
        [TestCase(16u, 16u, 0u)]      // 256 → 0
        [TestCase(25u, 11u, 19u)]     // 275→19
        [TestCase(100u, 3u, 44u)]     // 300→44
        [TestCase(255u, 2u, 254u)]    // 510→254
        [TestCase(255u, 255u, 1u)]      // 65025→1
        public void UIntN_Mul_Wrap(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That((x * y).Raw, Is.EqualTo(expected));
        }

        /* ---------- Division (÷0 séparé) ---------- */
        [TestCase(0u, 1u, 0u)]
        [TestCase(255u, 1u, 255u)]
        [TestCase(255u, 255u, 1u)]
        [TestCase(255u, 2u, 127u)]
        [TestCase(100u, 4u, 25u)]
        [TestCase(5u, 10u, 0u)]
        public void UIntN_Div_Wrap(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That((x / y).Raw, Is.EqualTo(expected));
        }

        [Test]
        public void UIntN_Div_By_Zero_Throws()
        {
            var a = new UIntN<B8>(42);
            var b = new UIntN<B8>(0);
            Assert.Throws<DivideByZeroException>(() => _ = a / b);
        }

        /* ---------- Modulo (b≠0) ---------- */
        [TestCase(0u, 7u, 0u)]
        [TestCase(13u, 5u, 3u)]
        [TestCase(255u, 10u, 5u)]
        [TestCase(260u, 3u, 1u)]       // 4 & 0xFF = 4, 4 % 3 = 1
        [TestCase(5u, 10u, 5u)]
        public void UIntN_Mod_Wrap(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That((x % y).Raw, Is.EqualTo(expected));
        }

        [Test]
        public void UIntN_Mod_By_Zero_Throws()
        {
            var a = new UIntN<B8>(123);
            var b = new UIntN<B8>(0);
            Assert.Throws<DivideByZeroException>(() => _ = a % b);
        }

        /* ---------- Incrément / Décrément ---------- */
        [TestCase(0u, 1u)]
        [TestCase(255u, 0u)]    // wrap
        public void UIntN_Increment_Wrap(uint start, uint expected)
        {
            var v = new UIntN<B8>(start);
            v++;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        [TestCase(1u, 0u)]
        [TestCase(0u, 255u)]   // wrap
        public void UIntN_Decrement_Wrap(uint start, uint expected)
        {
            var v = new UIntN<B8>(start);
            v--;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- METHODES STATIQUES POUR ARITHMETIQUE ---
         * Add, Sub, Mul, Div, Mod
         ==================================*/
        #region --- OPERATEURS ARITHMÉTIQUES (exhaustif, UIntN<B8>) ---

        /* ========== 1. Opérateur + et alias Add ========== */
        [TestCase(0u, 0u, 0u)]
        [TestCase(1u, 254u, 255u)]
        [TestCase(128u, 128u, 0u)]     // 256 wrap
        [TestCase(255u, 255u, 254u)]   // overflow profond
        public void UIntN_Add_Both_Paths(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x + y).Raw, Is.EqualTo(expected));   // operator +
                Assert.That(UIntN<B8>.Add(x, y).Raw, Is.EqualTo(expected)); // alias Add
            });
        }

        /* ========== 2. Opérateur – et alias Sub ========== */
        [TestCase(0u, 1u, 255u)]
        [TestCase(200u, 200u, 0u)]
        [TestCase(1u, 255u, 2u)]     // 1-255 = -254 → 2
        public void UIntN_Sub_Both_Paths(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x - y).Raw, Is.EqualTo(expected));
                Assert.That(UIntN<B8>.Sub(x, y).Raw, Is.EqualTo(expected));
            });
        }

        /* ========== 3. Opérateur * et alias Mul ========== */
        [TestCase(0u, 123u, 0u)]
        [TestCase(25u, 11u, 19u)]      // 275 wrap
        [TestCase(255u, 2u, 254u)]
        public void UIntN_Mul_Both_Paths(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x * y).Raw, Is.EqualTo(expected));
                Assert.That(UIntN<B8>.Mul(x, y).Raw, Is.EqualTo(expected));
            });
        }

        /* ========== 4. Opérateur / et alias Div ========== */
        [TestCase(255u, 1u, 255u)]
        [TestCase(255u, 2u, 127u)]
        [TestCase(5u, 10u, 0u)]
        public void UIntN_Div_Both_Paths(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x / y).Raw, Is.EqualTo(expected));
                Assert.That(UIntN<B8>.Div(x, y).Raw, Is.EqualTo(expected));
            });
        }

        [Test]
        public void UIntN_Div_By_Zero_Throws_alias()
        {
            var v = new UIntN<B8>(42);
            Assert.Throws<DivideByZeroException>(() => _ = v / UIntN<B8>.Zero);
            Assert.Throws<DivideByZeroException>(() => _ = UIntN<B8>.Div(v, UIntN<B8>.Zero));
        }

        /* ========== 5. Opérateur % et alias Mod ========== */
        [TestCase(260u, 3u, 1u)]
        [TestCase(13u, 5u, 3u)]
        public void UIntN_Mod_Both_Paths(uint a, uint b, uint expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.Multiple(() =>
            {
                Assert.That((x % y).Raw, Is.EqualTo(expected));
                Assert.That(UIntN<B8>.Mod(x, y).Raw, Is.EqualTo(expected));
            });
        }

        [Test]
        public void UIntN_Mod_By_Zero_Throws_alias()
        {
            var v = new UIntN<B8>(99);
            Assert.Throws<DivideByZeroException>(() => _ = v % UIntN<B8>.Zero);
            Assert.Throws<DivideByZeroException>(() => _ = UIntN<B8>.Mod(v, UIntN<B8>.Zero));
        }

        /* ========== 6. Incrément / Décrément ========== */
        [TestCase(255u, 0u)]
        [TestCase(0u, 1u)]
        public void UIntN_Increment_Wrap_alias(uint start, uint expected)
        {
            var v = new UIntN<B8>(start);
            v++;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        [TestCase(0u, 255u)]
        [TestCase(1u, 0u)]
        public void UIntN_Decrement_Wrap_alias(uint start, uint expected)
        {
            var v = new UIntN<B8>(start);
            v--;
            Assert.That(v.Raw, Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- PUISSANCE DE 2 (SHIFT SAFE) ---
         * MulPow2, DivPow2, ModPow2
         ==================================*/
        #region --- PUISSANCE DE 2 (SHIFT SAFE)  (exhaustif, UIntN<B8>) ---

        /* ──────── 1. MulPow2 : val << n (wrap 8 bits) ──────── */
        [TestCase(0u, 0, 0u)]      // 0 absorbant
        [TestCase(0u, 7, 0u)]
        [TestCase(1u, 0, 1u)]      // identité
        [TestCase(1u, 7, 128u)]    // plus grand shift sans wrap
        [TestCase(2u, 7, 0u)]      // 256  wrap
        [TestCase(3u, 2, 12u)]     // exemple simple
        [TestCase(64u, 2, 0u)]      // 256 wrap
        [TestCase(127u, 1, 254u)]
        [TestCase(128u, 1, 0u)]      // 256 wrap
        [TestCase(255u, 3, 248u)]    // (0xFF<<3)=0x7F8 & 0xFF = 0xF8 = 248
        public void UIntN_MulPow2_Wrap(uint val, int shift, uint expected)
        {
            var res = UIntN<B8>.MulPow2(new UIntN<B8>(val), shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(8)]   // 8 == Bits ⇒ erreur
        [TestCase(32)]
        public void UIntN_MulPow2_InvalidShift(int shift)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UIntN<B8>.MulPow2(UIntN<B8>.One, shift));
        }

        /* ──────── 2. DivPow2 : val >> n ──────── */
        [TestCase(0u, 0, 0u)]
        [TestCase(0u, 7, 0u)]
        [TestCase(1u, 0, 1u)]
        [TestCase(1u, 7, 0u)]
        [TestCase(255u, 1, 127u)]   // /2
        [TestCase(255u, 7, 1u)]     // /128
        [TestCase(32u, 3, 4u)]
        [TestCase(128u, 0, 128u)]
        [TestCase(5u, 7, 0u)]     // num < 2^shift
        public void UIntN_DivPow2_Wrap(uint val, int shift, uint expected)
        {
            var res = UIntN<B8>.DivPow2(new UIntN<B8>(val), shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(8)]
        [TestCase(32)]
        public void UIntN_DivPow2_InvalidShift(int shift)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UIntN<B8>.DivPow2(UIntN<B8>.One, shift));
        }

        /* ──────── 3. ModPow2 : val & (2^n – 1) ──────── */
        [TestCase(0u, 0, 0u)]
        [TestCase(255u, 0, 0u)]
        [TestCase(255u, 8, 255u)]   // plein octet
        [TestCase(77u, 3, 5u)]     // 0b01001101 & 0b111 = 0b101
        [TestCase(250u, 7, 122u)]   // masque 0x7F
        [TestCase(123u, 1, 1u)]     // &1
        [TestCase(123u, 4, 11u)]    // &0x0F
        public void UIntN_ModPow2_Wrap(uint val, int shift, uint expected)
        {
            var res = UIntN<B8>.ModPow2(new UIntN<B8>(val), shift);
            Assert.That(res.Raw, Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(9)]     // Bits+1
        [TestCase(33)]
        public void UIntN_ModPow2_InvalidShift(int shift)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UIntN<B8>.ModPow2(UIntN<B8>.One, shift));
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
        #region --- OPÉRATIONS BITWISE (exhaustives, UIntN<B8>) ---

        // ──────────────────  &  ──────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 0u)]
        [TestCase(255u, 170u, 170u)]
        [TestCase(0b10101010u, 0b11001100u, 0b10001000u)]
        public void And(uint a, uint b, uint expected)
            => Assert.That((new UIntN<B8>(a) & new UIntN<B8>(b)).Raw, Is.EqualTo(expected));

        // ──────────────────  |  ──────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 170u)]
        [TestCase(255u, 170u, 255u)]
        [TestCase(0b10101010u, 0b11001100u, 0b11101110u)]
        public void Or(uint a, uint b, uint expected)
            => Assert.That((new UIntN<B8>(a) | new UIntN<B8>(b)).Raw, Is.EqualTo(expected));

        // ──────────────────  ^  ──────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 170u)]
        [TestCase(200u, 200u, 0u)]
        [TestCase(0b10101010u, 0b11001100u, 0b01100110u)]
        public void Xor(uint a, uint b, uint expected)
            => Assert.That((new UIntN<B8>(a) ^ new UIntN<B8>(b)).Raw, Is.EqualTo(expected));

        // ──────────────────  ~  ──────────────────
        [TestCase(0u, 255u)]
        [TestCase(255u, 0u)]
        [TestCase(0b10101010u, 0b01010101u)]
        public void Not(uint a, uint expected)
            => Assert.That((~new UIntN<B8>(a)).Raw, Is.EqualTo(expected));

        // ───────────────  <<  (LEFT)  ───────────────
        [TestCase(170u, 0, 170u)]   // identité
        [TestCase(1u, 1, 2u)]
        [TestCase(1u, 7, 128u)]   // shift max sans wrap
        [TestCase(1u, 8, 0u)]     // shift == 8  → wrap complet
        [TestCase(1u, 12, 0u)]     // 12 & 31 = 12, wrap 0
        [TestCase(1u, -1, 0u)]     // -1 & 31 = 31, wrap 0
        public void LeftShift(uint val, int n, uint expected)
            => Assert.That((new UIntN<B8>(val) << n).Raw, Is.EqualTo(expected));

        // ───────────────  >>  (RIGHT) ───────────────
        [TestCase(170u, 0, 170u)]   // identité
        [TestCase(240u, 4, 15u)]
        [TestCase(255u, 1, 127u)]
        [TestCase(128u, 7, 1u)]     // shift max
        [TestCase(255u, 8, 0u)]     // shift >= 8 → 0
        [TestCase(255u, 32, 255u)]  // 32 & 31 = 0
        [TestCase(255u, -1, 0u)]    // -1 & 31 = 31
        public void RightShift(uint val, int n, uint expected)
            => Assert.That((new UIntN<B8>(val) >> n).Raw, Is.EqualTo(expected));

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
        #region --- METHODE STATIQUE BITWISE (alias) (exhaustif, UIntN<B8>) ---

        // ───────────────────────────────
        //   And
        // ───────────────────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 0u)]
        [TestCase(255u, 170u, 170u)]                 // plein masque
        [TestCase(0b10101010u, 0b11001100u, 0b10001000u)]
        public void UIntN_And(uint a, uint b, uint expected)
            => Assert.That(UIntN<B8>.And(new(a), new(b)).Raw, Is.EqualTo(expected));

        // ───────────────────────────────
        //   Or
        // ───────────────────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 170u)]
        [TestCase(255u, 170u, 255u)]                 // plein masque
        [TestCase(0b10101010u, 0b11001100u, 0b11101110u)]
        public void UIntN_Or(uint a, uint b, uint expected)
            => Assert.That(UIntN<B8>.Or(new(a), new(b)).Raw, Is.EqualTo(expected));

        // ───────────────────────────────
        //   Xor
        // ───────────────────────────────
        [TestCase(0u, 0u, 0u)]
        [TestCase(0u, 170u, 170u)]                 // A ^ 0 = A
        [TestCase(200u, 200u, 0u)]                   // A ^ A = 0
        [TestCase(0b10101010u, 0b11001100u, 0b01100110u)]
        public void UIntN_Xor(uint a, uint b, uint expected)
            => Assert.That(UIntN<B8>.Xor(new(a), new(b)).Raw, Is.EqualTo(expected));

        // ───────────────────────────────
        //   Not
        // ───────────────────────────────
        [TestCase(0u, 255u)]                       // ~0  = 0xFF
        [TestCase(255u, 0u)]                         // ~FF = 0
        [TestCase(0b10101010u, 0b01010101u)]
        public void UIntN_Not(uint val, uint expected)
            => Assert.That(UIntN<B8>.Not(new(val)).Raw, Is.EqualTo(expected));

        // ───────────────────────────────
        //   Nand / Nor / Xnor
        // ───────────────────────────────
        [Test]
        public void UIntN_Nand_Equals_NotAnd()
        {
            var a = new UIntN<B8>(0xAA);
            var b = new UIntN<B8>(0xCC);
            Assert.That(UIntN<B8>.Nand(a, b).Raw,
                        Is.EqualTo(UIntN<B8>.Not(UIntN<B8>.And(a, b)).Raw));
        }

        [Test]
        public void UIntN_Nor_Equals_NotOr()
        {
            var a = new UIntN<B8>(0xAA);
            var b = new UIntN<B8>(0xCC);
            Assert.That(UIntN<B8>.Nor(a, b).Raw,
                        Is.EqualTo(UIntN<B8>.Not(UIntN<B8>.Or(a, b)).Raw));
        }

        [Test]
        public void UIntN_Xnor_Equals_NotXor()
        {
            var a = new UIntN<B8>(0xAA);
            var b = new UIntN<B8>(0xCC);
            Assert.That(UIntN<B8>.Xnor(a, b).Raw,
                        Is.EqualTo(UIntN<B8>.Not(UIntN<B8>.Xor(a, b)).Raw));
        }

        // ───────────────────────────────
        //   Shl (left‑shift) – wrap 8 bits
        // ───────────────────────────────
        [TestCase(170u, 0, 170u)]    // identité
        [TestCase(1u, 1, 2u)]
        [TestCase(1u, 7, 128u)]    // shift max sans wrap
        [TestCase(1u, 8, 0u)]    // shift == 8  -> wrap complet
        [TestCase(1u, 12, 0u)]     // 12 & 31 = 12, wrap 0
        [TestCase(1u, -1, 0u)]     // -1 & 31 = 31, wrap 0
        public void UIntN_Shl(uint val, int n, uint expected)
            => Assert.That(UIntN<B8>.Shl(new(val), n).Raw, Is.EqualTo(expected));

        // ───────────────────────────────
        //   Shr (right‑shift)
        // ───────────────────────────────
        [TestCase(255u, 0, 255u)]    // identité
        [TestCase(240u, 4, 15u)]
        [TestCase(128u, 7, 1u)]
        [TestCase(255u, 8, 0u)]    // shift >= 8 -> 0
        [TestCase(255u, 32, 255u)]   // 32 & 31 = 0
        [TestCase(255u, -1, 0u)]    // -1 & 31 = 31
        public void UIntN_Shr(uint val, int n, uint expected)
            => Assert.That(UIntN<B8>.Shr(new(val), n).Raw, Is.EqualTo(expected));

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
        #region --- COMPARAISONS (exhaustif, UIntN<B8>) ---

        // ───────────── Opérateurs == / != ─────────────
        [TestCase(42u, 42u, true)]
        [TestCase(42u, 99u, false)]
        [TestCase(256u, 0u, true)]     // wrap identique
        public void OpEq(uint a, uint b, bool expected)
        {
            Assert.That(new UIntN<B8>(a) == new UIntN<B8>(b), Is.EqualTo(expected));
            Assert.That(new UIntN<B8>(a) != new UIntN<B8>(b), Is.EqualTo(!expected));
        }

        // ───────────── <  /  <=  /  >  /  >= ─────────────
        [TestCase(0u, 255u)]
        [TestCase(10u, 20u)]
        public void Op_Order(uint small, uint big)
        {
            var s = new UIntN<B8>(small);
            var b = new UIntN<B8>(big);

            Assert.Multiple(() =>
            {
                Assert.That(s < b, Is.True);
                Assert.That(s <= b, Is.True);
                Assert.That(b > s, Is.True);
                Assert.That(b >= s, Is.True);
                Assert.That(s >= s && b >= b && s <= s, Is.True); // réflexivité
            });
        }

        // Paires descendantes : big > small
        [TestCase(255u, 0u)]
        public void Op_Order_Descending(uint big, uint small)
        {
            var b = new UIntN<B8>(big);
            var s = new UIntN<B8>(small);

            Assert.Multiple(() =>
            {
                Assert.That(b > s, Is.True);
                Assert.That(b >= s, Is.True);
                Assert.That(s < b, Is.True);
                Assert.That(s <= b, Is.True);
            });
        }

        // ───────────── Equals(object) ─────────────
        [Test]
        public void Equals_Object_Cases()
        {
            var a = new UIntN<B8>(77);
            object same = new UIntN<B8>(77);
            object other = new UIntN<B8>(78);
            object notNum = "string";

            Assert.That(a.Equals(same), Is.True);
            Assert.That(a.Equals(other), Is.False);
            Assert.That(a.Equals(null), Is.False);
            Assert.That(a.Equals(notNum), Is.False);
        }

        // ───────────── GetHashCode ─────────────
        [Test]
        public void HashCode_Consistency()
        {
            var x1 = new UIntN<B8>(123);
            var x2 = new UIntN<B8>(123);
            var y = new UIntN<B8>(42);

            Assert.That(x1.GetHashCode(), Is.EqualTo(x2.GetHashCode()));
            Assert.That(x1.GetHashCode(), Is.Not.EqualTo(y.GetHashCode()));
        }

        // ───────────── Méthodes statiques Eq / Neq ... ─────────────
        [TestCase(33u, 33u, true)]
        [TestCase(33u, 44u, false)]
        public void Static_Eq_Neq(uint a, uint b, bool expected)
        {
            var x = new UIntN<B8>(a);
            var y = new UIntN<B8>(b);
            Assert.That(UIntN<B8>.Eq(x, y), Is.EqualTo(expected));
            Assert.That(UIntN<B8>.Neq(x, y), Is.EqualTo(!expected));
        }

        [TestCase(3u, 5u)]
        [TestCase(0u, 255u)]
        public void Static_Lt_Gt(uint small, uint big)
        {
            var s = new UIntN<B8>(small);
            var b = new UIntN<B8>(big);
            Assert.That(UIntN<B8>.Lt(s, b), Is.True);
            Assert.That(UIntN<B8>.Gt(b, s), Is.True);
        }

        [TestCase(255u, 0u)]
        public void Static_Lt_Gt_Descending(uint big, uint small)
        {
            var b = new UIntN<B8>(big);
            var s = new UIntN<B8>(small);

            Assert.That(UIntN<B8>.Gt(b, s), Is.True);
            Assert.That(UIntN<B8>.Lt(s, b), Is.True);
        }

        [TestCase(5u, 5u)]
        [TestCase(5u, 7u)]
        public void Static_Lte(uint a, uint b)
        {
            Assert.That(UIntN<B8>.Lte(new(a), new(b)), Is.EqualTo(a <= b));
        }

        [TestCase(8u, 8u)]
        [TestCase(8u, 2u)]
        public void Static_Gte(uint a, uint b)
        {
            Assert.That(UIntN<B8>.Gte(new(a), new(b)), Is.EqualTo(a >= b));
        }

        // ───────────── IsZero helper ─────────────
        [TestCase(0u, true)]
        [TestCase(1u, false)]
        [TestCase(256u, true)]   // wrap vers 0
        public void Static_IsZero(uint val, bool expected)
        {
            Assert.That(UIntN<B8>.IsZero(new(val)), Is.EqualTo(expected));
        }

        #endregion

        /*==================================
         * --- OPERATIONS UTILITAIRES ---
         * Min
         * Max
         * Avg
         * IsPowerOfTwo
         ==================================*/
        #region --- OPERATIONS UTILITAIRES (exhaustif, UIntN<B8>) ---

        // ────────── Min / Max ──────────
        [TestCase(10u, 20u, 10u, 20u)]
        [TestCase(20u, 10u, 10u, 20u)]    // ordre inversé
        [TestCase(42u, 42u, 42u, 42u)]    // égalité
        [TestCase(0u, 255u, 0u, 255u)]    // extrêmes
        public void Min_Max(uint x, uint y, uint expectedMin, uint expectedMax)
        {
            var a = new UIntN<B8>(x);
            var b = new UIntN<B8>(y);

            Assert.That(UIntN<B8>.Min(a, b).Raw, Is.EqualTo(expectedMin));
            Assert.That(UIntN<B8>.Max(a, b).Raw, Is.EqualTo(expectedMax));
        }

        // ────────── Avg (moyenne arithm. sur 8 bits) ──────────
        [TestCase(0u, 0u, 0u)]      // identité
        [TestCase(10u, 20u, 15u)]      // valeur paire
        [TestCase(5u, 6u, 5u)]      // troncature (11/2 = 5)
        [TestCase(250u, 250u, 122u)]     // wrap avant /2
        [TestCase(255u, 1u, 0u)]     // 255+1 = 0   → 0/2
        public void Avg(uint x, uint y, uint expected)
        {
            Assert.That(UIntN<B8>.Avg(new(x), new(y)).Raw, Is.EqualTo(expected));
            // commutativité
            Assert.That(UIntN<B8>.Avg(new(y), new(x)).Raw, Is.EqualTo(expected));
        }

        // ────────── IsPowerOfTwo ──────────
        [TestCase(1u, true)]   // 2⁰
        [TestCase(2u, true)]
        [TestCase(4u, true)]
        [TestCase(128u, true)]   // 2⁷
        [TestCase(0u, false)]  // jamais
        [TestCase(3u, false)]
        [TestCase(96u, false)]  // multiple de 32 mais pas puissance exacte
        [TestCase(256u, false)]  // wrap -> 0
        public void IsPowerOfTwo(uint val, bool expected)
        {
            Assert.That(UIntN<B8>.IsPowerOfTwo(new(val)), Is.EqualTo(expected));
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
        #region --- SATURATION (exhaustif, UIntN<B8>) ---
        // ===== AddSat =====
        [TestCase(10u, 20u, 30u)]   // aucun overflow
        [TestCase(250u, 10u, 255u)]   // overflow saturé (B8)
        [TestCase(128u, 127u, 255u)]   // bord supérieur (B8)
        [TestCase(60000u, 6000u, 65535u)] // overflow saturé (B16)
        public void AddSat(uint rawA, uint rawB, uint expected)
        {
            // B8 ou B16 déduits du grand max
            if (expected <= 0xFF)
            {
                var a = new UIntN<B8>(rawA);
                var b = new UIntN<B8>(rawB);
                Assert.That(UIntN<B8>.AddSat(a, b).Raw, Is.EqualTo(expected));
            }
            else
            {
                var a = new UIntN<B16>(rawA);
                var b = new UIntN<B16>(rawB);
                Assert.That(UIntN<B16>.AddSat(a, b).Raw, Is.EqualTo(expected));
            }
        }

        // ===== SubSat =====
        [TestCase(30u, 20u, 10u)]
        [TestCase(10u, 20u, 0u)]     // underflow saturé
        [TestCase(5000u, 123u, 4877u)]
        public void SubSat(uint rawA, uint rawB, uint expected)
        {
            var a = new UIntN<B16>(rawA);
            var b = new UIntN<B16>(rawB);
            Assert.That(UIntN<B16>.SubSat(a, b).Raw, Is.EqualTo(expected));
        }

        // ===== MulSat =====
        [TestCase(10u, 10u, 100u)]
        [TestCase(16u, 20u, 255u)]     // overflow (B8)
        [TestCase(4000u, 20000u, 65535u)] // overflow (B16)
        public void MulSat(uint rawA, uint rawB, uint expected)
        {
            if (expected <= 0xFF)
                Assert.That(UIntN<B8>.MulSat(new UIntN<B8>(rawA), new UIntN<B8>(rawB)).Raw,
                            Is.EqualTo(expected));
            else
                Assert.That(UIntN<B16>.MulSat(new UIntN<B16>(rawA), new UIntN<B16>(rawB)).Raw,
                            Is.EqualTo(expected));
        }

        // ===== Clamp =====
        [TestCase(12u, 10u, 42u, 12u)]   // inside
        [TestCase(5u, 10u, 42u, 10u)]   // below
        [TestCase(77u, 10u, 42u, 42u)]   // above
        public void Clamp(uint v, uint min, uint max, uint expected)
        {
            var res = UIntN<B8>.Clamp(new UIntN<B8>(v),
                                      new UIntN<B8>(min),
                                      new UIntN<B8>(max)).Raw;
            Assert.That(res, Is.EqualTo(expected));
        }

        // ===== Clamp01 =====
        [TestCase(0u, 0u)]
        [TestCase(1u, 1u)]
        [TestCase(77u, 1u)]
        public void Clamp01(uint raw, uint expected)
        {
            Assert.That(UIntN<B8>.Clamp01(new UIntN<B8>(raw)).Raw,
                        Is.EqualTo(expected));
        }

        // ===== ClampWithOffset =====
        [TestCase(5u, 10u, 42u, 5, 7, 15u)]      // below + offsetMin
        [TestCase(77u, 10u, 42u, 5, 7, 49u)]     // above + offsetMax
        [TestCase(12u, 10u, 42u, 5, 7, 12u)]     // inside unchanged
        [TestCase(1u, 2u, 4u, -5, 3, 0u)]        // offset négatif, clamp à 0
        [TestCase(7u, 2u, 4u, 5, 251, 255u)]     // offsetMax pousse au‑delà de 255

        // nouveaux cas utiles pour la couverture exhaustive :
        [TestCase(200u, 100u, 250u, 10, -10, 200u)]    // dans [110,240] => inchangé
        [TestCase(90u, 100u, 250u, 10, -10, 110u)]     // en dessous => clamp min+offsetMin
        [TestCase(255u, 100u, 250u, 0, 10, 255u)]      // above max+offsetMax (260 wrap 4) => clamp à 255 (overflow limité par type)
        [TestCase(240u, 100u, 250u, -100, 0, 240u)]    // offset min négatif => clamp à 140
        [TestCase(50u, 100u, 150u, 50, 0, 150u)]       // offsetMin > offsetMax, clamp à min+offsetMin
        [TestCase(200u, 0u, 255u, 0, 0, 200u)]         // full range, pas de clamp
        [TestCase(0u, 10u, 20u, -20, 50, 0u)]         // offset min très négatif, clamp à min+offsetMin
        [TestCase(5u, 250u, 20u, 10, 10, 255u)]         // bornes inversées (min > max), clamp à min+offsetMin

        public void UIntN_ClampWithOffset(uint v, uint min, uint max,
                                   int offMin, int offMax,
                                   uint expected)
        {
            var res = UIntN<B8>.ClampWithOffset(
                          new UIntN<B8>(v),
                          new UIntN<B8>(min),
                          new UIntN<B8>(max),
                          offMin, offMax).Raw;
            Assert.That(res, Is.EqualTo(expected));
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
        #region --- FONCTIONS TRIGONOMÉTRIQUES (exhaustif, UIntN<B8>) ---

        [TestCase(0, 0)]       // Sin(0°) = 0
        [TestCase(64, 127)]    // Sin(90°) = +1.0 => 127
        [TestCase(128, 0)]     // Sin(180°) = 0
        [TestCase(192, -127)]  
        public void Sin_UIntN_B8_Approx(int angle, int expected)
        {
            var x = new UIntN<B8>((uint)angle);
            int result = (int)UIntN<B8>.Sin(x);
            Assert.That(result, Is.EqualTo(expected).Within(1));
        }

        [TestCase(0, 127)]     // Cos(0°) = +1.0
        [TestCase(64, 0)]      // Cos(90°) = 0
        [TestCase(128, -128)]  // Cos(180°) = -1.0
        public void Cos_UIntN_B8_Approx(int angle, int expected)
        {
            var x = new UIntN<B8>((uint)angle);
            int result = (int)UIntN<B8>.Cos(x);
            Assert.That(result, Is.EqualTo(expected).Within(1));
        }

        // Pour la tangente, même principe pour le wrap (sauf si tu retournes déjà int32 saturé)
        [TestCase(0, 0)]      // Tan(0°) = 0
        [TestCase(64, -128)]  // Tan(90°) = inf (handle overflow or saturate) ici -128
        [TestCase(128, 0)]    // Tan(180°) = 0
        public void Tan_UIntN_B8_Approx(int angle, int expected)
        {
            var x = new UIntN<B8>((uint)angle);
            int result = (int)UIntN<B8>.Tan(x);
            Assert.That(result, Is.EqualTo(expected).Within(1));
        }


        [TestCase(127, 0)]     // asin(0) ≈ 0
        [TestCase(255, 127)]   // asin(+1.0) = +π/2
        [TestCase(0, -128)]    // asin(–1.0) = –π/2
        public void Asin_UIntN_B8_Approx(int val, int expected)
        {
            var x = new UIntN<B8>((uint)val);
            var result = UIntN<B8>.Asin(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(2),
                $"asin({val}) attendu≈{expected}, obtenu={result.Raw}");
        }

        [TestCase(255, 0)]     // acos(+1.0) = 0°
        [TestCase(127, 64)]    // acos(0.0)  = 90°
        [TestCase(0, 128)]     // acos(-1.0) = 180°
        public void Acos_UIntN_B8_Approx(int val, int expected)
        {
            var x = new UIntN<B8>((uint)val);
            var result = UIntN<B8>.Acos(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(2),
                $"acos({val}) attendu≈{expected}, obtenu={result.Raw}");
        }

        // atan(0) = 0° ; atan(127/255 ≈ 0.498) ≈ 26.565° ; atan(255/255 = 1) = 45°
        [TestCase(0, 0)]   // 0.0  -> 0°
        [TestCase(127, 38)]   // ~0.498 -> ~26.565° -> 38
        [TestCase(255, 64)]   // 1.0  -> 45° -> 64
        public void Atan_UIntN_B8_Approx(int val, int expected)
        {
            var x = new UIntN<B8>((uint)val);
            var result = UIntN<B8>.Atan(x);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(2));
        }

        // Plein cercle UIntN<B8> : 0°=0, 90°=64, 180°=128, 270°=192
        [TestCase(0, 255, 0)]   // +X
        [TestCase(255, 0, 64)]   // +Y
        [TestCase(0, 0, 0)]   // (0,0) convention → 0
        [TestCase(255, 255, 32)]   // 45°
        [TestCase(1, 255, 0)]   // ~0° (petit y)
        [TestCase(255, 1, 64)]   // ~90° (petit x)
        [TestCase(0, 1, 0)]   // +X (petit)
        [TestCase(0, 128, 0)]   // +X mid
        [TestCase(0, 2, 0)]   // +X très petit
        [TestCase(0, 255, 0)]   // +X répété (inutile mais ok)
        public void Atan2_UIntN_B8_Approx(int y, int x, int expected)
        {
            var a = new UIntN<B8>((uint)y);
            var b = new UIntN<B8>((uint)x);
            var result = UIntN<B8>.Atan2(a, b);
            Assert.That(result.Raw, Is.EqualTo(expected).Within(2),
                $"atan2({y},{x}) attendu≈{expected}, obtenu={result.Raw}");
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
        #region --- MANIPULATION BITS & ROTATIONS (exhaustif, UIntN<B8>) ---

        // ───────── Reverse ─────────
        [TestCase(0u, 0u)]    // 0000 0000
        [TestCase(255u, 255u)]    // 1111 1111
        [TestCase(0b00010110u, 0b01101000u)]  // 0x16 -> 0x68
        public void Reverse_Bits(uint src, uint expected)
        {
            var rev = UIntN<B8>.Reverse(new(src)).Raw;
            Assert.That(rev, Is.EqualTo(expected));
            // involution : Reverse(Reverse(x)) == x
            Assert.That(UIntN<B8>.Reverse(new(rev)).Raw, Is.EqualTo(src));
        }

        // ───────── PopCount ─────────
        [TestCase(0u, 0)]
        [TestCase(255u, 8)]
        [TestCase(0b11010101u, 5)]
        [TestCase(0b10000000u, 1)]
        public void PopCount_Bits(uint src, int expected)
            => Assert.That(UIntN<B8>.PopCount(new(src)), Is.EqualTo(expected));

        // ───────── Parity (odd popcount) ─────────
        [TestCase(0u, false)]
        [TestCase(255u, false)]   // 8 ones
        [TestCase(0b11010101u, true)]    // 5 ones
        [TestCase(0b00000001u, true)]
        public void Parity_Bits(uint src, bool expected)
            => Assert.That(UIntN<B8>.Parity(new(src)), Is.EqualTo(expected));

        // ───────── LeadingZeros ─────────
        [TestCase(0u, 8)]
        [TestCase(0b10000000u, 0)]
        [TestCase(0b00000001u, 7)]
        [TestCase(0b00001111u, 4)]
        public void LeadingZeros_Bits(uint src, int expected)
            => Assert.That(UIntN<B8>.LeadingZeros(new(src)), Is.EqualTo(expected));

        // ───────── TrailingZeros ─────────
        [TestCase(0u, 8)]
        [TestCase(0b00000001u, 0)]
        [TestCase(0b10000000u, 7)]
        [TestCase(0b11110000u, 4)]
        public void TrailingZeros_Bits(uint src, int expected)
            => Assert.That(UIntN<B8>.TrailingZeros(new(src)), Is.EqualTo(expected));

        // ───────── Rotate Left (Rol) ─────────
        [TestCase(0b10000001u, 1, 0b00000011u)]
        [TestCase(0b10000001u, 4, 0b00011000u)]
        [TestCase(0b01010101u, 0, 0b01010101u)] // shift 0 : identité
        [TestCase(0b00000001u, 8, 0b00000001u)] // shift ≡ 0 mod 8
        public void Rol(uint src, int n, uint expected)
            => Assert.That(UIntN<B8>.Rol(new(src), n).Raw, Is.EqualTo(expected));

        // ───────── Rotate Right (Ror) ─────────
        [TestCase(0b10000001u, 1, 0b11000000u)]
        [TestCase(0b10000001u, 4, 0b00011000u)]
        [TestCase(0b01010101u, 0, 0b01010101u)]
        [TestCase(0b10000000u, 8, 0b10000000u)]
        public void Ror(uint src, int n, uint expected)
            => Assert.That(UIntN<B8>.Ror(new(src), n).Raw, Is.EqualTo(expected));

        // ───────── Bsr (Bit‑Scan Reverse, MSB index) ─────────
        [TestCase(0u, -1)]
        [TestCase(0b10000000u, 7)]
        [TestCase(0b00000001u, 0)]
        [TestCase(0b00101000u, 5)]
        public void Bsr(uint src, int expected)
            => Assert.That(UIntN<B8>.Bsr(new(src)), Is.EqualTo(expected));

        // ───────── Bsf (Bit‑Scan Forward, LSB index) ─────────
        [TestCase(0u, -1)]
        [TestCase(0b10000000u, 7)]
        [TestCase(0b00000001u, 0)]
        [TestCase(0b00101000u, 3)]
        public void Bsf(uint src, int expected)
            => Assert.That(UIntN<B8>.Bsf(new(src)), Is.EqualTo(expected));

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
        #region --- CONSTANTES (exhaustif, UIntN<B8>) ---

        // ─────────── Valeurs de base ───────────
        [Test] public void Const_Zero() => Assert.That(UIntN<B8>.Zero.Raw, Is.EqualTo(0u));

        [Test] public void Const_One() => Assert.That(UIntN<B8>.One.Raw, Is.EqualTo(1u));

        [Test]
        public void Const_Half()
            => Assert.That(UIntN<B8>.Half.Raw, Is.EqualTo(127u));   // 255 / 2

        [Test]
        public void Const_AllOnes()
            => Assert.That(UIntN<B8>.AllOnes.Raw, Is.EqualTo(255u));

        [Test]
        public void Const_Msb()
        {
            Assert.That(UIntN<B8>.Msb.Raw, Is.EqualTo(128u));
            // MSB doit être Bit(7)
            Assert.That(UIntN<B8>.Bit(7).Raw, Is.EqualTo(UIntN<B8>.Msb.Raw));
        }

        [Test]
        public void Const_Lsb()
        {
            // LSB de 8 bits doit être 1 (00000001)
            Assert.That(UIntN<B8>.Lsb.Raw, Is.EqualTo(1u));
            // LSB doit être Bit(0)
            Assert.That(UIntN<B8>.Bit(0).Raw, Is.EqualTo(UIntN<B8>.Lsb.Raw));
        }

        // ─────────── Bit(n) : 1 << n ───────────
        [TestCase(0, 1u)]
        [TestCase(1, 2u)]
        [TestCase(2, 4u)]
        [TestCase(3, 8u)]
        [TestCase(4, 16u)]
        [TestCase(5, 32u)]
        [TestCase(6, 64u)]
        [TestCase(7, 128u)]
        public void Const_Bit_InRange(int n, uint expected)
            => Assert.That(UIntN<B8>.Bit(n).Raw, Is.EqualTo(expected));

        // Bit(0) doit être identique à One
        [Test]
        public void Bit0_Equals_One()
            => Assert.That(UIntN<B8>.Bit(0).Raw, Is.EqualTo(UIntN<B8>.One.Raw));

        // ─────────── Bit(n) : index hors‑plage ───────────
        [TestCase(-1)]
        [TestCase(8)]
        [TestCase(32)]
        public void Const_Bit_OutOfRange(int n)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UIntN<B8>.Bit(n));
        }

        // ─────────── Relations de cohérence ───────────
        [Test]
        public void Relations_Among_Constants()
        {
            // Half * 2 + 1  = AllOnes   (255)
            var twiceHalfPlusOne = UIntN<B8>.Half + UIntN<B8>.Half + UIntN<B8>.One;
            Assert.That(twiceHalfPlusOne.Raw, Is.EqualTo(UIntN<B8>.AllOnes.Raw));

            // Zero + AllOnes = AllOnes  (wrap conserve)
            var sum = UIntN<B8>.Zero + UIntN<B8>.AllOnes;
            Assert.That(sum.Raw, Is.EqualTo(UIntN<B8>.AllOnes.Raw));
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
        #region --- ACCÈS OCTETS (exhaustif, UIntN<B8>) ---

        // ──────────────────────── Byte(val, n) ────────────────────────
        [TestCase(0xABu, 0, 0xABu)]               // index 0
        public void Byte_Static_Valid(uint value, int index, uint expected)
            => Assert.That(UIntN<B8>.Byte(new(value), index), Is.EqualTo(expected));

        [TestCase(-1)]             // n < 0
        [TestCase(1)]             // n >= 1  (hors plage pour B8)
        public void Byte_Static_OutOfRange(int index)
        {
            var v = new UIntN<B8>(0xAB);
            Assert.Throws<ArgumentOutOfRangeException>(() => UIntN<B8>.Byte(v, index));
        }

        // ──────────────────────── ToBytes / FromBytes ─────────────────
        [TestCase(0x00u)]
        [TestCase(0x5Cu)]
        [TestCase(0xFFu)]
        public void ToBytes_FromBytes_Roundtrip(uint src)
        {
            var a = new UIntN<B8>(src);
            var arr = a.ToBytes();
            Assert.That(arr.Length, Is.EqualTo(1));
            Assert.That(arr[0], Is.EqualTo((byte)src));

            var b = UIntN<B8>.FromBytes(arr);
            Assert.That(b.Raw, Is.EqualTo(src));
        }

        [Test]
        public void FromBytes_NullOrWrongLength()
        {
            // null  → NullReferenceException
            Assert.Throws<NullReferenceException>(() =>
                UIntN<B8>.FromBytes(null));

            // longueur 0 → ArgumentException (vérification interne)
            Assert.Throws<ArgumentException>(() =>
                UIntN<B8>.FromBytes(Array.Empty<byte>()));

            // longueur > 1 : pas d’exception, on prend le premier octet
            byte[] arr = { 0x12, 0x34 };
            var v = UIntN<B8>.FromBytes(arr);
            Assert.That(v.Raw, Is.EqualTo(0x12u));
        }

        // ──────────────────────── GetByte ─────────────────
        [TestCase(0xA5u, 0, 0xA5u)]
        public void GetByte_Valid(uint src, int n, uint expected)
            => Assert.That(new UIntN<B8>(src).GetByte(n), Is.EqualTo(expected));

        [TestCase(1)]
        [TestCase(-1)]
        public void GetByte_OutOfRange(int n)
        {
            var v = new UIntN<B8>(0xA5);
            Assert.Throws<ArgumentOutOfRangeException>(() => v.GetByte(n));
        }

        // ──────────────────────── SetByte (immutabilité) ─────────────────
        [Test]
        public void SetByte_Valid_And_Immutable()
        {
            var original = new UIntN<B8>(0x0F);
            var changed = original.SetByte(0, 0xAA);

            Assert.That(changed.Raw, Is.EqualTo(0xAAu));
            Assert.That(original.Raw, Is.EqualTo(0x0Fu));   // l’instance source reste inchangée
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void SetByte_OutOfRange(int n)
        {
            var v = new UIntN<B8>(0x0F);
            Assert.Throws<ArgumentOutOfRangeException>(() => v.SetByte(n, 0xFF));
        }

        // ──────────────────────── ReplaceByte ─────────────────
        [Test]
        public void ReplaceByte_Valid()
        {
            var a = new UIntN<B8>(0xF0);
            var b = new UIntN<B8>(0x12);

            var res = a.ReplaceByte(0, b);
            Assert.That(res.Raw, Is.EqualTo(0x12u));
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void ReplaceByte_OutOfRange(int n)
        {
            var a = new UIntN<B8>(0xF0);
            var b = new UIntN<B8>(0x12);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.ReplaceByte(n, b));
        }

        #endregion

        /*==================================
         * --- CONVERSION EN CHAÎNE (STRING) ---
         * ToString
         * DebugString
         * ToBinaryString
         * ToHexString
         ==================================*/
        #region --- CONVERSION EN CHAÎNE (exhaustif, UIntN<B8>) ---

        // ─────────────── ToString() ‑ décimal ───────────────
        [TestCase(0u, "0")]
        [TestCase(1u, "1")]
        [TestCase(255u, "255")]
        [TestCase(256u, "0")]      // wrap 256 → 0
        [TestCase(4294967295u, "255")]    // wrap (‑1) → 255
        public void ToString_Decimal(uint val, string expected)
            => Assert.That(new UIntN<B8>(val).ToString(), Is.EqualTo(expected));

        // ─────────────── DebugString() ───────────────
        // format : UIntN<B8>(val) [bin=XXXXXXXX hex=YY]
        [TestCase(0u, "00000000", "00")]
        [TestCase(1u, "00000001", "01")]
        [TestCase(127u, "01111111", "7F")]
        [TestCase(255u, "11111111", "FF")]
        public void DebugString_Format(uint val, string bin, string hex)
        {
            string expected = $"UIntN<B8>({val}) [bin={bin} hex={hex}]";
            Assert.That(new UIntN<B8>(val).DebugString(), Is.EqualTo(expected));
        }

        // ─────────────── ToBinaryString() ───────────────
        [TestCase(0u, "00000000")]
        [TestCase(1u, "00000001")]
        [TestCase(128u, "10000000")]
        [TestCase(0xAFu, "10101111")]
        [TestCase(255u, "11111111")]
        public void ToBinaryString_8Chars(uint val, string expected)
        {
            string s = new UIntN<B8>(val).ToBinaryString();
            Assert.That(s, Is.EqualTo(expected));
            Assert.That(s.Length, Is.EqualTo(8));          // toujours 8 caractères
        }

        // ─────────────── ToHexString(prefix) ───────────────
        [TestCase(0u, true, "0x00")]
        [TestCase(1u, true, "0x01")]
        [TestCase(0xABu, true, "0xAB")]
        [TestCase(255u, true, "0xFF")]
        [TestCase(0u, false, "00")]
        [TestCase(1u, false, "01")]
        [TestCase(0xABu, false, "AB")]
        [TestCase(255u, false, "FF")]
        public void ToHexString_Uppercase(uint val, bool prefix, string expected)
        {
            string s = new UIntN<B8>(val).ToHexString(prefix);
            Assert.That(s, Is.EqualTo(expected));
            // sans préfixe → exactement 2 caractères
            if (!prefix) Assert.That(s.Length, Is.EqualTo(2));
        }

        // ─────────────── Round‑trip ToBytes / Debug helpers ───────────────
        [Test]
        public void StringRoundTrip_ToBytes()
        {
            var x = new UIntN<B8>(0x4E);          // 78
            string dec = x.ToString();
            var y = new UIntN<B8>(uint.Parse(dec));
            Assert.That(y.Raw, Is.EqualTo(x.Raw));   // round‑trip décimal
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
        [TestCase("255", 255u)]
        [TestCase("42", 42u)]
        [TestCase("260", 4u)]    // wrap (260 & 0xFF)
        [TestCase("-1", 255u)]   // wrap (-1 & 0xFF)
        public void Parse_Decimal_Valid(string s, uint expected)
        {
            var val = UIntN<B8>.Parse(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UIntN<B8>.TryParse(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Décimal erreurs ---
        [TestCase("")]
        [TestCase("abc")]
        [TestCase("99999999999999999")]
        public void Parse_Decimal_Invalid(string s)
        {
            Assert.Throws(Is.TypeOf<FormatException>().Or.TypeOf<OverflowException>(),() => UIntN<B8>.Parse(s));
            Assert.That(UIntN<B8>.TryParse(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Decimal_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.Parse(null));
            Assert.That(UIntN<B8>.TryParse(null, out var _), Is.False);
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
            var val = UIntN<B8>.ParseHex(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UIntN<B8>.TryParseHex(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("0x")]
        [TestCase("xyz")]
        public void Parse_Hex_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.ParseHex(s));
            Assert.That(UIntN<B8>.TryParseHex(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Hex_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.ParseHex(null));
            Assert.That(UIntN<B8>.TryParseHex(null, out var _), Is.False);
        }

        // --- Binaire ---
        [TestCase("0b0", 0u)]
        [TestCase("0b11111111", 255u)]
        [TestCase("11111111", 255u)]
        [TestCase("00000000", 0u)]
        [TestCase("10101010", 170u)]
        public void Parse_Binary_Valid(string s, uint expected)
        {
            var val = UIntN<B8>.ParseBinary(s);
            Assert.That(val.Raw, Is.EqualTo(expected));
            Assert.That(UIntN<B8>.TryParseBinary(s, out var v2), Is.True);
            Assert.That(v2.Raw, Is.EqualTo(expected));
        }

        // --- Binaire erreurs ---
        [TestCase("")]
        [TestCase("0b")]
        [TestCase("21001100")]   // chiffre non-binaire
        public void Parse_Binary_Invalid(string s)
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.ParseBinary(s));
            Assert.That(UIntN<B8>.TryParseBinary(s, out var _), Is.False);
        }

        [Test]
        public void Parse_Binary_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.ParseBinary(null));
            Assert.That(UIntN<B8>.TryParseBinary(null, out var _), Is.False);
        }

        // --- Round-trip JSON natif ---
        [TestCase(0u)]
        [TestCase(127u)]
        [TestCase(255u)]
        public void ToJson_RoundTrip(uint src)
        {
            var a = new UIntN<B8>(src);
            string json = a.ToJson();
            var b = UIntN<B8>.FromJson(json);
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
            var x = UIntN<B8>.FromJson(s);
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
            Assert.Throws<FormatException>(() => UIntN<B8>.FromJson(s));
        }

        [Test]
        public void FromJson_Invalid_Null()
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.FromJson(null));
        }

        [Test]
        public void TryParse_Debug_0b()
        {
            Assert.That(UIntN<B8>.TryParse("0b", out var _), Is.False, "TryParse doit retourner false pour '0b'");
            Assert.That(UIntN<B8>.TryParseHex("0b", out var _), Is.False, "TryParseHex doit retourner false pour '0b'");
            Assert.That(UIntN<B8>.TryParseBinary("0b", out var _), Is.False, "TryParseBinary doit retourner false pour '0b'");
        }

        #endregion

        /*==================================
         * --- SERIALISATION META ---
         * ToJsonWithMeta
         * FromJsonWithMeta
         ==================================*/
        #region --- SERIALISATION META (exhaustif, multi-N, erreurs) ---

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(255u)]
        public void ToJsonWithMeta_RoundTrip_B8(uint raw)
        {
            var a = new UIntN<B8>(raw);
            string json = a.ToJsonWithMeta();
            var b = UIntN<B8>.FromJsonWithMeta<B8>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(65535u)]
        public void ToJsonWithMeta_RoundTrip_B16(uint raw)
        {
            var a = new UIntN<B16>(raw);
            string json = a.ToJsonWithMeta();
            var b = UIntN<B16>.FromJsonWithMeta<B16>(json);
            Assert.That(b.Raw, Is.EqualTo(a.Raw));
        }

        // --- Erreur : meta bits non concordants ---
        [Test]
        public void FromJsonWithMeta_BitsMismatch_Throws()
        {
            var a = new UIntN<B8>(123);
            string json = a.ToJsonWithMeta();
            Assert.Throws<FormatException>(() => UIntN<B16>.FromJsonWithMeta<B16>(json));
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
            Assert.Throws<FormatException>(() => UIntN<B8>.FromJsonWithMeta<B8>(json));
        }

        // --- Test spécifique pour null ---
        [Test]
        public void FromJsonWithMeta_Null_Throws()
        {
            Assert.Throws<FormatException>(() => UIntN<B8>.FromJsonWithMeta<B8>(null));
        }

        #endregion

    }
}

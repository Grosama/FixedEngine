using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class ExpConstsTests
    {
        [Test]
        public void E_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                // Calcule la valeur "idéale" en 64 bits, puis cast en int32 overflow (hardware wrap)
                long unwrapped = (long)System.Math.Round(System.Math.E * (1L << i));
                int expected = unchecked((int)unwrapped); // wrap signed comme en hardware
                int actual = FixedEngine.Math.ExpConsts.E_Q[i];

                Assert.That(actual, Is.EqualTo(expected),
                    $"E_Q[{i}] : {actual} (bits 0x{actual:X8}) ≠ e*2^{i} wrapped = {expected} (bits 0x{expected:X8})");
            }
        }

        [Test]
        public void INV_E_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long unwrapped = (long)System.Math.Round((1.0 / System.Math.E) * (1L << i));
                int expected = unchecked((int)unwrapped);
                int actual = FixedEngine.Math.ExpConsts.INV_E_Q[i];

                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_E_Q[{i}] : {actual} (bits 0x{actual:X8}) ≠ (1/e)*2^{i} wrapped = {expected} (bits 0x{expected:X8})");
            }
        }
    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class MiscConstsTests
    {
        [Test]
        public void PHI_Q_BitFaithful()
        {
            double phi = (1.0 + System.Math.Sqrt(5.0)) / 2.0;
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(phi * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.MiscConsts.PHI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"PHI_Q[{i}]: {actual} ≠ phi*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_PHI_Q_BitFaithful()
        {
            double invPhi = 2.0 / (1.0 + System.Math.Sqrt(5.0)); // ou 1/phi
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(invPhi * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.MiscConsts.INV_PHI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_PHI_Q[{i}]: {actual} ≠ 1/phi*2^{i}={expected}");
            }
        }

        [Test]
        public void GAMMA_Q_BitFaithful()
        {
            double gamma = 0.5772156649015328606; // Constante d'Euler-Mascheroni
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(gamma * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.MiscConsts.GAMMA_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"GAMMA_Q[{i}]: {actual} ≠ gamma*2^{i}={expected}");
            }
        }

        [Test]
        public void CATALAN_Q_BitFaithful()
        {
            double catalan = 0.91596559417721901505;
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(catalan * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.MiscConsts.CATALAN_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"CATALAN_Q[{i}]: {actual} ≠ catalan*2^{i}={expected}");
            }
        }

        [Test]
        public void ZETA3_Q_BitFaithful()
        {
            double zeta3 = 1.2020569031595942854; // Apery's constant
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(zeta3 * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.MiscConsts.ZETA3_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"ZETA3_Q[{i}]: {actual} ≠ zeta3*2^{i}={expected}");
            }
        }
    }
}

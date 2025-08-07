using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class PhiConstsTests
    {
        [Test]
        public void LN_PHI_Q_BitFaithful()
        {
            double lnPhi = System.Math.Log((1.0 + System.Math.Sqrt(5.0)) / 2.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(lnPhi * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.PhiConsts.LN_PHI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LN_PHI_Q[{i}]: {actual} ≠ ln(phi)*2^{i}={expected}");
            }
        }

        [Test]
        public void LOG2_PHI_Q_BitFaithful()
        {
            double log2Phi = System.Math.Log((1.0 + System.Math.Sqrt(5.0)) / 2.0, 2.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(log2Phi * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.PhiConsts.LOG2_PHI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LOG2_PHI_Q[{i}]: {actual} ≠ log2(phi)*2^{i}={expected}");
            }
        }

        [Test]
        public void SQRT_PHI_Q_BitFaithful()
        {
            double sqrtPhi = System.Math.Sqrt((1.0 + System.Math.Sqrt(5.0)) / 2.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(sqrtPhi * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.PhiConsts.SQRT_PHI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"SQRT_PHI_Q[{i}]: {actual} ≠ sqrt(phi)*2^{i}={expected}");
            }
        }
    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class SqrtConstsTests
    {
        [Test]
        public void SQRT2_Q_BitFaithful()
        {
            double valBase = System.Math.Sqrt(2.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.SQRT2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"SQRT2_Q[{i}]: {actual} ≠ sqrt(2)*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_SQRT2_Q_BitFaithful()
        {
            double valBase = 1.0 / System.Math.Sqrt(2.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.INV_SQRT2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_SQRT2_Q[{i}]: {actual} ≠ 1/sqrt(2)*2^{i}={expected}");
            }
        }

        [Test]
        public void SQRT3_Q_BitFaithful()
        {
            double valBase = System.Math.Sqrt(3.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.SQRT3_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"SQRT3_Q[{i}]: {actual} ≠ sqrt(3)*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_SQRT3_Q_BitFaithful()
        {
            double valBase = 1.0 / System.Math.Sqrt(3.0);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.INV_SQRT3_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_SQRT3_Q[{i}]: {actual} ≠ 1/sqrt(3)*2^{i}={expected}");
            }
        }

        [Test]
        public void SQRT_PI_Q_BitFaithful()
        {
            double valBase = System.Math.Sqrt(System.Math.PI);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.SQRT_PI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"SQRT_PI_Q[{i}]: {actual} ≠ sqrt(pi)*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_SQRT_PI_Q_BitFaithful()
        {
            double valBase = 1.0 / System.Math.Sqrt(System.Math.PI);
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(valBase * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.SqrtConsts.INV_SQRT_PI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_SQRT_PI_Q[{i}]: {actual} ≠ 1/sqrt(pi)*2^{i}={expected}");
            }
        }
    }
}

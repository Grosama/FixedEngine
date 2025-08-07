using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class TrigConstsTests
    {
        [Test]
        public void PI_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(System.Math.PI * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.PI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"PI_Q[{i}]: {actual} ≠ pi*2^{i}={expected}");
            }
        }

        [Test]
        public void PI2_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(2 * System.Math.PI * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.PI2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"PI2_Q[{i}]: {actual} ≠ 2pi*2^{i}={expected}");
            }
        }

        [Test]
        public void PI_2_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round((System.Math.PI / 2.0) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.PI_2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"PI_2_Q[{i}]: {actual} ≠ pi/2*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_PI_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round((1.0 / System.Math.PI) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.INV_PI_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_PI_Q[{i}]: {actual} ≠ 1/pi*2^{i}={expected}");
            }
        }

        [Test]
        public void INV_PI2_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round((1.0 / (2 * System.Math.PI)) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.INV_PI2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"INV_PI2_Q[{i}]: {actual} ≠ 1/(2pi)*2^{i}={expected}");
            }
        }

        [Test]
        public void DEG_TO_RAD_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round((System.Math.PI / 180.0) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.DEG_TO_RAD_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"DEG_TO_RAD_Q[{i}]: {actual} ≠ pi/180*2^{i}={expected}");
            }
        }

        [Test]
        public void RAD_TO_DEG_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round((180.0 / System.Math.PI) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.TrigConsts.RAD_TO_DEG_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"RAD_TO_DEG_Q[{i}]: {actual} ≠ 180/pi*2^{i}={expected}");
            }
        }
    }
}

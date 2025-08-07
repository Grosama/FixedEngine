using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedEngine.Tests.Math.Consts
{
    [TestFixture]
    public class LogConstsTests
    {
        [Test]
        public void LN2_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(System.Math.Log(2.0) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.LogConsts.LN2_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LN2_Q[{i}]: {actual} ≠ ln(2)*2^{i} = {expected}");
            }
        }

        [Test]
        public void LOG2E_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(System.Math.Log2(System.Math.E) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.LogConsts.LOG2E_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LOG2E_Q[{i}]: {actual} ≠ log2(e)*2^{i} = {expected}");
            }
        }

        [Test]
        public void LN10_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(System.Math.Log(10.0) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.LogConsts.LN10_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LN10_Q[{i}]: {actual} ≠ ln(10)*2^{i} = {expected}");
            }
        }

        [Test]
        public void LOG2_10_Q_BitFaithful()
        {
            for (int i = 0; i <= 32; i++)
            {
                long val = (long)System.Math.Round(System.Math.Log2(10.0) * (1L << i));
                int expected = unchecked((int)val);
                int actual = FixedEngine.Math.Consts.LogConsts.LOG2_10_Q[i];
                Assert.That(actual, Is.EqualTo(expected),
                    $"LOG2_10_Q[{i}]: {actual} ≠ log2(10)*2^{i} = {expected}");
            }
        }
    }
}

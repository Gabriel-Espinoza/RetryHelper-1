﻿using System;
using System.IO;
using NUnit.Framework;
using Retry;

namespace Tests
{
    [TestFixture]
    public sealed class OnFailureTest : AssertionHelper
    {
        private RetryHelper _target;

        [SetUp]
        public void SetUp()
        {
            _target = new RetryHelper();
            _target.DefaultTryInterval = TimeSpan.FromMilliseconds(RetryHelperTest.Interval);
        }

        [Test]
        [Timeout(1000)]
        public void TestOnFailureAfterFiveTimes()
        {
            var times = 5;
            var generator = new Generator(times);
            var onFailureTriggered = 0;
            _target.Try(() => generator.Next())
                   .OnFailure(t =>
                   {
                       Expect(t, False);
                       onFailureTriggered++;
                   })
                   .Until(t => t);
            Expect(onFailureTriggered, EqualTo(times));
        }

        [Test]
        [Timeout(1000)]
        public void TestOnFailureShouldNotFireIfSucceedAtFirstTime()
        {
            var times = 0;
            var generator = new Generator(times);
            var onFailureTriggered = 0;
            _target.Try(() => generator.Next())
                   .OnFailure(t => onFailureTriggered++)
                   .Until(t => t);
            Expect(onFailureTriggered, EqualTo(0));
        }

        [Test]
        public void TestCircularReadStream()
        {
            const int len = 100;
            var stream = new MemoryStream();
            for(int i = 0; i < len; i++)
            {
                stream.WriteByte((byte)i);
            }
            stream.Seek(0, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(stream);
            for(int i = 0; i < len * 3; i++)
            {
                var b = RetryHelper.Instance
                                   .Try(() => binaryReader.ReadByte())
                                   .WithTryInterval(0)
                                   .OnFailure(t => stream.Seek(0, SeekOrigin.Begin))
                                   .UntilNoException<EndOfStreamException>();
                Console.Write("{0} ", b);
            }
        }

        [Test]
        public void TestOnFailureWithTryCount()
        {
            var times = 5;
            var generator = new Generator(times);
            var onFailureTriggered = 0;
            _target.Try(() => generator.Next())
                   .OnFailure((t, count) =>
                   {
                       Expect(t, False);
                       Expect(count, EqualTo(++onFailureTriggered));
                   })
                   .Until(t => t);
            Expect(onFailureTriggered, EqualTo(times));
        }
    }
}

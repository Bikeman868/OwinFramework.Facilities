﻿using System;
using System.Threading;
using NUnit.Framework;
using OwinFramework.Facilities.Cache.Local;
using OwinFramework.InterfacesV1.Facilities;

namespace UnitTests
{
    [TestFixture]
    public class LocalCacheFacilityTests: Moq.Modules.TestBase
    {
        private ICache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CacheFacility();
        }

        [Test]
        public void Should_store_and_retrieve_values()
        {
            var exist1 = _cache.Put("key1", "value1", null);
            var exist2 = _cache.Put("key2", "value2", null);
            var exist3 = _cache.Put("key1", "value3", null);

            Assert.IsFalse(exist1, "Key 1 does not exist");
            Assert.IsFalse(exist2, "Key 2 does not exist");
            Assert.IsTrue(exist3, "Key 1 does exist");

            Assert.AreEqual("value3", _cache.Get("key1", ""));
            Assert.AreEqual("value2", _cache.Get("key2", ""));
        }

        [Test]
        public void Should_lock_values_for_update()
        {
            var lockTime = TimeSpan.FromSeconds(1);
            var sleepTime = TimeSpan.FromMilliseconds(50);

            Action threadAction = () =>
            {
                for (var i = 0; i < 10; i++)
                {
                    var value = _cache.Get("key", 0, lockTime);
                    Thread.Sleep(sleepTime);
                    _cache.Put("key", value + 1);
                }
            };

            var threads = new[]
            {
                new Thread(() => threadAction()),
                new Thread(() => threadAction()),
                new Thread(() => threadAction()),
                new Thread(() => threadAction())
            };

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join(TimeSpan.FromMinutes(1));

            Assert.AreEqual(40, _cache.Get("key", 0));
        }

        [Test]
        public void Should_expire_content()
        {
            _cache.Put("key1", 1);
            _cache.Put("key2", 2, TimeSpan.FromMilliseconds(250));
            _cache.Put("key3", 3, TimeSpan.FromMilliseconds(750));

            Assert.AreEqual(1, _cache.Get("key1", 0));
            Assert.AreEqual(2, _cache.Get("key2", 0));
            Assert.AreEqual(3, _cache.Get("key3", 0));

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            Assert.AreEqual(1, _cache.Get("key1", 0));
            Assert.AreEqual(0, _cache.Get("key2", 0));
            Assert.AreEqual(3, _cache.Get("key3", 0));

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            Assert.AreEqual(1, _cache.Get("key1", 0));
            Assert.AreEqual(0, _cache.Get("key2", 0));
            Assert.AreEqual(0, _cache.Get("key3", 0));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace TestForTestGenerator.Tests
{
    public class Class1Tests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void metTest()
        {
            Assert.Fail("Generated");
        }
    }
}
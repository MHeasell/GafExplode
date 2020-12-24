using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GafExplode.Gaf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GafExplode.Tests
{
    [TestClass]
    public class RectTest
    {
        [TestMethod]
        public void TestRectMerge()
        {
            Assert.AreEqual(new Rect(0, 0, 5, 6), Rect.Merge(new Rect(0, 0, 5, 2), new Rect(0, 0, 4, 6)));

            Assert.AreEqual(new Rect(-5, 0, 10, 3), Rect.Merge(new Rect(-5, 0, 10, 2), new Rect(0, 0, 3, 3)));
        }
    }
}

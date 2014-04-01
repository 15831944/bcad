﻿using BCad.Entities;
using BCad.Extensions;
using BCad.Utilities;
using Xunit;

namespace BCad.Test
{
    public class RotateTests : AbstractDrawingTests
    {
        private void DoRotate(Entity entityToRotate, Vector origin, double angleInDegrees, Entity expectedResult)
        {
            var actual = EditUtilities.Rotate(entityToRotate, origin, angleInDegrees);
            Assert.True(expectedResult.EquivalentTo(actual));
        }

        [Fact]
        public void OriginRotateTest()
        {
            DoRotate(new Line(new Point(0, 0, 0), new Point(1, 0, 0), IndexedColor.Auto),
                Point.Origin,
                90,
                new Line(new Point(0, 0, 0), new Point(0, 1, 0), IndexedColor.Auto));
        }

        [Fact]
        public void NonOriginRotateTest()
        {
            DoRotate(new Line(new Point(2, 2, 0), new Point(3, 2, 0), IndexedColor.Auto),
                new Point(1, 1, 0),
                90,
                new Line(new Point(0, 2, 0), new Point(0, 3, 0), IndexedColor.Auto));
        }
    }
}

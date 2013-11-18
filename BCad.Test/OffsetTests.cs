﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BCad.Entities;
using BCad.Utilities;
using Xunit;

namespace BCad.Test
{
    public class OffsetTests : AbstractDrawingTests
    {
        [Fact]
        public void CircleOffsetInsideTest()
        {
            var offset = EditUtilities.Offset(
                Workspace,
                new Circle(Point.Origin, 2.0, Vector.ZAxis, IndexedColor.Auto),
                Point.Origin,
                1.0);
            Assert.True(offset is Circle);
            var circle = (Circle)offset;
            Assert.Equal(Point.Origin, circle.Center);
            Assert.Equal(1.0, circle.Radius);
        }

        [Fact]
        public void CircleOffsetOutsideTest()
        {
            var circle = (Circle)EditUtilities.Offset(
                Workspace,
                new Circle(Point.Origin, 2.0, Vector.ZAxis, IndexedColor.Auto),
                new Point(3, 0, 0),
                1.0);
            Assert.Equal(Point.Origin, circle.Center);
            Assert.Equal(3.0, circle.Radius);
        }

        [Fact]
        public void VerticalLineOffsetLeftTest()
        {
            var offset = (Line)EditUtilities.Offset(
                Workspace,
                new Line(new Point(1, 0, 0), new Point(1, 1, 0), IndexedColor.Auto),
                Point.Origin,
                1.0);
            Assert.Equal(new Point(0, 0, 0), offset.P1);
            Assert.Equal(new Point(0, 1, 0), offset.P2);
        }

        [Fact]
        public void VerticalLineOffsetRightTest()
        {
            var offset = (Line)EditUtilities.Offset(
                Workspace,
                new Line(new Point(1, 0, 0), new Point(1, 1, 0), IndexedColor.Auto),
                new Point(2, 0, 0),
                1.0);
            Assert.Equal(new Point(2, 0, 0), offset.P1);
            Assert.Equal(new Point(2, 1, 0), offset.P2);
        }

        [Fact]
        public void HorizontalLineOffsetUpTest()
        {
            var offset = (Line)EditUtilities.Offset(
                Workspace,
                new Line(new Point(0, 1, 0), new Point(1, 1, 0), IndexedColor.Auto),
                new Point(0, 2, 0),
                1.0);
            Assert.Equal(new Point(0, 2, 0), offset.P1);
            Assert.Equal(new Point(1, 2, 0), offset.P2);
        }

        [Fact]
        public void HorizontalLineOffsetDownTest()
        {
            var offset = (Line)EditUtilities.Offset(
                Workspace,
                new Line(new Point(0, 1, 0), new Point(1, 1, 0), IndexedColor.Auto),
                Point.Origin,
                1.0);
            Assert.Equal(new Point(0, 0, 0), offset.P1);
            Assert.Equal(new Point(1, 0, 0), offset.P2);
        }

        [Fact]
        public void DiagonalLineOffsetTest()
        {
            var offset = (Line)EditUtilities.Offset(
                Workspace,
                new Line(new Point(0, 1, 0), new Point(1, 2, 0), IndexedColor.Auto),
                Point.Origin,
                1.0);
            AssertClose(new Point(0.707106781186547, 0.292893218813453, 0), offset.P1);
            AssertClose(new Point(1.707106781186547, 1.292893218813453, 0), offset.P2);
        }

        [Fact]
        public void OffsetPointDirectlyOnEntity()
        {
            // line
            var offset = EditUtilities.Offset(
                Workspace,
                new Line(new Point(-1, 0, 0), new Point(1, 0, 0), IndexedColor.Auto),
                Point.Origin,
                1.0);
            Assert.Null(offset);
            
            // circle
            offset = EditUtilities.Offset(
                Workspace,
                new Circle(Point.Origin, 1.0, Vector.ZAxis, IndexedColor.Auto),
                new Point(1.0, 0, 0),
                1.0);
            Assert.Null(offset);
        }

        [Fact]
        public void CircleOffsetProjectionBug()
        {
            // enuse we're using the correct projection matrix when verifying
            // whether the offset point is inside the circle or not
            var offset = (Circle)EditUtilities.Offset(
                Workspace,
                new Circle(new Point(100, 0, 0), 50, Vector.ZAxis, IndexedColor.Auto),
                new Point(100, 0, 0),
                10);
            Assert.Equal(new Point(100, 0, 0), offset.Center);
            Assert.Equal(40, offset.Radius);
        }
    }
}

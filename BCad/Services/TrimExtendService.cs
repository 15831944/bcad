﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using BCad.Entities;
using BCad.Extensions;
using BCad.Helpers;
using BCad.Primitives;
using System;

namespace BCad.Services
{
    [Export(typeof(ITrimExtendService))]
    internal class TrimExtendService : ITrimExtendService
    {
        public void Trim(Drawing drawing, SelectedEntity entityToTrim, IEnumerable<IPrimitive> boundaryPrimitives, out IEnumerable<Entity> removed, out IEnumerable<Entity> added)
        {
            var selectionPrimitives = entityToTrim.Entity.GetPrimitives();

            // find all intersection points
            var intersectionPoints = boundaryPrimitives
                .SelectMany(b => selectionPrimitives.Select(sel => b.IntersectionPoints(sel)))
                .Where(p => p != null)
                .SelectMany(b => b)
                .Where(p => p != null);

            if (intersectionPoints.Any())
            {
                // perform the trim operation
                switch (entityToTrim.Entity.Kind)
                {
                    case EntityKind.Line:
                        TrimLine((Line)entityToTrim.Entity, entityToTrim.SelectionPoint, intersectionPoints, out removed, out added);
                        break;
                    case EntityKind.Arc:
                    case EntityKind.Circle:
                    case EntityKind.Ellipse:
                        TrimEllipse(entityToTrim.Entity, entityToTrim.SelectionPoint, intersectionPoints, out removed, out added);
                        break;
                    default:
                        Debug.Fail("only lines are supported");
                        removed = new List<Entity>();
                        added = new List<Entity>();
                        break;
                }
            }
            else
            {
                removed = new List<Entity>();
                added = new List<Entity>();
            }
        }

        private static void TrimLine(Line lineToTrim, Point pivot, IEnumerable<Point> intersectionPoints, out IEnumerable<Entity> removed, out IEnumerable<Entity> added)
        {
            var removedList = new List<Entity>();
            var addedList = new List<Entity>();

            // split intersection points based on which side of the selection point they are
            var left = new List<Point>();
            var right = new List<Point>();
            var pivotDist = (pivot - lineToTrim.P1).LengthSquared;
            var fullDist = (lineToTrim.P2 - lineToTrim.P1).LengthSquared;
            foreach (var point in intersectionPoints)
            {
                var isectDist = (point - lineToTrim.P1).LengthSquared;
                if (MathHelper.BetweenNarrow(0.0, pivotDist, isectDist))
                {
                    left.Add(point);
                }
                else if (MathHelper.BetweenNarrow(pivotDist, fullDist, isectDist))
                {
                    right.Add(point);
                }
            }

            // find the closest points on each side.  these are the new endpoints
            var leftPoint = left.OrderBy(p => (p - pivot).LengthSquared).FirstOrDefault();
            var rightPoint = right.OrderBy(p => (p - pivot).LengthSquared).FirstOrDefault();

            if (leftPoint != null || rightPoint != null)
            {
                // remove the original line
                removedList.Add(lineToTrim);

                // add the new shorted lines where appropriate
                if (leftPoint != null)
                {
                    addedList.Add(lineToTrim.Update(p1: lineToTrim.P1, p2: leftPoint));
                }
                if (rightPoint != null)
                {
                    addedList.Add(lineToTrim.Update(p1: rightPoint, p2: lineToTrim.P2));
                }
            }

            removed = removedList;
            added = addedList;
        }

        private static void TrimEllipse(Entity entityToTrim, Point pivot, IEnumerable<Point> intersectionPoints, out IEnumerable<Entity> removed, out IEnumerable<Entity> added)
        {
            var addedList = new List<Entity>();
            var removedList = new List<Entity>();

            // prep common variables
            double startAngle, endAngle;
            if (entityToTrim.Kind == EntityKind.Arc)
            {
                startAngle = ((Arc)entityToTrim).StartAngle;
                endAngle = ((Arc)entityToTrim).EndAngle;
            }
            else if (entityToTrim.Kind == EntityKind.Ellipse)
            {
                startAngle = ((Ellipse)entityToTrim).StartAngle;
                endAngle = ((Ellipse)entityToTrim).EndAngle;
            }
            else
            {
                startAngle = 0.0;
                endAngle = 360.0;
            }

            // convert points to angles
            var inverse = entityToTrim.GetUnitCircleProjection();
            inverse.Invert();

            var angles = intersectionPoints
                .Select(p => p.Transform(inverse))
                .Select(p => (Math.Atan2(p.Y, p.X) * MathHelper.RadiansToDegrees).CorrectAngleDegrees())
                .Where(a => a != startAngle && a != endAngle)
                .OrderBy(a => a)
                .ToList();
            var unitPivot = pivot.Transform(inverse);
            var selectionAngle = (Math.Atan2(unitPivot.Y, unitPivot.X) * MathHelper.RadiansToDegrees).CorrectAngleDegrees();
            var selectionAfterIndex = angles.Where(a => a < selectionAngle).Count() - 1;
            if (selectionAfterIndex < 0)
                selectionAfterIndex = angles.Count - 1;
            var previousIndex = selectionAfterIndex;
            var nextIndex = previousIndex + 1;
            if (nextIndex >= angles.Count)
                nextIndex = 0;

            // prepare normalized angles (for arc trimming)
            List<double> normalizedAngles;
            double normalizedSelectionAngle;
            if (startAngle > endAngle)
            {
                normalizedAngles = angles.Select(a => a >= startAngle ? a - 360.0 : a).ToList();
                normalizedSelectionAngle = selectionAngle >= startAngle ? selectionAngle - 360.0 : selectionAngle;
            }
            else
            {
                normalizedAngles = angles;
                normalizedSelectionAngle = selectionAngle;
            }

            var lesserAngles = normalizedAngles.Where(a => a < normalizedSelectionAngle).ToList();
            var greaterAngles = normalizedAngles.Where(a => a > normalizedSelectionAngle).ToList();

            switch (entityToTrim.Kind)
            {
                case EntityKind.Arc:
                    var arc = (Arc)entityToTrim;
                    if (lesserAngles.Any() || greaterAngles.Any())
                    {
                        removedList.Add(entityToTrim);
                        if (lesserAngles.Any())
                        {
                            addedList.Add(arc.Update(endAngle: lesserAngles.Max().CorrectAngleDegrees()));
                        }
                        if (greaterAngles.Any())
                        {
                            addedList.Add(arc.Update(startAngle: greaterAngles.Min().CorrectAngleDegrees()));
                        }
                    }
                    break;
                case EntityKind.Circle:
                    var circle = (Circle)entityToTrim;
                    if (angles.Count >= 2)
                    {
                        // 2 cutting edges required
                        removedList.Add(entityToTrim);
                        addedList.Add(new Arc(
                            circle.Center,
                            circle.Radius,
                            angles[nextIndex],
                            angles[previousIndex],
                            circle.Normal,
                            circle.Color));
                    }
                    break;
                case EntityKind.Ellipse:
                    var el = (Ellipse)entityToTrim;
                    if (el.StartAngle == 0.0 && el.EndAngle == 360.0)
                    {
                        // treat like a circle
                        if (angles.Count >= 2)
                        {
                            removedList.Add(entityToTrim);
                            addedList.Add(el.Update(startAngle: angles[nextIndex], endAngle: angles[previousIndex]));
                        }
                    }
                    else
                    {
                        // tread like an arc
                        if (lesserAngles.Any() || greaterAngles.Any())
                        {
                            removedList.Add(entityToTrim);
                            if (lesserAngles.Any())
                            {
                                addedList.Add(el.Update(endAngle: lesserAngles.Max().CorrectAngleDegrees()));
                            }
                            if (greaterAngles.Any())
                            {
                                addedList.Add(el.Update(startAngle: greaterAngles.Min().CorrectAngleDegrees()));
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("This should never happen", "entityToTrim.Kind");
            }

            added = addedList;
            removed = removedList;
        }
    }
}